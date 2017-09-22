using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;

namespace il2cpp
{
	internal class TypeGenerator
	{
		private readonly GeneratorContext GenContext;
		private readonly TypeX CurrType;

		public TypeGenerator(GeneratorContext genContext, TypeX tyX)
		{
			GenContext = genContext;
			CurrType = tyX;
		}

		public CompileUnit Generate()
		{
			CompileUnit unit = new CompileUnit();
			unit.Name = GenContext.GetTypeName(CurrType);

			// 生成类结构
			CodePrinter prtDecl = new CodePrinter();

			prtDecl.AppendFormatLine("// {0}", CurrType.GetNameKey());
			var baseType = CurrType.BaseType;

			// 接口类型继承 object
			if (baseType == null && CurrType.Def.IsInterface)
				baseType = GenContext.TypeMgr.GetTypeByName("Object");

			if (baseType != null)
			{
				string strBaseTypeName = GenContext.GetTypeName(baseType);
				unit.DeclDepends.Add(strBaseTypeName);

				prtDecl.AppendFormatLine("struct {0} : {1}",
					GenContext.GetTypeName(CurrType),
					strBaseTypeName);
			}
			else
			{
				prtDecl.AppendFormatLine("struct {0}",
					GenContext.GetTypeName(CurrType));
			}

			prtDecl.AppendLine("{");
			++prtDecl.Indents;

			// 生成对象内置字段
			if (CurrType.GetNameKey() == "Object")
			{
				prtDecl.AppendLine("uint32_t TypeID;");
			}

			// 重排字段
			var fields = LayoutFields();
			// 生成字段
			foreach (var fldX in fields)
			{
				string strFldTypeName = GenContext.GetTypeName(fldX.FieldType);
				if (Helper.IsValueType(fldX.FieldType))
					unit.DeclDepends.Add(strFldTypeName);

				prtDecl.AppendFormatLine("{0} {1};",
					strFldTypeName,
					GenContext.GetFieldName(fldX));
			}

			--prtDecl.Indents;
			prtDecl.AppendLine("};");

			CodePrinter prtImpl = new CodePrinter();

			// 生成方法
			foreach (MethodX metX in CurrType.Methods)
			{
				var metGen = new MethodGenerator(GenContext, metX);
				metGen.Generate();

				prtDecl.AppendFormatLine("// {0} -> {1}", metX.DeclType.GetNameKey(), metX.GetReplacedNameKey());
				prtDecl.AppendLine(metGen.DeclCode);
				unit.DeclDepends.UnionWith(metGen.DeclDepends);

				prtImpl.Append(metGen.ImplCode);
				unit.ImplDepends.UnionWith(metGen.ImplDepends);
			}

			unit.DeclCode = prtDecl.ToString();
			unit.ImplCode = prtImpl.ToString();

			unit.Optimize();

			return unit;
		}

		private List<FieldX> LayoutFields()
		{
			var fields = CurrType.Fields.ToList();

			var layoutType = CurrType.Def.Layout;

			if (layoutType == TypeAttributes.AutoLayout)
			{
				fields.Sort((lhs, rhs) =>
					GenContext.GetTypeLayoutOrder(lhs.FieldType).CompareTo(GenContext.GetTypeLayoutOrder(rhs.FieldType)));
			}
			else if (layoutType == TypeAttributes.SequentialLayout)
			{
				for (int i = 0; i < fields.Count - 1; ++i)
					Debug.Assert(fields[i].Def.Rid < fields[i + 1].Def.Rid);
				//fields.Sort((lhs, rhs) => lhs.Def.Rid.CompareTo(rhs.Def.Rid));
			}
			else if (layoutType == TypeAttributes.ExplicitLayout)
			{
				throw new NotImplementedException();
			}

			return fields;
		}
	}
}
