using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	public static class Helpers
	{
		public static bool EqualsWithDecl(this MethodDef self, MethodDef other)
		{
			if (ReferenceEquals(self, other))
				return true;
			return MethodEqualityComparer.CompareDeclaringTypes.Equals(self, other);
		}

		public static bool EqualsNoDecl(this MethodDef self, MethodDef other)
		{
			if (ReferenceEquals(self, other))
				return true;
			return MethodEqualityComparer.DontCompareDeclaringTypes.Equals(self, other);
		}

		public static int GetHashNoDecl(this MethodDef self)
		{
			return MethodEqualityComparer.DontCompareDeclaringTypes.GetHashCode(self);
		}

		public static bool EqualsNoDecl(this FieldDef self, FieldDef other)
		{
			if (ReferenceEquals(self, other))
				return true;
			return FieldEqualityComparer.DontCompareDeclaringTypes.Equals(self, other);
		}

		public static int GetHashNoDecl(this FieldDef self)
		{
			return FieldEqualityComparer.DontCompareDeclaringTypes.GetHashCode(self);
		}

		public static bool TypeEquals(this TypeDef self, TypeDef other)
		{
			if (ReferenceEquals(self, other))
				return true;
			return TypeEqualityComparer.Instance.Equals(self, other);
		}

		public static int TypeHashCode(this TypeDef self)
		{
			return self.Name.GetHashCode();
		}

		public static NonLeafSig ModifierClone(this NonLeafSig self)
		{
			switch (self)
			{
				case SZArraySig _:
					return new SZArraySig(null);

				case ByRefSig _:
					return new ByRefSig(null);

				case PtrSig _:
					return new PtrSig(null);

				case ArraySig arySig:
					return new ArraySig(null, arySig.Rank, arySig.Sizes, arySig.LowerBounds);

				case CModOptSig moptSig:
					return new CModOptSig(moptSig.Modifier, null);

				case CModReqdSig mreqSig:
					return new CModReqdSig(mreqSig.Modifier, null);

				default:
					Debug.Fail("ModifierClone " + self);
					break;
			}
			return null;
		}

		private static SigComparer ModifierComparer_ = new SigComparer();
		public static bool ModifierEquals(this NonLeafSig self, NonLeafSig other)
		{
			if (ReferenceEquals(self, other))
				return true;

			if (self.ElementType != other.ElementType)
				return false;

			return ModifierComparer_.Equals(self, other);
		}

		public static string ModifierToString(this NonLeafSig self, bool isPretty)
		{
			if (isPretty && self.ElementType == ElementType.CModReqd)
			{
				if (self.ToString() == " modreq(System.Runtime.CompilerServices.IsVolatile)")
					return " volatile";
			}
			return self.ToString();
		}

		public static string PrettyName(this TypeDef self)
		{
			if (self.Namespace == "System")
			{
				switch (self.Name)
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

			return (self.IsValueType ? "[valuetype]" : "") + self.FullName;
		}

		public static StringBuilder AppendFormatLine(this StringBuilder self, string format, params object[] args)
		{
			return self.AppendFormat(format, args).AppendLine();
		}
	}
}
