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

		// 对象终结器虚调用是否已经生成
		private bool IsVCallFinalizerGenerated = false;

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
				//! 解析协变
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
							// 处理非入口虚调用重定向
							if (!resMetX.Def.IsNewSlot)
							{
								bool status = resMetX.DeclType.GetNewSlotMethod(resMetX.Def, out var slotTypeName, out var slotMetDef);
								Debug.Assert(status);
								Debug.Assert(slotMetDef != null);

								if (slotMetDef != resMetX.Def)
								{
									resMetX.IsSkipProcessing = true;

									TypeX slotTyX = GetTypeByName(slotTypeName);
									Debug.Assert(slotTyX != null);

									MethodX slotMetX = new MethodX(slotTyX, slotMetDef);
									slotMetX.GenArgs = resMetX.HasGenArgs ? new List<TypeSig>(resMetX.GenArgs) : null;
									slotMetX = AddMethod(slotMetX);

									resMetX = slotMetX;
								}
							}
							AddVCallEntry(resMetX);
						}
						else if (resMetX.IsVirtual &&
								(inst.OpCode.Code == Code.Call ||
								 inst.OpCode.Code == Code.Ldftn))
						{
							// 处理方法替换
							if (resMetX.DeclType.IsMethodReplaced(resMetX.Def, out var repTypeName, out var repMetDef))
							{
								resMetX.IsSkipProcessing = true;

								TypeX repTyX = GetTypeByName(repTypeName);
								Debug.Assert(repTyX != null);

								MethodX repMetX = new MethodX(repTyX, repMetDef);
								repMetX.GenArgs = resMetX.HasGenArgs ? new List<TypeSig>(resMetX.GenArgs) : null;
								repMetX = AddMethod(repMetX);

								resMetX = repMetX;
							}
							else
								isReAddMethod = true;
						}
						else
							isReAddMethod = true;

						if (isReAddMethod)
						{
							// 尝试重新加入处理队列
							resMetX.IsSkipProcessing = false;
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
				string entryType = virtMetX.DeclType.GetNameKey();
				MethodDef entryDef = virtMetX.Def;

				// 在虚入口所在类型内解析虚方法
				if (virtMetX.DeclType.IsInstantiated)
					ResolveVMethod(virtMetX, virtMetX.DeclType, entryType, entryDef);

				// 在继承类型内解析虚方法
				foreach (TypeX derivedTyX in virtMetX.DeclType.DerivedTypes)
					ResolveVMethod(virtMetX, derivedTyX, entryType, entryDef);
			}
		}

		private void ResolveVMethod(
			MethodX virtMetX,
			TypeX derivedTyX,
			string entryType,
			MethodDef entryDef)
		{
			// 跳过没有实例化的类型
			if (!derivedTyX.IsInstantiated)
				return;

			// 在继承类型中查找虚方法
			string implType;
			MethodDef implDef;
			TypeX implTyX;

			for (;;)
			{
				if (!derivedTyX.QueryVTable(
					entryType, entryDef,
					out implType, out implDef))
				{
					throw new TypeLoadException("Resolve virtual method failed");
				}

				// 构造实现方法
				implTyX = GetTypeByName(implType);

				if (!implDef.IsNewSlot)
				{
					//! 解析对应的虚方法并尝试替换
				}

				if (implTyX.IsMethodReplaced(implDef, out var repTypeName, out var repMetDef))
				{
					entryType = repTypeName;
					entryDef = repMetDef;
				}
				else
					break;
			}

			MethodX implMetX = new MethodX(implTyX, implDef);
			implMetX.GenArgs = virtMetX.HasGenArgs ? new List<TypeSig>(virtMetX.GenArgs) : null;
			implMetX = AddMethod(implMetX);

			// 关联实现方法到虚方法
			virtMetX.AddOverrideImpl(implMetX);

			// 处理该方法
			implMetX.IsSkipProcessing = false;
			AddPendingMethod(implMetX);
		}

		public FieldX ResolveFieldDef(FieldDef fldDef)
		{
			TypeX declType = ResolveITypeDefOrRef(fldDef.DeclaringType, null);
			FieldX fldX = new FieldX(declType, fldDef);
			return AddField(fldX);
		}

		public FieldX ResolveFieldRef(MemberRef memRef, IGenericReplacer replacer)
		{
			Debug.Assert(memRef.IsFieldRef);

			TypeX declType = ResolveITypeDefOrRef(memRef.DeclaringType, replacer);
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
			TypeX declType = ResolveITypeDefOrRef(metDef.DeclaringType, null);
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
				TypeX declType = ResolveITypeDefOrRef(memRef.DeclaringType, replacer);
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
			TypeX declType = ResolveITypeDefOrRef(metSpec.DeclaringType, replacer);
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
				PendingMethods.Enqueue(metX);
		}

		private void ExpandType(TypeX tyX)
		{
			IGenericReplacer replacer = new GenericReplacer(tyX, null);

			// 解析基类
			if (tyX.Def.BaseType != null)
				tyX.BaseType = ResolveITypeDefOrRef(tyX.Def.BaseType, replacer);
			// 解析接口
			if (tyX.Def.HasInterfaces)
			{
				foreach (var inf in tyX.Def.Interfaces)
					tyX.Interfaces.Add(ResolveITypeDefOrRef(inf.Interface, replacer));
			}

			// 更新子类集合
			tyX.UpdateDerivedTypes();
		}

		// 解析类型并添加到映射
		public TypeX ResolveITypeDefOrRef(ITypeDefOrRef tyDefRef, IGenericReplacer replacer)
		{
			TypeX tyX = ResolveITypeDefOrRefImpl(tyDefRef, replacer);
			Debug.Assert(tyX != null);

			// 尝试添加到类型映射
			string nameKey = tyX.GetNameKey();
			if (TypeMap.TryGetValue(nameKey, out var otyX))
				return otyX;
			TypeMap.Add(nameKey, tyX);

			// 展开方法
			ExpandType(tyX);

			return tyX;
		}

		// 解析类型引用
		private TypeX ResolveITypeDefOrRefImpl(ITypeDefOrRef tyDefRef, IGenericReplacer replacer)
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
					return ResolveITypeDefOrRefImpl(tyDefRefSig.TypeDefOrRef, null);

				case GenericInstSig genInstSig:
					{
						TypeX genType = ResolveITypeDefOrRefImpl(genInstSig.GenericType.TypeDefOrRef, null);
						genType.GenArgs = Helper.ReplaceGenericSigList(genInstSig.GenericArguments, replacer);
						return genType;
					}

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public MethodTable ResolveMethodTable(ITypeDefOrRef tyDefRef, IGenericReplacer replacer)
		{
			MethodTable mtable = ResolveMethodTableImpl(tyDefRef, replacer);

			string nameKey = mtable.GetNameKey();
			if (MethodTableMap.TryGetValue(nameKey, out var omtable))
				return omtable;
			MethodTableMap.Add(nameKey, mtable);

			mtable.ResolveTable();

			return mtable;
		}

		private MethodTable ResolveMethodTableImpl(ITypeDefOrRef tyDefRef, IGenericReplacer replacer)
		{
			switch (tyDefRef)
			{
				case TypeDef tyDef:
					return new MethodTable(Context, tyDef);

				case TypeRef tyRef:
					{
						TypeDef tyDef = tyRef.Resolve();
						if (tyDef != null)
							return new MethodTable(Context, tyDef);

						throw new NotSupportedException();
					}

				case TypeSpec tySpec:
					return ResolveMethodTableImpl(tySpec.TypeSig, replacer);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private MethodTable ResolveMethodTableImpl(TypeSig tySig, IGenericReplacer replacer)
		{
			switch (tySig)
			{
				case TypeDefOrRefSig tyDefRefSig:
					return ResolveMethodTableImpl(tyDefRefSig.TypeDefOrRef, null);

				case GenericInstSig genInstSig:
					{
						MethodTable mtable = ResolveMethodTableImpl(genInstSig.GenericType.TypeDefOrRef, null);
						mtable.GenArgs = Helper.ReplaceGenericSigList(genInstSig.GenericArguments, replacer);
						return mtable;
					}

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
