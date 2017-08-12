using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	// 类型对应的代码
	internal class TypeCppCode
	{
		// 类型名
		public readonly string Name;
		// 声明代码
		public StringBuilder DeclCode = new StringBuilder();
		// 实现代码
		public StringBuilder ImplCode = new StringBuilder();
		// 声明依赖的类型
		public HashSet<string> DeclDependNames = new HashSet<string>();
		public HashSet<TypeCppCode> DeclDependTypes = new HashSet<TypeCppCode>();
		// 实现依赖的类型
		public HashSet<string> ImplDependNames = new HashSet<string>();
		public HashSet<TypeCppCode> ImplDependTypes = new HashSet<TypeCppCode>();
		// 实现依赖的字符串常量
		public HashSet<string> DependStrings = new HashSet<string>();

		// 编译单元
		public CppCompileUnit CompileUnit;
		// 依赖计数
		public uint DependCounter;
		// 排序索引
		private uint SortedID;

		public TypeCppCode(string name)
		{
			Name = name;
		}

		public uint GetSortedID()
		{
			if (SortedID == 0)
			{
				uint id = 0;

				foreach (var typeCode in DeclDependTypes)
					id = Math.Max(id, typeCode.GetSortedID());

				SortedID = id + 1;
			}
			return SortedID;
		}
	}

	// 编译单元
	public class CppCompileUnit
	{
		// 单元名
		public readonly string Name;
		// 声明代码
		public StringBuilder DeclCode = new StringBuilder();
		// 实现代码
		public StringBuilder ImplCode = new StringBuilder();
		// 包含的代码对象
		internal List<TypeCppCode> CodeList = new List<TypeCppCode>();

		private int ImplLength;
		private int DeclLength;

		public CppCompileUnit(string name)
		{
			Name = name;
		}

		internal void AddCode(TypeCppCode cppCode)
		{
			CodeList.Add(cppCode);
			ImplLength += cppCode.ImplCode.Length;
			DeclLength += cppCode.DeclCode.Length;
		}

		public bool IsFull()
		{
			return ImplLength > 0 ||
				   DeclLength > 0;
		}
	}

	// 类型生成器
	public class TypeGenerator
	{
		// 类型管理器
		internal readonly TypeManager TypeMgr;
		// 字符串生成器
		private readonly StringGenerator StringGen;
		// 方法生成器
		private readonly MethodGenerator MethodGen;

		// 类型代码映射
		private readonly Dictionary<string, TypeCppCode> CodeMap = new Dictionary<string, TypeCppCode>();

		// 静态变量初始化代码
		private readonly StringBuilder StaticInitDecl = new StringBuilder();
		private readonly StringBuilder StaticInitBody = new StringBuilder();

		// 编译单元列表
		public readonly List<CppCompileUnit> CompileUnits = new List<CppCompileUnit>();
		private int CppUnitName;

		public TypeGenerator(TypeManager typeMgr)
		{
			TypeMgr = typeMgr;
			StringGen = new StringGenerator(this);
			MethodGen = new MethodGenerator(typeMgr, StringGen, this);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var unit in CompileUnits)
			{
				sb.AppendFormat("[{0}.h]\n{1}\n",
					unit.Name,
					unit.DeclCode);

				if (unit.ImplCode.Length > 0)
				{
					sb.AppendFormat("[{0}.cpp]\n{1}\n",
						unit.Name,
						unit.ImplCode);
				}
			}
			return sb.ToString();
		}

		public void ToFolder(string folder)
		{
			Directory.CreateDirectory(folder);
			List<string> unitNames = new List<string>();
			foreach (var unit in CompileUnits)
			{
				string path = Path.Combine(folder, unit.Name + ".h");
				File.WriteAllBytes(path, Encoding.UTF8.GetBytes(unit.DeclCode.ToString()));

				if (unit.ImplCode.Length > 0)
				{
					unitNames.Add(unit.Name);
					path = Path.Combine(folder, unit.Name + ".cpp");
					File.WriteAllBytes(path, Encoding.UTF8.GetBytes(unit.ImplCode.ToString()));
				}
			}

			string compileCmd = "clang -O3 -S -emit-llvm il2cpp.cpp main.cpp ";
			string linkCmd = "llvm-link -S -o linked.ll il2cpp.ll main.ll ";
			string optCmd = "opt -O3 -S -o opted.ll linked.ll";
			string genCmd = "clang -g -O3 -o final.exe opted.ll";

			StringBuilder sbShell = new StringBuilder();

			sbShell.Append(compileCmd);
			foreach (var unit in unitNames)
				sbShell.AppendFormat("{0}.cpp ", unit);
			sbShell.AppendLine();

			sbShell.Append(linkCmd);
			foreach (var unit in unitNames)
				sbShell.AppendFormat("{0}.ll ", unit);
			sbShell.AppendLine();

			sbShell.AppendLine(optCmd);

			sbShell.AppendLine(genCmd);

			File.WriteAllText(Path.Combine(folder, "build.cmd"), sbShell.ToString());
		}

		public void GenerateAll()
		{
			NameHelper.Reset();
			CodeMap.Clear();
			StaticInitDecl.Clear();
			StaticInitBody.Clear();
			CompileUnits.Clear();
			CppUnitName = 0;

			// 提升 object 为第一个类型
			var types = new List<TypeX>(TypeMgr.Types);
			for (int i = 0; i < types.Count; ++i)
			{
				var type = types[i];
				if (type.Def.ToTypeSig().IsObjectSig())
				{
					types[i] = types[0];
					types[0] = type;
					break;
				}
			}

			// 生成所有类型
			foreach (var type in types)
			{
				ProcessType(type);
			}

			GenerateCompileUnits();
		}

		private void ProcessType(TypeX currType)
		{
			// 生成类型结构体代码
			TypeCppCode cppCode = GenDeclCode(currType);

			// 生成方法代码
			foreach (var metX in currType.Methods)
			{
				MethodGen.Process(metX);

				cppCode.DeclCode.Append(MethodGen.DeclCode);
				cppCode.ImplCode.Append(MethodGen.ImplCode);

				cppCode.DeclDependNames.UnionWith(MethodGen.DeclDependNames);
				cppCode.ImplDependNames.UnionWith(MethodGen.ImplDependNames);

				cppCode.DependStrings.UnionWith(MethodGen.DependStrings);
			}

			GenIsinstCode(currType, out var declCode, out var implCode);
			cppCode.DeclCode.Append(declCode);
			cppCode.ImplCode.Append(implCode);
		}

		private TypeCppCode GenDeclCode(TypeX currType)
		{
			CodePrinter prt = new CodePrinter();

			// 构造类型注释
			prt.AppendFormatLine("// {0}",
				currType.PrettyName());

			// 构造类型结构体代码
			string typeName;
			string baseTypeName = null;
			if (currType.BaseType != null &&
				!currType.Def.IsValueType)
			{
				baseTypeName = currType.BaseType.GetCppName();
				typeName = currType.GetCppName();
				prt.AppendFormatLine("struct {0} : {1}\n{{",
					typeName,
					baseTypeName);
			}
			else if (currType.Def.ToTypeSig().IsObjectSig())
			{
				typeName = currType.GetCppName();
				prt.AppendFormatLine("struct {0} : il2cppObject\n{{",
					typeName);
			}
			else
			{
				Debug.Assert(
					currType.Def.IsValueType ||
					currType.Def.IsInterface);

				typeName = currType.GetCppName();
				prt.AppendFormatLine("struct {0}\n{{",
					typeName);
			}

			// 添加代码映射
			TypeCppCode cppCode = new TypeCppCode(typeName);
			CodeMap.Add(typeName, cppCode);

			// 添加基类型依赖
			if (baseTypeName != null)
				cppCode.DeclDependNames.Add(baseTypeName);

			++prt.Indents;

			// 构造结构体成员
			List<FieldX> staticFields = new List<FieldX>();
			foreach (var fldX in currType.Fields)
			{
				if (fldX.Def.IsStatic)
				{
					staticFields.Add(fldX);
					continue;
				}

				string fieldTypeName = fldX.FieldType.GetCppName(TypeMgr);
				prt.AppendFormatLine("// {0}\n{1} {2};",
					fldX.PrettyName(),
					fieldTypeName,
					fldX.GetCppName());

				// 添加字段类型依赖
				if (fldX.FieldType.IsValueType)
					cppCode.DeclDependNames.Add(fieldTypeName);
			}

			--prt.Indents;
			prt.AppendLine("};");

			// 构造装箱类型
			if (currType.Def.IsValueType)
			{
				prt.AppendFormatLine("struct box_{0} : il2cppObject\n{{",
					typeName);
				++prt.Indents;

				prt.AppendFormatLine("{0} value;",
					currType.ToTypeSig().GetCppName(TypeMgr));

				--prt.Indents;
				prt.AppendLine("};");
			}

			// 构造静态字段全局变量
			StringBuilder sbImpl = new StringBuilder();
			foreach (var sfldX in staticFields)
			{
				string sfldTypeName = sfldX.FieldType.GetCppName(TypeMgr);
				string sfldCppName = sfldX.GetCppName();
				string sfldPrettyName = sfldX.PrettyName(true);
				string sfldDef = string.Format("{0} {1};\n",
					sfldTypeName,
					sfldCppName);

				prt.AppendFormat("// {0}\nextern {1}",
					sfldPrettyName,
					sfldDef);

				sbImpl.AppendFormat("// {0}\n{1}",
									sfldPrettyName,
									sfldDef);

				StaticInitDecl.Append("extern " + sfldDef);

				StaticInitBody.AppendFormat("{0} = {1};\n",
					sfldCppName,
					sfldX.FieldType.GetInitValue(TypeMgr));

				// 添加字段类型依赖
				if (sfldX.FieldType.IsValueType)
				{
					cppCode.DeclDependNames.Add(sfldTypeName);
					cppCode.ImplDependNames.Add(sfldTypeName);
				}
			}

			// 静态构造函数防止多次调用的标记
			if (currType.CctorMethod != null)
			{
				string onceName = string.Format("onceflag_{0}",
					typeName);
				string onceDef = string.Format("int8_t {0};\n",
					onceName);

				string locktidName = string.Format("locktid_{0}",
					typeName);
				string locktidDef = string.Format("uintptr_t {0};\n",
					locktidName);

				prt.Append("extern " + onceDef);
				prt.Append("extern " + locktidDef);

				sbImpl.Append(onceDef);
				sbImpl.Append(locktidDef);

				StaticInitDecl.Append("extern " + onceDef);
				StaticInitDecl.Append("extern " + locktidDef);

				StaticInitBody.AppendFormat("{0} = 0;\n",
					onceName);
				StaticInitBody.AppendFormat("{0} = 0;\n",
					locktidName);
			}

			cppCode.DeclCode.Append(prt);
			cppCode.ImplCode.Append(sbImpl);

			return cppCode;
		}

		private void GenIsinstCode(TypeX currType, out string codeDecl, out string codeImpl)
		{
			List<TypeX> typeIDs = new List<TypeX>(currType.DerivedTypes);
			typeIDs.Add(currType);
			typeIDs.Sort((x, y) => x.GetCppTypeID().CompareTo(y.GetCppTypeID()));

			CodePrinter prt = new CodePrinter();
			prt.AppendFormat("bool isinst_{0}(uint32_t typeID)",
				currType.GetCppName());

			codeDecl = prt + ";\n";

			prt.AppendLine("\n{");
			++prt.Indents;

			prt.AppendLine("switch (typeID)\n{");
			++prt.Indents;

			foreach (var tyX in typeIDs)
			{
				prt.AppendFormatLine("// {0}\ncase {1}:",
					tyX.PrettyName(),
					tyX.GetCppTypeID());
			}

			++prt.Indents;
			prt.AppendLine("return true;");
			prt.Indents -= 2;

			prt.AppendLine("}");

			prt.AppendLine("return false;");

			--prt.Indents;
			prt.AppendLine("}");

			codeImpl = prt.ToString();
		}

		private CppCompileUnit GetCompileUnit()
		{
			int count = CompileUnits.Count;
			if (count > 0)
			{
				var last = CompileUnits[count - 1];
				if (!last.IsFull())
					return last;
			}
			var unit = new CppCompileUnit("CppUnit_" + CppUnitName++);
			CompileUnits.Add(unit);
			return unit;
		}

		private bool GetCodeFromMap(string typeName, out TypeCppCode typeCode)
		{
			bool res = CodeMap.TryGetValue(typeName, out typeCode);
			if (!res)
				Console.WriteLine("Can't find type: {0}", typeName);
			return res;
		}

		private void GenerateCompileUnits()
		{
			List<TypeCppCode> codeSorter = new List<TypeCppCode>(CodeMap.Values);
			// 构建类型代码依赖关联
			foreach (var cppCode in codeSorter)
			{
				foreach (string typeName in cppCode.DeclDependNames)
				{
					if (GetCodeFromMap(typeName, out var typeCode))
						cppCode.DeclDependTypes.Add(typeCode);
				}
				cppCode.DeclDependNames = null;

				foreach (string typeName in cppCode.ImplDependNames)
				{
					if (GetCodeFromMap(typeName, out var typeCode))
						cppCode.ImplDependTypes.Add(typeCode);
				}
				cppCode.ImplDependNames = null;
			}
			CodeMap.Clear();

			// 统计依赖计数
			foreach (var cppCode in codeSorter)
			{
				// 预生成排序索引
				cppCode.GetSortedID();

				foreach (var typeCode in cppCode.DeclDependTypes)
					++typeCode.DependCounter;

				foreach (var typeCode in cppCode.ImplDependTypes)
					++typeCode.DependCounter;
			}

			// 排序代码
			codeSorter.Sort((x, y) =>
			{
				int cmp = x.GetSortedID().CompareTo(y.GetSortedID());
				if (cmp == 0)
					cmp = y.DependCounter.CompareTo(x.DependCounter);
				return cmp;
			});

			// 划分编译单元
			foreach (var cppCode in codeSorter)
			{
				var unit = GetCompileUnit();
				unit.AddCode(cppCode);
				cppCode.CompileUnit = unit;
			}

			StringGen.GenDefineCode(
				100,
				out var strSplitMap,
				out var strCodeMap,
				out string strTypeDefs);

			// 生成代码
			HashSet<string> dependSet = new HashSet<string>();
			foreach (var unit in CompileUnits)
			{
				// 防止包含自身
				dependSet.Add(unit.Name);

				unit.DeclCode.Append("#pragma once\n");
				unit.DeclCode.Append("#include \"il2cpp.h\"\n");

				foreach (var cppCode in unit.CodeList)
				{
					// 生成头文件依赖包含
					foreach (var typeCode in cppCode.DeclDependTypes)
					{
						string unitName = typeCode.CompileUnit.Name;
						if (!dependSet.Contains(unitName))
						{
							dependSet.Add(unitName);
							unit.DeclCode.AppendFormat("#include \"{0}.h\"\n",
								unitName);
						}
					}
				}
				foreach (var cppCode in unit.CodeList)
				{
					// 拼接声明代码
					unit.DeclCode.Append(cppCode.DeclCode);
					cppCode.DeclCode = null;
					cppCode.DeclDependTypes = null;
				}

				foreach (var cppCode in unit.CodeList)
				{
					// 生成源文件依赖包含
					foreach (var typeCode in cppCode.ImplDependTypes)
					{
						string unitName = typeCode.CompileUnit.Name;
						if (!dependSet.Contains(unitName))
						{
							dependSet.Add(unitName);
							unit.ImplCode.AppendFormat("#include \"{0}.h\"\n",
								unitName);
						}
					}

					// 生成字符串包含
					if (strSplitMap != null &&
						cppCode.DependStrings.Count > 0)
					{
						HashSet<int> strUnitSet = new HashSet<int>();
						foreach (string str in cppCode.DependStrings)
							strUnitSet.Add(strSplitMap[str]);
						cppCode.DependStrings = null;

						foreach (var strUnitId in strUnitSet)
						{
							string strUnit = "StringUnit_" + strUnitId;
							unit.ImplCode.AppendFormat("#include \"{0}.h\"\n",
								strUnit);
						}
					}
				}
				foreach (var cppCode in unit.CodeList)
				{
					// 拼接实现代码
					unit.ImplCode.Append(cppCode.ImplCode);
					cppCode.ImplCode = null;
					cppCode.ImplDependTypes = null;
				}

				// 如果包含内容则追加头文件
				if (unit.ImplCode.Length > 0)
					unit.ImplCode.Insert(0, string.Format("#include \"{0}.h\"\n", unit.Name));

				unit.CodeList = null;
				dependSet.Clear();
			}

			foreach (var item in strCodeMap)
			{
				var strUnit = new CppCompileUnit("StringUnit_" + item.Key);
				CompileUnits.Add(strUnit);
				strUnit.DeclCode.Append("#pragma once\n");
				strUnit.DeclCode.Append("#include \"StringTypes.h\"\n");
				strUnit.DeclCode.Append(item.Value);
			}

			var strTypeDefUnit = new CppCompileUnit("StringTypes");
			CompileUnits.Add(strTypeDefUnit);
			strTypeDefUnit.DeclCode.Append("#pragma once\n");
			strTypeDefUnit.DeclCode.Append("#include \"il2cpp.h\"\n");
			strTypeDefUnit.DeclCode.Append(strTypeDefs);

			// 添加初始化静态变量的函数
			if (StaticInitBody.Length > 0)
			{
				var firstUnit = CompileUnits[0];

				string initDecl = "void il2cpp_InitStaticVars()";
				firstUnit.DeclCode.Append(initDecl + ";\n");

				CodePrinter staticInitPrt = new CodePrinter();
				staticInitPrt.Append(StaticInitDecl.ToString());
				staticInitPrt.Append(initDecl);
				staticInitPrt.AppendLine("\n{");
				++staticInitPrt.Indents;
				staticInitPrt.Append(StaticInitBody.ToString());
				--staticInitPrt.Indents;
				staticInitPrt.AppendLine("}");

				firstUnit.ImplCode.Append(staticInitPrt);
			}
		}
	}
}
