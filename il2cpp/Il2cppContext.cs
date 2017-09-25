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

		public GenerateResult Generate()
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
				string path;
				if (!string.IsNullOrEmpty(unit.DeclCode))
				{
					path = Path.Combine(folder, unit.Name + ".h");
					File.WriteAllBytes(path, Encoding.UTF8.GetBytes(unit.DeclCode));
				}

				path = Path.Combine(folder, unit.Name + ".cpp");
				File.WriteAllBytes(path, Encoding.UTF8.GetBytes(unit.ImplCode));
			}

			// 生成编译脚本
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("@echo off");

			sb.AppendLine("echo Stage 1: Compiling GC");
			sb.AppendLine("clang -O3 -S -emit-llvm -D_CRT_SECURE_NO_WARNINGS -DDONT_USE_USER32_DLL -Ibdwgc/include bdwgc/extra/gc.c");

			sb.AppendLine("echo Stage 2: Compiling GC Helpers");
			sb.AppendLine("clang -O3 -S -emit-llvm -Ibdwgc/include il2cppGC.cpp");

			sb.AppendLine("echo Stage 3: Compiling Generated Codes");
			sb.Append("clang -O3 -S -emit-llvm -DIL2CPP_LLVM il2cpp.cpp");
			foreach (var unit in units)
				sb.AppendFormat(" {0}.cpp", unit.Name);
			sb.AppendLine();

			sb.AppendLine("echo Stage 4: Linking Codes");
			sb.Append("llvm-link -S -o link.ll il2cpp.ll");
			foreach (var unit in units)
				sb.AppendFormat(" {0}.ll", unit.Name);
			sb.AppendLine();

			sb.AppendLine("echo Stage 5: Optimization Pass 1");
			sb.AppendLine("clang -O3 -S -emit-llvm -o opt.ll link.ll");
			sb.AppendLine("echo Stage 5: Optimization Pass 2");
			sb.AppendLine("clang -O3 -S -emit-llvm -o opt2.ll opt.ll");
			sb.AppendLine("echo Stage 5: Optimization Pass 3");
			sb.AppendLine("clang -O3 -S -emit-llvm -o opt3.ll opt2.ll");
			sb.AppendLine("echo Stage 5: Optimization Pass 4");
			sb.AppendLine("clang -O3 -S -emit-llvm -o opt4.ll opt3.ll");
			sb.AppendLine("IRPatcher opt4.ll");

			sb.AppendLine("echo Stage 6: Linking GC");
			sb.AppendLine("llvm-link -S -o linkgc.ll opt4.ll il2cppGC.ll gc.ll");

			sb.AppendLine("echo Stage 7: Final Optimization");
			sb.AppendLine("clang -O3 -S -emit-llvm -o optgc.ll linkgc.ll");
			sb.AppendLine("clang -O3 -S -emit-llvm -o optgc2.ll optgc.ll");

			sb.AppendLine("echo Stage 8: Generating Executable File");
			sb.AppendLine("clang -O3 -o final.exe optgc2.ll");

			sb.AppendLine("echo Completed!");
			sb.AppendLine("pause");

			File.WriteAllText(Path.Combine(folder, "build.cmd"), sb.ToString());

			// 释放运行时代码
			string strRuntimePack = "il2cpp.runtime.zip";
			var zip = new FastZip();
			zip.ExtractZip(Assembly.GetExecutingAssembly().GetManifestResourceStream(strRuntimePack),
				folder,
				FastZip.Overwrite.Always,
				null, null, null, false, true);
		}
	}
}
