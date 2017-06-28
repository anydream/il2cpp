using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp2
{
	// 类型实例的泛型参数
	public class GenericArgs
	{
		private IList<TypeSig> GenArgs_;
		public IList<TypeSig> GenArgs => GenArgs_;
		public bool HasGenArgs => GenArgs_ != null && GenArgs_.Count > 0;

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

		public string GenericToString()
		{
			if (GenArgs_ == null)
				return "";

			StringBuilder sb = new StringBuilder();
			sb.Append('<');

			bool last = false;
			foreach (var arg in GenArgs_)
			{
				if (last)
					sb.Append(',');
				last = true;

				sb.Append(arg.FullName);
			}
			sb.Append('>');

			return sb.ToString();
		}
	}

	// 展开的类型
	public class TypeX : GenericArgs
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

		public override string ToString()
		{
			return Def.FullName + GenericToString();
		}

		public bool AddMethod(MethodX metX)
		{
			if (!Methods.ContainsKey(metX))
			{
				Methods.Add(metX, metX);
				return true;
			}
			return false;
		}
	}

	// 展开的方法
	public class MethodX : GenericArgs
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

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("{0} {1}{2}",
				ReturnType != null ? ReturnType.FullName : "<?>",
				Def.Name,
				GenericToString());

			sb.Append('(');

			if (ParamTypes == null)
				sb.Append("<?>");
			else
			{
				bool last = false;
				foreach (var arg in ParamTypes)
				{
					if (last)
						sb.Append(',');
					last = true;
					sb.Append(arg.FullName);
				}
			}
			sb.Append(')');

			return sb.ToString();
		}
	}

	public class TypeManager
	{
		public ModuleDefMD Module { get; private set; }
		private readonly Queue<MethodX> PendingMets = new Queue<MethodX>();
		public readonly Dictionary<TypeX, TypeX> Types = new Dictionary<TypeX, TypeX>();

		public void Reset()
		{
		}

		public void Load(string path)
		{
			Reset();

			Module = ModuleDefMD.Load(path);

			AssemblyResolver asmRes = new AssemblyResolver();
			ModuleContext modCtx = new ModuleContext(asmRes);
			asmRes.DefaultModuleContext = modCtx;
			asmRes.EnableTypeDefCache = true;

			Module.Context = modCtx;
			Module.Context.AssemblyResolver.AddToCache(Module);
		}

		public void Process()
		{
			while (PendingMets.Count > 0)
			{
				MethodX currMetX = PendingMets.Dequeue();

				if (!currMetX.Def.HasBody)
					continue;

				GenericReplacer replacer = new GenericReplacer();
				replacer.SetType(currMetX.DeclType);
				replacer.SetMethod(currMetX);

				foreach (var inst in currMetX.Def.Body.Instructions)
				{
					AnalyzeInstruction(inst, replacer);
				}
			}
		}

		private void AnalyzeInstruction(Instruction inst, GenericReplacer replacer)
		{
			switch (inst.OpCode.OperandType)
			{
				case OperandType.InlineMethod:
					{
						switch (inst.Operand)
						{
							case MethodDef metDef:
								AnalyzeMethod(metDef, replacer);
								break;

							case MemberRef memRef:
								AnalyzeMethod(memRef, replacer);
								break;

							case MethodSpec metSpec:
								AnalyzeMethod(metSpec, replacer);
								break;
						}

						break;
					}

				case OperandType.InlineField:
					{
						break;
					}
			}
		}

		private TypeX AddType(TypeX tyX)
		{
			if (Types.TryGetValue(tyX, out var otyX))
				return otyX;

			Types.Add(tyX, tyX);
			ExpandType(tyX);

			return tyX;
		}

		private void ExpandType(TypeX tyX)
		{
			GenericReplacer replacer = new GenericReplacer();
			replacer.SetType(tyX);

			if (tyX.Def.BaseType != null)
				tyX.BaseType = AnalyzeBaseType(tyX.Def.BaseType, replacer);
			if (tyX.Def.HasInterfaces)
			{
				foreach (var inf in tyX.Def.Interfaces)
					tyX.Interfaces.Add(AnalyzeBaseType(inf.Interface, replacer));
			}
		}

		private TypeX AnalyzeBaseType(ITypeDefOrRef typeDefRef, GenericReplacer replacer = null)
		{
			return AddType(ResolveBaseTypeImpl(typeDefRef, replacer));
		}

		private TypeX ResolveBaseTypeImpl(ITypeDefOrRef typeDefRef, GenericReplacer replacer)
		{
			switch (typeDefRef)
			{
				case TypeDef typeDef:
					return new TypeX(typeDef);

				case TypeRef typeRef:
					return new TypeX(typeRef.ResolveTypeDef());

				case TypeSpec typeSpec:
					return ResolveBaseTypeImpl(typeSpec.TypeSig, replacer);

				default:
					Debug.Fail("ResolveBaseTypeImpl ITypeDefOrRef " + typeDefRef.GetType().Name);
					return null;
			}
		}

		private TypeX ResolveBaseTypeImpl(TypeSig typeSig, GenericReplacer replacer)
		{
			switch (typeSig)
			{
				case TypeDefOrRefSig typeDefRefSig:
					return ResolveBaseTypeImpl(typeDefRefSig.TypeDefOrRef, null);

				case GenericInstSig genInstSig:
					{
						TypeX genType = ResolveBaseTypeImpl(genInstSig.GenericType, null);
						genType.SetGenericArgs(ResolveTypeSigList(genInstSig.GenericArguments, replacer));
						return genType;
					}

				default:
					Debug.Fail("ResolveBaseTypeImpl TypeSig " + typeSig.GetType().Name);
					return null;
			}
		}

		private TypeSig ResolveTypeSig(TypeSig typeSig, GenericReplacer replacer)
		{
			if (replacer == null || !replacer.IsValid)
				return typeSig;

			var duplicator = new TypeSigDuplicator();
			duplicator.GenReplacer = replacer;
			return duplicator.Duplicate(typeSig);
		}

		private IList<TypeSig> ResolveTypeSigList(IList<TypeSig> sigList, GenericReplacer replacer)
		{
			if (replacer == null || !replacer.IsValid)
				return new List<TypeSig>(sigList);

			var duplicator = new TypeSigDuplicator();
			duplicator.GenReplacer = replacer;

			var result = new List<TypeSig>();
			foreach (var typeSig in sigList)
				result.Add(duplicator.Duplicate(typeSig));
			return result;
		}

		private void AddPendingMethod(MethodX metX)
		{
			PendingMets.Enqueue(metX);
		}

		private void ExpandMethod(MethodX metX)
		{
			AddPendingMethod(metX);

			GenericReplacer replacer = new GenericReplacer();
			replacer.SetType(metX.DeclType);
			replacer.SetMethod(metX);

			metX.ReturnType = ResolveTypeSig(metX.Def.ReturnType, replacer);
			metX.ParamTypes = ResolveTypeSigList(metX.Def.Parameters.Select(arg => arg.Type).ToList(), replacer);
		}

		public void AnalyzeMethod(MethodDef metDef, GenericReplacer replacer)
		{
			TypeX declType = AnalyzeBaseType(metDef.DeclaringType);

			MethodX metX = new MethodX(metDef, declType, null);
			if (declType.AddMethod(metX))
				ExpandMethod(metX);
		}

		public void AnalyzeMethod(MemberRef memRef, GenericReplacer replacer)
		{
			Debug.Assert(memRef.IsMethodRef);
			TypeX declType = AnalyzeBaseType(memRef.DeclaringType, replacer);

			MethodX metX = new MethodX(memRef.ResolveMethod(), declType, null);
			if (declType.AddMethod(metX))
				ExpandMethod(metX);
		}

		public void AnalyzeMethod(MethodSpec metSpec, GenericReplacer replacer)
		{
			TypeX declType = AnalyzeBaseType(metSpec.DeclaringType, replacer);

			IList<TypeSig> genArgs = null;
			var metGenArgs = metSpec.GenericInstMethodSig?.GenericArguments;
			if (metGenArgs != null)
				genArgs = ResolveTypeSigList(metGenArgs, replacer);

			MethodX metX = new MethodX(metSpec.ResolveMethodDef(), declType, genArgs);
			if (declType.AddMethod(metX))
				ExpandMethod(metX);
		}
	}
}
