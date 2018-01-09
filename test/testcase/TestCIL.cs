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

			res = InterfaceFolding.Nested_I.TestCase0.Test.Main();
			if (res != 100)
				return 14;

			res = InterfaceFolding.Nested_I.TestCase1.Test.Main();
			if (res != 100)
				return 15;

			res = InterfaceFolding.Nested_I.TestCase2.Test.Main();
			if (res != 100)
				return 16;

			res = InterfaceFolding.Nested_I.TestCase3.Test.Main();
			if (res != 100)
				return 17;

			res = InterfaceFolding.Nested_I.TestCase4.Test.Main();
			if (res != 100)
				return 18;

			res = InterfaceFolding.Nested_I.TestCase5.Test.Main();
			if (res != 100)
				return 19;

			res = InterfaceFolding.Nested_I.TestCase6.Test.Main();
			if (res != 100)
				return 20;

			res = InterfaceFolding.Nested_I_Nested_J.TestCase0.Test.Main();
			if (res != 100)
				return 21;

			res = InterfaceFolding.Nested_I_Nested_J.TestCase1.Test.Main();
			if (res != 100)
				return 22;

			res = InterfaceFolding.Nested_I_Nested_J.TestCase2.Test.Main();
			if (res != 100)
				return 23;

			res = InterfaceFolding.Nested_I_Nested_J.TestCase3.Test.Main();
			if (res != 100)
				return 24;

			res = InterfaceFolding.Nested_I_Nested_J.TestCase4.Test.Main();
			if (res != 100)
				return 25;

			res = InterfaceFolding.Nested_I_Nested_J.TestCase5.Test.Main();
			if (res != 100)
				return 26;

			res = InterfaceFolding.Nested_I_Nested_J.TestCase6.Test.Main();
			if (res != 100)
				return 27;

			res = InterfaceFolding.Nested_J.TestCase0.Test.Main();
			if (res != 100)
				return 28;

			res = InterfaceFolding.Nested_J.TestCase1.Test.Main();
			if (res != 100)
				return 29;

			res = InterfaceFolding.Nested_J.TestCase2.Test.Main();
			if (res != 100)
				return 30;

			res = InterfaceFolding.Nested_J.TestCase3.Test.Main();
			if (res != 100)
				return 31;

			res = InterfaceFolding.Nested_J.TestCase4.Test.Main();
			if (res != 100)
				return 32;

			res = InterfaceFolding.Nested_J.TestCase5.Test.Main();
			if (res != 100)
				return 33;

			res = InterfaceFolding.Nested_J.TestCase6.Test.Main();
			if (res != 100)
				return 34;

			res = InterfaceFolding.Nested_J_Nested_I.TestCase0.Test.Main();
			if (res != 100)
				return 35;

			res = InterfaceFolding.Nested_J_Nested_I.TestCase1.Test.Main();
			if (res != 100)
				return 36;

			res = InterfaceFolding.Nested_J_Nested_I.TestCase2.Test.Main();
			if (res != 100)
				return 37;

			res = InterfaceFolding.Nested_J_Nested_I.TestCase3.Test.Main();
			if (res != 100)
				return 38;

			res = InterfaceFolding.Nested_J_Nested_I.TestCase4.Test.Main();
			if (res != 100)
				return 39;

			res = InterfaceFolding.Nested_J_Nested_I.TestCase5.Test.Main();
			if (res != 100)
				return 40;

			res = InterfaceFolding.Nested_J_Nested_I.TestCase6.Test.Main();
			if (res != 100)
				return 41;


			res = MethodImpl.override_override1.CMain.Main();
			if (res != 100)
				return 42;

			res = MethodImpl.self_override1.CMain.Main();
			if (res != 100)
				return 43;

			res = MethodImpl.self_override3.CMain.Main();
			if (res != 100)
				return 44;

			res = MethodImpl.Desktop.override_override1.CMain.Main();
			if (res != 100)
				return 45;

			res = MethodImpl.Desktop.self_override1.CMain.Main();
			if (res != 100)
				return 46;

			res = MethodImpl.Desktop.self_override2.CMain.Main();
			if (res != 100)
				return 47;

			res = MethodImpl.Desktop.self_override3.CMain.Main();
			if (res != 100)
				return 48;

			res = MethodImpl.Desktop.self_override5.CMain.Main();
			if (res != 100)
				return 49;

			return 0;
		}
	}
}