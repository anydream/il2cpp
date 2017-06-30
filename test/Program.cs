using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;
using il2cpp2;

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
		private static string PrintAllTypes(IList<TypeX> allTypes)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("======");

			foreach (var type in allTypes)
			{
				if (type.IsEmptyType)
					continue;

				sb.AppendLine(type.PrettyName());

				foreach (var met in type.Methods)
				{
					sb.AppendFormatLine("-> {0}{1}", met.PrettyName(), met.IsCallVirtOnly ? " = 0" : "");
					if (met.HasOverrideImpls)
					{
						var impls = met.OverrideImplsList;
						for (int i = 0, sz = impls.Count; i < sz; ++i)
						{
							var impl = impls[i];
							sb.AppendFormatLine("   {0} {1}: {2}",
								i + 1 == sz ? '\\' : '|',
								impl.PrettyName(),
								impl.DeclType.PrettyName());
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
				if (attr.TypeFullName == "TestIL.TestClassAttribute")
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

		private static void TestProcess(TypeManager marker)
		{
			foreach (var typeDef in marker.Module.Types)
			{
				if (GetTestClassResult(typeDef, out string expected))
				{
					var oldColor = Console.ForegroundColor;
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.Write("{0}: ", typeDef.FullName);
					Console.ForegroundColor = oldColor;

					marker.AddEntry(typeDef.FindMethod("Entry"));
					marker.Process();
					string result = PrintAllTypes(marker.Types);

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

					marker.Reset();
				}
			}
		}

		private static void Main(string[] args)
		{
			TypeManager typeMgr = new TypeManager();
			typeMgr.Load(@"../../MSILTester/bin/debug/MSILTester.exe");

#if true
			TestProcess(typeMgr);
#else
			typeMgr.AddEntry(typeMgr.Module.EntryPoint);
			typeMgr.Process();
			string result = PrintAllTypes(typeMgr.Types);
			Console.WriteLine(result);
#endif
		}
	}
}
