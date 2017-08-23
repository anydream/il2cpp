using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp
{
	// 泛型签名替换器
	internal class GenericReplacer
	{
		public TypeX OwnerType;
		public MethodX OwnerMethod;

		public TypeSig Replace(GenericVar genVarSig)
		{
			if (genVarSig.OwnerType.Attributes == OwnerType.DefAttr &&
				genVarSig.OwnerType.FullName == OwnerType.DefFullName)
				return OwnerType.GenArgs[(int)genVarSig.Number];
			return genVarSig;
		}

		public TypeSig Replace(GenericMVar genMVarSig)
		{
			if (genMVarSig.OwnerMethod.Attributes == OwnerMethod.DefAttr &&
				genMVarSig.OwnerMethod.FullName == OwnerMethod.DefFullName)
				return OwnerMethod.GenArgs[(int)genMVarSig.Number];
			return genMVarSig;
		}
	}

	// 类型管理器
	internal class TypeManager
	{
		// 当前环境
		private readonly Il2cppContext Context;

		// 实例类型映射
		private readonly Dictionary<string, TypeX> TypeMap = new Dictionary<string, TypeX>();
		// 方法待处理队列
		private readonly Queue<MethodX> PendingMethods = new Queue<MethodX>();

		internal TypeManager(Il2cppContext context)
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

					// 展开指令列表
					BuildInstructions(metX);
				}
				while (PendingMethods.Count > 0);

				//! 解析协变
				//! 解析虚调用
			}
		}

		private void BuildInstructions(MethodX metX)
		{
			Debug.Assert(metX.InstList == null);

			if (metX.DefInstList == null)
				return;

			GenericReplacer replacer = new GenericReplacer();
			replacer.OwnerType = metX.DeclType;
			replacer.OwnerMethod = metX;

			var defInstList = metX.DefInstList;
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
			metX.DefInstList = null;
		}

		private void ResolveOperand(InstInfo inst, GenericReplacer replacer)
		{
			switch (inst.OpCode.OperandType)
			{
				case OperandType.InlineMethod:
					{
						MethodX resMetX = null;
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

						if (!resMetX.HasThis)
						{
							//! 生成静态构造
						}

						if (inst.OpCode.Code == Code.Newobj)
						{
							// 设置实例化标记
							resMetX.DeclType.IsInstantiated = true;
							//! 生成静态构造和终结器
						}
						else if (inst.OpCode.Code == Code.Callvirt ||
								 inst.OpCode.Code == Code.Ldvirtftn)
						{
							// 记录虚入口
						}

						inst.Operand = resMetX;
						return;
					}

				case OperandType.InlineField:
					{
						FieldX resFldX = null;
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

		public FieldX ResolveFieldDef(FieldDef fldDef)
		{
			TypeX declType = ResolveITypeDefOrRef(fldDef.DeclaringType, null);
			FieldX fldX = new FieldX(declType, fldDef);
			return AddField(fldX);
		}

		public FieldX ResolveFieldRef(MemberRef memRef, GenericReplacer replacer)
		{
			Debug.Assert(memRef.IsFieldRef);

			TypeX declType = ResolveITypeDefOrRef(memRef.DeclaringType, replacer);
			FieldX fldX = new FieldX(declType, memRef.ResolveField());
			return AddField(fldX);
		}

		private FieldX AddField(FieldX fldX)
		{
			Debug.Assert(fldX != null);

			GenericReplacer replacer = new GenericReplacer();
			replacer.OwnerType = fldX.DeclType;

			// 展开签名所需的类型
			fldX.FieldType = ReplaceGenericSig(fldX.DefSig, replacer);

			// 尝试添加到所属类型
			string nameKey = fldX.GetNameKey();
			if (fldX.DeclType.GetField(nameKey, out var ofldX))
				return ofldX;
			fldX.DeclType.AddField(nameKey, fldX);

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
		public MethodX ResolveMethodRef(MemberRef memRef, GenericReplacer replacer)
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
				MethodX metX = new MethodX(declType, memRef.ResolveMethod());
				return AddMethod(metX);
			}
		}

		// 解析泛型方法并添加
		public MethodX ResolveMethodSpec(MethodSpec metSpec, GenericReplacer replacer)
		{
			TypeX declType = ResolveITypeDefOrRef(metSpec.DeclaringType, replacer);

			var metGenArgs = metSpec.GenericInstMethodSig?.GenericArguments;
			Debug.Assert(metGenArgs != null);
			IList<TypeSig> genArgs = ReplaceGenericSigList(metGenArgs, replacer);

			MethodX metX = new MethodX(declType, metSpec.ResolveMethodDef());
			metX.GenArgs = genArgs;
			return AddMethod(metX);
		}

		// 添加方法到类型
		private MethodX AddMethod(MethodX metX)
		{
			Debug.Assert(metX != null);

			GenericReplacer replacer = new GenericReplacer();
			replacer.OwnerType = metX.DeclType;
			replacer.OwnerMethod = metX;

			// 展开签名所需的类型
			metX.ReturnType = ReplaceGenericSig(metX.DefSig.RetType, replacer);
			metX.ParamTypes = ReplaceGenericSigList(metX.DefSig.Params, replacer);
			if (metX.HasThis)
				metX.ParamTypes.Insert(0, ReplaceGenericSig(metX.DeclType.GetThisTypeSig(), replacer));
			metX.ParamAfterSentinel = ReplaceGenericSigList(metX.DefSig.ParamsAfterSentinel, replacer);

			// 尝试添加到所属类型
			string nameKey = metX.GetNameKey();
			if (metX.DeclType.GetMethod(nameKey, out var ometX))
				return ometX;
			metX.DeclType.AddMethod(nameKey, metX);

			// 展开局部变量类型
			metX.LocalTypes = ReplaceGenericSigList(metX.LocalTypes, replacer);

			//! 展开异常处理信息
			metX.DefHandlers = null;

			// 添加到待处理队列
			PendingMethods.Enqueue(metX);

			return metX;
		}

		private void ExpandType(TypeX tyX)
		{
			GenericReplacer replacer = new GenericReplacer();
			replacer.OwnerType = tyX;

			// 解析基类
			if (tyX.DefBaseType != null)
				tyX.BaseType = ResolveITypeDefOrRef(tyX.DefBaseType, replacer);
			// 解析接口
			if (tyX.DefInterfaces != null)
			{
				foreach (var inf in tyX.DefInterfaces)
					tyX.Interfaces.Add(ResolveITypeDefOrRef(inf.Interface, replacer));
			}

			// 检查是否为可实例化的类型
			tyX.IsInstantiatable = true;
			if (tyX.HasGenArgs)
			{
				foreach (var genArg in tyX.GenArgs)
				{
					if (!IsInstantiatableTypeSig(genArg))
					{
						tyX.IsInstantiatable = false;
						break;
					}
				}
			}

			if (tyX.IsInstantiatable)
			{
				// 更新子类集合
				tyX.UpdateDerivedTypes();
			}

			tyX.DefBaseType = null;
			tyX.DefInterfaces = null;
		}

		// 解析类型并添加到映射
		public TypeX ResolveITypeDefOrRef(ITypeDefOrRef tyDefRef, GenericReplacer replacer)
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
		private TypeX ResolveITypeDefOrRefImpl(ITypeDefOrRef tyDefRef, GenericReplacer replacer)
		{
			switch (tyDefRef)
			{
				case TypeDef tyDef:
					return new TypeX(Context, CorrectTypeDefVersion(tyDef));

				case TypeRef tyRef:
					{
						TypeDef tyDef = tyRef.Resolve();
						if (tyDef != null)
							return new TypeX(Context, CorrectTypeDefVersion(tyDef));

						throw new NotSupportedException();
					}

				case TypeSpec tySpec:
					return ResolveTypeSigImpl(tySpec.TypeSig, replacer);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		// 解析类型签名
		private TypeX ResolveTypeSigImpl(TypeSig tySig, GenericReplacer replacer)
		{
			switch (tySig)
			{
				case TypeDefOrRefSig tyDefRefSig:
					return ResolveITypeDefOrRefImpl(tyDefRefSig.TypeDefOrRef, null);

				case GenericInstSig genInstSig:
					{
						TypeX genType = ResolveITypeDefOrRefImpl(genInstSig.GenericType.TypeDefOrRef, null);
						genType.GenArgs = ReplaceGenericSigList(genInstSig.GenericArguments, replacer);
						return genType;
					}

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		// 如果类型版本不匹配则加载匹配的类型
		private TypeDef CorrectTypeDefVersion(TypeDef tyDef)
		{
			if (!tyDef.DefinitionAssembly.IsCorLib())
				return tyDef;

			if (tyDef.Module.RuntimeVersion == Context.RuntimeVersion)
				return tyDef;

			TypeRef tyRef = Context.Module.CorLibTypes.GetTypeRef(tyDef.Namespace, tyDef.Name);
			Debug.Assert(tyRef != null);

			tyDef = tyRef.Resolve();
			Debug.Assert(tyDef != null);

			return tyDef;
		}

		// 替换类型中的泛型签名
		public static TypeSig ReplaceGenericSig(TypeSig tySig, GenericReplacer replacer)
		{
			if (replacer == null || !IsReplaceNeeded(tySig))
				return tySig;

			return ReplaceGenericSigImpl(tySig, replacer);
		}

		private static TypeSig ReplaceGenericSigImpl(TypeSig tySig, GenericReplacer replacer)
		{
			if (tySig == null)
				return null;

			switch (tySig.ElementType)
			{
				case ElementType.Class:
				case ElementType.ValueType:
					return tySig;

				case ElementType.Ptr:
					return new PtrSig(ReplaceGenericSigImpl(tySig.Next, replacer));
				case ElementType.ByRef:
					return new ByRefSig(ReplaceGenericSigImpl(tySig.Next, replacer));
				case ElementType.Pinned:
					return new PinnedSig(ReplaceGenericSigImpl(tySig.Next, replacer));
				case ElementType.SZArray:
					return new SZArraySig(ReplaceGenericSigImpl(tySig.Next, replacer));

				case ElementType.Array:
					{
						ArraySig arySig = (ArraySig)tySig;
						return new ArraySig(ReplaceGenericSigImpl(arySig.Next, replacer),
							arySig.Rank,
							arySig.Sizes,
							arySig.LowerBounds);
					}
				case ElementType.CModReqd:
					{
						CModReqdSig modreqdSig = (CModReqdSig)tySig;
						return new CModReqdSig(modreqdSig.Modifier, ReplaceGenericSigImpl(modreqdSig.Next, replacer));
					}
				case ElementType.CModOpt:
					{
						CModOptSig modoptSig = (CModOptSig)tySig;
						return new CModOptSig(modoptSig.Modifier, ReplaceGenericSigImpl(modoptSig.Next, replacer));
					}
				case ElementType.GenericInst:
					{
						GenericInstSig genInstSig = (GenericInstSig)tySig;
						return new GenericInstSig(genInstSig.GenericType, ReplaceGenericSigListImpl(genInstSig.GenericArguments, replacer));
					}

				case ElementType.Var:
					{
						GenericVar genVarSig = (GenericVar)tySig;
						TypeSig result = replacer.Replace(genVarSig);
						if (result != null)
							return result;
						return new GenericVar(genVarSig.Number, genVarSig.OwnerType);
					}
				case ElementType.MVar:
					{
						GenericMVar genMVarSig = (GenericMVar)tySig;
						TypeSig result = replacer.Replace(genMVarSig);
						if (result != null)
							return result;
						return new GenericMVar(genMVarSig.Number, genMVarSig.OwnerMethod);
					}

				default:
					if (tySig is CorLibTypeSig)
						return tySig;

					throw new NotSupportedException();
			}
		}

		// 替换类型签名列表
		public static IList<TypeSig> ReplaceGenericSigList(IList<TypeSig> tySigList, GenericReplacer replacer)
		{
			return tySigList?.Select(tySig => ReplaceGenericSig(tySig, replacer)).ToList();
		}

		private static IList<TypeSig> ReplaceGenericSigListImpl(IList<TypeSig> tySigList, GenericReplacer replacer)
		{
			return tySigList?.Select(tySig => ReplaceGenericSigImpl(tySig, replacer)).ToList();
		}

		// 检查是否存在要替换的泛型签名
		private static bool IsReplaceNeeded(TypeSig tySig)
		{
			while (tySig != null)
			{
				switch (tySig.ElementType)
				{
					case ElementType.Var:
					case ElementType.MVar:
						return true;

					case ElementType.GenericInst:
						{
							GenericInstSig genInstSig = (GenericInstSig)tySig;
							foreach (var arg in genInstSig.GenericArguments)
							{
								if (IsReplaceNeeded(arg))
									return true;
							}
							break;
						}
				}

				tySig = tySig.Next;
			}
			return false;
		}

		// 检查类型签名是否可实例化
		private static bool IsInstantiatableTypeSig(TypeSig tySig)
		{
			while (tySig != null)
			{
				switch (tySig.ElementType)
				{
					case ElementType.Var:
						return false;
					case ElementType.MVar:
						throw new ArgumentOutOfRangeException();

					case ElementType.GenericInst:
						{
							GenericInstSig genInstSig = (GenericInstSig)tySig;
							foreach (var arg in genInstSig.GenericArguments)
							{
								if (!IsInstantiatableTypeSig(arg))
									return false;
							}
							break;
						}
				}

				tySig = tySig.Next;
			}
			return true;
		}
	}
}
