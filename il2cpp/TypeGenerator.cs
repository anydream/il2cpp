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
			string strTypeName = GenContext.GetTypeName(CurrType, false);
			unit.Name = strTypeName;

			// 重排字段
			var fields = LayoutFields(out var sfields);

			string nameKey = CurrType.GetNameKey();
			bool currIsObject = nameKey == "Object";

			// 生成类结构
			CodePrinter prtDecl = new CodePrinter();

			if (!CurrType.IsEnumType)
			{
				prtDecl.AppendFormatLine("// {0}", Helper.EscapeString(nameKey));

				ushort packSize = CurrType.Def.HasClassLayout ? CurrType.Def.PackingSize : (ushort)0;
				if (packSize > 2 && !Helper.IsPowerOfTwo(packSize))
					throw new TypeLoadException();

				if (packSize != 0)
				{
					prtDecl.AppendLine("#if defined(IL2CPP_MSVC_LIKE)");
					prtDecl.AppendFormatLine("#pragma pack(push, {0})", packSize);
					prtDecl.AppendLine("#endif");
				}

				var baseType = CurrType.BaseType;

				// 值类型不继承任何基类
				if (CurrType.IsValueType)
					baseType = null;

				// 接口类型继承 object
				if (baseType == null && CurrType.Def.IsInterface)
					baseType = GenContext.GetTypeByName("Object");

				if (baseType != null)
				{
					string strBaseTypeName = GenContext.GetTypeName(baseType);
					unit.DeclDepends.Add(strBaseTypeName);

					prtDecl.AppendFormatLine("struct {0} : {1}",
						strTypeName,
						strBaseTypeName);
				}
				else
				{
					prtDecl.AppendFormatLine("struct {0}",
						strTypeName);
				}

				prtDecl.AppendLine("{");
				++prtDecl.Indents;

				// 生成对象内置字段
				if (CurrType.IsArrayType)
				{
					var arrayInfo = CurrType.ArrayInfo;
					if (!arrayInfo.IsSZArray)
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
						prtDecl.AppendLine("uint8_t Flags[4];");
					}
					else if (nameKey == "System.Array")
					{
						prtDecl.AppendLine("uint32_t Length;");
						prtDecl.AppendLine("uint32_t ElemSize : 24;");
						prtDecl.AppendLine("uint32_t Rank : 8;");
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

				if (packSize != 0)
				{
					prtDecl.AppendFormatLine("}} IL2CPP_PACKED_TAIL({0});",
						packSize);
				}
				else
					prtDecl.AppendLine("};");

				if (packSize != 0)
				{
					prtDecl.AppendLine("#if defined(IL2CPP_MSVC_LIKE)");
					prtDecl.AppendLine("#pragma pack(pop)");
					prtDecl.AppendLine("#endif");
				}
			}

			CodePrinter prtImpl = new CodePrinter();

			// 生成静态字段
			foreach (var sfldX in sfields)
			{
				RefValueTypeDecl(unit, sfldX.FieldType);

				string sfldName = GenContext.GetFieldName(sfldX);
				string fldDecl = string.Format("{0} {1};",
					GenContext.GetTypeName(sfldX.FieldType),
					sfldName);

				prtDecl.AppendFormatLine("// {0} -> {1}",
					Helper.EscapeString(sfldX.DeclType.GetNameKey()),
					Helper.EscapeString(sfldX.GetReplacedNameKey()));
				prtDecl.AppendLine("extern " + fldDecl);
				prtImpl.AppendLine(fldDecl);

				bool hasRef = GenContext.IsRefOrContainsRef(GenContext.GetTypeBySig(sfldX.FieldType));
				GenContext.AddStaticField(strTypeName, sfldName, hasRef);
			}

			// 生成类型判断函数
			GenIsTypeFunc(prtDecl, prtImpl, currIsObject);

			// 生成方法
			foreach (MethodX metX in CurrType.Methods)
			{
				var metGen = new MethodGenerator(GenContext, metX);
				metGen.Generate();

				AppendRuntimeFlags(metX, prtDecl);

				prtDecl.AppendFormatLine("// {0}{1}{2} -> {3}",
					Helper.IsExtern(metX.Def) ? "extern " : null,
					metX.Def.IsInternalCall ? "internalcall " : null,
					Helper.EscapeString(metX.DeclType.GetNameKey()),
					Helper.EscapeString(metX.GetReplacedNameKey()));

				prtDecl.Append(metGen.DeclCode);
				unit.DeclDepends.UnionWith(metGen.DeclDepends);

				prtImpl.Append(metGen.ImplCode);
				unit.ImplDepends.UnionWith(metGen.ImplDepends);

				unit.StringDepends.UnionWith(metGen.StringDepends);
			}

			GenerateMetadata(prtDecl, prtImpl);

			unit.DeclCode = prtDecl.ToString();
			unit.ImplCode = prtImpl.ToString();

			return unit;
		}

		private void GenIsTypeFunc(CodePrinter prtDecl, CodePrinter prtImpl, bool currIsObject)
		{
			if (CurrType.IsValueType || !CurrType.NeedGenIsType)
				return;

			CodePrinter prt = new CodePrinter();

			prt.AppendFormat("uint8_t {0}(uint32_t typeID)",
				GenContext.GetIsTypeFuncName(CurrType));

			string strDecl = prt.ToString() + ";\n";

			prt.AppendLine("\n{");
			++prt.Indents;

			var derivedRange = new List<TypeX>(CurrType.DerivedTypes);
			derivedRange.Add(CurrType);

			var derivedEnumTypes = CurrType.UnBoxedType?.DerivedEnumTypes;
			if (derivedEnumTypes != null)
				derivedRange.AddRange(derivedEnumTypes);

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
						Helper.EscapeString(derTyX.GetNameKey()));
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
			if (tyX != null && !tyX.IsBasicType())
				unit.DeclDepends.Add(GenContext.GetTypeName(tyX));
		}

		private List<FieldX> LayoutFields(out List<FieldX> sfields)
		{
			sfields = new List<FieldX>();
			List<FieldX> fields = new List<FieldX>();

			foreach (var fldX in CurrType.Fields)
			{
				if (fldX.Def.IsLiteral)
					continue;

				if (fldX.IsStatic)
					sfields.Add(fldX);
				else if (fldX.IsInstance)
					fields.Add(fldX);
			}

			var layoutType = CurrType.Def.Layout;

			string typeName = CurrType.GetNameKey();
			if (typeName == "String" || typeName == "System.Array")
				layoutType = TypeAttributes.SequentialLayout;

			if (layoutType == TypeAttributes.AutoLayout)
			{
				fields.Sort((lhs, rhs) =>
				{
					int cmp = GenContext.GetTypeLayoutOrder(rhs.FieldType).CompareTo(GenContext.GetTypeLayoutOrder(lhs.FieldType));
					if (cmp == 0)
						return lhs.Def.Rid.CompareTo(rhs.Def.Rid);
					return cmp;
				});
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

		private void GenerateMetadata(CodePrinter prtDecl, CodePrinter prtImpl)
		{
			if (CurrType.GenMetadata)
			{
				string strMDataName = GenContext.GetMetaName(CurrType, true);
				string strDecl = string.Format("il2cppTypeInfo {0}",
					GenContext.GetMetaName(CurrType));
				prtDecl.AppendFormatLine("extern {0};", strDecl);

				string varName = GenMetaStringLiteral(prtImpl, "Name", CurrType.Def.Name, strMDataName);
				string varNamespace = GenMetaStringLiteral(prtImpl, "Namespace", CurrType.Def.Namespace, strMDataName);

				prtImpl.AppendFormatLine("{0} =", strDecl);
				prtImpl.AppendLine("{");
				++prtImpl.Indents;
				prtImpl.AppendFormatLine("(uint16_t*){0},", varName);
				prtImpl.AppendFormatLine("(uint16_t*){0},", varNamespace);
				--prtImpl.Indents;
				prtImpl.AppendLine("};");
			}

			foreach (var metX in CurrType.Methods)
			{
				if (metX.GenMetadata)
				{
					string strMDataName = GenContext.GetMetaName(metX, true);
					string strDecl = string.Format("il2cppMethodInfo {0}",
						GenContext.GetMetaName(metX));
					prtDecl.AppendFormatLine("extern {0};", strDecl);

					string varName = GenMetaStringLiteral(prtImpl, "Name", metX.Def.Name, strMDataName);

					prtImpl.AppendFormatLine("{0} =", strDecl);
					prtImpl.AppendLine("{");
					++prtImpl.Indents;
					prtImpl.AppendFormatLine("(uint16_t*){0},", varName);
					--prtImpl.Indents;
					prtImpl.AppendLine("};");
				}
			}

			foreach (var fldX in CurrType.Fields)
			{
				if (fldX.GenMetadata)
				{
					string strMDataName = GenContext.GetMetaName(fldX, true);
					string strDecl = string.Format("il2cppFieldInfo {0}",
						GenContext.GetMetaName(fldX));
					prtDecl.AppendFormatLine("extern {0};", strDecl);

					string varName = GenMetaStringLiteral(prtImpl, "Name", fldX.Def.Name, strMDataName);

					var initValue = fldX.Def.InitialValue;
					bool hasInitValue = initValue != null && initValue.Length > 0;

					string varInitData = null;
					if (hasInitValue)
					{
						varInitData = GenMetaBytesLiteral(prtImpl, "InitData", initValue, strMDataName);
					}

					prtImpl.AppendFormatLine("{0} =", strDecl);
					prtImpl.AppendLine("{");
					++prtImpl.Indents;
					prtImpl.AppendFormatLine("(uint16_t*){0},", varName);
					prtImpl.AppendLine("nullptr,");
					prtImpl.AppendLine("nullptr,");
					prtImpl.AppendLine("nullptr,");

					prtImpl.AppendLine("{");
					++prtImpl.Indents;
					if (hasInitValue)
					{
						prtImpl.AppendFormatLine("{0},\n{1}",
							varInitData,
							initValue.Length);
					}
					else
					{
						prtImpl.AppendLine("nullptr,\n0");
					}
					--prtImpl.Indents;
					prtImpl.AppendLine("},");

					prtImpl.AppendFormatLine("{0},",
						(uint)fldX.Def.Attributes);

					if (fldX.IsInstance && !CurrType.IsEnumType)
					{
						prtImpl.AppendFormatLine("IL2CPP_OFFSETOF(&{0}::{1})",
							GenContext.GetTypeName(CurrType),
							GenContext.GetFieldName(fldX));
					}
					else
					{
						prtImpl.AppendLine("0");
					}

					--prtImpl.Indents;
					prtImpl.AppendLine("};");
				}
			}
		}

		private static string GenMetaStringLiteral(CodePrinter prt, string postfix, string str, string mdataName)
		{
			string strNameData = StringGenerator.StringToArrayOrRaw(str, out bool isRaw);
			string varName = string.Format("{0}_{1}",
				mdataName,
				postfix);

			prt.AppendFormatLine("static const {0} {1}[] = {2};",
				isRaw ? "char16_t" : "uint16_t",
				varName,
				strNameData);

			return varName;
		}

		private static string GenMetaBytesLiteral(CodePrinter prt, string postfix, byte[] data, string mdataName)
		{
			string varName = string.Format("{0}_{1}",
				mdataName,
				postfix);

			prt.AppendFormatLine("static const uint8_t {0}_InitData[] = {1};",
				mdataName,
				Helper.ByteArrayToCode(data));

			return varName;
		}
	}
}
