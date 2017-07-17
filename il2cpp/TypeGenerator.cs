using System.Text;

namespace il2cpp
{
	// 类型生成器
	public class TypeGenerator
	{
		// 类型管理器
		private readonly TypeManager TypeMgr;
		// 方法生成器
		private readonly MethodGenerator MethodGen;

		// 当前类型
		private TypeX CurrType;

		// 声明代码
		public readonly StringBuilder DeclCode = new StringBuilder();
		// 实现代码
		public readonly StringBuilder ImplCode = new StringBuilder();

		public TypeGenerator(TypeManager typeMgr)
		{
			TypeMgr = typeMgr;
			MethodGen = new MethodGenerator(typeMgr);
		}

		public void Process(TypeX tyX)
		{
			CurrType = tyX;
			DeclCode.Clear();
			ImplCode.Clear();

			// 生成类型结构体代码
			GenDeclCode();

			// 生成方法代码
			foreach (var metX in CurrType.Methods)
			{
				MethodGen.Process(metX);
				DeclCode.Append(MethodGen.DeclCode);
				ImplCode.Append(MethodGen.ImplCode);
			}
		}

		private void GenDeclCode()
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

			DeclCode.Append(prt.ToString());
		}
	}
}
