using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	public class Il2cppContext
	{
		public readonly ModuleDefMD Module;
		public readonly ICorLibTypes CorLibTypes;
		public readonly ModuleDef CorLibModule;
		public readonly string RuntimeVersion;

		internal TypeManager TypeMgr;

		public Il2cppContext(string imagePath)
		{
			Module = ModuleDefMD.Load(imagePath);
			Debug.Assert(Module != null);

			// 加载主模块
			AssemblyResolver asmRes = new AssemblyResolver();
			ModuleContext modCtx = new ModuleContext(asmRes);
			asmRes.DefaultModuleContext = modCtx;
			asmRes.EnableTypeDefCache = true;
			asmRes.FindExactMatch = false;
			asmRes.EnableFrameworkRedirect = true;
			asmRes.FrameworkRedirectVersion = Module.RuntimeVersion;

			Module.Context = modCtx;
			Module.Context.AssemblyResolver.AddToCache(Module);

			CorLibTypes = Module.CorLibTypes;
			CorLibModule = CorLibTypes.Object.TypeRef.Resolve().Module;
			RuntimeVersion = Module.RuntimeVersion;

			Reset();
		}

		public void Reset()
		{
			// 初始化管理器
			TypeMgr = new TypeManager(this);
		}

		public void AddEntry(MethodDef metDef)
		{
			TypeMgr?.ResolveMethodDef(metDef);
		}

		public void Resolve()
		{
			TypeMgr?.ResolveAll();
		}

		public List<CompileUnit> Generate()
		{
			if (TypeMgr == null)
				return null;

			TypeMgr.ClearForGenerator();
			return new GeneratorContext(TypeMgr).Generate();
		}

		public static void SaveToFolder(string folder, List<CompileUnit> units)
		{
			Directory.CreateDirectory(folder);
			foreach (var unit in units)
			{
				string path = Path.Combine(folder, unit.Name + ".h");
				File.WriteAllBytes(path, Encoding.UTF8.GetBytes(unit.DeclCode));

				path = Path.Combine(folder, unit.Name + ".cpp");
				File.WriteAllBytes(path, Encoding.UTF8.GetBytes(unit.ImplCode));
			}
		}
	}
}
