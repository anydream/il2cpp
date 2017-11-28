using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
				Reset();

			return new GeneratorContext(TypeMgr).Generate();
		}

		public static void SaveToFolder(string folder, List<CompileUnit> units, HashSet<string> addUnitNames, string addParams = null)
		{
			Directory.CreateDirectory(folder);
			HashSet<string> unitNames = new HashSet<string>(addUnitNames);
			foreach (var unit in units)
			{
				if (!string.IsNullOrEmpty(unit.DeclCode))
				{
					string path = Path.Combine(folder, unit.Name + ".h");
					File.WriteAllBytes(path, Encoding.UTF8.GetBytes(unit.DeclCode));
				}

				if (!string.IsNullOrEmpty(unit.ImplCode))
				{
					unitNames.Add(unit.Name);
					string path = Path.Combine(folder, unit.Name + ".cpp");
					File.WriteAllBytes(path, Encoding.UTF8.GetBytes(unit.ImplCode));
				}
			}

			// 生成编译脚本
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("@echo off");
			sb.AppendFormat("BuildTheCode {0} il2cpp.cpp il2cppICall.cpp", addParams);
			foreach (string unitName in unitNames)
				sb.AppendFormat(" {0}.cpp", unitName);
			sb.AppendLine();

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
