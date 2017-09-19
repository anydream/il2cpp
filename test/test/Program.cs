using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using il2cpp;

namespace test
{
	internal class Testbed
	{
		public string TestDir;
		public Action<Il2cppContext, TypeDef, string, string, string> OnType;

		public void Start()
		{
			var files = Directory.GetFiles(TestDir, "*.exe", SearchOption.AllDirectories);
			foreach (string file in files)
			{
				string dir = Path.GetDirectoryName(file);
				TestAssembly(dir, file);
			}
		}

		private void TestAssembly(string imageDir, string imagePath)
		{
			string imageName = Path.GetFileName(imagePath);
			string subDir = GetRelativePath(imageDir, TestDir);

			Il2cppContext context = new Il2cppContext(imagePath);
			foreach (TypeDef typeDef in context.Module.Types)
			{
				OnType(context, typeDef, imageDir, imageName, subDir);
			}
		}

		private static string GetRelativePath(string path, string relativeTo)
		{
			string fullPath = Path.GetFullPath(path + '/');
			string fullRelative = Path.GetFullPath(relativeTo);
			return fullPath.Substring(fullRelative.Length);
		}
	}

	internal class Program
	{
		private static int TotalTests;
		private static int PassedTests;

		private static MethodDef IsTestBinding(TypeDef typeDef)
		{
			if (typeDef.HasCustomAttributes)
			{
				var attr = typeDef.CustomAttributes[0];
				if (attr.AttributeType.Name == "TestAttribute")
				{
					return typeDef.FindMethod("Entry");
				}
			}
			else
			{
				MethodDef entryPoint = typeDef.Module.EntryPoint;
				if (entryPoint != null && entryPoint.DeclaringType == typeDef)
				{
					if (entryPoint.HasBody &&
						entryPoint.Body.Instructions.Count > 2)
						return entryPoint;
				}
			}
			return null;
		}

		private static void TestBinding(
			Il2cppContext context, TypeDef typeDef,
			string imageDir, string imageName, string subDir)
		{
			MethodDef metDef = IsTestBinding(typeDef);
			if (metDef == null)
				return;

			string testName = string.Format("[{0}]{1}", imageName, typeDef.FullName);
			var oldColor = Console.ForegroundColor;
			Console.Write("{0} {1}: ", subDir, testName);

			context.AddEntry(metDef);

			var sw = new Stopwatch();
			sw.Start();
			string exceptionMsg = null;
			try
			{
				context.Resolve();
			}
			catch (TypeLoadException ex)
			{
				exceptionMsg = ex.Message;
			}
			sw.Stop();
			long elapsedMS = sw.ElapsedMilliseconds;
			Console.Write("{0}ms, ", elapsedMS);

			StringBuilder sb = new StringBuilder();
			if (exceptionMsg != null)
				sb.Append(exceptionMsg);
			else
			{
				HierarchyDump dumper = new HierarchyDump(context);

				/*sb.Append("* MethodTables:\n");
				dumper.DumpMethodTables(sb);*/
				sb.Append("* Types:\n");
				dumper.DumpTypes(sb);
			}

			var dumpData = Encoding.UTF8.GetBytes(sb.ToString());

			string validatedName = ValidatePath(testName);
			File.WriteAllBytes(
				Path.Combine(imageDir, validatedName + ".dump"),
				dumpData);

			byte[] cmpData = null;
			try
			{
				cmpData = File.ReadAllBytes(Path.Combine(imageDir, validatedName + ".txt"));
				cmpData = ReplaceNewLines(cmpData);
			}
			catch
			{
			}

			if (cmpData != null && dumpData.SequenceEqual(cmpData))
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("PASS");
				Console.ForegroundColor = oldColor;

				++PassedTests;
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("FAIL");
				Console.ForegroundColor = oldColor;
			}

			++TotalTests;

			context.Reset();
		}

		private static MethodDef IsTestCodeGen(TypeDef typeDef)
		{
			if (typeDef.HasCustomAttributes)
			{
				var attr = typeDef.CustomAttributes[0];
				if (attr.AttributeType.Name == "CodeGenAttribute")
				{
					return typeDef.FindMethod("Entry");
				}
			}
			return null;
		}

		private static void TestCodeGen(
			Il2cppContext context, TypeDef typeDef,
			string imageDir, string imageName, string subDir)
		{
			MethodDef metDef = IsTestCodeGen(typeDef);
			if (metDef == null)
				return;

			string testName = string.Format("[{0}]{1}", imageName, typeDef.FullName);
			var oldColor = Console.ForegroundColor;
			Console.Write("{0} {1}: ", subDir, testName);

			context.AddEntry(metDef);

			var sw = new Stopwatch();
			sw.Start();
			string exceptionMsg = null;
			try
			{
				context.Resolve();
			}
			catch (TypeLoadException ex)
			{
				exceptionMsg = ex.Message;
			}
			sw.Stop();
			long elapsedMS = sw.ElapsedMilliseconds;
			Console.Write("Resolve: {0}ms, ", elapsedMS);

			sw.Restart();
			var units = context.Generate();
			sw.Stop();
			elapsedMS = sw.ElapsedMilliseconds;
			Console.Write("Generate: {0}ms, ", elapsedMS);

			string validatedName = ValidatePath(testName);
			Il2cppContext.SaveToFolder(
				Path.Combine(imageDir, "gen", validatedName),
				units);

			Console.WriteLine();
		}

		private static byte[] ReplaceNewLines(byte[] data)
		{
			int len = data.Length;
			int writePtr = 0;
			int readPtr = 0;
			for (; readPtr < len; ++writePtr, ++readPtr)
			{
				byte curr = data[writePtr] = data[readPtr];
				if (curr == '\r')
				{
					int nextPtr = readPtr + 1;
					if (nextPtr < len)
					{
						byte next = data[nextPtr];
						if (next == '\n')
						{
							data[writePtr] = (byte)'\n';
							++readPtr;
						}
					}
					else
						break;
				}
			}

			int offset = readPtr - writePtr;
			if (offset > 0)
			{
				byte[] result = new byte[writePtr];
				Array.Copy(data, result, writePtr);
				return result;
			}
			else
				return data;
		}

		private static string ValidatePath(string str)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var ch in str)
			{
				if (ch == '<' || ch == '>')
					sb.Append('_');
				else
					sb.Append(ch);
			}
			return sb.ToString();
		}

		private static void Main(string[] args)
		{
#if false
			var testBinding = new Testbed();
			testBinding.TestDir = "../../../testcases/";
			testBinding.OnType = TestBinding;
			testBinding.Start();
			Console.WriteLine("\nTestBinding Passed: {0}/{1}", PassedTests, TotalTests);
#endif
			var testCodeGen = new Testbed();
			testCodeGen.TestDir = "../../../testcases/";
			testCodeGen.OnType = TestCodeGen;
			testCodeGen.Start();
		}
	}
}
