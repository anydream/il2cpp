namespace testcase
{
	[CodeGen]
	static class TestCIL
	{
		static int VSW576621()
		{
			MethodOverriding.VSW576621.C2 obj2 = new MethodOverriding.VSW576621.C2();

			if (obj2.M3() == 5)
				return 100;

			return -1;
		}

		public static int Entry()
		{
			int res = CollapsedMethods.InterfaceDefinition.HelloWorld.Main();
			if (res != 100)
				return 1;

			res = CollapsedMethods.Override.HelloWorld.Main();
			if (res != 100)
				return 2;

			res = VSW576621();
			if (res != 100)
				return 3;

			return 0;
		}
	}
}