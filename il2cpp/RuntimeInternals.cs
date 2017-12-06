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

			if (typeName == "String")
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
			}
			else if (typeName == "System.Runtime.CompilerServices.RuntimeHelpers")
			{
				if (metX.Def.Name == "IsReferenceOrContainsReferences")
				{
					Debug.Assert(metX.HasGenArgs && metX.GenArgs.Count == 1);
					TypeX targetType = genContext.GetTypeBySig(metX.GenArgs[0]);
					prt.AppendFormatLine("return {0};",
						genContext.IsRefOrContainsRef(targetType) ? "1" : "0");

					return true;
				}
			}
			return false;
		}
	}
}
