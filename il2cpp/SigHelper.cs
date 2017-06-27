using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;

namespace il2cpp
{
	internal class TypeSigDuplicator
	{
		public TypeSig Replace(GenericVar genVar)
		{
			return genVar;
		}

		public TypeSig Replace(GenericMVar genMVar)
		{
			return genMVar;
		}

		public TypeSig Duplicate(TypeSig typeSig)
		{
			if (typeSig == null)
				return null;

			switch (typeSig.ElementType)
			{
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
						ArraySig arySig = typeSig as ArraySig;
						return new ArraySig(Duplicate(arySig.Next), arySig.Rank, Duplicate(arySig.Sizes), Duplicate(arySig.LowerBounds));
					}

				case ElementType.Var:
					{
						GenericVar genVar = typeSig as GenericVar;
						TypeSig result = Replace(genVar);
						if (result != null)
							return result;
						return new GenericVar(genVar.Number, genVar.OwnerType);
					}

				case ElementType.MVar:
					{
						GenericMVar genMVar = typeSig as GenericMVar;
						TypeSig result = Replace(genMVar);
						if (result != null)
							return result;
						return new GenericMVar(genMVar.Number, genMVar.OwnerMethod);
					}

				case ElementType.CModReqd:
					{
						CModReqdSig modreq = typeSig as CModReqdSig;
						return new CModReqdSig(modreq.Modifier, Duplicate(modreq.Next));
					}

				case ElementType.CModOpt:
					{
						CModOptSig modopt = typeSig as CModOptSig;
						return new CModOptSig(modopt.Modifier, Duplicate(modopt.Next));
					}

				default:
					Debug.Fail("TypeSig Duplicate Error: " + typeSig.GetType().Name);
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
		public MethodBaseSig Duplicate(MethodBaseSig metSig)
		{
			if (metSig is PropertySig propSig)
			{
				return new PropertySig(
					propSig.HasThis,
					Duplicate(propSig.RetType),
					Duplicate(propSig.Params).ToArray());
			}
			else
			{
				return new MethodSig(
					metSig.CallingConvention,
					metSig.GenParamCount,
					Duplicate(metSig.RetType),
					Duplicate(metSig.Params));
			}
		}
	}
}
