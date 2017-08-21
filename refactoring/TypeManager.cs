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

				//! 解析虚调用
				//! 解析协变
			}
		}

		private void BuildInstructions(MethodX metX)
		{
			if (metX.InstList != null)
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

			// 修复跳转位置
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

						return;
					}

				case OperandType.InlineField:
					{
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

			IList<TypeSig> genArgs = null;
			var metGenArgs = metSpec.GenericInstMethodSig?.GenericArguments;
			if (metGenArgs != null)
				genArgs = ReplaceGenericSigList(metGenArgs, replacer);

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
				metX.ParamTypes.Insert(0, ReplaceGenericSig(metX.DefThisSig, replacer));
			metX.ParamAfterSentinel = ReplaceGenericSigList(metX.DefSig.ParamsAfterSentinel, replacer);

			// 尝试添加
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

		// 解析类型并添加到映射
		public TypeX ResolveITypeDefOrRef(ITypeDefOrRef tyDefRef, GenericReplacer replacer)
		{
			TypeX tyX = ResolveITypeDefOrRefImpl(tyDefRef, replacer);
			Debug.Assert(tyX != null);

			string nameKey = tyX.GetNameKey();
			if (TypeMap.TryGetValue(nameKey, out var otyX))
				return otyX;

			TypeMap.Add(nameKey, tyX);

			//! expandtype

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
		private static TypeSig ReplaceGenericSig(TypeSig tySig, GenericReplacer replacer)
		{
			if (tySig == null)
				return null;

			if (!IsReplaceNeeded(tySig))
				return tySig;

			Debug.Assert(replacer != null);

			switch (tySig.ElementType)
			{
				case ElementType.Class:
				case ElementType.ValueType:
					return tySig;

				case ElementType.Ptr:
					return new PtrSig(ReplaceGenericSig(tySig.Next, replacer));
				case ElementType.ByRef:
					return new ByRefSig(ReplaceGenericSig(tySig.Next, replacer));
				case ElementType.Pinned:
					return new PinnedSig(ReplaceGenericSig(tySig.Next, replacer));
				case ElementType.SZArray:
					return new SZArraySig(ReplaceGenericSig(tySig.Next, replacer));

				case ElementType.Array:
					{
						ArraySig arySig = (ArraySig)tySig;
						return new ArraySig(ReplaceGenericSig(arySig.Next, replacer),
							arySig.Rank,
							arySig.Sizes,
							arySig.LowerBounds);
					}
				case ElementType.CModReqd:
					{
						CModReqdSig modreqdSig = (CModReqdSig)tySig;
						return new CModReqdSig(modreqdSig.Modifier, ReplaceGenericSig(modreqdSig.Next, replacer));
					}
				case ElementType.CModOpt:
					{
						CModOptSig modoptSig = (CModOptSig)tySig;
						return new CModOptSig(modoptSig.Modifier, ReplaceGenericSig(modoptSig.Next, replacer));
					}
				case ElementType.GenericInst:
					{
						GenericInstSig genInstSig = (GenericInstSig)tySig;
						return new GenericInstSig(genInstSig.GenericType, ReplaceGenericSigList(genInstSig.GenericArguments, replacer));
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
		private static IList<TypeSig> ReplaceGenericSigList(IList<TypeSig> tySigList, GenericReplacer replacer)
		{
			return tySigList?.Select(tySig => ReplaceGenericSig(tySig, replacer)).ToList();
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
						}
						break;
				}

				tySig = tySig.Next;
			}
			return false;
		}
	}
}
