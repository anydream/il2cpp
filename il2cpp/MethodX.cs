using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	internal class MethodX : GenericArgs
	{
		// 所属类型
		public readonly TypeX DeclType;

		// 方法定义
		public readonly MethodDef Def;
		// 方法签名
		public MethodSig DefSig => Def.MethodSig;

		// 唯一名称
		private string NameKey;

		// 返回值类型
		public TypeSig ReturnType;
		// 参数类型列表, 包含 this 类型
		public IList<TypeSig> ParamTypes;
		public IList<TypeSig> ParamAfterSentinel;
		// 局部变量类型列表
		public IList<TypeSig> LocalTypes;
		//! 异常处理器列表
		// 指令列表
		public InstInfo[] InstList;

		// 虚方法绑定的实现方法
		public HashSet<MethodX> OverrideImpls;
		public bool HasOverrideImpls => OverrideImpls.IsCollectionValid();

		public bool HasThis => DefSig.HasThis;
		public bool IsStatic => !HasThis;
		public bool IsVirtual => Def.IsVirtual;

		// 是否已处理过
		public bool IsProcessed;
		// 是否跳过处理
		public bool IsSkipProcessing;

		// 生成的方法名称
		public string GeneratedMethodName;

		public MethodX(TypeX declType, MethodDef metDef)
		{
			Debug.Assert(declType != null);
			Debug.Assert(metDef.DeclaringType == declType.Def);
			DeclType = declType;
			Def = metDef;

			Debug.Assert(HasThis && !metDef.IsStatic || !HasThis && metDef.IsStatic);

			if (metDef.HasBody)
			{
				if (metDef.Body.HasVariables)
				{
					LocalTypes = new List<TypeSig>();
					foreach (var loc in metDef.Body.Variables)
					{
						Debug.Assert(loc.Index == LocalTypes.Count);
						LocalTypes.Add(loc.Type);
					}
				}
			}
		}

		public string GetNameKey()
		{
			if (NameKey == null)
			{
				// Name|RetType<GenArgs>(DefArgList)|CC
				StringBuilder sb = new StringBuilder();
				Helper.MethodNameKeyWithGen(sb, Def.Name, GenArgs, DefSig.RetType, DefSig.Params, DefSig.CallingConvention);

				NameKey = sb.ToString();
			}
			return NameKey;
		}

		public string GetReplacedNameKey()
		{
			Debug.Assert(ReturnType != null);
			Debug.Assert(ParamTypes != null);

			StringBuilder sb = new StringBuilder();
			Helper.MethodNameKeyWithGen(sb, Def.Name, GenArgs, ReturnType, ParamTypes, DefSig.CallingConvention);

			return sb.ToString();
		}

		public void AddOverrideImpl(MethodX impl)
		{
			if (OverrideImpls == null)
				OverrideImpls = new HashSet<MethodX>();
			OverrideImpls.Add(impl);
		}
	}
}
