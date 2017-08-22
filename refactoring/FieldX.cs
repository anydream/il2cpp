using System.Diagnostics;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	internal class FieldX
	{
		// 所属类型
		public readonly TypeX DeclType;

		// 字段名
		public readonly string DefName;
		// 字段签名
		public readonly TypeSig DefSig;
		// 字段属性
		public readonly FieldAttributes DefAttr;

		// 唯一名称
		private string NameKey;

		public TypeSig FieldType;

		internal FieldX(TypeX declType, FieldDef fldDef)
		{
			DeclType = declType;
			DefName = fldDef.Name;
			DefSig = fldDef.FieldType;
			DefAttr = fldDef.Attributes;
		}

		public string GetNameKey()
		{
			if (NameKey == null)
			{
				Debug.Assert(FieldType != null);

				// Name|FieldType|Attr
				StringBuilder sb = new StringBuilder();
				sb.Append(NameManager.EscapeName(DefName));
				sb.Append('|');
				NameManager.TypeSigName(sb, FieldType);
				sb.Append('|');
				sb.Append(((uint)DefAttr).ToString("X"));

				NameKey = sb.ToString();
			}
			return NameKey;
		}
	}
}
