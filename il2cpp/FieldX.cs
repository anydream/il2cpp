using System.Diagnostics;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	internal class FieldX
	{
		// 所属类型
		public readonly TypeX DeclType;

		// 字段定义
		public readonly FieldDef Def;

		// 唯一名称
		private string NameKey;

		// 字段类型
		public TypeSig FieldType;
		public TypeX FieldTypeX;

		public bool IsStatic => Def.IsStatic;

		// 生成的字段名称
		public string GeneratedFieldName;

		public FieldX(TypeX declType, FieldDef fldDef)
		{
			Debug.Assert(declType != null);
			Debug.Assert(fldDef.DeclaringType == declType.Def);
			DeclType = declType;
			Def = fldDef;
		}

		public override string ToString()
		{
			return DeclType + " -> " + NameKey;
		}

		public string GetNameKey()
		{
			if (NameKey == null)
			{
				// Name|FieldType
				StringBuilder sb = new StringBuilder();
				Helper.FieldNameKey(sb, Def.Name, Def.FieldType);

				NameKey = sb.ToString();
			}
			return NameKey;
		}

		public string GetReplacedNameKey()
		{
			Debug.Assert(FieldType != null);

			StringBuilder sb = new StringBuilder();
			Helper.FieldNameKey(sb, Def.Name, FieldType);

			return sb.ToString();
		}
	}
}
