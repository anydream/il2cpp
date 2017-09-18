using System.Collections.Generic;

namespace il2cpp
{
	internal class CompileUnit
	{
		public string Name;
		public string DeclCode;
		public string ImplCode;
		public readonly HashSet<string> DeclDepends = new HashSet<string>();
		public readonly HashSet<string> ImplDepends = new HashSet<string>();
	}

	internal class TypeGenerator
	{
		private readonly NameGenerator NameGen;
		private readonly TypeX CurrType;

		public TypeGenerator(NameGenerator nameGen, TypeX tyX)
		{
			NameGen = nameGen;
			CurrType = tyX;
		}

		public CompileUnit Generate()
		{
			CompileUnit unit = new CompileUnit();

			foreach (MethodX metX in CurrType.Methods)
			{
				var metGen = new MethodGenerator(NameGen, metX);
				metGen.Generate();
			}

			return unit;
		}
	}
}
