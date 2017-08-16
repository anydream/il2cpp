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

		private static string GetElemTypeName(ElementType et)
		{
			switch (et)
			{
				case ElementType.Void:
					return "void";
				case ElementType.I1:
					return "int8_t";
				case ElementType.U1:
				case ElementType.Boolean:
					return "uint8_t";
				case ElementType.I2:
					return "int16_t";
				case ElementType.U2:
				case ElementType.Char:
					return "uint16_t";
				case ElementType.I4:
					return "int32_t";
				case ElementType.U4:
					return "uint32_t";
				case ElementType.I8:
					return "int64_t";
				case ElementType.U8:
					return "uint64_t";
				case ElementType.R4:
					return "float";
				case ElementType.R8:
					return "double";
				case ElementType.I:
					return "intptr_t";
				case ElementType.U:
					return "uintptr_t";

				case ElementType.Object:
					return "il2cppObject*";
				case ElementType.String:
					return "il2cppString*";
			}
			return null;
		}

		private static void SigToCppName(TypeSig sig, StringBuilder sb, TypeManager typeMgr)
		{
			string elemName = GetElemTypeName(sig.ElementType);
			if (elemName != null)
			{
				sb.Append(elemName);
				return;
			}

			switch (sig.ElementType)
			{
				case ElementType.Class:
				case ElementType.ValueType:
				case ElementType.GenericInst:
					{
						TypeX type = typeMgr.GetTypeByName(sig.FullName);
						if (type != null)
						{
							if (type.Def.IsValueType)
								sb.Append(type.GetCppName());
							else
								sb.Append("struct " + type.GetCppName() + '*');
						}
						else
						{
							if (sig.IsValueType)
								sb.Append("il2cppValueType");
							else
								sb.Append("il2cppObject*");
						}
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
					throw new ArgumentOutOfRangeException("SigToCppName TypeSig " + sig.FullName);
			}
		}

		public static string GetCppName(this TypeSig sig, TypeManager typeMgr)
		{
			StringBuilder sb = new StringBuilder();
			SigToCppName(sig, sb, typeMgr);
			return sb.ToString();
		}

		public static string GetInitValue(this TypeSig sig, TypeManager typeMgr)
		{
			switch (sig.ElementType)
			{
				case ElementType.I1:
				case ElementType.U1:
				case ElementType.I2:
				case ElementType.U2:
				case ElementType.I4:
				case ElementType.U4:
				case ElementType.I8:
				case ElementType.U8:
				case ElementType.R4:
				case ElementType.R8:
				case ElementType.I:
				case ElementType.U:
				case ElementType.Char:
				case ElementType.Boolean:
					return "0";
			}

			if (sig.IsValueType)
				return sig.GetCppName(typeMgr) + "()";
			return "nullptr";
		}

		public static string GetCppName(this TypeX tyX, bool elemType = false)
		{
			if (elemType)
			{
				string elemName = GetElemTypeName(tyX.ToTypeSig().ElementType);
				if (elemName != null)
					return elemName;
			}

			if (tyX.CppName_ == null)
				tyX.CppName_ = (tyX.Def.IsValueType ? "stru_" : "cls_") + ToCppName(tyX.FullName);
			return tyX.CppName_;
		}

		public static uint GetCppTypeID(this TypeX tyX)
		{
			if (tyX == null)
				return 0;
			if (tyX.CppTypeID_ == 0)
				tyX.CppTypeID_ = ++TypeIDCounter;
			return tyX.CppTypeID_;
		}

		public static string GetCppName(this MethodX metX, string prefix)
		{
			if (metX.CppName_ == null)
			{
				if (metX.Def.IsInternalCall)
					metX.CppName_ = "icall_" + ToCppName(metX.Name, false);
				else
					metX.CppName_ = ToCppName(metX.Name);
			}

			return prefix + metX.CppName_;
		}

		public static string GetCppName(this FieldX fldX)
		{
			if (fldX.CppName_ == null)
			{
				fldX.CppName_ = fldX.Def.IsStatic ?
					"sfld_" + ToCppName(fldX.DeclType.FullName + "::" + fldX.Def.Name) :
					"fld_" + ToCppName(fldX.Def.Name);
			}
			return fldX.CppName_;
		}

		private static string ToCppName(string fullName, bool hasHash = true)
		{
			StringBuilder sb = new StringBuilder();

			if (hasHash)
			{
				string hash = ToRadix(NameCounter++, (uint)DigMap.Length);
				sb.Append(hash + '_');
			}

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
