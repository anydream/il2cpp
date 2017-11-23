using System;
using System.Collections.Generic;
using System.Diagnostics;
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

			// 重排字段
			var fields = LayoutFields(out var sfields);

			string nameKey = CurrType.GetNameKey();
			bool currIsObject = nameKey == "Object";

			// 生成类结构
			CodePrinter prtDecl = new CodePrinter();

			if (!CurrType.IsEnumType)
			{
				prtDecl.AppendFormatLine("// {0}", nameKey);
				var baseType = CurrType.BaseType;

				// 值类型不继承任何基类
				if (CurrType.IsValueType)
					baseType = null;

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
				if (CurrType.IsArrayType)
				{
					var arrayInfo = CurrType.ArrayInfo;
					if (arrayInfo.IsSZArray)
						prtDecl.AppendLine("uint32_t Length;");
					else
					{
						uint rank = arrayInfo.Rank;
						for (int i = 0; i < rank; ++i)
							prtDecl.AppendFormatLine("int32_t LowerBound{0};\nuint32_t Size{0};", i);
					}
				}
				else
				{
					if (currIsObject)
					{
						prtDecl.AppendLine("uint32_t TypeID;");
					}
					else if (nameKey == "System.Array")
					{
						prtDecl.AppendLine("uint32_t ElemSize;");
						prtDecl.AppendLine("uint32_t Rank;");
					}
				}

				bool isExplicitLayout = CurrType.Def.IsExplicitLayout;
				uint classSize = CurrType.Def.ClassSize;
				bool hasStructSize = CurrType.Def.HasClassLayout && classSize != 0;
				if (isExplicitLayout || hasStructSize)
				{
					prtDecl.AppendLine("union\n{");
					++prtDecl.Indents;

					if (!isExplicitLayout && fields.Count != 0)
					{
						prtDecl.AppendLine("struct\n{");
						++prtDecl.Indents;
					}
				}

				int fldCounter = 0;

				// 生成字段
				foreach (var fldX in fields)
				{
					RefValueTypeDecl(unit, fldX.FieldType);

					bool isFieldExLayout = isExplicitLayout && fldX.Def.FieldOffset != 0;
					if (isFieldExLayout)
					{
						prtDecl.AppendLine("struct\n{");
						++prtDecl.Indents;
						prtDecl.AppendFormatLine("uint8_t padding_{0}[{1}];",
							fldCounter,
							fldX.Def.FieldOffset);
					}

					prtDecl.AppendLine("// " + fldX.GetReplacedNameKey());
					prtDecl.AppendFormatLine("{0} {1};",
						GenContext.GetTypeName(fldX.FieldType),
						GenContext.GetFieldName(fldX));

					if (isFieldExLayout)
					{
						--prtDecl.Indents;
						prtDecl.AppendLine("};");
					}

					++fldCounter;
				}

				if (isExplicitLayout || hasStructSize)
				{
					if (!isExplicitLayout && fields.Count != 0)
					{
						--prtDecl.Indents;
						prtDecl.AppendLine("};");
					}
					if (hasStructSize)
					{
						prtDecl.AppendFormatLine("uint8_t padding_struct[{0}];",
							classSize);
					}

					--prtDecl.Indents;
					prtDecl.AppendLine("};");
				}

				--prtDecl.Indents;
				prtDecl.AppendLine("};");
			}

			CodePrinter prtImpl = new CodePrinter();

			// 生成类型判断函数
			GenerateIsType(prtDecl, prtImpl, currIsObject);

			// 生成静态字段
			foreach (var sfldX in sfields)
			{
				RefValueTypeDecl(unit, sfldX.FieldType);

				string fldDecl = string.Format("{0} {1};",
					GenContext.GetTypeName(sfldX.FieldType),
					GenContext.GetFieldName(sfldX));

				prtDecl.AppendFormatLine("// {0} -> {1}", sfldX.DeclType.GetNameKey(), sfldX.GetReplacedNameKey());
				prtDecl.AppendLine("extern " + fldDecl);
				prtImpl.AppendLine(fldDecl);
			}

			// 生成方法
			foreach (MethodX metX in CurrType.Methods)
			{
				var metGen = new MethodGenerator(GenContext, metX);
				metGen.Generate();

				AppendRuntimeFlags(metX, prtDecl);

				prtDecl.AppendFormatLine("// {0}{1}{2} -> {3}",
					!metX.Def.HasBody && !metX.Def.IsAbstract ? "extern " : null,
					metX.Def.IsInternalCall ? "internalcall " : null,
					metX.DeclType.GetNameKey(),
					metX.GetReplacedNameKey());

				prtDecl.Append(metGen.DeclCode);
				unit.DeclDepends.UnionWith(metGen.DeclDepends);

				prtImpl.Append(metGen.ImplCode);
				unit.ImplDepends.UnionWith(metGen.ImplDepends);

				unit.StringDepends.UnionWith(metGen.StringDepends);
			}

			unit.DeclCode = prtDecl.ToString();
			unit.ImplCode = prtImpl.ToString();

			return unit;
		}

		private void GenerateIsType(CodePrinter prtDecl, CodePrinter prtImpl, bool currIsObject)
		{
			if (CurrType.IsValueType)
				return;

			CodePrinter prt = new CodePrinter();

			prt.AppendFormat("uint8_t istype_{0}(uint32_t typeID)",
				GenContext.GetTypeName(CurrType));

			string strDecl = prt.ToString() + ";\n";

			prt.AppendLine("\n{");
			++prt.Indents;

			var derivedRange = new List<TypeX>(CurrType.DerivedTypes);
			derivedRange.Add(CurrType);

			List<TypeX> derTypes = new List<TypeX>();
			foreach (var derTyX in derivedRange)
			{
				// 跳过不分配在堆上的类型
				if (!derTyX.IsInstantiated || derTyX.Def.IsInterface)
					continue;
				// 如果当前类型是 object, 则跳过值类型
				if (currIsObject && derTyX.IsValueType)
					continue;
				derTypes.Add(derTyX);
			}

			if (derTypes.Count > 0)
			{
				prt.AppendLine("switch (typeID)\n{");
				++prt.Indents;

				derTypes.Sort((lhs, rhs) =>
					GenContext.GetTypeID(lhs).CompareTo(GenContext.GetTypeID(rhs)));

				foreach (var derTyX in derTypes)
				{
					prt.AppendFormatLine("// {0}",
						derTyX.GetNameKey());
					prt.AppendFormatLine("case {0}:",
						GenContext.GetTypeID(derTyX));
				}

				++prt.Indents;
				prt.AppendLine("return 1;");
				--prt.Indents;

				--prt.Indents;
				prt.AppendLine("}");
			}

			prt.AppendLine("return 0;");

			--prt.Indents;
			prt.AppendLine("}");

			prtDecl.Append(strDecl);
			prtImpl.Append(prt.ToString());
		}

		private void RefValueTypeDecl(CompileUnit unit, TypeSig tySig)
		{
			if (!tySig.IsValueType)
				return;

			TypeX tyX = GenContext.GetTypeBySig(tySig);
			if (tyX != null)
				unit.DeclDepends.Add(GenContext.GetTypeName(tyX));
		}

		private List<FieldX> LayoutFields(out List<FieldX> sfields)
		{
			sfields = new List<FieldX>();

			List<FieldX> fields = new List<FieldX>();
			foreach (var fldX in CurrType.Fields)
			{
				if (fldX.IsStatic)
					sfields.Add(fldX);
				else
					fields.Add(fldX);
			}

			var layoutType = CurrType.Def.Layout;

			string typeName = CurrType.GetNameKey();
			if (typeName == "String" || typeName == "System.Array")
				layoutType = TypeAttributes.SequentialLayout;

			if (layoutType == TypeAttributes.AutoLayout)
			{
				fields.Sort((lhs, rhs) =>
					GenContext.GetTypeLayoutOrder(rhs.FieldType).CompareTo(GenContext.GetTypeLayoutOrder(lhs.FieldType)));
			}
			else if (layoutType == TypeAttributes.SequentialLayout ||
					 layoutType == TypeAttributes.ExplicitLayout)
			{
				fields.Sort((lhs, rhs) => lhs.Def.Rid.CompareTo(rhs.Def.Rid));

				for (int i = 0; i < fields.Count - 1; ++i)
					Debug.Assert(fields[i].Def.Rid < fields[i + 1].Def.Rid);
			}

			return fields;
		}

		private void AppendRuntimeFlags(MethodX metX, CodePrinter prt)
		{
			string typeName = metX.DeclType.GetNameKey();
			if (typeName == "il2cpprt.ThrowHelper")
			{
				prt.AppendFormatLine("#define IL2CPP_BRIDGE_HAS_{0}",
					GenContext.GetMethodName(metX, null));
			}
		}
	}
}
