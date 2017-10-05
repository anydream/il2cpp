using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BuildTheCode
{
	internal static class Helper
	{
		public static byte[] GetFileHash(string file)
		{
			try
			{
				using (var md5 = MD5.Create())
				{
					using (var stream = File.OpenRead(file))
					{
						return md5.ComputeHash(stream);
					}
				}
			}
			catch (IOException)
			{
				return null;
			}
		}

		public static DateTime? GetModifyTime(string path)
		{
			if (!File.Exists(path))
			{
				return null;
			}
			return File.GetLastWriteTimeUtc(path);
		}

		public static bool GetDependFiles(string dfile, out List<string> depFiles)
		{
			try
			{
				string dcontent = File.ReadAllText(dfile);
				depFiles = ParseDependFiles(dcontent);
				return true;
			}
			catch (IOException)
			{
				depFiles = null;
				return false;
			}
		}

		public static bool PatchTextFile(string file, string src, string dst)
		{
			try
			{
				string content = File.ReadAllText(file, Encoding.UTF8);
				if (content.IndexOf(src, StringComparison.Ordinal) == -1)
					return true;

				content = content.Replace(src, dst);
				var buf = Encoding.UTF8.GetBytes(content);
				File.WriteAllBytes(file, buf);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static List<string> ParseDependFiles(string input)
		{
			input = input.Replace("\r", null);

			List<string> output = new List<string>();
			StringBuilder sb = new StringBuilder();

			void AddOutput(string s)
			{
				if (s.Length > 0)
					output.Add(s);
			}

			for (int i = 0, sz = input.Length; i < sz; ++i)
			{
				char ch = input[i];
				if (ch == '\\' && i + 1 < sz)
				{
					char chNext = input[i + 1];
					if (chNext == '\\' ||
						chNext == ' ')
					{
						sb.Append(chNext);
						++i;
						continue;
					}
					else if (chNext == '\n')
					{
						AddOutput(sb.ToString());
						sb.Clear();
						++i;
						continue;
					}
				}
				else if (ch == '\n' || ch == ' ')
				{
					AddOutput(sb.ToString());
					sb.Clear();
					continue;
				}
				sb.Append(ch);
			}

			if (sb.Length > 0)
				AddOutput(sb.ToString());

			output.RemoveRange(0, 2);
			return output;
		}

		public static void RunCommand(
			string program,
			string arguments,
			string workDir,
			Action<string> onOutput,
			Action<string> onError,
			bool isWait = true)
		{
			if (program == null)
			{
				program = "cmd";
				arguments = "/c " + arguments;
			}

			Process pSpawn = new Process
			{
				StartInfo =
				{
					WorkingDirectory = workDir,
					FileName = program,
					Arguments = arguments,
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					UseShellExecute = false
				}
			};

			if (onOutput != null)
			{
				pSpawn.OutputDataReceived += (sender, args) =>
				{
					if (args.Data != null)
						onOutput(args.Data);
				};
			}
			if (onError != null)
			{
				pSpawn.ErrorDataReceived += (sender, args) =>
				{
					if (args.Data != null)
						onError(args.Data);
				};
			}

			pSpawn.Start();

			if (onOutput != null)
				pSpawn.BeginOutputReadLine();

			if (onError != null)
				pSpawn.BeginErrorReadLine();

			if (isWait)
				pSpawn.WaitForExit();
		}
	}

	internal class ActionUnit
	{
		public readonly string Command;
		public readonly string WorkDir;
		public readonly string OutDir;
		public readonly string TargetFile;
		public readonly HashSet<string> DependFiles = new HashSet<string>();
		public Action<string> OnOutput;
		public Action<string> OnError;

		public enum Status
		{
			Skipped,
			Completed,
			Error
		}

		public Status UnitStatus { get; protected set; } = Status.Skipped;

		protected DateTime? TargetModifyTime;

		public ActionUnit(
			string cmd,
			string workDir, string outDir,
			string target, HashSet<string> depFiles)
		{
			Command = cmd;
			WorkDir = Path.GetFullPath(workDir);
			Directory.SetCurrentDirectory(WorkDir);

			OutDir = Path.GetFullPath(outDir);
			Directory.CreateDirectory(outDir);

			TargetFile = Path.GetFullPath(target);
			foreach (string dep in depFiles)
				DependFiles.Add(Path.GetFullPath(dep));
		}

		public Status Invoke()
		{
			TargetModifyTime = Helper.GetModifyTime(TargetFile);
			ExpandDepends();

			if (!IsNeedDoAction())
				return UnitStatus = Status.Skipped;

			if (DoAction())
				return UnitStatus = Status.Completed;

			return UnitStatus = Status.Error;
		}

		public void CompletedUpdate()
		{
			UpdateCommand();
			foreach (string dep in DependFiles)
				UpdateHash(dep);
		}

		protected virtual bool DoAction()
		{
			bool isError = false;
			Helper.RunCommand(
				null,
				Command,
				WorkDir,
				OnOutput,
				strErr =>
				{
					isError = true;
					OnError(strErr);
				});

			return !isError;
		}

		protected virtual bool IsNeedDoAction()
		{
			if (IsCommandChanged())
				return true;

			foreach (string dep in DependFiles)
			{
				if (IsFileChanged(dep))
					return true;
			}

			return false;
		}

		protected virtual bool IsCommandChanged()
		{
			string savedCmd = ReadSavedCommand();
			if (savedCmd == null)
				return true;

			if (savedCmd != Command)
				return true;

			return false;
		}

		protected virtual string ReadSavedCommand()
		{
			string cmdFile = GetCommandFilePath();

			try
			{
				return Encoding.UTF8.GetString(File.ReadAllBytes(cmdFile));
			}
			catch (IOException)
			{
				return null;
			}
		}

		protected virtual void UpdateCommand()
		{
			string cmdFile = GetCommandFilePath();

			try
			{
				File.WriteAllBytes(cmdFile, Encoding.UTF8.GetBytes(Command));
			}
			catch (IOException)
			{
			}
		}

		protected virtual string GetCommandFilePath()
		{
			string fname = Path.GetFileName(TargetFile);
			return Path.Combine(OutDir, fname + ".comd");
		}

		protected virtual bool IsFileChanged(string path)
		{
			if (TargetModifyTime == null)
				return true;

			var mt = Helper.GetModifyTime(path);
			if (mt == null)
				return true;

			if (mt > TargetModifyTime && IsHashChanged(path))
				return true;

			return false;
		}

		protected virtual bool IsHashChanged(string path)
		{
			byte[] savedHash = ReadSavedHash(path);
			if (savedHash == null)
				return true;

			byte[] calcHash = Helper.GetFileHash(path);
			if (calcHash == null)
				return true;

			if (!savedHash.SequenceEqual(calcHash))
				return true;

			return false;
		}

		protected virtual byte[] ReadSavedHash(string path)
		{
			string hashFile = GetHashFilePath(path);

			try
			{
				return File.ReadAllBytes(hashFile);
			}
			catch (IOException)
			{
				return null;
			}
		}

		protected virtual void UpdateHash(string path)
		{
			byte[] hashBytes = Helper.GetFileHash(path);
			string hashFile = GetHashFilePath(path);

			try
			{
				File.WriteAllBytes(hashFile, hashBytes);
			}
			catch (IOException)
			{
			}
		}

		protected virtual string GetHashFilePath(string path)
		{
			if (path.StartsWith(WorkDir))
			{
				string fname = Path.GetFileName(path);
				return Path.Combine(OutDir, fname + ".hash");
			}
			else
			{
				string fname = Path.GetFileName(path);
				return Path.Combine(OutDir, fname + "_" + path.GetHashCode().ToString("X") + ".dhash");
			}
		}

		protected virtual void ExpandDepends()
		{
		}
	}

	internal class CompileUnit : ActionUnit
	{
		public CompileUnit(
			string cmd,
			string workDir, string outDir,
			string target, HashSet<string> depFiles)
			: base(cmd, workDir, outDir, target, depFiles)
		{
		}

		protected override bool DoAction()
		{
			bool isError = false;
			Helper.RunCommand(
				null,
				Command,
				WorkDir,
				OnOutput,
				strErr =>
				{
					if (!isError && strErr.IndexOf("error") != -1)
						isError = true;

					if (isError)
						OnError(strErr);
					else
						OnOutput(strErr);
				});

			return !isError;
		}

		protected override void ExpandDepends()
		{
			HashSet<string> depSet = new HashSet<string>();
			foreach (string dep in DependFiles)
			{
				string dfile = Path.Combine(OutDir, Path.GetFileName(dep) + ".d");

				if (Helper.GetDependFiles(dfile, out var depFiles))
					depSet.UnionWith(depFiles);
			}
			foreach (string dep in depSet)
				DependFiles.Add(Path.GetFullPath(dep));
		}
	}

	internal class Maker
	{
		public string OptLevel = "-O3";
		public int GenOptCount = 6;
		public int FinalOptCount = 2;

		public readonly string WorkDir;
		public readonly string OutDir;

		public Maker(string workDir, string outDir)
		{
			WorkDir = workDir;
			OutDir = outDir;
		}

		public void Invoke(HashSet<string> srcFiles)
		{
			Dictionary<string, CompileUnit> unitMap = new Dictionary<string, CompileUnit>();
			HashSet<string> objSet = new HashSet<string>();

			// 编译生成的文件
			foreach (string srcFile in srcFiles)
			{
				AddCompileUnit(unitMap, objSet, srcFile,
					"-Wall -Xclang -flto-visibility-public-std -D_CRT_SECURE_NO_WARNINGS -DIL2CPP_PATCH_LLVM");
			}
			if (!ParallelCompile(unitMap))
				return;

			// 链接
			string linkedFile = Linking("!linked.bc", objSet);
			if (linkedFile == null)
				return;

			unitMap.Clear();
			objSet.Clear();

			// 优化
			string optedFile = Optimizing(linkedFile, null, GenOptCount, true);
			if (optedFile == null)
				return;

			// 替换实现
			if (!Helper.PatchTextFile(optedFile, "@calloc", "@_il2cpp_GC_PatchCalloc"))
				return;

			// 编译 GC
			AddCompileUnit(unitMap, objSet,
				"bdwgc/extra/gc.c",
				"-D_CRT_SECURE_NO_WARNINGS -DDONT_USE_USER32_DLL -DNO_GETENV -DGC_NOT_DLL -Ibdwgc/include");
			AddCompileUnit(unitMap, objSet,
				"il2cppGC.cpp",
				"-Wall -Xclang -flto-visibility-public-std -D_CRT_SECURE_NO_WARNINGS -DIL2CPP_PATCH_LLVM -DDONT_USE_USER32_DLL -DNO_GETENV -DGC_NOT_DLL -Ibdwgc/include");
			if (!ParallelCompile(unitMap))
				return;

			// 链接 GC
			objSet.Add(optedFile);
			linkedFile = Linking("!linked_gc.bc", objSet);
			if (linkedFile == null)
				return;

			unitMap.Clear();
			objSet.Clear();

			// 最终优化
			optedFile = Optimizing(linkedFile, "gc_", FinalOptCount, false);
			if (optedFile == null)
				return;

			// 生成目标文件
			string finalObj = "final.o";
			CompileUnit finalCompile = new CompileUnit(
				string.Format("clang {0} -o {1} {2} -c",
					optedFile,
					finalObj,
					OptLevel),
				WorkDir,
				OutDir,
				finalObj,
				new HashSet<string> { optedFile });

			finalCompile.OnOutput = Console.WriteLine;
			finalCompile.OnError = Console.Error.WriteLine;

			var status = finalCompile.Invoke();
			if (status == ActionUnit.Status.Completed)
			{
				finalCompile.CompletedUpdate();
				Console.WriteLine("Object file generated.");
			}
			else if (status == ActionUnit.Status.Error)
				return;

			// 生成可执行文件
			string finalExe = "final.exe";
			finalCompile = new CompileUnit(
			   string.Format("clang {0} -o {1} {2}",
				   finalObj,
				   finalExe,
				   OptLevel),
			   WorkDir,
			   OutDir,
			   finalExe,
			   new HashSet<string> { finalObj });

			finalCompile.OnOutput = Console.WriteLine;
			finalCompile.OnError = Console.Error.WriteLine;

			status = finalCompile.Invoke();
			if (status == ActionUnit.Status.Completed)
			{
				finalCompile.CompletedUpdate();
				Console.WriteLine("Executable generated.");
			}
			else if (status == ActionUnit.Status.Error)
				return;

			Console.WriteLine("Compile finished.");
		}

		private void AddCompileUnit(
			Dictionary<string, CompileUnit> unitMap,
			HashSet<string> objSet,
			string srcFile,
			string cflags)
		{
			string outFile = Path.Combine(OutDir, EscapePath(srcFile) + ".bc");
			objSet.Add(outFile);

			CompileUnit compUnit = new CompileUnit(
				string.Format("clang {0} -o {1} {2} -MD -c -emit-llvm " + cflags,
					srcFile,
					outFile,
					OptLevel),
				WorkDir,
				OutDir,
				outFile,
				new HashSet<string> { srcFile });

			compUnit.OnOutput = Console.WriteLine;
			compUnit.OnError = Console.Error.WriteLine;

			unitMap.Add(srcFile, compUnit);
		}

		private bool ParallelCompile(Dictionary<string, CompileUnit> unitMap)
		{
			Parallel.ForEach(unitMap, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
				kv =>
				{
					var status = kv.Value.Invoke();
					switch (status)
					{
						case ActionUnit.Status.Skipped:
							Console.WriteLine("Skipped: {0}", kv.Key);
							break;
						case ActionUnit.Status.Completed:
							Console.WriteLine("Compiled: {0}", kv.Key);
							break;
						case ActionUnit.Status.Error:
							Console.Error.WriteLine("Error: {0}", kv.Key);
							break;
					}
				});

			foreach (var unit in unitMap.Values)
			{
				if (unit.UnitStatus == ActionUnit.Status.Completed)
					unit.CompletedUpdate();
			}

			foreach (var unit in unitMap.Values)
			{
				if (unit.UnitStatus == ActionUnit.Status.Error)
					return false;
			}

			return true;
		}

		private string Linking(string outName, HashSet<string> objSet)
		{
			string outFile = Path.Combine(OutDir, outName);
			ActionUnit linkUnit = new ActionUnit(
				string.Format("llvm-link -o {0} {1}",
					outFile,
					string.Join(" ", objSet)),
				WorkDir,
				OutDir,
				outFile,
				objSet);

			linkUnit.OnOutput = Console.WriteLine;
			linkUnit.OnError = Console.Error.WriteLine;

			var linkStatus = linkUnit.Invoke();
			switch (linkStatus)
			{
				case ActionUnit.Status.Skipped:
					Console.WriteLine("LinkSkipped: {0}", outFile);
					break;
				case ActionUnit.Status.Completed:
					linkUnit.CompletedUpdate();
					Console.WriteLine("LinkCompleted: {0}", outFile);
					break;
				case ActionUnit.Status.Error:
					Console.Error.WriteLine("LinkError: {0}", outFile);
					return null;
			}

			return outFile;
		}

		private string Optimizing(string lastOptFile, string outNamePostfix, int optCount, bool isLastText)
		{
			for (int i = 0; i < optCount; ++i)
			{
				string strExt;
				string strFlag;

				if (!isLastText || i != optCount - 1)
				{
					strExt = ".bc";
					strFlag = "-c";
				}
				else
				{
					strExt = ".ll";
					strFlag = "-S";
				}

				string optFile = Path.Combine(OutDir, "!opt_" + outNamePostfix + i + strExt);

				CompileUnit optUnit = new CompileUnit(
					string.Format("clang {0} -o {1} {2} {3} -emit-llvm",
						lastOptFile,
						optFile,
						OptLevel,
						strFlag),
					WorkDir,
					OutDir,
					optFile,
					new HashSet<string> { lastOptFile });

				optUnit.OnOutput = Console.WriteLine;
				optUnit.OnError = Console.Error.WriteLine;

				var status = optUnit.Invoke();
				switch (status)
				{
					case ActionUnit.Status.Skipped:
						Console.WriteLine("OptSkipped: {0}", lastOptFile);
						break;
					case ActionUnit.Status.Completed:
						optUnit.CompletedUpdate();
						Console.WriteLine("OptCompleted: {0}", lastOptFile);
						break;
					case ActionUnit.Status.Error:
						Console.Error.WriteLine("OptError: {0}", lastOptFile);
						return null;
				}

				lastOptFile = optFile;
			}

			return lastOptFile;
		}

		private static string EscapePath(string path)
		{
			return path.Replace('/', '$')
				.Replace('\\', '$')
				.Replace(':', '#');
		}
	}

	static class Program
	{
		static string OptLevel = "-O3";
		static int GenOptCount = 6;
		static int FinalOptCount = 2;

		static List<string> ParseArgs(string[] args)
		{
			List<string> result = new List<string>();
			for (int i = 0; i < args.Length; ++i)
			{
				string arg = args[i];
				if (arg.StartsWith("-"))
				{
					string cmd = arg.Substring(1);
					if (cmd.Length > 0)
					{
						if (cmd[0] == 'O')
						{
							OptLevel = arg;
							continue;
						}

						string cmdArg = null;
						int eq = cmd.IndexOf('=');
						if (eq != -1)
						{
							cmdArg = cmd.Substring(eq + 1);
							cmd = cmd.Substring(0, eq);
						}
						else if (i + 1 < args.Length)
						{
							cmdArg = args[i + 1];
							++i;
						}

						if (cmd == "optcount")
						{
							if (!string.IsNullOrEmpty(cmdArg))
								int.TryParse(cmdArg, out GenOptCount);
							continue;
						}
						else if (cmd == "foptcount")
						{
							if (!string.IsNullOrEmpty(cmdArg))
								int.TryParse(cmdArg, out FinalOptCount);
							continue;
						}
					}
					Console.Error.WriteLine("Unknown command {0}", arg);
				}
				else
					result.Add(arg);
			}
			return result;
		}

		static void Main(string[] args)
		{
			try
			{
				Helper.RunCommand("clang", "-v", null, null, null);
			}
			catch (System.ComponentModel.Win32Exception)
			{
				Console.Error.Write("Cannot find clang compiler,\nplease download it from ");
				var oldColor = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Error.WriteLine("http://releases.llvm.org/download.html");
				Console.ForegroundColor = oldColor;
				Console.ReadKey();
				return;
			}

			if (args.Length > 0)
			{
				var srcFiles = ParseArgs(args);

				var make = new Maker(".", "output");
				make.Invoke(new HashSet<string>(srcFiles));
			}
			else
			{
				Console.Error.WriteLine("Please run 'build.cmd' to compile");
				Console.ReadKey();
			}
		}
	}
}
