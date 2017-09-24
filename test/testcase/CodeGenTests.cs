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

	//[CodeGen]
	static class TestCallVirt
	{
		interface Inf
		{
			int Foo();
		}

		class ClsA : Inf
		{
			public int Foo()
			{
				return 123;
			}
		}

		class ClsB : Inf
		{
			public int Foo()
			{
				return 456;
			}
		}

		private static int Bla(Inf inf)
		{
			return inf.Foo();
		}

		public static int Entry()
		{
			return Bla(new ClsB()) - Bla(new ClsA());
		}
	}

	//[CodeGen]
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

			sum += fary[1, 2] + fary.LongLength;

			short[,,] sary3d = new short[2, 3, 4];
			/*{
				{
					{ 1, 2, 3, 4 }, { 5, 6, 7, 8 }, { 9, 10, 11, 12 }
				},
				{
					{ 13, 14, 15, 16 }, { 17, 18, 19, 20 }, { 21, 22, 23, 24 }
				}
			};*/
			short num = 0;
			for (int x = 0; x < 2; ++x)
			{
				for (int y = 0; y < 3; ++y)
				{
					for (int z = 0; z < 4; ++z)
					{
						sary3d[x, y, z] = ++num;
					}
				}
			}

			short last = 0;
			foreach (short n in sary3d)
			{
				if (n - last == 1)
				{
					sum += n;
					last = n;
				}
				else
					return 0;
			}

			sum += sary3d.GetUpperBound(0) - sary3d.GetLowerBound(0);
			sum += sary3d.GetUpperBound(1) - sary3d.GetLowerBound(1);
			sum += sary3d.GetUpperBound(2) - sary3d.GetLowerBound(2);

			sum += sary3d[1, 2, 3] + sary3d.LongLength;

			return sum;
		}
	}
}
