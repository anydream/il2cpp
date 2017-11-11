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
			else if (typeName == "System.Array")
			{
				if (metName == "get_Rank")
				{
					prt.AppendLine(
						@"if (arg_0->Rank == 0)
	return 1;
return arg_0->Rank;");

					return true;
				}
				else if (metName == "get_Length")
				{
					prt.AppendLine(
						@"if (arg_0->Rank == 0)
	return ((int32_t*)&arg_0[1])[0];
else
{
	int32_t length = 1;
	for (int32_t i = 0, sz = arg_0->Rank; i < sz; ++i)
		length *= ((int32_t*)&arg_0[1])[i * 2 + 1];
	return length;
}");

					return true;
				}
				else if (metName == "get_LongLength")
				{
					prt.AppendLine(
						@"if (arg_0->Rank == 0)
	return ((int32_t*)&arg_0[1])[0];
else
{
	int64_t length = 1;
	for (int32_t i = 0, sz = arg_0->Rank; i < sz; ++i)
		length *= ((int32_t*)&arg_0[1])[i * 2 + 1];
	return length;
}");

					return true;
				}
				else if (metName == "GetLength")
				{
					prt.AppendLine(
						@"if (arg_0->Rank == 0)
{
	IL2CPP_CHECK_RANGE(0, 1, arg_1);
	return ((int32_t*)&arg_0[1])[0];
}
else
{
	IL2CPP_CHECK_RANGE(0, arg_0->Rank, arg_1);
	return ((int32_t*)&arg_0[1])[arg_1 * 2 + 1];
}");

					return true;
				}
				else if (metName == "GetLowerBound")
				{
					prt.AppendLine(
						@"if (arg_0->Rank == 0)
{
	IL2CPP_CHECK_RANGE(0, 1, arg_1);
	return 0;
}
else
{
	IL2CPP_CHECK_RANGE(0, arg_0->Rank, arg_1);
	return ((int32_t*)&arg_0[1])[arg_1 * 2];
}");

					return true;
				}
				else if (metName == "GetUpperBound")
				{
					prt.AppendLine(
						@"if (arg_0->Rank == 0)
{
	IL2CPP_CHECK_RANGE(0, 1, arg_1);
	return ((int32_t*)&arg_0[1])[0] - 1;
}
else
{
	IL2CPP_CHECK_RANGE(0, arg_0->Rank, arg_1);
	return ((int32_t*)&arg_0[1])[arg_1 * 2] + ((int32_t*)&arg_0[1])[arg_1 * 2 + 1] - 1;
}");

					return true;
				}
			}
			else if (typeName == "System.Environment")
			{
				if (metName == "GetResourceFromDefault")
				{
					prt.AppendLine("return arg_0;");
					return true;
				}
			}
			else if (typeName == "System.Runtime.CompilerServices.RuntimeHelpers")
			{
				if (metName == "GetHashCode")
				{
					prt.AppendLine("return (int32_t)((uintptr_t)arg_0 ^ ((uintptr_t)arg_0 >> 32));");
					return true;
				}
			}
			return false;
		}
	}
}
