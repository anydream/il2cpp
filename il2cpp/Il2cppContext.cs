using System.Diagnostics;
using dnlib.DotNet;

namespace il2cpp
{
	public class Il2cppContext
	{
		public readonly ModuleDefMD Module;
		public readonly string RuntimeVersion;

		internal readonly TypeManager TypeMgr;
		internal readonly NameManager NameMgr;

		public Il2cppContext(string imagePath)
		{
			// 初始化管理器
			TypeMgr = new TypeManager(this);
			NameMgr = new NameManager(this);

			// 加载主模块
			AssemblyResolver asmRes = new AssemblyResolver();
			ModuleContext modCtx = new ModuleContext(asmRes);
			asmRes.DefaultModuleContext = modCtx;
			asmRes.EnableTypeDefCache = true;
			asmRes.FindExactMatch = false;
			asmRes.EnableFrameworkRedirect = true;

			Module = ModuleDefMD.Load(imagePath);
			Debug.Assert(Module != null);

			Module.Context = modCtx;
			Module.Context.AssemblyResolver.AddToCache(Module);

			RuntimeVersion = Module.RuntimeVersion;
		}

		public void AddEntry(MethodDef metDef)
		{
			TypeMgr.ResolveMethodDef(metDef);
		}

		public void Process()
		{
			TypeMgr.ResolveAll();
		}
	}
}
