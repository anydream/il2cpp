using System.Diagnostics;
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
				case ElementType.ValueType:
					{
						TypeX type = typeMgr.GetNamedType(sig.FullName, sig.Module.RuntimeVersion);
						if (type != null)
							sb.Append(type.GetCppName());
						else
							sb.Append("il2cppDummy");
					}
					return;

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
					if (sig is CorLibTypeSig corTypeSig)
					{
						// 基础类型
						if (corTypeSig.Equals(typeMgr.CorTypes.Void))
							sb.Append("void");
						else if (corTypeSig.Equals(typeMgr.CorTypes.Int32))
							sb.Append("int32_t");
						else if (corTypeSig.Equals(typeMgr.CorTypes.UInt32))
							sb.Append("uint32_t");
						else if (corTypeSig.Equals(typeMgr.CorTypes.Boolean))
							sb.Append("int32_t");
						else if (corTypeSig.Equals(typeMgr.CorTypes.Int64))
							sb.Append("int64_t");
						else if (corTypeSig.Equals(typeMgr.CorTypes.UInt64))
							sb.Append("uint64_t");
						else if (corTypeSig.Equals(typeMgr.CorTypes.Int16))
							sb.Append("int16_t");
						else if (corTypeSig.Equals(typeMgr.CorTypes.UInt16))
							sb.Append("uint16_t");
						else if (corTypeSig.Equals(typeMgr.CorTypes.SByte))
							sb.Append("int8_t");
						else if (corTypeSig.Equals(typeMgr.CorTypes.Byte))
							sb.Append("uint8_t");
						else if (corTypeSig.Equals(typeMgr.CorTypes.Char))
							sb.Append("uint16_t");
						else if (corTypeSig.Equals(typeMgr.CorTypes.IntPtr))
							sb.Append("intptr_t");
						else if (corTypeSig.Equals(typeMgr.CorTypes.UIntPtr))
							sb.Append("uintptr_t");
						else
							Debug.Fail("SigToCppName CorLibTypeSig " + corTypeSig);

						return;
					}

					Debug.Fail("SigToCppName TypeSig " + sig.FullName);
					return;
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
				metX.CppName_ = ToCppName(metX.FullFuncName);

			return prefix + metX.CppName_;
		}

		public static string GetCppName(this FieldX fldX)
		{
			if (fldX.CppName_ == null)
				fldX.CppName_ = "fld_" + ToCppName(fldX.Def.Name);
			return fldX.CppName_;
		}

		private static string ToCppName(string fullName)
		{
			StringBuilder sb = new StringBuilder();

			string hash = ToRadix(NameCounter++, (uint)DigMap.Length);
			sb.Append(hash + "_");

			for (int i = 0; i < fullName.Length; ++i)
			{
				if (IsLegalIdentChar(fullName[i]))
					sb.Append(fullName[i]);
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

		private static string DigMap = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
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
