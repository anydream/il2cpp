using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using il2cpp;

namespace test
{
	static class Helper
	{
		public static StringBuilder AppendFormatLine(this StringBuilder self, string format, params object[] args)
		{
			return self.AppendFormat(format, args).AppendLine();
		}
	}

	internal class Program
	{
		private static string PrintAllTypes(IList<TypeX> allTypes, bool showVersion)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("======");

			foreach (var type in allTypes)
			{
				if (type.IsEmptyType)
					continue;

				sb.AppendLine(type.PrettyName() + (showVersion ? " [" + type.RuntimeVersion + "]" : ""));

				foreach (var met in type.Methods)
				{
					sb.AppendFormatLine("-> {0}{1}", met.PrettyName(), met.IsCallVirtOnly ? " = 0" : "");
					if (met.HasOverrideImpls)
					{
						var impls = met.OverrideImpls.ToList();
						for (int i = 0, sz = impls.Count; i < sz; ++i)
						{
							var impl = impls[i];
							sb.AppendFormatLine("   {0} {1}: {2}",
								i + 1 == sz ? '\\' : '|',
								impl.PrettyName(),
								impl.DeclType.PrettyName() + (showVersion ? " [" + impl.DeclType.RuntimeVersion + "]" : ""));
						}
					}
				}

				foreach (var fld in type.Fields)
					sb.AppendFormatLine("--> {0}", fld.PrettyName());

				sb.AppendLine();
			}

			sb.AppendLine("======");

			return sb.ToString();
		}

		private static bool GetTestClassResult(TypeDef typeDef, out string expected)
		{
			if (typeDef.HasCustomAttributes)
			{
				var attr = typeDef.CustomAttributes[0];
				if (attr.AttributeType.Name == "TestClassAttribute")
				{
					if (attr.HasConstructorArguments)
					{
						expected = attr.ConstructorArguments[0].Value.ToString();
						return true;
					}
				}
			}

			expected = "";
			return false;
		}

		private static void TestReferenceMarker(TypeManager typeMgr)
		{
			foreach (var typeDef in typeMgr.Module.Types)
			{
				//if (typeDef.FullName != "TestIL.TestExplicitOverride2")
				//	continue;

				if (GetTestClassResult(typeDef, out string expected))
				{
					var oldColor = Console.ForegroundColor;
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.Write("{0}: ", typeDef.FullName);
					Console.ForegroundColor = oldColor;

					typeMgr.AddEntry(typeDef.FindMethod("Entry"));
					typeMgr.Process();
					string result = PrintAllTypes(typeMgr.Types, false);

					if (result == expected)
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
						Console.WriteLine(result);
					}

					typeMgr.Reset();
				}
			}
		}

		private static void TestCodeGen(TypeManager typeMgr)
		{
			TypeGenerator typeGen = new TypeGenerator(typeMgr);
			StringBuilder sb = new StringBuilder();

			foreach (var typeDef in typeMgr.Module.Types)
			{
				if (GetTestClassResult(typeDef, out string expected))
				{
					var oldColor = Console.ForegroundColor;
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.Write("{0}: ", typeDef.FullName);
					Console.ForegroundColor = oldColor;

					typeMgr.AddEntry(typeDef.FindMethod("Entry"));
					typeMgr.Process();
					typeGen.GenerateAll();

					foreach (var unit in typeGen.CompileUnits)
					{
						sb.AppendFormat("[{0}.h]\n{1}\n[{0}.cpp]\n{2}\n",
							unit.Name,
							unit.DeclCode,
							unit.ImplCode);
					}

					string result = sb.ToString();
					sb.Clear();

					if (result == expected)
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
						Console.WriteLine(result);
					}

					typeMgr.Reset();
				}
			}
		}

		private static void Main(string[] args)
		{
#if true
			TypeManager typeMgr = new TypeManager();
			typeMgr.Load(@"../../CodeGenTester/bin/debug/CodeGenTester.exe");
			TestCodeGen(typeMgr);
#endif

#if false
			TypeManager typeMgr = new TypeManager();
			typeMgr.Load(@"../../MSILTester/bin/debug/MSILTester.exe");
			TestReferenceMarker(typeMgr);
#endif

#if false
			var sw = new Stopwatch();
			sw.Start();

			typeMgr.AddEntry(typeMgr.Module.EntryPoint);
			typeMgr.Process();

			sw.Stop();
			Console.WriteLine("Elapsed: {0}ms", sw.ElapsedMilliseconds);

			TypeGenerator typeGen = new TypeGenerator(typeMgr);
			typeGen.GenerateAll();
#endif
		}
	}
}
