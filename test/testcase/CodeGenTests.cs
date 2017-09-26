using System;
using System.Diagnostics;

namespace testcase
{
	class CodeGenAttribute : Attribute
	{
	}

	static class Helper
	{
		public static bool IsEquals(this float lhs, float rhs, float prec = 0.00001f)
		{
			double abs = lhs - rhs;
			if (abs < 0)
				abs = -abs;
			return abs < prec;
		}
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
			return Fib(43);
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

	[CodeGen]
	static class TestSZArray
	{
		public static int Entry()
		{
			float[] fary = new float[10];

			var rank = fary.Rank;
			var len = fary.Length;
			var llen = fary.LongLength;
			var len2 = fary.GetLength(0);
			var llen2 = fary.GetLongLength(0);
			var lb = fary.GetLowerBound(0);
			var ub = fary.GetUpperBound(0);

			if (rank != 1)
				return 1;
			if (len != len2)
				return 2;
			if (llen != llen2)
				return 3;
			if (len != llen)
				return 4;
			if (lb != 0)
				return 5;
			if (ub != 9)
				return 6;

			fary[0] = 1.1f;
			fary[3] = 3.3f;
			fary[5] = 5.5f;

			if (fary[0] != 1.1f ||
				fary[3] != 3.3f ||
				fary[5] != 5.5f)
				return 7;

			float sum = 0;
			foreach (float n in fary)
				sum += n;

			if (!sum.IsEquals(9.9f))
				return 8;

			ushort[] usary = new ushort[5];

			rank = usary.Rank;
			len = usary.Length;
			llen = usary.LongLength;
			len2 = usary.GetLength(0);
			llen2 = usary.GetLongLength(0);
			lb = usary.GetLowerBound(0);
			ub = usary.GetUpperBound(0);

			if (rank != 1)
				return 9;
			if (len != len2)
				return 10;
			if (llen != llen2)
				return 11;
			if (len != llen)
				return 12;
			if (lb != 0)
				return 13;
			if (ub != 4)
				return 14;

			usary[1] = 42;
			usary[3] = 0xFFFF;

			if (usary[1] != 42 ||
				usary[3] != 65535)
				return 15;

			foreach (ushort n in usary)
				sum += n;

			if (!sum.IsEquals(65586.9f))
				return 16;

			return 0;
		}
	}

	[CodeGen]
	static class TestSZArrayPerf
	{
		public static long Entry(int times)
		{
			long sum = 0;
			for (int i = 0; i < times; ++i)
				sum += TestSZArray.Entry();
			return sum;
		}
	}

	[CodeGen]
	static class TestMDArray
	{
		public static int Entry()
		{
			float[,] fary = new float[2, 3];

			var rank = fary.Rank;
			var len = fary.Length;
			var llen = fary.LongLength;
			var len0 = fary.GetLength(0);
			var len1 = fary.GetLength(1);
			var llen0 = fary.GetLongLength(0);
			var llen1 = fary.GetLongLength(1);
			var lb0 = fary.GetLowerBound(0);
			var ub0 = fary.GetUpperBound(0);
			var lb1 = fary.GetLowerBound(1);
			var ub1 = fary.GetUpperBound(1);

			if (rank != 2)
				return 1;
			if (len != llen)
				return 2;
			if (len0 != 2)
				return 3;
			if (len1 != 3)
				return 4;
			if (llen0 != 2)
				return 5;
			if (llen1 != 3)
				return 6;
			if (lb0 != 0)
				return 7;
			if (ub0 != 1)
				return 8;
			if (lb1 != 0)
				return 9;
			if (ub1 != 2)
				return 10;
			if (len != 6)
				return 11;

			fary[0, 0] = 123.1f;
			fary[1, 0] = 456.2f;
			fary[1, 2] = 789.3f;

			if (fary[0, 0] != 123.1f)
				return 12;
			if (fary[1, 0] != 456.2f)
				return 13;
			if (fary[1, 2] != 789.3f)
				return 14;

			float sum = 0;
			foreach (float n in fary)
				sum += n;

			if (!sum.IsEquals(1368.6f))
				return 15;

			short[,,] sary3d = new short[2, 3, 4];
			/*{
				{
					{ 1, 2, 3, 4 }, { 5, 6, 7, 8 }, { 9, 10, 11, 12 }
				},
				{
					{ 13, 14, 15, 16 }, { 17, 18, 19, 20 }, { 21, 22, 23, 24 }
				}
			};*/

			rank = sary3d.Rank;
			len = sary3d.Length;
			llen = sary3d.LongLength;
			len0 = sary3d.GetLength(0);
			len1 = sary3d.GetLength(1);
			var len2 = sary3d.GetLength(2);
			llen0 = sary3d.GetLongLength(0);
			llen1 = sary3d.GetLongLength(1);
			var llen2 = sary3d.GetLongLength(2);
			lb0 = sary3d.GetLowerBound(0);
			ub0 = sary3d.GetUpperBound(0);
			lb1 = sary3d.GetLowerBound(1);
			ub1 = sary3d.GetUpperBound(1);
			var lb2 = sary3d.GetLowerBound(2);
			var ub2 = sary3d.GetUpperBound(2);

			if (rank != 3)
				return 16;
			if (len != llen)
				return 17;
			if (len != 24)
				return 18;
			if (len0 != 2)
				return 19;
			if (len1 != 3)
				return 20;
			if (len2 != 4)
				return 21;
			if (llen0 != 2)
				return 22;
			if (llen1 != 3)
				return 23;
			if (llen2 != 4)
				return 24;
			if (lb0 != 0)
				return 25;
			if (ub0 != 1)
				return 26;
			if (lb1 != 0)
				return 27;
			if (ub1 != 2)
				return 28;
			if (lb2 != 0)
				return 29;
			if (ub2 != 3)
				return 30;

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

			num = 0;
			foreach (short n in sary3d)
			{
				if (n != ++num)
					return 31;
			}

			return 0;
		}
	}

	[CodeGen]
	static class TestMDArrayPerf
	{
		public static long Entry(int times)
		{
			long sum = 0;
			for (int i = 0; i < times; ++i)
				sum += TestMDArray.Entry();
			return sum;
		}
	}

	internal class Program
	{
		private static void Main()
		{
			//Console.Write("Input Times: ");
			//int times = int.Parse(Console.ReadLine());
			//Console.WriteLine("Times: {0}", times);
			/*var sw = new Stopwatch();
			sw.Start();
			long res = Fibonacci.Entry();
			sw.Stop();

			Console.WriteLine("Result: {0}, Elapsed: {1}ms", res, sw.ElapsedMilliseconds);*/
		}
	}
}
