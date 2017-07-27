using System;

namespace CodeGenTester
{
	class TestClassAttribute : Attribute
	{
		public string Result;

		public TestClassAttribute(string result)
		{
			Result = result;
		}
	}

	[TestClass(@"")]
	class TestBasicInst
	{
		public static int Fibonacci(int n)
		{
			int a = 0;
			int b = 1;

			for (int i = 0; i < n; i++)
			{
				int temp = a;
				a = b;
				b = temp + b;
			}
			return a;
		}

		static void Entry()
		{
			for (int i = 0; i < 15; ++i)
			{
				Fibonacci(i);
			}
		}
	}

	class Program
	{
		static void Main()
		{

		}
	}
}
