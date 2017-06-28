using System.Collections.Generic;
using dnlib.DotNet;

namespace il2cpp2
{
	// 类型实例的泛型参数
	internal class GenericArgs
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

	// 展开的类型
	internal class TypeX : GenericArgs
	{
		// 类型定义
		public readonly TypeDef Def;

		// 基类
		public TypeX BaseType;
		// 实现的接口
		private IList<TypeX> Interfaces_;
		public IList<TypeX> Interfaces => Interfaces_ ?? (Interfaces_ = new List<TypeX>());
		public bool HasInterfaces => Interfaces_ != null && Interfaces_.Count > 0;
		// 包含的方法
		public readonly Dictionary<MethodX, MethodX> Methods = new Dictionary<MethodX, MethodX>();

		public TypeX(TypeDef typeDef, IList<TypeSig> genArgs)
		{
			Def = typeDef;
			SetGenericArgs(genArgs);
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

		public MethodX TryAddMethod(MethodX metX, out bool isNew)
		{
			if (!Methods.TryGetValue(metX, out var ometX))
			{
				Methods.Add(metX, metX);
				isNew = true;
				return metX;
			}
			isNew = false;
			return ometX;
		}
	}

	// 展开的方法
	internal class MethodX : GenericArgs
	{
		// 方法定义
		public readonly MethodDef Def;

		// 所属类型
		public readonly TypeX DeclType;
		// 展开后的返回值
		public TypeSig ReturnType;
		// 展开后的参数列表
		public IList<TypeSig> ParamTypes;

		public MethodX(MethodDef metDef, TypeX declType, IList<TypeSig> genArgs)
		{
			Def = metDef;
			DeclType = declType;
			SetGenericArgs(genArgs);
		}

		public override int GetHashCode()
		{
			return Def.Name.GetHashCode() ^
				   GenericHashCode();
		}

		public bool Equals(MethodX other)
		{
			return MethodEqualityComparer.DontCompareDeclaringTypes.Equals(Def, other.Def) &&
				   GenericEquals(other);
		}

		public override bool Equals(object obj)
		{
			return obj is MethodX other && Equals(other);
		}
	}

	internal class TypeManager
	{
		private ModuleDefMD Module_;
		public readonly Dictionary<TypeX, TypeX> Types = new Dictionary<TypeX, TypeX>();

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

		public TypeX TryAddType(TypeX tyX, out bool isNew)
		{
			if (!Types.TryGetValue(tyX, out var otyX))
			{
				Types.Add(tyX, tyX);
				isNew = true;
				return tyX;
			}
			isNew = false;
			return otyX;
		}

		public void AnalyzeMethod(MethodDef metDef)
		{

		}
	}
}
