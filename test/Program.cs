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
				sb.AppendLine(type.PrettyName());

				foreach (var met in type.Methods)
				{
					sb.AppendFormatLine("-> {0}{1}", met.PrettyName(), met.IsCallVirtOnly ? " = 0" : "");
					if (met.HasOverrideImpls)
					{
						var implList = new List<MethodX>(met.OverrideImpls);
						for (int i = 0, sz = implList.Count; i < sz; ++i)
						{
							var impl = implList[i];
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

#if false
	internal class Program
	{
		private static string PrintAllTypes(List<TypeX> allTypes)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("======");

			var refOnlyTypes = new List<TypeX>();

			foreach (var type in allTypes)
			{
				if (type.IsRefOnly)
				{
					refOnlyTypes.Add(type);
					continue;
				}

				sb.AppendLine(type.PrettyName());
				if (type.HasMethods)
				{
					foreach (var met in type.Methods)
					{
						sb.AppendFormatLine("-> {0}{1}", met.PrettyName(), met.IsSkipped ? " = 0" : "");
						if (met.HasOverrideImpls)
						{
							for (int i = 0, sz = met.OverrideImpls.Count; i < sz; ++i)
							{
								var impl = met.OverrideImpls[i];
								sb.AppendFormatLine("   {0} {1}: {2}",
									i + 1 == sz ? '\\' : '|',
									impl.PrettyName(),
									impl.DeclType.PrettyName());
							}
						}
					}
				}
				if (type.HasFields)
				{
					foreach (var fld in type.Fields)
						sb.AppendFormatLine("--> {0}", fld.PrettyName());
				}
				sb.AppendLine();
			}

			sb.AppendLine("======");

			foreach (var type in refOnlyTypes)
				sb.AppendLine(type.PrettyName());

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

		private static void TestProcess(CodeMarker marker)
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
					string result = PrintAllTypes(marker.AllTypes);

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
			CodeMarker marker = new CodeMarker();
			marker.Load("../../MSILTester/bin/debug/MSILTester.exe");

#if true
			TestProcess(marker);
#else
			marker.AddEntry(marker.Module.EntryPoint);
			marker.Process();
			string result = PrintAllTypes(marker.AllTypes);
			Console.WriteLine(result);
#endif
		}
	}
#endif
}
