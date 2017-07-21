using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace il2cpp
{
	// 类型生成器
	public class TypeGenerator
	{
		// 类型管理器
		private readonly TypeManager TypeMgr;
		// 方法生成器
		private readonly MethodGenerator MethodGen;

		// 当前类型
		private TypeX CurrType;

		// 声明代码
		public string DeclCode;
		// 实现代码
		public string ImplCode;

		private readonly List<CppCompileUnit> CppUnitList = new List<CppCompileUnit>();
		private int CppNameCounter;

		public TypeGenerator(TypeManager typeMgr)
		{
			TypeMgr = typeMgr;
			MethodGen = new MethodGenerator(typeMgr);
		}

		public void GenerateAll()
		{
			NameHelper.Reset();
			CppNameCounter = 0;

			var types = TypeMgr.Types;
			foreach (var type in types)
			{
				ProcessType(type);
				SelectCppUnit().AddType(type, DeclCode, ImplCode);
			}

			foreach (var unit in CppUnitList)
			{
				unit.GenerateCode();

				Console.WriteLine("// [{0}.h]\n{1}\n// [{0}.cpp]\n{2}",
					unit.Name,
					unit.DeclCode,
					unit.ImplCode);
			}
		}

		private CppCompileUnit SelectCppUnit()
		{
			CppCompileUnit curr;
			if (CppUnitList.Count > 0)
			{
				curr = CppUnitList[CppUnitList.Count - 1];
				if (curr.ImplCodeCounter <= 30000)
					return curr;
			}

			curr = new CppCompileUnit(GenCppUnitName());
			CppUnitList.Add(curr);

			return curr;
		}

		private string GenCppUnitName()
		{
			return "CppUnit_" + CppNameCounter++;
		}

		private void ProcessType(TypeX tyX)
		{
			CurrType = tyX;
			DeclCode = null;
			ImplCode = null;

			// 生成类型结构体代码
			GenDeclCode();

			// 生成方法代码
			foreach (var metX in CurrType.Methods)
			{
				MethodGen.Process(metX);
				DeclCode += MethodGen.DeclCode;
				ImplCode += MethodGen.ImplCode;
			}
		}

		private void GenDeclCode()
		{
			CodePrinter prt = new CodePrinter();
			prt.AppendFormatLine("// {0}, {1}",
				CurrType.FullName,
				CurrType.RuntimeVersion);

			if (CurrType.BaseType != null)
			{
				string baseName = CurrType.BaseType.GetCppName();
				prt.AppendFormatLine("struct {0} : {1}\n{{",
					CurrType.GetCppName(),
					baseName);
			}
			else
			{
				prt.AppendFormatLine("struct {0}\n{{",
					CurrType.GetCppName());
			}
			++prt.Indents;

			foreach (var fldX in CurrType.Fields)
			{
				prt.AppendFormatLine("// {0}\n{1} {2};",
					fldX.PrettyName(),
					fldX.FieldType.GetCppName(TypeMgr),
					fldX.GetCppName());
			}

			--prt.Indents;
			prt.AppendLine("};");

			DeclCode += prt.ToString();
		}
	}

	public class CppCompileUnit
	{
		private class CodeInfo
		{
			public readonly TypeX CodeType;
			public readonly string DeclCode;
			public readonly string ImplCode;

			public CodeInfo(TypeX tyX, string codeDecl, string codeImpl)
			{
				CodeType = tyX;
				DeclCode = codeDecl;
				ImplCode = codeImpl;
			}
		}

		private readonly List<CodeInfo> CodeInfoList = new List<CodeInfo>();
		private readonly HashSet<TypeX> DependTypes = new HashSet<TypeX>();

		public readonly string Name;
		public string DeclCode;
		public string ImplCode;
		public int ImplCodeCounter;

		public CppCompileUnit(string name)
		{
			Name = name;
		}

		public void AddType(TypeX tyX, string codeDecl, string codeImpl)
		{
			Debug.Assert(tyX.CppUnit == null);
			tyX.CppUnit = this;

			CodeInfoList.Add(new CodeInfo(tyX, codeDecl, codeImpl));
			DependTypes.Add(tyX);

			ImplCodeCounter += codeImpl?.Length ?? 0;
		}

		public void GenerateCode()
		{
			Debug.Assert(DeclCode == null);

			StringBuilder sbDecl = new StringBuilder();
			StringBuilder sbImpl = new StringBuilder();

			sbDecl.AppendLine("#pragma once");
			sbImpl.AppendFormat("#include \"{0}.h\"\n", Name);

			foreach (var type in DependTypes)
			{
				foreach (var dep in type.DependTypes)
				{
					if (dep.CppUnit != this)
					{
						sbDecl.AppendFormat("#include \"{0}.h\"\n", dep.CppUnit.Name);
					}
				}
			}

			CodeInfoList.Sort((x, y) => x.CodeType.GetSortedID().CompareTo(y.CodeType.GetSortedID()));

			foreach (var info in CodeInfoList)
			{
				sbDecl.Append(info.DeclCode);
				sbImpl.Append(info.ImplCode);
			}

			DeclCode = sbDecl.ToString();
			ImplCode = sbImpl.ToString();
		}
	}
}
