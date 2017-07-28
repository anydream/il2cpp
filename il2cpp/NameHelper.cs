using System;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	internal static class NameHelper
	{
		public static uint NameCounter;
		public static uint TypeIDCounter;

		public static void Reset()
		{
			NameCounter = 0;
			TypeIDCounter = 0;
		}

		private static void SigToCppName(TypeSig sig, StringBuilder sb, TypeManager typeMgr)
		{
			switch (sig.ElementType)
			{
				case ElementType.Void:
					sb.Append("void");
					return;
				case ElementType.I1:
					sb.Append("int8_t");
					return;
				case ElementType.U1:
					sb.Append("uint8_t");
					return;
				case ElementType.I2:
					sb.Append("int16_t");
					return;
				case ElementType.U2:
				case ElementType.Char:
					sb.Append("uint16_t");
					return;
				case ElementType.I4:
				case ElementType.Boolean:
					sb.Append("int32_t");
					return;
				case ElementType.U4:
					sb.Append("uint32_t");
					return;
				case ElementType.I8:
					sb.Append("int64_t");
					return;
				case ElementType.U8:
					sb.Append("uint64_t");
					return;
				case ElementType.R4:
					sb.Append("float");
					return;
				case ElementType.R8:
					sb.Append("double");
					return;
				case ElementType.I:
					sb.Append("intptr_t");
					return;
				case ElementType.U:
					sb.Append("uintptr_t");
					return;

				case ElementType.ValueType:
					{
						TypeX type = typeMgr.GetNamedType(sig.FullName, sig.Module.RuntimeVersion);
						if (type != null)
							sb.Append(type.GetCppName());
						else
							sb.Append("il2cppDummy");
					}
					return;

				case ElementType.Object:
				case ElementType.Class:
					{
						TypeX type = typeMgr.GetNamedType(sig.FullName, sig.Module.RuntimeVersion);
						if (type != null)
							sb.Append("struct " + type.GetCppName() + '*');
						else
							sb.Append("il2cppDummy*");
					}
					return;

				case ElementType.Ptr:
				case ElementType.ByRef:
					SigToCppName(sig.Next, sb, typeMgr);
					sb.Append('*');
					return;

				case ElementType.SZArray:
					sb.Append("il2cppSZArray<");
					SigToCppName(sig.Next, sb, typeMgr);
					sb.Append(">*");
					return;

				case ElementType.Array:
					//! il2cppArray<next, 0, 10, 0, 10, ...>*
					break;

				case ElementType.Pinned:
				case ElementType.CModReqd:
				case ElementType.CModOpt:
					SigToCppName(sig.Next, sb, typeMgr);
					return;

				default:
					/*if (sig is CorLibTypeSig corTypeSig)
					{
						// 基础类型
						switch (corTypeSig.FullName)
						{
							default:
								throw new ArgumentOutOfRangeException("SigToCppName CorLibTypeSig " + corTypeSig);
						}

						return;
					}*/

					throw new ArgumentOutOfRangeException("SigToCppName TypeSig " + sig.FullName);
			}
		}

		public static string GetCppName(this TypeSig sig, TypeManager typeMgr)
		{
			StringBuilder sb = new StringBuilder();
			SigToCppName(sig, sb, typeMgr);
			return sb.ToString();
		}

		public static string GetCppName(this TypeX tyX)
		{
			if (tyX.CppName_ == null)
				tyX.CppName_ = (tyX.Def.IsValueType ? "stru_" : "cls_") + ToCppName(tyX.FullName);
			return tyX.CppName_;
		}

		public static uint GetCppTypeID(this TypeX tyX)
		{
			if (tyX.CppTypeID_ == 0)
				tyX.CppTypeID_ = ++TypeIDCounter;
			return tyX.CppTypeID_;
		}

		public static string GetCppName(this MethodX metX, string prefix)
		{
			if (metX.CppName_ == null)
				metX.CppName_ = ToCppName(metX.Name);

			return prefix + metX.CppName_;
		}

		public static string GetCppName(this FieldX fldX)
		{
			if (fldX.CppName_ == null)
				fldX.CppName_ = (fldX.Def.IsStatic ? "sfld_" : "fld_") + ToCppName(fldX.Def.Name);
			return fldX.CppName_;
		}

		private static string ToCppName(string fullName)
		{
			StringBuilder sb = new StringBuilder();

			string hash = ToRadix(NameCounter++, (uint)DigMap.Length);
			sb.Append(hash + '_');

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
