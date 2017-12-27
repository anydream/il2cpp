using System.Diagnostics;
using System.Linq;

namespace il2cpp
{
	internal static class RuntimeInternals
	{
		public static bool GenInternalMethod(MethodX metX, CodePrinter prt, GeneratorContext genContext)
		{
			string typeName = metX.DeclType.GetNameKey();
			string metName = metX.Def.Name;

			if (typeName == "Object")
			{
				if (metName == "GetInternalTypeID")
				{
					prt.AppendLine("return (int32_t)arg_0->TypeID;");
					return true;
				}
			}
			else if (typeName == "String")
			{
				if (metName == "get_Length")
				{
					FieldX fldLen = metX.DeclType.Fields.FirstOrDefault(
						fld => fld.FieldType.ElementType == dnlib.DotNet.ElementType.I4);
					prt.AppendFormatLine(@"return arg_0->{0};",
						genContext.GetFieldName(fldLen));

					return true;
				}
				else if (metName == "get_Chars")
				{
					FieldX fldLen = metX.DeclType.Fields.FirstOrDefault(
						fld => fld.FieldType.ElementType == dnlib.DotNet.ElementType.I4);
					FieldX fldFirstChar = metX.DeclType.Fields.FirstOrDefault(
						fld => fld.FieldType.ElementType == dnlib.DotNet.ElementType.Char);

					prt.AppendFormatLine("IL2CPP_CHECK_RANGE(0, arg_0->{0}, arg_1);",
						genContext.GetFieldName(fldLen));
					prt.AppendFormatLine("return ((uint16_t*)&arg_0->{0})[arg_1];",
						genContext.GetFieldName(fldFirstChar));

					return true;
				}
				else if (metName == "InternalMarvin32HashString")
				{
					FieldX fldLen = metX.DeclType.Fields.FirstOrDefault(
						fld => fld.FieldType.ElementType == dnlib.DotNet.ElementType.I4);
					FieldX fldFirstChar = metX.DeclType.Fields.FirstOrDefault(
						fld => fld.FieldType.ElementType == dnlib.DotNet.ElementType.Char);

					prt.AppendFormatLine("return il2cpp_HashString(&arg_0->{0}, arg_0->{1});",
						genContext.GetFieldName(fldFirstChar),
						genContext.GetFieldName(fldLen));

					return true;
				}
			}
			else if (typeName == "System.Runtime.CompilerServices.RuntimeHelpers")
			{
				if (metName == "IsReferenceOrContainsReferences")
				{
					Debug.Assert(metX.HasGenArgs && metX.GenArgs.Count == 1);
					TypeX targetType = genContext.GetTypeBySig(metX.GenArgs[0]);
					prt.AppendFormatLine("return {0};",
						genContext.IsRefOrContainsRef(targetType) ? "1" : "0");

					return true;
				}
				else if (metName == "InitializeArray")
				{
					TypeX tyArg1 = genContext.GetTypeBySig(metX.ParamTypes[1]);
					Debug.Assert(tyArg1 != null);
					FieldX rtFldX = tyArg1.Fields.First();

					prt.AppendFormatLine("il2cpp_Array__Init(arg_0, (il2cppFieldInfo*)arg_1.{0});",
						genContext.GetFieldName(rtFldX));

					return true;
				}
				else if (metName == "GetInternalTypeID")
				{
					return true;
				}
				else if (metName == "FastCompareBits")
				{
					return true;
				}
				else if (metName == "Equals")
				{
					prt.AppendLine("return arg_0 == arg_1 ? 1 : 0;");
					return true;
				}
			}
			else if (typeName == "System.Math")
			{
				if (metName == "Sqrt")
				{
					prt.AppendLine("return il2cpp_Sqrt(arg_0);");
					return true;
				}
				else if (metName == "Sin")
				{
					prt.AppendLine("return il2cpp_Sin(arg_0);");
					return true;
				}
				else if (metName == "Cos")
				{
					prt.AppendLine("return il2cpp_Cos(arg_0);");
					return true;
				}
				else if (metName == "Tan")
				{
					prt.AppendLine("return il2cpp_Tan(arg_0);");
					return true;
				}
				else if (metName == "Pow")
				{
					prt.AppendLine("return il2cpp_Pow(arg_0, arg_1);");
					return true;
				}
			}

			return false;
		}
	}
}
