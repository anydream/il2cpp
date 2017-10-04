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
	internal static class Make
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
			try
			{
				string srcFile = GetRelativePath(unitSrcFile, srcDir);
				string unitName = EscapePath(srcFile);

				if (outputName == null)
					outputName = unitName + ".bc";

				outputFile = Path.Combine(outDir, outputName);
				string depFile = hasDepFile ? Path.Combine(outDir, unitName + ".d") : null;
				string hashFile = Path.Combine(outDir, unitName + ".hash");

				string concatArgs = srcFile + " -o " + outputFile + " " + (hasDepFile ? "-MD " : null) + unitArgs;

				if (!IsNeedCompile(srcFile, outputFile, depFile, hashFile, concatArgs))
				{
					onOutput(string.Format("Skipped: {0}", srcFile));
					return false;
				}

				RunCommand("clang", concatArgs, srcDir, onOutput, onError);

				WriteHashFile(hashFile, srcFile, concatArgs);

				onOutput(string.Format("Compiled: {0} -> {1}", srcFile, outputFile));

				return true;
			}
			catch (Exception ex)
			{
				onError(ex.ToString());
			}
			outputFile = null;
			return false;
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

		private static bool IsNeedCompile(string srcFile, string objFile, string depFile, string hashFile, string command)
		{
			if (!ReadHashFile(hashFile, out var savedCmd, out var savedHash))
				return true;

			if (savedCmd != command)
				return true;

			if (!GetModifyTime(srcFile, out var srcMT))
				return true;

			if (!GetModifyTime(objFile, out var objMT))
				return true;

			if (srcMT > objMT)
				return IsHashChanged(srcFile, savedHash);

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

			onOutput(string.Format("Linked: {0}", linkFile));
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

		private static void WriteHashFile(string hashFile, string srcFile, string command)
		{
			byte[] cmdBytes = Encoding.UTF8.GetBytes(command);
			byte[] hashBytes = GetFileHash(srcFile);

			using (MemoryStream ms = new MemoryStream())
			{
				using (BinaryWriter bw = new BinaryWriter(ms))
				{
					bw.Write(cmdBytes.Length);
					bw.Write(cmdBytes);
					bw.Write(hashBytes.Length);
					bw.Write(hashBytes);

					using (FileStream fs = File.Open(hashFile, FileMode.Create))
						ms.WriteTo(fs);
				}
			}
		}

		private static bool ReadHashFile(string hashFile, out string command, out byte[] hashCode)
		{
			try
			{
				byte[] buf = File.ReadAllBytes(hashFile);

				using (MemoryStream ms = new MemoryStream(buf))
				{
					using (BinaryReader br = new BinaryReader(ms))
					{
						int cmdLen = br.ReadInt32();
						if (cmdLen < 0 || cmdLen > br.BaseStream.Length - br.BaseStream.Position)
							throw new IOException();
						command = Encoding.UTF8.GetString(br.ReadBytes(cmdLen));
						int hashLen = br.ReadInt32();
						if (hashLen < 0 || hashLen > br.BaseStream.Length - br.BaseStream.Position)
							throw new IOException();
						hashCode = br.ReadBytes(hashLen);
					}
				}
			}
			catch (Exception)
			{
				command = null;
				hashCode = null;
				return false;
			}
			return true;
		}

		private static bool IsHashChanged(string srcFile, byte[] hashCode)
		{
			try
			{
				var calcHash = GetFileHash(srcFile);
				if (calcHash.SequenceEqual(hashCode))
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
		static string OptLevel = "-O3";
		static int GenOptCount = 6;
		static int FinalOptCount = 2;

		static void MakeIl2cpp(
			string srcDir,
			string outDir,
			List<string> genFiles)
		{
			Directory.SetCurrentDirectory(srcDir);

			// 编译生成的文件
			var objFiles = Make.Compile(
				Make.ToUnits(genFiles, OptLevel + " -c -emit-llvm -Wall -Xclang -flto-visibility-public-std -D_CRT_SECURE_NO_WARNINGS -DIL2CPP_PATCH_LLVM"),
				null, outDir,
				Console.WriteLine,
				Console.Error.WriteLine);

			// 连接
			string linkedFile = Path.Combine(outDir, "!linked.bc");
			Make.Link(
				objFiles,
				linkedFile,
				null,
				Console.WriteLine,
				strErr =>
				{
					Console.Error.WriteLine("[LinkError] {0}", strErr);
				});

			// 优化
			string lastOptFile = linkedFile;
			for (int i = 0; i < GenOptCount; ++i)
			{
				bool isLast = i == GenOptCount - 1;

				string optFile;
				string args;
				if (isLast)
				{
					optFile = "!opt_" + i + ".ll";
					args = OptLevel + " -S -emit-llvm";
				}
				else
				{
					optFile = "!opt_" + i + ".bc";
					args = OptLevel + " -c -emit-llvm";
				}

				Make.Compile(
				  lastOptFile, args,
				  optFile,
				  null,
				  outDir,
				  false,
				  Console.WriteLine,
				  Console.Error.WriteLine,
				  out lastOptFile);
			}

			PatchFile(lastOptFile, "@calloc", "@_il2cpp_GC_PatchCalloc");

			// 编译 GC
			Make.Compile(
				"bdwgc/extra/gc.c",
				OptLevel + " -c -emit-llvm -D_CRT_SECURE_NO_WARNINGS -DDONT_USE_USER32_DLL -DNO_GETENV -DGC_NOT_DLL -Ibdwgc/include",
				null,
				null,
				outDir,
				true,
				Console.WriteLine,
				Console.Error.WriteLine,
				out var outGCFile);

			Make.Compile(
				"il2cppGC.cpp",
				OptLevel + " -c -emit-llvm -Wall -D_CRT_SECURE_NO_WARNINGS -DIL2CPP_PATCH_LLVM -Ibdwgc/include",
				null,
				null,
				outDir,
				true,
				Console.WriteLine,
				Console.Error.WriteLine,
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
					Console.Error.WriteLine("[LinkGCError] {0}", strErr);
				});

			// 最终优化
			lastOptFile = linkedFile;
			for (int i = 0; i < FinalOptCount; ++i)
			{
				string optFile = "!opt_gc_" + i + ".bc";

				Make.Compile(
					lastOptFile,
					OptLevel + " -c -emit-llvm",
					optFile,
					null,
					outDir,
					false,
					Console.WriteLine,
					Console.Error.WriteLine,
					out lastOptFile);
			}

			string finalFile = "final.exe";
			// 生成可执行文件
			Make.Compile(
				lastOptFile,
				OptLevel,
				finalFile,
				null,
				outDir,
				false,
				Console.WriteLine,
				strErr =>
				{
					Console.Error.WriteLine("[FinalLink] {0}", strErr);
				},
				out var outExeFile);

			if (File.Exists(finalFile))
				File.Delete(finalFile);
			if (File.Exists(outExeFile))
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
				Console.Error.WriteLine(ex.Message);
			}
		}

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
				Make.RunCommand("clang", "-v", null, null, null);
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
				var compileUnits = ParseArgs(args);
				MakeIl2cpp(".", "output", compileUnits);
			}
			else
			{
				Console.Error.WriteLine("Please run 'build.cmd' to compile");
				Console.ReadKey();
			}
		}
	}
}
