using System;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	internal class GeneratorContext
	{
		private readonly TypeManager TypeMgr;

		public GeneratorContext(TypeManager typeMgr)
		{
			TypeMgr = typeMgr;
		}

		public string GetTypeName(TypeSig tySig)
		{
			switch (tySig.ElementType)
			{
				case ElementType.I1:
					return "int8_t";
				case ElementType.I2:
					return "int16_t";
				case ElementType.I4:
					return "int32_t";
				case ElementType.I8:
					return "int64_t";
				case ElementType.U1:
					return "uint8_t";
				case ElementType.U2:
					return "uint16_t";
				case ElementType.U4:
					return "uint32_t";
				case ElementType.U8:
					return "uint64_t";
				case ElementType.Boolean:
					return "uint8_t";
				case ElementType.Char:
					return "uint16_t";
				case ElementType.R4:
					return "float";
				case ElementType.R8:
					return "double";

				case ElementType.I:
					return "intptr_t";
				case ElementType.U:
					return "uintptr_t";
				case ElementType.Ptr:
				case ElementType.ByRef:
					return "void*";

				default:
					throw new NotImplementedException();
			}

			return null;
		}

		public string GetTypeName(TypeX tyX)
		{
			string strName = tyX.GenTypeName;
			if (strName != null)
				return strName;

			strName = tyX.Def.IsValueType ? "stru_" : "cls_";

			string nameKey = tyX.GetNameKey();
			if (tyX.Def.DefinitionAssembly.IsCorLib())
				strName += EscapeName(nameKey);
			else
				strName += NameHash(nameKey) + '_' + EscapeName(nameKey);

			tyX.GenTypeName = strName;
			return strName;
		}

		private static string EscapeName(string fullName)
		{
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < fullName.Length; ++i)
			{
				char ch = fullName[i];
				if (IsLegalIdentChar(ch))
					sb.Append(ch);
				else if (ch >= 0x7F)
					sb.AppendFormat("{0:X}", ch);
				else
					sb.Append('_');
			}
			return sb.ToString();
		}

		private static bool IsLegalIdentChar(char ch)
		{
			return ch >= 'a' && ch <= 'z' ||
				   ch >= 'A' && ch <= 'Z' ||
				   ch >= '0' && ch <= '9' ||
				   ch == '_';
		}

		private static string NameHash(string fullName)
		{
			return ToRadix((uint)fullName.GetHashCode(), (uint)DigMap.Length);
		}

		private const string DigMap = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
		private static string ToRadix(uint value, uint radix)
		{
			StringBuilder sb = new StringBuilder();
			do
			{
				uint dig = value % radix;
				value /= radix;
				sb.Append(DigMap[(int)dig]);
			} while (value != 0);

			return sb.ToString();
		}
	}
}
