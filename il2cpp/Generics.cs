using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	public interface IGenericResolver
	{
		TypeX ResolveGenericSig(GenericSig genSig);
	}

	internal class MethodGenericResolver : IGenericResolver
	{
		private readonly MethodX Method_;

		public MethodGenericResolver(MethodX metX)
		{
			Method_ = metX;
		}

		public TypeX ResolveGenericSig(GenericSig genSig)
		{
			if (genSig.HasOwnerMethod)
			{
				if (genSig.OwnerMethod.EqualsWithDecl(Method_.Def))
				{
					return Method_.GenArgs[(int)genSig.Number];
				}
			}
			else
			{
				Debug.Assert(genSig.HasOwnerType);
				if (genSig.OwnerType.TypeEquals(Method_.DeclType.Def))
				{
					return Method_.DeclType.GenArgs[(int)genSig.Number];
				}
			}

			Debug.Fail("MethodGenericResolver " + genSig);
			return null;
		}
	}

	internal class TypeGenericResolver : IGenericResolver
	{
		private readonly TypeX Type_;

		public TypeGenericResolver(TypeX tyX)
		{
			Type_ = tyX;
		}

		public TypeX ResolveGenericSig(GenericSig genSig)
		{
			if (genSig.HasOwnerType)
			{
				if (genSig.OwnerType.TypeEquals(Type_.Def))
				{
					return Type_.GenArgs[(int)genSig.Number];
				}
			}

			Debug.Fail("TypeGenericResolver " + genSig);
			return null;
		}
	}

	public class GenericArgs
	{
		// 泛型类型列表
		private List<TypeX> GenArgs_;
		public List<TypeX> GenArgs
		{
			get => GenArgs_;
			set
			{
				Debug.Assert(GenArgs_ == null);
				Debug.Assert(value == null || value.Count > 0);
				GenArgs_ = value;
			}
		}

		public int GenericHashCode()
		{
			if (GenArgs == null)
				return 0;

			int code = GenArgs.Count;
			foreach (TypeX type in GenArgs)
				code ^= type.GetHashCode();

			return code;
		}

		public bool GenericEquals(GenericArgs other)
		{
			if (GenArgs == null && other.GenArgs == null)
				return true;
			if (GenArgs == null || other.GenArgs == null)
				return false;

			if (GenArgs.Count != other.GenArgs.Count)
				return false;

			for (int i = 0; i < GenArgs.Count; ++i)
			{
				if (!GenArgs[i].Equals(other.GenArgs[i]))
					return false;
			}
			return true;
		}

		public string GenericToString(bool isPretty = false)
		{
			if (GenArgs == null)
				return "";

			StringBuilder sb = new StringBuilder();
			sb.Append('<');

			bool isLast = false;
			foreach (TypeX tyX in GenArgs)
			{
				if (isLast)
					sb.Append(',');
				isLast = true;
				sb.Append(isPretty ? tyX.PrettyName() : tyX.ToString());
			}

			sb.Append('>');

			return sb.ToString();
		}
	}
}
