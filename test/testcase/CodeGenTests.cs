
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
			fary[0] = 1.1f;
			fary[3] = 3.3f;
			fary[5] = 5.5f;

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

	[CodeGen]
	static class TestMDArray
	{
		public static float Entry()
		{
			float[,] fary = new float[2, 3];
			fary[0, 0] = 123.1f;
			fary[1, 0] = 456.2f;
			fary[1, 2] = 789.3f;

			float sum = 0;
			foreach (float n in fary)
				sum += n;

			return sum + fary[1, 2] + fary.LongLength + fary.Length + fary.GetUpperBound(1);
		}
	}
}
