namespace testcase
{
	[CodeGen]
	static class TestCIL
	{
		public static int Entry()
		{
			int res = InterfaceDefinition.HelloWorld.Main();
			if (res != 100)
				return 1;

			return 0;
		}
	}
}