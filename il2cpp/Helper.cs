using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.Threading;

namespace il2cpp
{
	// 泛型签名替换器
	internal interface IGenericReplacer
	{
		TypeSig Replace(GenericVar genVarSig);
		TypeSig Replace(GenericMVar genMVarSig);
	}

	internal class GenericReplacer : IGenericReplacer
	{
		public readonly TypeX OwnerType;
		public readonly MethodX OwnerMethod;

		public GenericReplacer(TypeX ownerTyX, MethodX ownerMetX)
		{
			OwnerType = ownerTyX;
			OwnerMethod = ownerMetX;
		}

		public TypeSig Replace(GenericVar genVarSig)
		{
			if (genVarSig.OwnerType == OwnerType.Def)
				return OwnerType.GenArgs[(int)genVarSig.Number];
			return null;
		}

		public TypeSig Replace(GenericMVar genMVarSig)
		{
			if (genMVarSig.OwnerMethod == OwnerMethod.Def)
				return OwnerMethod.GenArgs[(int)genMVarSig.Number];
			return null;
		}
	}

	internal class TypeDefGenReplacer : IGenericReplacer
	{
		public readonly TypeDef OwnerType;
		public readonly IList<TypeSig> TypeGenArgs;

		public TypeDefGenReplacer(TypeDef ownerTyDef, IList<TypeSig> tyGenArgs)
		{
			OwnerType = ownerTyDef;
			TypeGenArgs = tyGenArgs;
		}

		public TypeSig Replace(GenericVar genVarSig)
		{
			if (genVarSig.OwnerType == OwnerType)
				return TypeGenArgs[(int)genVarSig.Number];
			return null;
		}

		public TypeSig Replace(GenericMVar genMVarSig)
		{
			return null;
		}
	}

	// 辅助扩展方法
	internal static class Helper
	{
		public static bool IsCollectionValid<T>(this ICollection<T> co)
		{
			return co != null && co.Count > 0;
		}

		public static TValue GetOrCreate<TKey, TValue>(
			this IDictionary<TKey, TValue> dict,
			TKey key,
			Func<TValue> creator)
		{
			if (dict.TryGetValue(key, out TValue val))
				return val;
			val = creator();
			dict.Add(key, val);
			return val;
		}

		public static List<T> SortStable<T>(this List<T> ary, Comparison<T> comp)
		{
			List<int> idxAry = new List<int>();
			for (int i = 0; i < ary.Count; ++i)
				idxAry.Add(i);

			idxAry.Sort((ilhs, irhs) =>
			{
				int res = comp(ary[ilhs], ary[irhs]);
				if (res == 0)
					return ilhs.CompareTo(irhs);
				return res;
			});

			List<T> result = new List<T>();
			foreach (int idx in idxAry)
				result.Add(ary[idx]);

			return result;
		}

		public static TypeSig RemoveModifiers(TypeSig tySig)
		{
			while (tySig.ElementType == ElementType.CModReqd ||
				   tySig.ElementType == ElementType.CModOpt)
			{
				tySig = tySig.Next;
			}
			return tySig;
		}

		// 替换类型中的泛型签名
		public static TypeSig ReplaceGenericSig(TypeSig tySig, IGenericReplacer replacer)
		{
			if (replacer == null || !IsReplaceNeeded(tySig))
				return tySig;

			return ReplaceGenericSigImpl(tySig, replacer);
		}

		private static TypeSig ReplaceGenericSigImpl(TypeSig tySig, IGenericReplacer replacer)
		{
			if (tySig == null)
				return null;

			switch (tySig.ElementType)
			{
				case ElementType.Class:
				case ElementType.ValueType:
				case ElementType.TypedByRef:
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
						return genVarSig;
					}
				case ElementType.MVar:
					{
						GenericMVar genMVarSig = (GenericMVar)tySig;
						TypeSig result = replacer.Replace(genMVarSig);
						if (result != null)
							return result;
						return genMVarSig;
					}

				default:
					if (tySig is CorLibTypeSig)
						return tySig;

					throw new NotSupportedException();
			}
		}

