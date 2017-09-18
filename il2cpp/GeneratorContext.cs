using dnlib.DotNet;

namespace il2cpp
{
	internal class GeneratorContext
	{
		private readonly TypeManager TypeMgr;

		public GeneratorContext(TypeManager typeMgr)
		{
			TypeMgr = typeMgr;
		}

		public string GetTypeName(TypeSig tySig)
		{
			return null;
		}
	}
}
