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

		public static void Compile(
			List<CompileUnit> units,
			string relDir,
			string outDir,
			Action<string> onOutput,
			Action<string> onError)
		{
			Directory.CreateDirectory(outDir);

			int compiled = 0;
			Parallel.ForEach(units, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
				unit =>
				{
					string srcFile = GetRelativePath(unit.SrcFile, relDir);
					string unitName = EscapePath(srcFile);

					string objFile = Path.Combine(outDir, unitName + ".bc");
					string depFile = Path.Combine(outDir, unitName + ".d");
					string hashFile = Path.Combine(outDir, unitName + ".hash");
					if (!IsNeedCompile(srcFile, objFile, depFile, hashFile))
					{
						Console.WriteLine("Skipped: {0}", srcFile);
						return;
					}

					string prependArgs = srcFile + " -o " + objFile + " -c -emit-llvm -MD ";
					RunCommand("clang", prependArgs + unit.Arguments, relDir, onOutput, onError);

					File.WriteAllBytes(hashFile, GetFileHash(srcFile));

					Interlocked.Increment(ref compiled);
					Console.WriteLine("Compiled: {0}", srcFile);
				});

			Console.WriteLine("Compiled {0} of {1}", compiled, units.Count);
		}

		private static bool IsNeedCompile(string srcFile, string objFile, string depFile, string hashFile)
		{
			if (!GetModifyTime(srcFile, out var srcMT))
				return true;

			if (!GetModifyTime(objFile, out var objMT))
				return true;

			if (srcMT > objMT)
				return IsHashChanged(srcFile, hashFile);

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
			using (var md5 = MD5.Create())
			{
				using (var stream = File.OpenRead(file))
				{
					return md5.ComputeHash(stream);
				}
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
		static void Main(string[] args)
		{

		}
	}
}
