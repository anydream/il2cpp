using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace BuildTheCode
{
	class Make
	{
		public struct CompileUnit
		{
			public string SrcFile;
			public string Arguments;
		}

		public static List<CompileUnit> ToUnits(List<string> srcs, string arguments)
		{
			List<CompileUnit> units = new List<CompileUnit>();
			foreach (string src in srcs)
				units.Add(new CompileUnit { SrcFile = src, Arguments = arguments });

			return units;
		}

		public static bool Compile(
			string unitSrcFile,
			string unitArgs,
			string outputName,
			string srcDir,
			string outDir,
			bool hasDepFile,
			Action<string> onOutput,
			Action<string> onError,
			out string outputFile)
		{
			string srcFile = GetRelativePath(unitSrcFile, srcDir);
			string unitName = EscapePath(srcFile);

			if (outputName == null)
				outputName = unitName + ".bc";

			outputFile = Path.Combine(outDir, outputName);
			string depFile = hasDepFile ? Path.Combine(outDir, unitName + ".d") : null;
			string hashFile = Path.Combine(outDir, unitName + ".hash");

			if (!IsNeedCompile(srcFile, outputFile, depFile, hashFile))
			{
				onOutput(string.Format("Skipped: {0}", srcFile));
				return false;
			}

			string prependArgs = srcFile + " -o " + outputFile + " " + (hasDepFile ? "-MD " : null);
			RunCommand("clang", prependArgs + unitArgs, srcDir, onOutput, onError);

			File.WriteAllBytes(hashFile, GetFileHash(srcFile));

			onOutput(string.Format("Compiled: {0} -> {1}", srcFile, outputFile));

			return true;
		}

		public static List<string> Compile(
			List<CompileUnit> units,
			string srcDir,
			string outDir,
			Action<string> onOutput,
			Action<string> onError)
		{
			Directory.CreateDirectory(outDir);

			List<string> objFiles = new List<string>();

			int compiled = 0;
			Parallel.ForEach(units, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
				unit =>
				{
					bool result = Compile(
						unit.SrcFile,
						 unit.Arguments,
						null,
						srcDir,
						outDir,
						true,
						onOutput,
						onError,
						out var outputFile);

					lock (objFiles)
					{
						objFiles.Add(outputFile);
					}

					if (result)
						Interlocked.Increment(ref compiled);
				});

			onOutput(string.Format("Compiled {0} of {1}", compiled, units.Count));

			return objFiles;
		}

		private static bool IsNeedCompile(string srcFile, string objFile, string depFile, string hashFile)
		{
			if (!GetModifyTime(srcFile, out var srcMT))
				return true;

			if (!GetModifyTime(objFile, out var objMT))
				return true;

			if (srcMT > objMT)
				return IsHashChanged(srcFile, hashFile);

			if (depFile != null)
			{
				try
				{
					string depContent = File.ReadAllText(depFile);
					var depLines = ParseDependsFile(depContent);

					foreach (string hfile in depLines)
					{
						if (!GetModifyTime(hfile, out var hMT) || hMT > objMT)
							return true;
					}
				}
				catch (IOException)
				{
					return true;
				}
			}

			return false;
		}

		public static void Link(
			List<string> objFiles,
			string linkFile,
			string srcDir,
			Action<string> onOutput,
			Action<string> onError)
		{
			if (!IsNeedLink(objFiles, linkFile))
			{
				onOutput(string.Format("Skipped Linking {0}", linkFile));
				return;
			}

			string args = "-o " + linkFile + " " + string.Join(" ", objFiles);

			RunCommand("llvm-link", args, srcDir, onOutput, onError);

			onOutput(string.Format("Linked {0}", linkFile));
		}

		private static bool IsNeedLink(List<string> objFiles, string linkFile)
		{
			if (!GetModifyTime(linkFile, out var linkMT))
				return true;

			foreach (string objFile in objFiles)
			{
				if (!GetModifyTime(objFile, out var objMT) || objMT > linkMT)
					return true;
			}
			return false;
		}

		private static bool IsHashChanged(string srcFile, string hashFile)
		{
			try
			{
				var savedHash = File.ReadAllBytes(hashFile);
				var calcHash = GetFileHash(srcFile);

				if (savedHash.SequenceEqual(calcHash))
					return false;
			}
			catch (IOException)
			{
			}
			return true;
		}

		private static byte[] GetFileHash(string file)
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
				return new byte[0];
			}
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

		private static List<string> ParseDependsFile(string input)
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

		private static bool GetModifyTime(string path, out DateTime tm)
		{
			if (!File.Exists(path))
			{
				tm = DateTime.MaxValue;
				return false;
			}

			tm = File.GetLastWriteTimeUtc(path);
			return true;
		}

		private static string GetRelativePath(string path, string relativeTo)
		{
			string fullPath = Path.GetFullPath(path);

			if (string.IsNullOrEmpty(relativeTo))
				relativeTo = ".";

			string fullRelative = Path.GetFullPath(relativeTo + '/');
			if (fullPath.Length >= fullRelative.Length)
				return fullPath.Substring(fullRelative.Length);
			else
				return path;
		}

		private static string EscapePath(string path)
		{
			path = path.Replace('/', '$');
			path = path.Replace('\\', '$');
			path = path.Replace(':', '#');
			return path;
		}
	}

	class Program
	{
		static void MakeIl2cpp(
			string srcDir,
			string outDir,
			List<string> genFiles)
		{
			Directory.SetCurrentDirectory(srcDir);

			// 编译生成的文件
			var objFiles = Make.Compile(
				Make.ToUnits(genFiles, "-O3 -c -emit-llvm -Wall -Xclang -flto-visibility-public-std -D_CRT_SECURE_NO_WARNINGS -DIL2CPP_PATCH_LLVM"),
				null, outDir,
				Console.WriteLine,
				strErr =>
				{
					Console.WriteLine("[CompileError] {0}", strErr);
				});

			// 连接
			string linkedFile = Path.Combine(outDir, "!linked.bc");
			Make.Link(
				objFiles,
				linkedFile,
				null,
				Console.WriteLine,
				strErr =>
				{
					Console.WriteLine("[LinkError] {0}", strErr);
				});

			// 优化
			string lastOptFile = linkedFile;
			int optCount = 6;
			for (int i = 0; i < optCount; ++i)
			{
				bool isLast = i == optCount - 1;

				string optFile;
				string args;
				if (isLast)
				{
					optFile = "!opt_" + i + ".ll";
					args = "-O3 -S -emit-llvm";
				}
				else
				{
					optFile = "!opt_" + i + ".bc";
					args = "-O3 -c -emit-llvm";
				}

				Make.Compile(
				  lastOptFile, args,
				  optFile,
				  null,
				  outDir,
				  false,
				  Console.WriteLine,
				  Console.WriteLine,
				  out lastOptFile);
			}

			PatchFile(lastOptFile, "@calloc", "@_il2cpp_GC_PatchCalloc");

			// 编译 GC
			Make.Compile(
				"bdwgc/extra/gc.c",
				"-O3 -c -emit-llvm -D_CRT_SECURE_NO_WARNINGS -DDONT_USE_USER32_DLL -DNO_GETENV -DGC_NOT_DLL -Ibdwgc/include",
				null,
				null,
				outDir,
				true,
				Console.WriteLine,
				Console.WriteLine,
				out var outGCFile);

			Make.Compile(
				"il2cppGC.cpp",
				"-O3 -c -emit-llvm -Wall -D_CRT_SECURE_NO_WARNINGS -DIL2CPP_PATCH_LLVM -Ibdwgc/include",
				null,
				null,
				outDir,
				true,
				Console.WriteLine,
				Console.WriteLine,
				out var outGCHlpFile);

			// 连接 GC
			linkedFile = Path.Combine(outDir, "!linked_gc.bc");
			Make.Link(
				new List<string> { lastOptFile, outGCFile, outGCHlpFile },
				linkedFile,
				null,
				Console.WriteLine,
				strErr =>
				{
					Console.WriteLine("[LinkGCError] {0}", strErr);
				});

			// 最终优化
			lastOptFile = linkedFile;
			optCount = 2;
			for (int i = 0; i < optCount; ++i)
			{
				string optFile = "!opt_gc_" + i + ".bc";

				Make.Compile(
					lastOptFile,
					"-O3 -c -emit-llvm",
					optFile,
					null,
					outDir,
					false,
					Console.WriteLine,
					Console.WriteLine,
					out lastOptFile);
			}

			string finalFile = "final.exe";
			// 生成可执行文件
			Make.Compile(
				lastOptFile,
				"-O3",
				finalFile,
				null,
				outDir,
				false,
				Console.WriteLine,
				strErr =>
				{
					Console.WriteLine("[FinalLink] {0}", strErr);
				},
				out var outExeFile);

			if (File.Exists(finalFile))
				File.Delete(finalFile);
			File.Copy(outExeFile, finalFile);
		}

		static void PatchFile(string file, string src, string dst)
		{
			try
			{
				string content = File.ReadAllText(file, Encoding.UTF8);
				if (content.IndexOf(src, StringComparison.Ordinal) == -1)
					return;

				content = content.Replace(src, dst);
				var buf = Encoding.UTF8.GetBytes(content);
				File.WriteAllBytes(file, buf);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		static void Main(string[] args)
		{
			if (args.Length > 0)
				MakeIl2cpp(".", "output", args.ToList());
		}
	}
}
