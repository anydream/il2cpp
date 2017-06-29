﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;

namespace il2cpp2
{
	// 泛型展开器
	internal class GenericReplacer
	{
		public TypeDef OwnerType { get; private set; }
		public IList<TypeSig> TypeGenArgs { get; private set; }
		public MethodDef OwnerMethod { get; private set; }
		public IList<TypeSig> MethodGenArgs { get; private set; }
		public bool HasType => OwnerType != null && TypeGenArgs != null && TypeGenArgs.Count > 0;
		public bool HasMethod => OwnerMethod != null && MethodGenArgs != null && MethodGenArgs.Count > 0;
		public bool IsValid => HasType || HasMethod;

		public void SetType(TypeX tyX)
		{
			if (tyX.HasGenArgs)
			{
				OwnerType = tyX.Def;
				TypeGenArgs = tyX.GenArgs;
			}
		}

		public void SetMethod(MethodX metX)
		{
			if (metX.HasGenArgs)
			{
				OwnerMethod = metX.Def;
				MethodGenArgs = metX.GenArgs;
			}
		}

		public TypeSig Replace(GenericVar genVar)
		{
			if (TypeEqualityComparer.Instance.Equals(genVar.OwnerType, OwnerType))
				return TypeGenArgs[(int)genVar.Number];
			return genVar;
		}

		public TypeSig Replace(GenericMVar genMVar)
		{
			if (MethodEqualityComparer.DontCompareDeclaringTypes.Equals(genMVar.OwnerMethod, OwnerMethod))
				return MethodGenArgs[(int)genMVar.Number];
			return genMVar;
		}
	}

	// 类型签名复制器
	internal class TypeSigDuplicator
	{
		public GenericReplacer GenReplacer;

		public TypeSig Duplicate(TypeSig typeSig)
		{
			if (typeSig == null)
				return null;

			switch (typeSig.ElementType)
			{
				case ElementType.Class:
				case ElementType.ValueType:
					return typeSig;

				case ElementType.Ptr:
					return new PtrSig(Duplicate(typeSig.Next));

				case ElementType.ByRef:
					return new ByRefSig(Duplicate(typeSig.Next));

				case ElementType.SZArray:
					return new SZArraySig(Duplicate(typeSig.Next));

				case ElementType.Pinned:
					return new PinnedSig(Duplicate(typeSig.Next));

				case ElementType.Array:
					{
						ArraySig arySig = (ArraySig)typeSig;
						return new ArraySig(Duplicate(arySig.Next), arySig.Rank, Duplicate(arySig.Sizes), Duplicate(arySig.LowerBounds));
					}

				case ElementType.Var:
					{
						GenericVar genVar = (GenericVar)typeSig;
						TypeSig result = GenReplacer.Replace(genVar);
						if (result != null)
							return result;
						return new GenericVar(genVar.Number, genVar.OwnerType);
					}

				case ElementType.MVar:
					{
						GenericMVar genMVar = (GenericMVar)typeSig;
						TypeSig result = GenReplacer.Replace(genMVar);
						if (result != null)
							return result;
						return new GenericMVar(genMVar.Number, genMVar.OwnerMethod);
					}

				case ElementType.GenericInst:
					{
						GenericInstSig genSig = (GenericInstSig)typeSig;
						return new GenericInstSig(genSig.GenericType, Duplicate(genSig.GenericArguments));
					}

				case ElementType.CModReqd:
					{
						CModReqdSig modreq = (CModReqdSig)typeSig;
						return new CModReqdSig(modreq.Modifier, Duplicate(modreq.Next));
					}

				case ElementType.CModOpt:
					{
						CModOptSig modopt = (CModOptSig)typeSig;
						return new CModOptSig(modopt.Modifier, Duplicate(modopt.Next));
					}

				default:
					if (typeSig is CorLibTypeSig)
						return typeSig;

					Debug.Fail("Duplicate TypeSig " + typeSig.GetType().Name);
					return null;
			}
		}

		public IList<TypeSig> Duplicate(IList<TypeSig> lst)
		{
			return lst?.Select(Duplicate).ToList();
		}

		protected static IList<uint> Duplicate(IList<uint> lst)
		{
			return lst == null ? null : new List<uint>(lst);
		}

		protected static IList<int> Duplicate(IList<int> lst)
		{
			return lst == null ? null : new List<int>(lst);
		}
	}

	internal class MethodSigDuplicator : TypeSigDuplicator
	{
		public MethodBaseSig Duplicate(MethodBaseSig metBaseSig)
		{
			switch (metBaseSig)
			{
				case PropertySig propSig:
					return new PropertySig(
						propSig.HasThis,
						Duplicate(propSig.RetType),
						Duplicate(propSig.Params).ToArray());

				case MethodSig metSig:
					return new MethodSig(
						metSig.CallingConvention,
						metSig.GenParamCount,
						Duplicate(metSig.RetType),
						Duplicate(metSig.Params));

				default:
					Debug.Fail("Duplicate " + metBaseSig.GetType().Name);
					return null;
			}
		}
	}
}