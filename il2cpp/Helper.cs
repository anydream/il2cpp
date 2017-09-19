using System;
using System.Collections.Generic;
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
							break;
						}
				}

				tySig = tySig.Next;
			}
			return false;
		}

		public static bool IsValueType(TypeSig tySig)
		{
			return tySig.ElementType == ElementType.ValueType ||
				   tySig.ElementType == ElementType.GenericInst && tySig.IsValueType;
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
			sb.Append(EscapeName(name));
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
			sb.Append(EscapeName(name));
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
			sb.Append(EscapeName(name));
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

		private static void ClassSigName(StringBuilder sb, TypeSig tySig)
		{
			string fullName = tySig.FullName;

			if (tySig.DefinitionAssembly.IsCorLib())
			{
				string basicName = IsBasicType(fullName);
				if (basicName != null)
					sb.Append(basicName);
				else
					sb.Append(fullName);
			}
			else
				sb.AppendFormat("[{0}]{1}",
					EscapeName(tySig.DefinitionAssembly.Name),
					EscapeName(fullName));
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
					EscapeName(tyDef.DefinitionAssembly.Name),
					EscapeName(fullName));
		}

		private static string IsBasicType(string fullName)
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

		private static string EscapeChar(char ch)
		{
			if (ch >= 'a' && ch <= 'z' ||
				ch >= 'A' && ch <= 'Z' ||
				ch >= '0' && ch <= '9' ||
				ch == '_' || ch == '`' || ch == '.' ||
				ch == ':' || ch == '/')
			{
				return null;
			}
			return @"\u" + ((uint)ch).ToString("X4");
		}

		// 转义特殊符号
		private static string EscapeName(string name)
		{
			string result = null;
			foreach (char ch in name)
			{
				string escape = EscapeChar(ch);
				if (escape == null)
					result += ch;
				else
					result += escape;
			}
			return result;
		}
	}
}
