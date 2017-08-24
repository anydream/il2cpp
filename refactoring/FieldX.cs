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
		public readonly TypeSig DefFieldType;
		// 字段属性
		public readonly FieldAttributes DefAttr;

		// 唯一名称
		private string NameKey;

		public TypeSig FieldType;

		public FieldX(TypeX declType, FieldDef fldDef)
		{
			Debug.Assert(declType != null);
			Debug.Assert(fldDef.DeclaringType == declType.Def);
			DeclType = declType;
			DefName = fldDef.Name;
			DefFieldType = fldDef.FieldType;
			DefAttr = fldDef.Attributes;
		}

		public override string ToString()
		{
			return DeclType + " -> " + NameKey;
		}

		public string GetNameKey()
		{
			if (NameKey == null)
			{
				// Name|FieldType|Attr
				StringBuilder sb = new StringBuilder();
				Helper.FieldNameKey(sb, DefName, DefFieldType);
				sb.Append('|');
				sb.Append(((uint)DefAttr).ToString("X"));

				NameKey = sb.ToString();
			}
			return NameKey;
		}

		public string GetReplacedNameKey()
		{
			Debug.Assert(FieldType != null);

			StringBuilder sb = new StringBuilder();
			Helper.FieldNameKey(sb, DefName, FieldType);

			return sb.ToString();
		}
	}
}
