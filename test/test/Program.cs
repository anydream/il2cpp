using System;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using il2cpp;

namespace test
{
	internal class Program
	{
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

		private static void TestType(Il2cppContext context, TypeDef typeDef, string imageDir, string imageName)
		{
			if (!IsTestClass(typeDef))
				return;

			string testName = string.Format("[{0}]{1}", imageName, typeDef.FullName);
			var oldColor = Console.ForegroundColor;
			Console.Write("{0}: ", testName);

			context.AddEntry(typeDef.FindMethod("Entry"));
			context.Process();

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

			var cmpData = File.ReadAllBytes(Path.Combine(imageDir, testName + ".txt"));

			if (dumpData.SequenceEqual(cmpData))
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("PASSED");
				Console.ForegroundColor = oldColor;
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("FAILED");
				Console.ForegroundColor = oldColor;

			}

			context.Reset();
		}

		private static void TestAssembly(string imageDir, string imagePath)
		{
			string imageName = Path.GetFileName(imagePath);

			Il2cppContext context = new Il2cppContext(imagePath);
			foreach (TypeDef typeDef in context.Module.Types)
			{
				TestType(context, typeDef, imageDir, imageName);
			}
		}

		private static void Main(string[] args)
		{
			var files = Directory.GetFiles("../../../testcases/", "*.exe", SearchOption.AllDirectories);
			foreach (string file in files)
			{
				string dir = Path.GetDirectoryName(file);
				TestAssembly(dir, file);
			}
		}
	}
}
