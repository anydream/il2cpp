using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;
using ICSharpCode.SharpZipLib.Zip;

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

			// 生成编译脚本
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("@echo off");

			sb.Append("clang -O3 -S -emit-llvm -DIL2CPP_LLVM main.cpp il2cpp.cpp");
			foreach (var unit in units)
				sb.AppendFormat(" {0}.cpp", unit.Name);
			sb.AppendLine();

			sb.Append("llvm-link -S -o link.ll main.ll il2cpp.ll");
			foreach (var unit in units)
				sb.AppendFormat(" {0}.ll", unit.Name);
			sb.AppendLine();

			sb.AppendLine("opt -O3 -S -o opt.ll link.ll");
			sb.AppendLine("clang -O3 -o final.exe opt.ll");
			sb.AppendLine("pause");
			File.WriteAllText(Path.Combine(folder, "build.cmd"), sb.ToString());

			// 释放运行时代码
			string strRuntimePack = "il2cpp.runtime.runtime.zip";
			var zip = new FastZip();
			zip.ExtractZip(Assembly.GetExecutingAssembly().GetManifestResourceStream(strRuntimePack),
				folder,
				FastZip.Overwrite.Always,
				null, null, null, false, true);
		}
	}
}
