using System;
using System.Diagnostics;
using dnlib.DotNet;

namespace il2cpp
{
	// 字段包装
	public class FieldX : IEquatable<FieldX>
	{
		// 定义
		public readonly FieldDef Def;
		// 所在类型
		public readonly TypeX DeclType;

		// 字段类型
		public TypeX FieldType { get; set; }

		public FieldX(FieldDef fldDef, TypeX declType)
		{
			Def = fldDef;
			DeclType = declType;
		}

		public override int GetHashCode()
		{
			return Def.GetHashNoDecl() ^
				   DeclType.GetHashCode();
		}

		public bool Equals(FieldX other)
		{
			Debug.Assert(other != null);

			if (ReferenceEquals(this, other))
				return true;

			return Def.EqualsNoDecl(other.Def) &&
				   DeclType.Equals(other.DeclType);
		}

		public override bool Equals(object obj)
		{
			return obj is FieldX other && Equals(other);
		}

		public override string ToString()
		{
			return $"{FieldType?.ToString() ?? "<?>"} {Def.Name}";
		}

		public string PrettyName()
		{
			return $"{FieldType?.PrettyName() ?? "<?>"} {Def.Name}";
		}
	}
}
