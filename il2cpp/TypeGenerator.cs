using System;
using System.Collections.Generic;
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

		public CppCompileUnit(string name)
		{
			Name = name;
		}

		public bool IsFull()
		{
			return ImplCode.Length > 50000 ||
				   DeclCode.Length > 10000;
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

		// 静态变量初始化代码体
		private StringBuilder StaticInitBody = new StringBuilder();

		// 编译单元列表
		public readonly List<CppCompileUnit> CompileUnits = new List<CppCompileUnit>();
		private int CppUnitName;

		// 是否把所有代码都生成到一个文件
		public bool IsAllInOne = false;

		public TypeGenerator(TypeManager typeMgr)
		{
			TypeMgr = typeMgr;
			MethodGen = new MethodGenerator(typeMgr, this);
		}

		public void GenerateAll()
		{
			NameHelper.Reset();
			CodeMap.Clear();
			StaticInitBody.Clear();
			CompileUnits.Clear();
			CppUnitName = 0;

			// 提升 object 为第一个类型
			var types = new List<TypeX>(TypeMgr.Types);
			for (int i = 0; i < types.Count; ++i)
			{
				var type = types[i];
				if (type.Def.ToTypeSig().ElementType == ElementType.Object)
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
			}
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

			// object 添加类型字段
			if (currType.Def.ToTypeSig().ElementType == ElementType.Object)
			{
				prt.AppendLine("uint32_t il2cppTypeID;");
			}

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

			// 构造静态字段全局变量
			StringBuilder sbImpl = IsAllInOne ? null : new StringBuilder();
			foreach (var sfldX in staticFields)
			{
				string sfldTypeName = sfldX.FieldType.GetCppName(TypeMgr);
				string sfldCppName = sfldX.GetCppName();
				string sfldPrettyName = sfldX.PrettyName(true);
				string sfldDef = string.Format("{0} {1};\n",
					sfldTypeName,
					sfldCppName);

				prt.AppendFormat("// {0}\n{1}{2}",
					sfldPrettyName,
					IsAllInOne ? "static " : "extern ",
					sfldDef);

				if (!IsAllInOne)
				{
					sbImpl.AppendFormat("// {0}\n{1}",
										sfldPrettyName,
										sfldDef);
				}

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
					currType.GetCppName());
				string onceDef = string.Format("int32_t {0};\n",
					onceName);

				prt.Append((IsAllInOne ? "static " : "extern ") + onceDef);

				if (!IsAllInOne)
				{
					sbImpl.Append(onceDef);
				}

				StaticInitBody.AppendFormat("{0} = 0;\n",
					onceName);
			}

			cppCode.DeclCode.Append(prt);
			if (!IsAllInOne)
				cppCode.ImplCode.Append(sbImpl);

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

			if (IsAllInOne)
			{
				var unit = new CppCompileUnit("CppUnitAll");
				CompileUnits.Add(unit);

				unit.DeclCode.Append("#include \"il2cpp.h\"\n");

				foreach (var cppCode in codeSorter)
				{
					unit.DeclCode.Append(cppCode.DeclCode);
					unit.ImplCode.Append(cppCode.ImplCode);

					cppCode.DeclCode = null;
					cppCode.DeclDependTypes = null;
					cppCode.ImplCode = null;
					cppCode.ImplDependTypes = null;
				}

				unit.DeclCode.Append(unit.ImplCode);
				unit.ImplCode = unit.DeclCode;
				unit.DeclCode = null;
			}
			else
			{
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
					// 防止包含自身
					dependSet.Add(unit.Name);

					unit.DeclCode.Append("#pragma once\n");
					unit.DeclCode.Append("#include \"il2cpp.h\"\n");
					unit.ImplCode.AppendFormat("#include \"{0}.h\"\n", unit.Name);

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

						// 拼接实现代码
						unit.ImplCode.Append(cppCode.ImplCode);
						cppCode.ImplCode = null;
						cppCode.ImplDependTypes = null;
					}
					unit.CodeList = null;
					dependSet.Clear();
				}
			}

			// 添加初始化静态变量的函数
			if (StaticInitBody.Length > 0)
			{
				var firstUnit = CompileUnits[0];

				CodePrinter staticInitPrt = new CodePrinter();
				staticInitPrt.AppendFormat("{0}void il2cpp_InitStaticVars()",
					IsAllInOne ? "static " : "");

				if (!IsAllInOne)
				{
					firstUnit.DeclCode.AppendFormat("{0};\n", staticInitPrt);
				}

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
