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
				FieldX fldLen = metX.DeclType.Fields.FirstOrDefault(
					fld => fld.FieldType.ElementType == dnlib.DotNet.ElementType.I4);
				FieldX fldFirstChar = metX.DeclType.Fields.FirstOrDefault(
					fld => fld.FieldType.ElementType == dnlib.DotNet.ElementType.Char);

				if (metName == "get_Length")
				{
					prt.AppendFormatLine(@"return arg_0->{0};",
						genContext.GetFieldName(fldLen));

					return true;
				}
				else if (metName == "get_Chars")
				{
					prt.AppendFormatLine("IL2CPP_CHECK_RANGE(0, arg_0->{0}, arg_1);",
						genContext.GetFieldName(fldLen));
					prt.AppendFormatLine("return ((uint16_t*)&arg_0->{0})[arg_1];",
						genContext.GetFieldName(fldFirstChar));

					return true;
				}
				else if (metName == "InternalMarvin32HashString")
				{
					prt.AppendFormatLine("return il2cpp_HashString(&arg_0->{0}, arg_0->{1});",
						genContext.GetFieldName(fldFirstChar),
						genContext.GetFieldName(fldLen));

					return true;
				}
				else if (metName == "FastAllocateString")
				{
					prt.AppendFormatLine(
						"cls_String* str = (cls_String*)IL2CPP_NEW(sizeof(cls_Object) + sizeof(int32_t) + sizeof(uint16_t) * (arg_0 + 1), {0}, 1);",
						genContext.GetStringTypeID());
					prt.AppendFormatLine("str->{0} = arg_0;",
						genContext.GetFieldName(fldLen));
					prt.AppendLine("return str;");
					return true;
				}
			}
			else if (typeName == "System.Runtime.CompilerServices.RuntimeHelpers")
			{
				if (metName == "IsReferenceOrContainsReferences")
				{
					var targetType = GetMethodGenType(metX, genContext);
					prt.AppendFormatLine("return {0};",
						genContext.IsRefOrContainsRef(targetType) ? "1" : "0");

					return true;
				}
				else if (metName == "CanCompareBits")
				{
					var targetType = GetMethodGenType(metX, genContext);
					bool canCompareBits = true;
					foreach (var fldX in targetType.Fields)
					{
						if (!Helper.IsBasicValueType(fldX.FieldType.ElementType))
						{
							canCompareBits = false;
							break;
						}
					}

					prt.AppendFormatLine("return {0};",
						canCompareBits ? "1" : "0");

					return true;
				}
				else if (metName == "GetInternalTypeID")
				{
					var targetType = GetMethodGenType(metX, genContext);
					prt.AppendFormatLine("return {0};",
						genContext.GetTypeID(targetType));

					return true;
				}
				else if (metName == "FastCompareBits")
				{
					prt.AppendLine("return IL2CPP_MEMCMP(arg_0, arg_1, sizeof(*arg_0)) == 0 ? 1 : 0;");
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
				else if (metName == "Equals")
				{
					prt.AppendLine("return arg_0 == arg_1 ? 1 : 0;");
					return true;
				}
			}
			else if (typeName == "System.Buffer")
			{
				if (metName == "__Memmove")
				{
					prt.AppendLine("IL2CPP_MEMMOVE(arg_0, arg_1, arg_2);");
					return true;
				}
			}
			else if (typeName == "System.Math")
			{
				if (metName == "Abs")
				{
					prt.AppendLine("return il2cpp_Abs(arg_0);");
					return true;
				}
				else if (metName == "Sqrt")
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

		private static TypeX GetMethodGenType(MethodX metX, GeneratorContext genContext)
		{
			Debug.Assert(metX.HasGenArgs && metX.GenArgs.Count == 1);
			TypeX genType = genContext.GetTypeBySig(metX.GenArgs[0]);
			Debug.Assert(genType != null);
			return genType;
		}
	}
}