		// 替换类型签名列表
		public static IList<TypeSig> ReplaceGenericSigList(IList<TypeSig> tySigList, IGenericReplacer replacer)
		{
			return tySigList?.Select(tySig => ReplaceGenericSig(tySig, replacer)).ToList();
		}

		private static IList<TypeSig> ReplaceGenericSigListImpl(IList<TypeSig> tySigList, IGenericReplacer replacer)
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
						}
						break;
				}

				tySig = tySig.Next;
			}
			return false;
		}

		public static bool IsEnumType(TypeSig tySig, out TypeSig enumTypeSig)
		{
			if (tySig.IsValueType)
			{
				TypeDef tyDef = tySig.ToTypeDefOrRef().ResolveTypeDef();
				if (tyDef.BaseType.FullName == "System.Enum")
				{
					FieldDef fldDef = tyDef.Fields.FirstOrDefault(f => !f.IsStatic);
					Debug.Assert(fldDef != null);
					enumTypeSig = fldDef.FieldType;
					return true;
				}
			}

			enumTypeSig = null;
			return false;
		}

		public static void TypeNameKey(
			StringBuilder sb,
			TypeDef tyDef,
			IList<TypeSig> genArgs)
		{
			ClassSigName(sb, tyDef);
			if (genArgs.IsCollectionValid())
			{
				sb.Append('<');
				TypeSigListName(sb, genArgs, true);
				sb.Append('>');
			}
		}

		public static void MethodNameKey(
			StringBuilder sb,
			string name,
			int genCount,
			TypeSig retType,
			IList<TypeSig> paramTypes,
			CallingConvention callConv)
		{
			sb.Append(name);
			sb.Append('|');

			TypeSigName(sb, retType, false);

			if (genCount > 0)
			{
				sb.Append('<');
				sb.Append(genCount);
				sb.Append('>');
			}

			sb.Append('(');
			TypeSigListName(sb, paramTypes, false);
			sb.Append(')');
			sb.Append('|');

			sb.Append(((uint)callConv).ToString("X"));
		}

		public static void MethodNameKeyWithGen(
			StringBuilder sb,
			string name,
			IList<TypeSig> genArgs,
			TypeSig retType,
			IList<TypeSig> paramTypes,
			CallingConvention callConv)
		{
			sb.Append(name);
			sb.Append('|');

			TypeSigName(sb, retType, false);

			if (genArgs.IsCollectionValid())
			{
				sb.Append('<');
				TypeSigListName(sb, genArgs, false);
				sb.Append('>');
			}

			sb.Append('(');
			TypeSigListName(sb, paramTypes, false);
			sb.Append(')');
			sb.Append('|');

			sb.Append(((uint)callConv).ToString("X"));
		}

		public static void MethodDefNameKey(
			StringBuilder sb,
			MethodDef metDef,
			IGenericReplacer replacer)
		{
			if (replacer == null)
			{
				MethodNameKey(
					sb,
					metDef.Name,
					metDef.GenericParameters.Count,
					metDef.MethodSig.RetType,
					metDef.MethodSig.Params,
					metDef.MethodSig.CallingConvention);
			}
			else
			{
				TypeSig retType = Helper.ReplaceGenericSig(metDef.MethodSig.RetType, replacer);
				IList<TypeSig> paramTypes = Helper.ReplaceGenericSigList(metDef.MethodSig.Params, replacer);

				MethodNameKey(
					sb,
					metDef.Name,
					metDef.GenericParameters.Count,
					retType,
					paramTypes,
					metDef.MethodSig.CallingConvention);
			}
		}

		public static void FieldNameKey(
			StringBuilder sb,
			string name,
			TypeSig fldType)
		{
			sb.Append(name);
			sb.Append('|');
			TypeSigName(sb, fldType, false);
		}

		public static void TypeSigListName(StringBuilder sb, IList<TypeSig> tySigList, bool printGenOwner)
		{
			bool last = false;
			foreach (var tySig in tySigList)
			{
				if (last)
					sb.Append(',');
				last = true;
				TypeSigName(sb, tySig, printGenOwner);
			}
		}

		public static void TypeSigName(StringBuilder sb, TypeSig tySig, bool printGenOwner, int depth = 0)
		{
			if (depth > 512)
				throw new TypeLoadException("The TypeSig chain is too long. Or there are some recursive generics that are expanded");

			if (tySig == null)
				return;

			switch (tySig.ElementType)
			{
				case ElementType.Class:
				case ElementType.ValueType:
				case ElementType.TypedByRef:
					ClassSigName(sb, tySig);
					return;

				case ElementType.Ptr:
					TypeSigName(sb, tySig.Next, printGenOwner, depth + 1);
					sb.Append('*');
					return;

				case ElementType.ByRef:
					TypeSigName(sb, tySig.Next, printGenOwner, depth + 1);
					sb.Append('&');
					return;

				case ElementType.Pinned:
					TypeSigName(sb, tySig.Next, printGenOwner, depth + 1);
					return;

				case ElementType.SZArray:
					TypeSigName(sb, tySig.Next, printGenOwner, depth + 1);
					sb.Append("[]");
					return;

				case ElementType.Array:
					{
						TypeSigName(sb, tySig.Next, printGenOwner, depth + 1);
						ArraySig arySig = (ArraySig)tySig;
						GetArraySigPostfix(sb, arySig);
						return;
					}

				case ElementType.CModReqd:
					TypeSigName(sb, tySig.Next, printGenOwner, depth + 1);
					sb.Append(" modreq(");
					ClassSigName(sb, ((CModReqdSig)tySig).Modifier.ResolveTypeDef());
					sb.Append(')');
					return;

				case ElementType.CModOpt:
					TypeSigName(sb, tySig.Next, printGenOwner, depth + 1);
					sb.Append(" modopt(");
					ClassSigName(sb, ((CModOptSig)tySig).Modifier.ResolveTypeDef());
					sb.Append(')');
					return;

				case ElementType.GenericInst:
					{
						GenericInstSig genInstSig = (GenericInstSig)tySig;
						TypeSigName(sb, genInstSig.GenericType, printGenOwner, depth + 1);
						sb.Append('<');
						TypeSigListName(sb, genInstSig.GenericArguments, printGenOwner);
						sb.Append('>');
						return;
					}

				case ElementType.Var:
				case ElementType.MVar:
					{
						var genSig = (GenericSig)tySig;
						if (genSig.IsMethodVar)
						{
							sb.Append("!!");
						}
						else
						{
							sb.Append('!');
							if (printGenOwner)
							{
								sb.Append('(');
								ClassSigName(sb, genSig.OwnerType);
								sb.Append(')');
							}
						}
						sb.Append(genSig.Number);
						return;
					}

				default:
					if (tySig is CorLibTypeSig)
					{
						ClassSigName(sb, tySig);
						return;
					}

					throw new ArgumentOutOfRangeException();
			}
		}

		public static void GetArraySigPostfix(StringBuilder sb, ArraySig arySig)
		{
			sb.Append('[');
			uint rank = arySig.Rank;
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
					int lower = arySig.LowerBounds.Get(i, NO_LOWER);
					uint size = arySig.Sizes.Get(i, NO_SIZE);
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
		}

		private static void ClassSigName(StringBuilder sb, TypeSig tySig)
		{
			string fullName = tySig.FullName;

			if (tySig.ElementType != ElementType.SZArray &&
				tySig.ElementType != ElementType.Array &&
				tySig.ElementType != ElementType.GenericInst &&
				tySig.DefinitionAssembly.IsCorLib())
			{
				string basicName = IsBasicType(fullName);
				if (basicName != null)
					sb.Append(basicName);
				else
					sb.Append(fullName);
			}
			else
				sb.AppendFormat("[{0}]{1}",
					AssemblyName(tySig.DefinitionAssembly),
					fullName);
		}

		private static void ClassSigName(StringBuilder sb, TypeDef tyDef)
		{
			string fullName = tyDef.FullName;

			if (tyDef.DefinitionAssembly.IsCorLib())
			{
				string basicName = IsBasicType(fullName);
				if (basicName != null)
					sb.Append(basicName);
				else
					sb.Append(fullName);
			}
			else
				sb.AppendFormat("[{0}]{1}",
								AssemblyName(tyDef.DefinitionAssembly),
								fullName);
		}

		public static bool IsExtern(MethodDef metDef)
		{
			return !metDef.HasBody && !metDef.IsAbstract;
		}

		public static bool IsInstanceField(FieldDef fldDef)
		{
			return !fldDef.IsStatic;
		}

		public static bool IsBasicValueType(ElementType elemType)
		{
			switch (elemType)
			{
				case ElementType.Boolean:
				case ElementType.Char:
				case ElementType.I1:
				case ElementType.I2:
				case ElementType.I4:
				case ElementType.I8:
				case ElementType.U1:
				case ElementType.U2:
				case ElementType.U4:
				case ElementType.U8:
				case ElementType.R4:
				case ElementType.R8:
				case ElementType.I:
				case ElementType.U:
				case ElementType.Ptr:
				case ElementType.ByRef:
					return true;
			}
			return false;
		}

		public static string IsBasicType(string fullName)
		{
			switch (fullName)
			{
				case "System.Void":
					return "Void";
				case "System.Boolean":
					return "Boolean";
				case "System.Char":
					return "Char";
				case "System.Object":
					return "Object";
				case "System.String":
					return "String";
				case "System.SByte":
					return "SByte";
				case "System.Byte":
					return "Byte";
				case "System.Int16":
					return "Int16";
				case "System.UInt16":
					return "UInt16";
				case "System.Int32":
					return "Int32";
				case "System.UInt32":
					return "UInt32";
				case "System.Int64":
					return "Int64";
				case "System.UInt64":
					return "UInt64";
				case "System.IntPtr":
					return "IntPtr";
				case "System.UIntPtr":
					return "UIntPtr";
				case "System.Single":
					return "Single";
				case "System.Double":
					return "Double";
			}
			return null;
		}

		public static string AssemblyName(IAssembly asm)
		{
			var sb = new StringBuilder();

			sb.Append(asm.Name);

			if (asm.Version != null)
			{
				sb.AppendFormat(",ver={0}", asm.Version);
			}

			if (!UTF8String.IsNullOrEmpty(asm.Culture))
			{
				sb.AppendFormat(",cul={0}", asm.Culture.String);
			}

			var publicKey = asm.PublicKeyOrToken;
			if (publicKey != null &&
				publicKey.Data != null &&
				publicKey.Data.Length != 0)
			{
				sb.AppendFormat(",{0}={1}",
					publicKey is PublicKeyToken ? "tok=" : "key=",
					publicKey);
			}

			return sb.ToString();
		}

		public static int CombineHash(params int[] hashCodes)
		{
			int result = 0;
			foreach (int hash in hashCodes)
				result = ((result << 5) + result) ^ hash;
			return result;
		}

		public static string EscapeString(string str)
		{
			return ReplaceNonCharacters(str, '?')
				.Replace("\\", "\\x5C")
				.Replace("\n", "\\n")
				.Replace("\r", "\\r")
				.Replace("\x0B", "\\x0B")
				.Replace("\x0C", "\\x0C")
				.Replace("\x85", "\\x85")
				.Replace("\u2028", "\\u2028")
				.Replace("\u2029", "\\u2029");
		}

		private static string ReplaceNonCharacters(string aString, char replacement)
		{
			var sb = new StringBuilder(aString.Length);
			for (var i = 0; i < aString.Length; i++)
			{
				if (char.IsSurrogatePair(aString, i))
				{
					int c = char.ConvertToUtf32(aString, i);
					i++;
					if (IsCharacter(c))
						sb.Append(char.ConvertFromUtf32(c));
					else
						sb.Append(replacement);
				}
				else
				{
					char c = aString[i];
					if (IsCharacter(c))
						sb.Append(c);
					else
						sb.Append(replacement);
				}
			}
			return sb.ToString();
		}

		private static bool IsCharacter(int point)
		{
			return point < 0xFDD0;
		}

		public static bool IsPowerOfTwo(ushort x)
		{
			return (x & (x - 1)) == 0;
		}

		public static string ByteArrayToCode(byte[] arr)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append('{');
			for (int i = 0; i < arr.Length; ++i)
			{
				if (i != 0)
					sb.Append(',');
				sb.Append(arr[i].ToString());
			}
			sb.Append('}');
			return sb.ToString();
		}
	}
}
