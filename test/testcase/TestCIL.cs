namespace testcase
{
	[CodeGen]
	static class TestCIL
	{
		public static int Entry()
		{
			int res = CollapsedMethods.InterfaceDefinition.HelloWorld.Main();
			if (res != 100)
				return 1;

			res = CollapsedMethods.Override.HelloWorld.Main();
			if (res != 100)
				return 2;

			return 0;
		}
	}
}