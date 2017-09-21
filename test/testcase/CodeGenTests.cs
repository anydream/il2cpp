
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

	[CodeGen]
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
		public static long Entry()
		{
			ushort[] ary = new ushort[10];
			ary[0] = 123;
			ary[9] = 456;
			int len = ary.Length;
			long llen = ary.LongLength;
			return len + llen + ary[0] + ary[9];
		}
	}
}
