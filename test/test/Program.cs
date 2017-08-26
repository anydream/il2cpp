using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using il2cpp;

namespace test
{
	internal class Program
	{
		private const string TestCaseDir = "../../../testcases/";
		private static int TotalTests;
		private static int PassedTests;

		private static string GetRelativePath(string path, string relativeTo)
		{
			string fullPath = Path.GetFullPath(path);
			string fullRelative = Path.GetFullPath(relativeTo);
			return fullPath.Substring(fullRelative.Length);
		}

		private static bool IsTestClass(TypeDef typeDef)
		{
			if (typeDef.HasCustomAttributes)
			{
				var attr = typeDef.CustomAttributes[0];
				if (attr.AttributeType.Name == "TestAttribute")
					return true;
			}
			return false;
		}

		private static void TestType(
			Il2cppContext context, TypeDef typeDef,
			string imageDir, string imageName, string subDir)
		{
			if (!IsTestClass(typeDef))
				return;

			string testName = string.Format("[{0}]{1}", imageName, typeDef.FullName);
			var oldColor = Console.ForegroundColor;
			Console.Write("{0} {1}: ", subDir, testName);

			context.AddEntry(typeDef.FindMethod("Entry"));

			var sw = new Stopwatch();
			sw.Start();
			context.Process();
			sw.Stop();
			long elapsedMS = sw.ElapsedMilliseconds;
			Console.Write("{0}ms, ", elapsedMS);

			HierarchyDump dumper = new HierarchyDump(context);
			StringBuilder sb = new StringBuilder();

			sb.Append("* MethodTables:\n");
			dumper.DumpMethodTables(sb);
			sb.Append("* Types:\n");
			dumper.DumpTypes(sb);

			var dumpData = Encoding.UTF8.GetBytes(sb.ToString());
			File.WriteAllBytes(
				Path.Combine(imageDir, testName + ".dump"),
				dumpData);

			byte[] cmpData = null;
			try
			{
				cmpData = File.ReadAllBytes(Path.Combine(imageDir, testName + ".txt"));
			}
			catch
			{
			}

			if (cmpData != null && dumpData.SequenceEqual(cmpData))
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("PASSED");
				Console.ForegroundColor = oldColor;

				++PassedTests;
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("FAILED");
				Console.ForegroundColor = oldColor;
			}

			++TotalTests;

			context.Reset();
		}

		private static void TestAssembly(string imageDir, string imagePath)
		{
			string imageName = Path.GetFileName(imagePath);
			string subDir = GetRelativePath(imageDir, TestCaseDir);

			Il2cppContext context = new Il2cppContext(imagePath);
			foreach (TypeDef typeDef in context.Module.Types)
			{
				TestType(context, typeDef, imageDir, imageName, subDir);
			}
		}

		private static void Main(string[] args)
		{
			var files = Directory.GetFiles(TestCaseDir, "*.exe", SearchOption.AllDirectories);
			foreach (string file in files)
			{
				string dir = Path.GetDirectoryName(file);
				TestAssembly(dir, file);
			}

			Console.WriteLine("\nPassed: {0}/{1}", PassedTests, TotalTests);
		}
	}
}
