using System;
using System.Collections.Generic;
using System.Text;
using dnlib.DotNet;
using dnlib.Threading;

namespace il2cpp
{
	internal class NameManager
	{
		// 当前环境
		private readonly Il2cppContext Context;

		internal uint NameCounter;
		internal uint TypeIdCounter;

		internal const string ClassPrefix = "cls_";
		internal const string StructPrefix = "stru_";
		internal const string FieldPrefix = "fld_";
		internal const string MethodPrefix = "met_";
		internal const string VMethodPrefix = "vmet_";
		internal const string VFuncPrefix = "vftn_";
		internal const string ICallPrefix = "icall_";
		internal const string TempPrefix = "tmp_";
		internal const string LocalPrefix = "loc_";
		internal const string ArgPrefix = "arg_";

		internal NameManager(Il2cppContext context)
		{
			Context = context;
		}

		public static void MethodSigName(
			StringBuilder sb,
			string name,
			IList<TypeSig> genArgs,
			TypeSig retType,
			IList<TypeSig> paramTypes,
			CallingConvention callConv)
		{
			sb.Append(EscapeName(name));
			sb.Append('|');

			TypeSigName(sb, retType);

			if (genArgs != null)
			{
				sb.Append('<');
				TypeSigListName(sb, genArgs);
				sb.Append('>');
			}

			sb.Append('(');
			TypeSigListName(sb, paramTypes);
			sb.Append(')');
			sb.Append('|');

			sb.Append(((uint)callConv).ToString("X"));
		}

		public static void TypeSigListName(StringBuilder sb, IList<TypeSig> tySigList)
		{
			bool last = false;
			foreach (var tySig in tySigList)
			{
				if (last)
					sb.Append(',');
				last = true;
				TypeSigName(sb, tySig);
			}
		}

		public static void TypeSigName(StringBuilder sb, TypeSig tySig)
		{
			if (tySig == null)
				return;

			switch (tySig.ElementType)
			{
				case ElementType.Class:
				case ElementType.ValueType:
					sb.Append(ClassSigName(tySig));
					return;

				case ElementType.Ptr:
					TypeSigName(sb, tySig.Next);
					sb.Append('*');
					return;

				case ElementType.ByRef:
					TypeSigName(sb, tySig.Next);
					sb.Append('&');
					return;

				case ElementType.Pinned:
					TypeSigName(sb, tySig.Next);
					return;

				case ElementType.SZArray:
					TypeSigName(sb, tySig.Next);
					sb.Append("[]");
					return;

				case ElementType.Array:
					{
						TypeSigName(sb, tySig.Next);
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
					TypeSigName(sb, tySig.Next);
					sb.AppendFormat(" modreq({0})", ((CModReqdSig)tySig).Modifier.FullName);
					return;

				case ElementType.CModOpt:
					TypeSigName(sb, tySig.Next);
					sb.AppendFormat(" modopt({0})", ((CModOptSig)tySig).Modifier.FullName);
					return;

				case ElementType.GenericInst:
					{
						GenericInstSig genInstSig = (GenericInstSig)tySig;
						TypeSigName(sb, genInstSig.GenericType);
						sb.Append('<');
						TypeSigListName(sb, genInstSig.GenericArguments);
						sb.Append('>');
						return;
					}

				case ElementType.Var:
				case ElementType.MVar:
					{
						var genSig = (GenericSig)tySig;
						if (genSig.IsMethodVar)
							sb.Append("!!");
						else
							sb.Append('!');
						sb.Append(genSig.Number);
						return;
					}

				default:
					if (tySig is CorLibTypeSig)
					{
						sb.Append(ClassSigName(tySig));
						return;
					}

					throw new ArgumentOutOfRangeException();
			}
		}

		private static string ClassSigName(TypeSig tySig)
		{
			if (tySig.DefinitionAssembly.IsCorLib())
				return tySig.TypeName;
			return tySig.FullName;
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
		public static string EscapeName(string name)
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
