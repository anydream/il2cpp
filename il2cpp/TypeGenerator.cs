using System;
using System.Collections.Generic;

namespace il2cpp
{
	// 类型对应的代码
	internal class TypeCppCode
	{
		// 类型名
		public readonly string Name;
		// 声明代码
		public string DeclCode;
		// 实现代码
		public string ImplCode;
		// 声明依赖的类型
		public HashSet<string> DeclDependNames = new HashSet<string>();
		public HashSet<TypeCppCode> DeclDependTypes = new HashSet<TypeCppCode>();
		// 实现依赖的类型
		public HashSet<string> ImplDependNames = new HashSet<string>();
		public HashSet<TypeCppCode> ImplDependTypes = new HashSet<TypeCppCode>();

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
	internal class CppCompileUnit
	{
		// 单元名
		public readonly string Name;
		// 声明代码
		public readonly CodePrinter DeclCode = new CodePrinter();
		// 实现代码
		public readonly CodePrinter ImplCode = new CodePrinter();
		// 包含的代码对象
		public List<TypeCppCode> CodeList = new List<TypeCppCode>();

		public CppCompileUnit(string name)
		{
			Name = name;
		}

		public bool IsFull()
		{
			return ImplCode.Length > 3000 ||
				   DeclCode.Length > 1000;
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

		// 编译单元列表
		private readonly List<CppCompileUnit> CompileUnits = new List<CppCompileUnit>();
		private int CppUnitName;

		public TypeGenerator(TypeManager typeMgr)
		{
			TypeMgr = typeMgr;
			MethodGen = new MethodGenerator(typeMgr);
		}

		public void GenerateAll()
		{
			NameHelper.Reset();
			CodeMap.Clear();
			CompileUnits.Clear();
			CppUnitName = 0;

			// 生成所有类型
			foreach (var type in TypeMgr.Types)
			{
				ProcessType(type);
			}

			GenerateCompileUnits();


			foreach (var unit in CompileUnits)
			{
				Console.WriteLine("[{0}.h]\n{1}\n[{0}.cpp]\n{2}",
					unit.Name,
					unit.DeclCode,
					unit.ImplCode);
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

				cppCode.DeclDependNames.UnionWith(MethodGen.DeclDependNames);
				cppCode.ImplDependNames.UnionWith(MethodGen.ImplDependNames);
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
				cppCode.DeclDependNames.Add(baseTypeName);

			++prt.Indents;

			// 构造结构体成员
			foreach (var fldX in currType.Fields)
			{
				string fieldTypeName = fldX.FieldType.GetCppName(TypeMgr);
				prt.AppendFormatLine("// {0}\n{1} {2};",
					fldX.FullName,
					fieldTypeName,
					fldX.GetCppName());

				// 添加字段类型依赖
				if (fldX.FieldType.IsValueType)
					cppCode.DeclDependNames.Add(fieldTypeName);
			}

			--prt.Indents;
			prt.AppendLine("};");

			cppCode.DeclCode = prt.ToString();
			return cppCode;
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
			return CodeMap.TryGetValue(typeName, out typeCode);
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
				unit.CodeList.Add(cppCode);
				cppCode.CompileUnit = unit;
			}

			// 生成代码
			HashSet<string> dependSet = new HashSet<string>();
			foreach (var unit in CompileUnits)
			{
				unit.DeclCode.AppendLine("#pragma once");
				unit.ImplCode.AppendFormatLine("#include \"{0}.h\"", unit.Name);

				foreach (var cppCode in unit.CodeList)
				{
					// 生成头文件依赖包含
					foreach (var typeCode in cppCode.DeclDependTypes)
					{
						string unitName = typeCode.CompileUnit.Name;
						if (!dependSet.Contains(unitName))
						{
							dependSet.Add(unitName);
							unit.DeclCode.AppendFormatLine("#include \"{0}.h\"",
								unitName);
						}
					}

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
							unit.ImplCode.AppendFormatLine("#include \"{0}.h\"",
								unitName);
						}
					}

					// 拼接实现代码
					unit.ImplCode.Append(cppCode.ImplCode);
					cppCode.ImplCode = null;
					cppCode.ImplDependTypes = null;
				}
				unit.CodeList = null;
				dependSet.Clear();
			}
		}
	}
}
