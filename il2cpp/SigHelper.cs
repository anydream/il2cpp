using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.Threading;

namespace il2cpp
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

	public static class SigHelper
	{
		public static bool IsVoidSig(this TypeSig sig)
		{
			return sig.FullName == "System.Void";
		}

		public static int SigListHashCode(IList<TypeSig> sigList)
		{
			if (sigList == null)
				return 0;
			return ~(sigList.Count + 1);
		}

		public static bool SigListEquals(IList<TypeSig> x, IList<TypeSig> y)
		{
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;
			if (x.Count != y.Count)
				return false;

			var comparer = new SigComparer();
			for (int i = 0; i < x.Count; ++i)
			{
				if (!comparer.Equals(x[i], y[i]))
					return false;
			}
			return true;
		}

		public static string PrettyName(this TypeX self)
		{
			StringBuilder sb = new StringBuilder();
			PrettyName(sb, self.Def.ToTypeSig());
			PrettyGenArgs(sb, self.GenArgs);
			return sb.ToString();
		}

		public static string PrettyName(this MethodX self)
		{
			StringBuilder sb = new StringBuilder();

			PrettyName(sb, self.ReturnType);
			sb.Append(' ' + self.Def.Name);

			PrettyGenArgs(sb, self.GenArgs);

			sb.Append('(');
			int i = 0;
			if (!self.Def.IsStatic)
				i = 1;

			bool last = false;
			for (; i < self.ParamTypes.Count; ++i)
			{
				if (last)
					sb.Append(',');
				last = true;
				var arg = self.ParamTypes[i];
				PrettyName(sb, arg);
			}
			sb.Append(')');

			return sb.ToString();
		}

		public static string PrettyName(this FieldX self)
		{
			StringBuilder sb = new StringBuilder();
			PrettyName(sb, self.FieldType);
			sb.Append(' ' + self.Def.Name);
			return sb.ToString();
		}

		private static void PrettyGenArgs(StringBuilder sb, IList<TypeSig> genArgs)
		{
			if (genArgs == null)
				return;

			sb.Append('<');
			bool last = false;
			foreach (var arg in genArgs)
			{
				if (last)
					sb.Append(',');
				last = true;
				PrettyName(sb, arg);
			}
			sb.Append('>');
		}

		private static void PrettyName(StringBuilder sb, TypeSig typeSig)
		{
			if (typeSig == null)
				return;

			switch (typeSig.ElementType)
			{
				case ElementType.Class:
				case ElementType.ValueType:
					sb.Append(SigPrettyName(typeSig));
					return;

				case ElementType.Ptr:
					PrettyName(sb, typeSig.Next);
					sb.Append('*');
					return;

				case ElementType.ByRef:
					PrettyName(sb, typeSig.Next);
					sb.Append('&');
					return;

				case ElementType.SZArray:
					PrettyName(sb, typeSig.Next);
					sb.Append("[]");
					return;

				case ElementType.Pinned:
					PrettyName(sb, typeSig.Next);
					return;

				case ElementType.Array:
					{
						PrettyName(sb, typeSig.Next);
						ArraySig arraySig = (ArraySig)typeSig;
						sb.Append('[');
						uint rank = arraySig.Rank;
						if (rank == 0)
							throw new NotSupportedException();
						else if (rank == 1)
							sb.Append('*');
						else
						{
							for (int i = 0; i < (int)rank; i++)
							{
								if (i != 0)
									sb.Append(',');

								const int NO_LOWER = int.MinValue;
								const uint NO_SIZE = uint.MaxValue;
								int lower = arraySig.LowerBounds.Get(i, NO_LOWER);
								uint size = arraySig.Sizes.Get(i, NO_SIZE);
								if (lower != NO_LOWER)
								{
									sb.Append(lower);
									sb.Append("..");
									if (size != NO_SIZE)
										sb.Append(lower + (int)size - 1);
									else
										sb.Append('.');
								}
							}
						}
						sb.Append(']');
						return;
					}

				case ElementType.Var:
				case ElementType.MVar:
					{
						var gs = (GenericSig)typeSig;
						sb.Append(gs.IsMethodVar ? "!!" : "!");
						sb.Append(gs.Number);
						return;
					}

				case ElementType.GenericInst:
					{
						GenericInstSig genSig = (GenericInstSig)typeSig;
						sb.Append(SigPrettyName(genSig.GenericType));

						sb.Append('<');
						bool last = false;
						foreach (var arg in genSig.GenericArguments)
						{
							if (last)
								sb.Append(',');
							last = true;
							PrettyName(sb, arg);
						}
						sb.Append('>');
						return;
					}

				case ElementType.CModReqd:
					{
						PrettyName(sb, typeSig.Next);
						CModReqdSig modreq = (CModReqdSig)typeSig;
						sb.AppendFormat(" modreq({0})", modreq.Modifier.FullName);
						return;
					}

				case ElementType.CModOpt:
					{
						PrettyName(sb, typeSig.Next);
						CModOptSig modopt = (CModOptSig)typeSig;
						sb.AppendFormat(" modopt({0})", modopt.Modifier.FullName);
						return;
					}

				default:
					if (typeSig is CorLibTypeSig corTypeSig)
					{
						sb.Append(SigPrettyName(corTypeSig));
						return;
					}

					throw new ArgumentOutOfRangeException("PrettyName " + typeSig.GetType().Name);
			}
		}

		private static string SigPrettyName(TypeSig typeSig)
		{
			if (typeSig.Namespace == "System")
			{
				switch (typeSig.TypeName)
				{
					case "Void":
						return "void";

					case "Object":
						return "object";

					case "Boolean":
						return "bool";

					case "Char":
						return "char";

					case "SByte":
						return "sbyte";

					case "Byte":
						return "byte";

					case "Int16":
						return "short";

					case "UInt16":
						return "ushort";

					case "Int32":
						return "int";

					case "UInt32":
						return "uint";

					case "Int64":
						return "long";

					case "UInt64":
						return "ulong";

					case "Single":
						return "float";

					case "Double":
						return "double";

					case "String":
						return "string";
				}
			}
			return (typeSig.IsValueType ? "[valuetype]" : "") + typeSig.FullName;
		}
	}
}
