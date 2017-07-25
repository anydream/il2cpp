using System.Collections.Generic;

namespace il2cpp
{
	// 类型对应的代码
	class TypeCppCode
	{
		// 类型名
		public readonly string Name;
		// 声明代码
		public string DeclCode;
		// 实现代码
		public string ImplCode;
		// 声明依赖的类型
		public readonly HashSet<string> DeclDependTypes = new HashSet<string>();
		// 实现依赖的类型
		public readonly HashSet<string> ImplDependTypes = new HashSet<string>();

		public TypeCppCode(string name)
		{
			Name = name;
		}
	}

	// 类型生成器
	public class TypeGenerator
	{
		// 类型管理器
		private readonly TypeManager TypeMgr;
		// 方法生成器
		private readonly MethodGenerator MethodGen;

		// 类型代码映射
		private readonly Dictionary<string, TypeCppCode> CodeMap = new Dictionary<string, TypeCppCode>();

		public TypeGenerator(TypeManager typeMgr)
		{
			TypeMgr = typeMgr;
			MethodGen = new MethodGenerator(typeMgr);
		}

		public void GenerateAll()
		{
			NameHelper.Reset();

			// 生成所有类型
			foreach (var type in TypeMgr.Types)
			{
				ProcessType(type);
			}
		}

		private void ProcessType(TypeX currType)
		{
			// 生成类型结构体代码
			TypeCppCode cppCode = GenDeclCode(currType);

			// 生成方法代码
			foreach (var metX in currType.Methods)
			{
				MethodGen.Process(metX);

				cppCode.DeclCode += MethodGen.DeclCode;
				cppCode.ImplCode += MethodGen.ImplCode;

				cppCode.DeclDependTypes.UnionWith(MethodGen.DeclDependTypes);
				cppCode.ImplDependTypes.UnionWith(MethodGen.ImplDependTypes);
			}
		}

		private TypeCppCode GenDeclCode(TypeX currType)
		{
			CodePrinter prt = new CodePrinter();

			// 构造类型注释
			prt.AppendFormatLine("// {0}, {1}",
				currType.FullName,
				currType.RuntimeVersion);

			// 构造类型结构体代码
			string typeName;
			string baseTypeName = null;
			if (currType.BaseType != null)
			{
				baseTypeName = currType.BaseType.GetCppName();
				typeName = currType.GetCppName();
				prt.AppendFormatLine("struct {0} : {1}\n{{",
					typeName,
					baseTypeName);
			}
			else
			{
				typeName = currType.GetCppName();
				prt.AppendFormatLine("struct {0}\n{{",
					typeName);
			}

			// 添加代码映射
			TypeCppCode cppCode = new TypeCppCode(typeName);
			CodeMap.Add(typeName, cppCode);

			// 添加基类型依赖
			if (baseTypeName != null)
				cppCode.DeclDependTypes.Add(baseTypeName);

			++prt.Indents;

			// 构造结构体成员
			foreach (var fldX in currType.Fields)
			{
				string fieldTypeName = fldX.FieldType.GetCppName(TypeMgr);
				prt.AppendFormatLine("// {0}\n{1} {2};",
					fldX.PrettyName(),
					fieldTypeName,
					fldX.GetCppName());

				// 添加字段类型依赖
				if (fldX.FieldType.IsValueType)
					cppCode.DeclDependTypes.Add(fieldTypeName);
			}

			--prt.Indents;
			prt.AppendLine("};");

			cppCode.DeclCode = prt.ToString();
			return cppCode;
		}
	}

	/*public class CppCompileUnit
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
		public int ImplCodeCounter { get; private set; }

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

			HashSet<string> nameSet = new HashSet<string>();
			foreach (var type in DependTypes)
			{
				foreach (var dep in type.DependTypes)
				{
					if (dep.CppUnit != this)
					{
						nameSet.Add(dep.CppUnit.Name);
					}
				}
			}
			foreach (var name in nameSet)
			{
				sbDecl.AppendFormat("#include \"{0}.h\"\n", name);
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
	}*/
}
