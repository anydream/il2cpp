namespace il2cpp
{
	public class TypeGenerator
	{
		private readonly TypeManager TypeMgr;
		private readonly EvalStack MethodGen;

		private TypeX CurrType;

		public string DeclCode;
		public string ImplCode;

		public TypeGenerator(TypeManager typeMgr)
		{
			TypeMgr = typeMgr;
			MethodGen = new EvalStack(typeMgr);
		}

		public void Process(TypeX tyX)
		{
			CurrType = tyX;
			DeclCode = null;
			ImplCode = null;

			GenDeclCode();
			GenImplCode();

			foreach (var metX in CurrType.Methods)
			{
				MethodGen.Process(metX);
			}
		}

		private void GenDeclCode()
		{
			DeclCode = string.Format("struct {0};\n", CurrType.GetCppName());
		}

		private void GenImplCode()
		{
			CodePrinter prt = new CodePrinter();
			prt.AppendFormatLine("// {0}, {1}", CurrType.PrettyName(), CurrType.RuntimeVersion);
			if (CurrType.BaseType != null)
			{
				prt.AppendFormatLine("struct {0} : {1}\n{{",
					CurrType.GetCppName(),
					CurrType.BaseType.GetCppName());
			}
			else
			{
				prt.AppendFormatLine("struct {0}\n{{",
					CurrType.GetCppName());
			}
			++prt.Indents;

			foreach (var fldX in CurrType.Fields)
			{
				prt.AppendFormatLine("// {0}\n{1} {2};",
					fldX.PrettyName(),
					fldX.FieldType.GetCppName(TypeMgr),
					fldX.GetCppName());
			}

			--prt.Indents;
			prt.AppendLine("};");

			ImplCode = prt.ToString();
		}
	}
}
