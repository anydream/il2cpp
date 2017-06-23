using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
		public bool HasGenArgs => GenArgs_ != null && GenArgs_.Count > 0;

		public int GenericHashCode()
		{
		    return GenArgs?.Aggregate(GenArgs.Count, (current, type) => current ^ type.GetHashCode()) ?? 0;
		}

		public bool GenericEquals(GenericArgs other)
		{
			if (GenArgs == null && other.GenArgs == null)
				return true;
			if (GenArgs == null || other.GenArgs == null)
				return false;

			if (GenArgs.Count != other.GenArgs.Count)
				return false;

		    return !GenArgs.Where((t, i) => !t.Equals(other.GenArgs[i])).Any();
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
