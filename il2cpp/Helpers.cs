using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

		public static string TypeGenericReplace(string sig, List<TypeX> tyGenArgs)
		{
			// 替换类型泛型为具体类型
			if (tyGenArgs != null)
			{
				for (int i = 0; i < tyGenArgs.Count; ++i)
				{
					string from = "!" + i;
					string to = tyGenArgs[i].ToString();
					sig = TypeGenericReplace(sig, from, to);
				}
			}
			return sig;
		}

		private static bool IsDigit(char ch)
		{
			return ch >= '0' && ch <= '9';
		}

		private static string TypeGenericReplace(string input, string from, string to)
		{
			int pos = 0;
			for (;;)
			{
				pos = input.IndexOf(from, pos, StringComparison.Ordinal);
				if (pos == -1)
					break;

				if (pos > 0 && input[pos - 1] == '!')
				{
					++pos;
					continue;
				}

				if (pos < input.Length - from.Length)
				{
					if (IsDigit(input[pos + from.Length]))
					{
						++pos;
						continue;
					}
				}

				input = input.Substring(0, pos) + to + input.Substring(pos + from.Length);
				pos += to.Length;
			}
			return input;
		}

		// 生成方法签名. 如果存在类型泛型则替换成具体类型
		public static string MakeSignature(string metName, string sig, List<TypeX> tyGenArgs)
		{
			sig = TypeGenericReplace(sig, tyGenArgs);
			return metName + ": " + sig;
		}

		public static string MakeSignature(MethodDef metDef, List<TypeX> tyGenArgs)
		{
			return MakeSignature(metDef.Name, metDef.Signature.ToString(), tyGenArgs);
		}

		public static string GetMethodRefSig(MemberRef metRef)
		{
			Debug.Assert(metRef.IsMethodRef);
			var parent = metRef.Class;
			IList<TypeSig> typeGenArgs = null;
			if (parent is TypeSpec)
			{
				var sig = ((TypeSpec)parent).TypeSig as GenericInstSig;
				if (sig != null)
					typeGenArgs = sig.GenericArguments;
			}
			return FullNameCreator.MethodFullName(null, null, metRef.MethodSig, typeGenArgs, null, null, null);
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

	public class UniqueList<T> : IEnumerable<T>
	{
		private readonly HashSet<T> Set_ = new HashSet<T>();
		private readonly List<T> List_ = new List<T>();

		public int Count => List_.Count;
		public T this[int key] => List_[key];

		public bool Add(T val)
		{
			if (Set_.Contains(val))
				return false;

			Set_.Add(val);
			List_.Add(val);
			return true;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return List_.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
