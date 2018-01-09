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

			res = SameMethodImpl.CollapsedInterfaces.HelloWorld.Main();
			if (res != 100)
				return 4;

			res = InterfaceFolding.Ambiguous.Test.Main();
			if (res != 100)
				return 5;

			res = InterfaceFolding.TestCase0.Test.Main();
			if (res != 100)
				return 6;

			res = InterfaceFolding.TestCase1.Test.Main();
			if (res != 100)
				return 7;

			res = InterfaceFolding.TestCase2.Test.Main();
			if (res != 100)
				return 8;

			res = InterfaceFolding.TestCase3.Test.Main();
			if (res != 100)
				return 9;

			res = InterfaceFolding.TestCase4.Test.Main();
			if (res != 100)
				return 10;

			res = InterfaceFolding.TestCase5.Test.Main();
			if (res != 100)
				return 11;

			res = InterfaceFolding.TestCase6.Test.Main();
			if (res != 100)
				return 12;

			res = InterfaceFolding.TestCase7.Test.Main();
			if (res != 100)
				return 13;

			return 0;
		}
	}
}