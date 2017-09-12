using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp
{
	// 类型管理器
	internal class TypeManager
	{
		// 当前环境
		private readonly Il2cppContext Context;

		// 实例类型映射
		public readonly Dictionary<string, TypeX> TypeMap = new Dictionary<string, TypeX>();
		// 方法表映射
		public readonly Dictionary<string, MethodTable> MethodTableMap = new Dictionary<string, MethodTable>();
		// 虚调用方法集合
		public readonly HashSet<MethodX> VCallEntries = new HashSet<MethodX>();
		// 方法待处理队列
		private readonly Queue<MethodX> PendingMethods = new Queue<MethodX>();
		// 协逆变映射
		private class VarianceGroup
		{
			public bool IsProcessed = false;
			public HashSet<TypeX> TypeGroup = new HashSet<TypeX>();
			public List<VarianceType> VarianceTypes;
		}
		private readonly Dictionary<TypeDef, VarianceGroup> VarianceMap = new Dictionary<TypeDef, VarianceGroup>();

		// 对象终结器虚调用是否已经生成
		private bool IsVCallFinalizerGenerated;

		public TypeManager(Il2cppContext context)
		{
			Context = context;
		}

		public TypeX GetTypeByName(string name)
		{
			if (TypeMap.TryGetValue(name, out var tyX))
				return tyX;
			return null;
		}

		// 解析所有引用
		public void ResolveAll()
		{
			while (PendingMethods.Count > 0)
			{
				do
				{
					MethodX metX = PendingMethods.Dequeue();

					// 跳过不需要处理的方法
					if (metX.IsSkipProcessing)
						continue;

					// 跳过已处理的方法
					if (metX.IsProcessed)
						continue;

					// 设置已处理标记
					metX.IsProcessed = true;

					// 展开指令列表
					BuildInstructions(metX);
				}
				while (PendingMethods.Count > 0);

				// 解析虚调用
				ResolveVCalls();
				// 解析协逆变
				ResolveVariances();
			}
		}

		private void BuildInstructions(MethodX metX)
		{
			Debug.Assert(metX.InstList == null);

			if (!metX.Def.HasBody || !metX.Def.Body.HasInstructions)
				return;

			IGenericReplacer replacer = new GenericReplacer(metX.DeclType, metX);

			var defInstList = metX.Def.Body.Instructions;
			int numInsts = defInstList.Count;

			InstInfo[] instList = new InstInfo[numInsts];

			Dictionary<uint, int> offsetMap = new Dictionary<uint, int>();
			List<InstInfo> branchInsts = new List<InstInfo>();

			// 构建指令列表
			for (int ip = 0; ip < numInsts; ++ip)
			{
				var defInst = defInstList[ip];
				var inst = instList[ip] = new InstInfo();
				inst.OpCode = defInst.OpCode;
				inst.Operand = defInst.Operand;
				inst.Offset = ip;

				offsetMap.Add(defInst.Offset, ip);
				switch (inst.OpCode.OperandType)
				{
					case OperandType.InlineBrTarget:
					case OperandType.ShortInlineBrTarget:
					case OperandType.InlineSwitch:
						branchInsts.Add(inst);
						break;

					default:
						ResolveOperand(inst, replacer);
						break;
				}
			}

			// 重定向跳转位置
			foreach (var inst in branchInsts)
			{
				if (inst.Operand is Instruction defInst)
				{
					inst.Operand = offsetMap[defInst.Offset];
				}
				else if (inst.Operand is Instruction[] defInsts)
				{
					int[] insts = new int[defInsts.Length];
					for (int i = 0; i < defInsts.Length; ++i)
					{
						insts[i] = offsetMap[defInsts[i].Offset];
					}
					inst.Operand = insts;
				}
			}

			metX.InstList = instList;
		}

		private void ResolveOperand(InstInfo inst, IGenericReplacer replacer)
		{
			switch (inst.OpCode.OperandType)
			{
				case OperandType.InlineMethod:
					{
						MethodX resMetX;
						switch (inst.Operand)
						{
							case MethodDef metDef:
								resMetX = ResolveMethodDef(metDef);
								break;
							case MemberRef memRef:
								resMetX = ResolveMethodRef(memRef, replacer);
								break;
							case MethodSpec metSpec:
								resMetX = ResolveMethodSpec(metSpec, replacer);
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						bool isReAddMethod = false;

						if (inst.OpCode.Code == Code.Newobj)
						{
							Debug.Assert(!resMetX.Def.IsStatic);
							Debug.Assert(resMetX.Def.IsConstructor);
							// 设置实例化标记
							resMetX.DeclType.IsInstantiated = true;
							// 生成静态构造和终结器
							GenStaticCctor(resMetX.DeclType);
							GenFinalizer(resMetX.DeclType);

							// 解析虚表
							resMetX.DeclType.ResolveVTable();
						}
						else if (resMetX.IsVirtual &&
								(inst.OpCode.Code == Code.Callvirt ||
								 inst.OpCode.Code == Code.Ldvirtftn))
						{
							AddVCallEntry(resMetX);
						}
						else if (resMetX.IsVirtual &&
								(inst.OpCode.Code == Code.Call ||
								 inst.OpCode.Code == Code.Ldftn))
						{
							// 处理方法替换
							if (resMetX.DeclType.QueryCallReplace(resMetX.Def, out TypeX implTyX, out var implDef))
							{
								resMetX.IsSkipProcessing = true;

								Debug.Assert(implTyX != null);
								MethodX implMetX = MakeMethodX(implTyX, implDef, resMetX.GenArgs);

								resMetX = implMetX;
							}
							else
								isReAddMethod = true;
						}
						else
							isReAddMethod = true;

						if (isReAddMethod)
						{
							// 尝试重新加入处理队列
							AddPendingMethod(resMetX);
						}

						if (resMetX.IsStatic)
						{
							// 生成静态构造
							GenStaticCctor(resMetX.DeclType);
						}

						inst.Operand = resMetX;
						return;
					}

				case OperandType.InlineField:
					{
						FieldX resFldX;
						switch (inst.Operand)
						{
							case FieldDef fldDef:
								resFldX = ResolveFieldDef(fldDef);
								break;
							case MemberRef memRef:
								resFldX = ResolveFieldRef(memRef, replacer);
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						if (resFldX.IsStatic)
						{
							// 生成静态构造
							GenStaticCctor(resFldX.DeclType);
						}

						inst.Operand = resFldX;
						return;
					}

				case OperandType.InlineType:
					{
						return;
					}

				case OperandType.InlineSig:
					{
						return;
					}
			}
		}

		private void GenStaticCctor(TypeX tyX)
		{
			if (tyX.IsCctorGenerated)
				return;
			tyX.IsCctorGenerated = true;

			MethodDef cctor = tyX.Def.Methods.FirstOrDefault(met => met.IsStatic && met.IsConstructor);
			if (cctor != null)
			{
				MethodX metX = new MethodX(tyX, cctor);
				tyX.CctorMethod = AddMethod(metX);
			}
		}

		private void GenFinalizer(TypeX tyX)
		{
			if (tyX.IsFinalizerGenerated)
				return;
			tyX.IsFinalizerGenerated = true;

			MethodDef fin = tyX.Def.Methods.FirstOrDefault(met => !met.IsStatic && met.Name == "Finalize");
			if (fin != null)
			{
				MethodX metX = new MethodX(tyX, fin);
				tyX.FinalizerMethod = AddMethod(metX);

				AddVCallObjectFinalizer();
			}
		}

		private void AddVCallObjectFinalizer()
		{
			if (IsVCallFinalizerGenerated)
				return;
			IsVCallFinalizerGenerated = true;

			MethodDef fin = Context.Module.CorLibTypes.Object.TypeRef.ResolveTypeDef().FindMethod("Finalize");
			MethodX vmetFinalizer = ResolveMethodDef(fin);

			AddVCallEntry(vmetFinalizer);
		}

		private void AddVCallEntry(MethodX virtMetX)
		{
			// 跳过该方法的处理
			virtMetX.IsSkipProcessing = true;
			// 登记到虚入口
			VCallEntries.Add(virtMetX);
		}

		private void ResolveVCalls()
		{
			foreach (MethodX virtMetX in VCallEntries)
			{
				TypeX entryTyX = virtMetX.DeclType;
				MethodDef entryDef = virtMetX.Def;

				// 在虚入口所在类型内解析虚方法
				if (virtMetX.DeclType.IsInstantiated)
					ResolveVMethod(virtMetX, virtMetX.DeclType, entryTyX, entryDef);

				// 在继承类型内解析虚方法
				foreach (TypeX derivedTyX in virtMetX.DeclType.DerivedTypes)
					ResolveVMethod(virtMetX, derivedTyX, entryTyX, entryDef);
			}
		}

		private void ResolveVMethod(
			MethodX virtMetX,
			TypeX derivedTyX,
			TypeX entryTyX,
			MethodDef entryDef)
		{
			// 跳过没有实例化的类型
			if (!derivedTyX.IsInstantiated)
				return;

			// 查询虚方法绑定
			derivedTyX.QueryCallVirt(entryTyX, entryDef, out TypeX implTyX, out var implDef);

			// 构造实现方法
			Debug.Assert(implTyX != null);
			MethodX implMetX = MakeMethodX(implTyX, implDef, virtMetX.GenArgs);

			// 关联实现方法到虚方法
			virtMetX.AddOverrideImpl(implMetX);

			// 处理该方法
			AddPendingMethod(implMetX);
		}

		public FieldX ResolveFieldDef(FieldDef fldDef)
		{
			TypeX declType = ResolveTypeDefOrRef(fldDef.DeclaringType, null);
			FieldX fldX = new FieldX(declType, fldDef);
			return AddField(fldX);
		}

		public FieldX ResolveFieldRef(MemberRef memRef, IGenericReplacer replacer)
		{
			Debug.Assert(memRef.IsFieldRef);

			TypeX declType = ResolveTypeDefOrRef(memRef.DeclaringType, replacer);
			FieldX fldX = new FieldX(declType, memRef.ResolveField());
			return AddField(fldX);
		}

		private static FieldX AddField(FieldX fldX)
		{
			Debug.Assert(fldX != null);

			// 尝试添加到所属类型
			string nameKey = fldX.GetNameKey();
			if (fldX.DeclType.GetField(nameKey, out var ofldX))
				return ofldX;
			fldX.DeclType.AddField(nameKey, fldX);

			IGenericReplacer replacer = new GenericReplacer(fldX.DeclType, null);
			// 展开字段类型
			fldX.FieldType = Helper.ReplaceGenericSig(fldX.Def.FieldType, replacer);

			return fldX;
		}

		// 解析方法定义并添加
		public MethodX ResolveMethodDef(MethodDef metDef)
		{
			TypeX declType = ResolveTypeDefOrRef(metDef.DeclaringType, null);
			MethodX metX = new MethodX(declType, metDef);
			return AddMethod(metX);
		}

		// 解析方法引用并添加
		public MethodX ResolveMethodRef(MemberRef memRef, IGenericReplacer replacer)
		{
			Debug.Assert(memRef.IsMethodRef);

			var elemType = memRef.DeclaringType.ToTypeSig().ElementType;
			if (elemType == ElementType.SZArray)
			{
				throw new NotImplementedException();
			}
			else if (elemType == ElementType.Array)
			{
				throw new NotImplementedException();
			}
			else
			{
				TypeX declType = ResolveTypeDefOrRef(memRef.DeclaringType, replacer);
				MethodDef metDef = memRef.ResolveMethod();
				if (metDef.DeclaringType != declType.Def)
				{
					// 处理引用类型不包含该方法的情况
					declType = declType.FindBaseType(metDef.DeclaringType);
					Debug.Assert(declType != null);
				}

				MethodX metX = new MethodX(declType, metDef);
				return AddMethod(metX);
			}
		}

		// 解析泛型方法并添加
		public MethodX ResolveMethodSpec(MethodSpec metSpec, IGenericReplacer replacer)
		{
			TypeX declType = ResolveTypeDefOrRef(metSpec.DeclaringType, replacer);
			MethodDef metDef = metSpec.ResolveMethodDef();
			if (metDef.DeclaringType != declType.Def)
			{
				// 处理引用类型不包含该方法的情况
				declType = declType.FindBaseType(metDef.DeclaringType);
				Debug.Assert(declType != null);
			}

			var metGenArgs = metSpec.GenericInstMethodSig?.GenericArguments;
			Debug.Assert(metGenArgs != null);
			IList<TypeSig> genArgs = Helper.ReplaceGenericSigList(metGenArgs, replacer);

			MethodX metX = new MethodX(declType, metDef);
			metX.GenArgs = genArgs;
			return AddMethod(metX);
		}

		// 添加方法到类型
		private MethodX AddMethod(MethodX metX)
		{
			Debug.Assert(metX != null);

			// 尝试添加到所属类型
			string nameKey = metX.GetNameKey();
			if (metX.DeclType.GetMethod(nameKey, out var ometX))
				return ometX;
			metX.DeclType.AddMethod(nameKey, metX);

			IGenericReplacer replacer = new GenericReplacer(metX.DeclType, metX);

			// 展开返回值和参数类型
			metX.ReturnType = Helper.ReplaceGenericSig(metX.DefSig.RetType, replacer);
			metX.ParamTypes = Helper.ReplaceGenericSigList(metX.DefSig.Params, replacer);
			if (metX.HasThis)
				metX.ParamTypes.Insert(0, Helper.ReplaceGenericSig(metX.DeclType.GetThisTypeSig(), replacer));
			metX.ParamAfterSentinel = Helper.ReplaceGenericSigList(metX.DefSig.ParamsAfterSentinel, replacer);

			// 展开局部变量类型
			metX.LocalTypes = Helper.ReplaceGenericSigList(metX.LocalTypes, replacer);

			//! 展开异常处理信息

			// 添加到待处理队列
			AddPendingMethod(metX);

			return metX;
		}

		private void AddPendingMethod(MethodX metX)
		{
			if (!metX.IsProcessed)
			{
				metX.IsSkipProcessing = false;
				PendingMethods.Enqueue(metX);
			}
		}

		private void ExpandType(TypeX tyX)
		{
			IGenericReplacer replacer = new GenericReplacer(tyX, null);

			// 解析基类
			if (tyX.Def.BaseType != null)
				tyX.BaseType = ResolveTypeDefOrRef(tyX.Def.BaseType, replacer);
			// 解析接口
			if (tyX.Def.HasInterfaces)
			{
				uint lastRid = 0;
				foreach (var inf in tyX.Def.Interfaces)
				{
					Debug.Assert(lastRid == 0 || inf.Rid > lastRid);
					lastRid = inf.Rid;
					tyX.Interfaces.Add(ResolveTypeDefOrRef(inf.Interface, replacer));
				}
			}

			// 解析协逆变
			TryAddVariance(tyX);

			// 更新子类集合
			tyX.UpdateDerivedTypes();
		}

		private void TryAddVariance(TypeX tyX)
		{
			if (!tyX.Def.HasGenericParameters)
				return;

			if (tyX.Variances != null)
				return;

			if (VarianceMap.TryGetValue(tyX.Def, out var vgroup))
			{
				if (vgroup != null)
				{
					tyX.Variances = vgroup.VarianceTypes;
					if (vgroup.TypeGroup.Add(tyX))
						vgroup.IsProcessed = false;
				}
			}
			else
			{
				var vaList = new List<VarianceType>();
				bool hasVariance = false;
				foreach (var arg in tyX.Def.GenericParameters)
				{
					if (arg.IsCovariant)
					{
						hasVariance = true;
						vaList.Add(VarianceType.Covariant);
					}
					else if (arg.IsContravariant)
					{
						hasVariance = true;
						vaList.Add(VarianceType.Contravariant);
					}
					else
					{
						Debug.Assert(arg.IsNonVariant);
						vaList.Add(VarianceType.NonVariant);
					}
				}

				if (hasVariance)
				{
					vgroup = new VarianceGroup();
					vgroup.VarianceTypes = vaList;
					vgroup.TypeGroup.Add(tyX);
					tyX.Variances = vaList;
					VarianceMap.Add(tyX.Def, vgroup);
				}
				else
					VarianceMap.Add(tyX.Def, null);
			}
		}

		private void ResolveVariances()
		{
			for (; ; )
			{
				bool isLoop = false;

				var vgList = VarianceMap.Values.ToList();
				foreach (var vgroup in vgList)
				{
					if (vgroup == null || vgroup.IsProcessed)
						continue;
					vgroup.IsProcessed = true;

					if (vgroup.TypeGroup.Count == 1)
						continue;

					var vlist = vgroup.TypeGroup.ToList();
					int len = vlist.Count;
					Debug.Assert(len > 1);

					for (int i = 0; i < len - 1; ++i)
					{
						for (int j = i + 1; j < len; ++j)
						{
							if (!TryLinkVariance(vlist[i], vlist[j]))
								TryLinkVariance(vlist[j], vlist[i]);
						}
					}
				}

				foreach (var vgroup in VarianceMap.Values)
				{
					if (vgroup != null && !vgroup.IsProcessed)
					{
						isLoop = true;
						break;
					}
				}

				if (!isLoop)
					break;
			}
		}

		private bool TryLinkVariance(TypeX baseTyX, TypeX derivedTyX)
		{
			Debug.Assert(baseTyX.Variances == derivedTyX.Variances);

			if (baseTyX.DerivedTypes.Contains(derivedTyX))
				return true;

			int len = baseTyX.Variances.Count;
			for (int i = 0; i < len; ++i)
			{
				VarianceType vaType = baseTyX.Variances[i];
				TypeSig baseSig = baseTyX.GenArgs[i];
				TypeSig derivedSig = derivedTyX.GenArgs[i];

				if (vaType == VarianceType.NonVariant)
				{
					// 无协逆变的参数类型不一致, 表示两个类型无关联性
					if (!TypeEqualityComparer.Instance.Equals(baseSig, derivedSig))
						return true;
				}
				else if (vaType == VarianceType.Covariant)
				{
					if (!IsDerivedType(baseSig, derivedSig))
						return false;
				}
				else if (vaType == VarianceType.Contravariant)
				{
					if (!IsDerivedType(derivedSig, baseSig))
						return false;
				}
			}

			derivedTyX.AddVarianceBaseType(baseTyX);

			return true;
		}

		private bool IsDerivedType(TypeSig baseSig, TypeSig derivedSig)
		{
			// 类型相同则允许转换
			if (TypeEqualityComparer.Instance.Equals(baseSig, derivedSig))
				return true;

			var baseElemType = baseSig.ElementType;
			var derivedElemType = derivedSig.ElementType;

			// 子类是 object, 不存在转换
			if (derivedElemType == ElementType.Object)
				return false;

			// 基类是 object
			if (baseElemType == ElementType.Object)
			{
				// 子类非值类型即可转换
				if (!derivedSig.IsValueType)
					return true;
				return false;
			}

			// 基类是值类型, 不存在转换
			if (baseSig.IsValueType)
				return false;

			//! 数组类型

			// 解析并判断其他类型
			var baseTyX = ResolveTypeDefOrRef(baseSig.ToTypeDefOrRef(), null);
			var derivedTyX = ResolveTypeDefOrRef(derivedSig.ToTypeDefOrRef(), null);

			return baseTyX.DerivedTypes.Contains(derivedTyX);
		}

		// 解析类型并添加到映射
		public TypeX ResolveTypeDefOrRef(ITypeDefOrRef tyDefRef, IGenericReplacer replacer)
		{
			TypeX tyX = ResolveTypeDefOrRefImpl(tyDefRef, replacer);
			Debug.Assert(tyX != null);

			// 尝试添加到类型映射
			string nameKey = tyX.GetNameKey();
			if (TypeMap.TryGetValue(nameKey, out var otyX))
				return otyX;
			TypeMap.Add(nameKey, tyX);

			// 展开类型
			ExpandType(tyX);

			return tyX;
		}

		// 解析类型引用
		private TypeX ResolveTypeDefOrRefImpl(ITypeDefOrRef tyDefRef, IGenericReplacer replacer)
		{
			switch (tyDefRef)
			{
				case TypeDef tyDef:
					return new TypeX(Context, tyDef);

				case TypeRef tyRef:
					{
						TypeDef tyDef = tyRef.Resolve();
						if (tyDef != null)
							return new TypeX(Context, tyDef);

						throw new NotSupportedException();
					}

				case TypeSpec tySpec:
					return ResolveTypeSigImpl(tySpec.TypeSig, replacer);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		// 解析类型签名
		private TypeX ResolveTypeSigImpl(TypeSig tySig, IGenericReplacer replacer)
		{
			switch (tySig)
			{
				case TypeDefOrRefSig tyDefRefSig:
					return ResolveTypeDefOrRefImpl(tyDefRefSig.TypeDefOrRef, null);

				case GenericInstSig genInstSig:
					{
						TypeX genType = ResolveTypeDefOrRefImpl(genInstSig.GenericType.TypeDefOrRef, null);
						genType.GenArgs = Helper.ReplaceGenericSigList(genInstSig.GenericArguments, replacer);
						return genType;
					}

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public MethodTable ResolveMethodTable(ITypeDefOrRef tyDefRef)
		{
			if (tyDefRef is TypeSpec tySpec)
				return ResolveMethodTableSpec(tySpec.TypeSig);
			else
				return ResolveMethodTableDefRef(tyDefRef);
		}

		public MethodTable ResolveMethodTableDefRef(ITypeDefOrRef tyDefRef)
		{
			MethodTable mtable = null;
			if (tyDefRef is TypeDef tyDef)
			{
				mtable = new MethodTable(Context, tyDef);
			}
			else if (tyDefRef is TypeRef tyRef)
			{
				TypeDef def = tyRef.Resolve();
				if (def != null)
					mtable = new MethodTable(Context, def);
				else
					throw new NotSupportedException();
			}
			else
				throw new ArgumentOutOfRangeException();

			string nameKey = mtable.GetNameKey();
			if (MethodTableMap.TryGetValue(nameKey, out var omtable))
				return omtable;
			MethodTableMap.Add(nameKey, mtable);

			mtable.ResolveTable();

			return mtable;
		}

		public MethodTable ResolveMethodTableSpec(TypeSig tySig)
		{
			if (tySig is GenericInstSig genInstSig)
			{
				return ResolveMethodTableSpec(genInstSig.GenericType.TypeDefOrRef, genInstSig.GenericArguments);
			}
			else if (tySig is TypeDefOrRefSig tyDefRefSig)
			{
				return ResolveMethodTableDefRef(tyDefRefSig.TypeDefOrRef);
			}
			else
				throw new ArgumentOutOfRangeException();
		}

		public MethodTable ResolveMethodTableSpec(ITypeDefOrRef tyDefRef, IList<TypeSig> genArgs)
		{
			MethodTable baseTable = ResolveMethodTableDefRef(tyDefRef);

			string nameKey = baseTable.GetExpandedNameKey(genArgs);
			if (MethodTableMap.TryGetValue(nameKey, out var omtable))
				return omtable;

			MethodTable mtable = baseTable.ExpandTable(genArgs);
			string expNameKey = mtable.GetNameKey();
			Debug.Assert(expNameKey == nameKey);
			MethodTableMap.Add(expNameKey, mtable);

			return mtable;
		}

		private MethodX MakeMethodX(TypeX declType, MethodDef metDef, IList<TypeSig> genArgs)
		{
			MethodX metX = new MethodX(declType, metDef);
			if (genArgs.IsCollectionValid())
				metX.GenArgs = new List<TypeSig>(genArgs);
			return AddMethod(metX);
		}
	}
}
