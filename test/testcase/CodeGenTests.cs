
using System;

namespace testcase
{
	class CodeGenAttribute : Attribute
	{
	}

	static class HelloWorld
	{
		public static int Entry()
		{
			return 42;
		}
	}

	//[CodeGen]
	static class Fibonacci
	{
		static long Fib(int n)
		{
			if (n < 2)
				return n;
			else
				return Fib(n - 1) + Fib(n - 2);
		}

		public static long Entry()
		{
			return Fib(40);
		}
	}

	[CodeGen]
	static class TestSZArray
	{
		public static float Entry()
		{
			float[] fary = new float[10];
			fary[0] = 1;
			fary[3] = 3;
			fary[5] = 5;

			float sum = 0;
			foreach (float n in fary)
				sum += n;

			ushort[] usary = new ushort[5];
			usary[1] = 42;
			usary[3] = 0xFFFF;
			foreach (ushort n in usary)
				sum += n;

			return sum - usary[3];
		}
	}
}
