using System.Collections.Generic;
using dnlib.DotNet;

namespace il2cpp2
{
	class GenericArgs
	{
		private IList<TypeSig> GenArgs_;
		public IList<TypeSig> GenArgs => GenArgs_;

		public void SetGenericArgs(IList<TypeSig> genArgs)
		{
			GenArgs_ = genArgs;
		}

		public int GenericHashCode()
		{
			if (GenArgs_ == null)
				return 0;

			var comparer = new SigComparer();
			int code = GenArgs_.Count;
			foreach (var sig in GenArgs_)
				code ^= comparer.GetHashCode(sig);
			return code;
		}

		public bool GenericEquals(GenericArgs other)
		{
			if (GenArgs_ == null && other.GenArgs_ == null)
				return true;
			if (GenArgs_ == null || other.GenArgs_ == null)
				return false;
			if (GenArgs_.Count != other.GenArgs_.Count)
				return false;

			var comparer = new SigComparer();
			for (int i = 0; i < GenArgs_.Count; ++i)
			{
				if (!comparer.Equals(GenArgs_[i], other.GenArgs_[i]))
					return false;
			}
			return true;
		}
	}

	class TypeX : GenericArgs
	{
		public readonly TypeDef Def;

		public TypeX BaseType;
		public IList<TypeX> Interfaces;

		public TypeX(TypeDef typeDef)
		{
			Def = typeDef;
		}

		public override int GetHashCode()
		{
			return Def.Name.GetHashCode() ^
				   GenericHashCode() ^
				   Def.Module.RuntimeVersion.GetHashCode();
		}

		public bool Equals(TypeX other)
		{
			return TypeEqualityComparer.Instance.Equals(Def, other.Def) &&
				   GenericEquals(other) &&
				   Def.Module.RuntimeVersion == other.Def.Module.RuntimeVersion;
		}

		public override bool Equals(object obj)
		{
			return obj is TypeX other && Equals(other);
		}
	}

	class MethodX : GenericArgs
	{
		public readonly MethodDef Def;

		// 所属类型
		public readonly TypeX DeclType;
		// 展开后的返回值
		public TypeSig ReturnType;
		// 展开后的参数列表
		public IList<TypeSig> ParamTypes;

		public MethodX(MethodDef metDef, TypeX declType)
		{
			Def = metDef;
			DeclType = declType;
		}
	}

	class TypeManager
	{
		private ModuleDefMD Module_;

		public void Reset()
		{
		}

		public void Load(string path)
		{
			Reset();

			Module_ = ModuleDefMD.Load(path);

			AssemblyResolver asmRes = new AssemblyResolver();
			ModuleContext modCtx = new ModuleContext(asmRes);
			asmRes.DefaultModuleContext = modCtx;
			asmRes.EnableTypeDefCache = true;

			Module_.Context = modCtx;
			Module_.Context.AssemblyResolver.AddToCache(Module_);
		}

		public void AnalyzeMethod(MethodDef metDef)
		{

		}
	}
}
