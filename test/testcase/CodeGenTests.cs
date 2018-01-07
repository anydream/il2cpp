using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using testInsts;

namespace testcase
{
	class CodeGenAttribute : Attribute
	{
	}

	static class Helper
	{
		public static bool IsEquals(this float lhs, float rhs, float prec = 0.00001f)
		{
			float abs = lhs - rhs;
			if (abs < 0)
				abs = -abs;
			return abs < prec;
		}

		public static bool IsEquals(this double lhs, double rhs, double prec = 0.00001)
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

		public static int Entry()
		{
			long res = Fib(37);
			if (res != 24157817)
				return 1;
			return 0;
		}
	}

	[CodeGen]
	static class Fibonacci2
	{
		static long Fib(int n)
		{
			long a = 1;
			long b = 1;
			for (int i = 0; i < n - 1; ++i)
			{
				long t = a;
				a = b;
				b = t + b;
			}
			return a;
		}

		public static int Entry()
		{
			long res = Fib(37);
			if (res != 24157817)
				return 1;
			return 0;
		}
	}

	//[CodeGen]
	static class FibonacciYield
	{
		static IEnumerable<long> Fib(int n)
		{
			if (n < 2)
				yield return n;
			else
				yield return Fib(n - 1).First() + Fib(n - 2).First();
		}

		public static int Entry()
		{
			long res = Fib(37).First();
			if (res != 24157817)
				return 1;
			return 0;
		}
	}

	[CodeGen]
	static class TestSameName
	{
		public static int Entry()
		{
			string nameA = TestInstructions.GetNameDLLA();
			string nameB = TestInstructions.GetNameDLLB();

			if (nameA != "dllA")
				return 1;
			if (nameB != "dllB")
				return 2;

			return 0;
		}
	}

	[CodeGen]
	static class TestConstStatic
	{
		class SCls
		{
			public const int num = 1230 / 10;
		}
		public static int Entry()
		{
			if (42 + SCls.num * SCls.num != 15171)
				return 1;
			return 0;
		}
	}

	[CodeGen]
	static unsafe class TestTypeSig
	{
		class Cls
		{
			public byte* ptr;
		}

		static byte* Foo(byte* p)
		{
			return p;
		}

		public static int Entry()
		{
			byte b = 12;
			byte* p = &b;
			byte* p2 = Foo(p);
			Cls cls = new Cls() { ptr = p2 };
			if (*cls.ptr != 12)
				return 1;

			byte*[] ary = new byte*[10];
			ary[0] = p;
			++*ary[0];
			if (*ary[0] != 13)
				return 2;

			if (b != 13)
				return 3;

			return 0;
		}
	}

	[CodeGen]
	static class TestStaticCctor
	{
		class MyCls
		{
			public static uint sfld;

			static MyCls()
			{
				sfld = 654321;
			}
		}

		class MyCls2
		{
			public static uint sfld;

			static MyCls2()
			{
				sfld = sfld + 54789651;
			}

			public uint Foo()
			{
				return sfld;
			}
		}

		public class ClsA
		{
			public static int sfld = 123;

			static ClsA()
			{
				sfld += ClsB.sfld;
			}
		}

		public class ClsB
		{
			public static int sfld = 456;

			static ClsB()
			{
				sfld += ClsA.sfld;
			}
		}

		public static int Entry()
		{
			if (MyCls.sfld != 654321)
				return 1;

			uint v1 = new MyCls2().Foo();
			uint v2 = new MyCls2().Foo();
			if (v1 != 54789651)
				return 2;
			if (v1 != v2)
				return 3;

			if (ClsA.sfld != 702)
				return 4;
			if (ClsB.sfld != 579)
				return 5;

			return 0;
		}
	}

	[CodeGen]
	static class TestStaticCctor2
	{
		class MyCls
		{
			public static float sfldR4 = 6.28f;

			public static void StaticEmpty()
			{
			}

			public static float StaticGet()
			{
				return sfldR4;
			}

			public static bool StaticEq(float cmp)
			{
				return sfldR4 == cmp;
			}
		}

		public static int Entry()
		{
			if (TestStaticCctor.ClsB.sfld != 1035)
				return 1;
			if (TestStaticCctor.ClsA.sfld != 579)
				return 2;

			MyCls.StaticEmpty();
			float val = MyCls.StaticGet();
			bool eq = MyCls.StaticEq(val);

			if (val != 6.28f)
				return 3;
			if (!eq)
				return 4;

			return 0;
		}
	}

	[CodeGen]
	static class TestObject
	{
		private static object s_NewLocker = new object();
		private static object s_DelLocker = new object();
		private static long s_NewCounter;
		private static long s_DelCounter;

		class Base
		{
			public int num;
			public byte[] payload = new byte[64];
			public Base()
			{
				lock (s_NewLocker)
				{
					++s_NewCounter;
				}
			}

			~Base()
			{
				lock (s_DelLocker)
				{
					++s_DelCounter;
				}
			}
		}

		class Middle : Base
		{
			public static int s_num;
			~Middle()
			{
				lock (s_DelLocker)
				{
					++s_num;
				}
			}
		}

		class Cls : Middle
		{ }

		static int TestFinalizer()
		{
			Cls[] ary = new Cls[9999];
			for (int i = 0; i < ary.Length; ++i)
			{
				ary[i] = new Cls() { num = i };
			}
			if (ary.Length != 9999)
				return -1;

			int sum = 0;
			foreach (Cls cls in ary)
				sum += cls.num;

			if (sum != 49985001)
				return -2;

			return 0;
		}

		public static int Entry()
		{
			int num = 0;
			for (int i = 0; i < 10; ++i)
			{
				object obj = new object();
				lock (obj)
				{
					++num;
					obj = null;
				}
			}
			if (num != 10)
				return 1;

			for (; ; )
			{
				int res = TestFinalizer();
				if (res != 0)
					return res;

				GC.Collect();

				if (s_DelCounter >= 999900)
				{
					if (s_NewCounter - s_DelCounter <= 19999)
					{
						if (Middle.s_num != s_DelCounter)
							return -3;
						return 0;
					}
				}
			}

			return -3;
		}
	}

	[CodeGen]
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

		struct Stru : Inf
		{
			public int num;

			public Stru(int n)
			{
				num = n;
			}
			public int Foo()
			{
				return num;
			}
		}

		private static int Bla(Inf inf)
		{
			return inf.Foo();
		}

		public static int Entry()
		{
			if (Bla(new ClsB()) - Bla(new ClsA()) != 333)
				return 1;
			if (Bla(new Stru(789)) != 789)
				return 2;
			Inf inf = new Stru(789);
			if (inf.Foo() != 789)
				return 3;

			return 0;
		}
	}

	[CodeGen]
	static class TestCallVirtStru
	{
		interface Inf<T>
		{
			T Foo(T t);
		}

		struct StruA<T> : Inf<T>
		{
			public T num;

			public StruA(T n)
			{
				num = n;
			}
			public T Foo(T t)
			{
				return num;
			}
		}

		class ClsB<T> : Inf<T>
		{
			public T num;
			public T Foo(T t)
			{
				return num;
			}
		}

		struct StruC : Inf<int>
		{
			public int Foo(int t)
			{
				return t * 2;
			}
		}

		class ClsD : Inf<int>
		{
			public int Foo(int t)
			{
				return t * 3;
			}
		}

		private static T Bla2<T>(Inf<T> inf, T t)
		{
			return inf.Foo(t);
		}

		public static int Entry()
		{
			if (Bla2(new StruA<int>(123), 123) != 123)
				return 1;
			var bb = new ClsB<int>();
			bb.num = 456;
			if (Bla2(bb, 123) != 456)
				return 2;
			if (Bla2(new StruC(), 123) != 246)
				return 3;
			if (Bla2(new ClsD(), 124) != 372)
				return 4;

			return 0;
		}
	}

	[CodeGen]
	static class TestString
	{
		class MyCls
		{
			public string strfld = "defaultstr";
		}

		private static string Foo(string s)
		{
			return s;
		}

		public static unsafe int Entry()
		{
			string s = "asdf\\1234\"zxcv'!~";

			if (s[4] != 0x5C)
				return -1;
			if (s[9] != 0x22)
				return -2;
			if (s[14] != 0x27)
				return -3;

			s = "0987\r\n!~";
			if (s[4] != 0x0D)
				return -4;
			if (s[5] != 0x0A)
				return -5;

			s = "你好世界";
			if (s[0] != 0x4F60)
				return -6;
			if (s[1] != 0x597D)
				return -7;
			if (s[2] != 0x4E16)
				return -8;
			if (s[3] != 0x754C)
				return -9;
			if (s.Length != 4)
				return -10;

			s = "";
			object o = s.Length == 0 ? s : null;
			if (!(o is string))
				return -11;

			s = "hello,world";
			if (s != "hello,world")
				return 1;
			if (s.Length != 11)
				return 2;

			if (Foo(s) != s)
				return 3;

			if (s[0] != 'h')
				return 4;
			if (s[10] != 'd')
				return 5;

			int hash = s.GetHashCode();
			if (hash != "hello,world".GetHashCode())
				return 6;

			MyCls cls = new MyCls();
			if (cls.strfld != "defaultstr")
				return 7;
			cls.strfld = "nice";
			if (cls.strfld != "nice")
				return 8;

			string strRep = "adsf @calloc 1234";
			if (strRep[6] != 'c' ||
				strRep[7] != 'a' ||
				strRep[8] != 'l' ||
				strRep[9] != 'l' ||
				strRep[10] != 'o' ||
				strRep[11] != 'c' ||
				strRep[12] != ' ')
				return 9;

			string strRep2 = "adsf  attributes # 1234";
			if (strRep2[6] != 'a' ||
				strRep2[7] != 't' ||
				strRep2[8] != 't' ||
				strRep2[9] != 'r' ||
				strRep2[10] != 'i' ||
				strRep2[11] != 'b' ||
				strRep2[12] != 'u' ||
				strRep2[13] != 't' ||
				strRep2[14] != 'e' ||
				strRep2[15] != 's' ||
				strRep2[16] != ' ')
				return 10;

			string aa = "Test";
			string bb = "拼接";
			string con = aa + bb;
			if (con != "Test拼接")
				return 11;

			if (con.Substring(2, 3) != "st拼")
				return 12;

			if (con.IndexOf('拼') != 4)
				return 13;

			string strCmp = "qwe\0r";
			if (strCmp != strCmp ||
				strCmp == "qwe\0\0")
				return 14;

			char* pstr = stackalloc char[10];
			pstr[5] = 'H';
			pstr[6] = 'e';
			pstr[7] = 'l';
			pstr[8] = 'l';
			pstr[9] = 'o';
			string str = new string(pstr, 5, 5);
			if (str != "Hello")
				return 15;

			return 0;
		}
	}

	[CodeGen]
	static class TestString2
	{
		public static int Entry()
		{
			string str = "1234asdf测试字符串字符串";
			if (str.IndexOf("df测试", StringComparison.Ordinal) != 6)
				return 1;

			if (str.LastIndexOf("字符串", StringComparison.Ordinal) != 13)
				return 2;

			return 0;
		}
	}

	[CodeGen]
	static class TestValueType
	{
		private static short sfldI2;

		struct MyStru
		{
			public int fldI4;
			public double fldR8;

			public MyStru(ref MyStru other)
			{
				fldI4 = 0;
				fldR8 = 0;

				fldI4 += other.fldI4;
				fldR8 += other.fldR8;
			}
		}

		class MyCls
		{
			public int fld;
		}

		struct MyStruHasRef
		{
			public long fldI8;
			public MyCls cls;
		}

		struct StruT<T>
		{
			public int aa;
			public T bb;
			public IList<T> cc;
		}

		struct StruSelf
		{
			public object obj;
		}

		struct StruNeq
		{
			public int fld;

			public override bool Equals(object obj)
			{
				return false;
			}
		}

		struct StruNeq2
		{
			public int fld;
			public StruNeq n;
		}

		struct StruCmp
		{
			public int aa;
			public double bb;
		}

		private static MyStru Foo1(MyStru s, ref MyStru rs, out MyStru os)
		{
			rs = s;
			os = new MyStru { fldI4 = 123, fldR8 = 3.1415926 };
			return os;
		}

		private static MyStruHasRef Foo2(MyStruHasRef s, ref MyStruHasRef rs, out MyStruHasRef os)
		{
			rs = new MyStruHasRef { fldI8 = ~1, cls = new MyCls { fld = 42 } };
			os = rs;
			rs.fldI8 -= s.fldI8;
			return rs;
		}

		private static bool TestInPlace()
		{
			MyStru v1 = new MyStru() { fldI4 = 123, fldR8 = 456 };
			MyStru v2 = new MyStru(ref v1);
			return v2.fldI4 == 123 && v2.fldR8 == 456;
		}

		private static unsafe bool TestLocalloc()
		{
			int* buf = stackalloc int[50];
			buf[49] = 123;

			int sum = 0;
			for (int i = 0; i < 50; ++i)
				sum += buf[i];

			if (sum != 123 || buf[49] != 123)
				return false;
			return true;
		}

		public static int Entry()
		{
			sfldI2 = 26;
			MyStru rs = new MyStru();
			var ret = Foo1(new MyStru { fldI4 = 10, fldR8 = 1.44 }, ref rs, out var os);

			if (ret.fldI4 != 123)
				return 1;
			if (ret.fldR8 != 3.1415926)
				return 2;
			if (ret.fldI4 != os.fldI4)
				return 3;
			if (ret.fldR8 != os.fldR8)
				return 4;
			if (rs.fldI4 != 10)
				return 5;
			if (rs.fldR8 != 1.44)
				return 6;

			MyStruHasRef rs2 = new MyStruHasRef();
			var ret2 = Foo2(new MyStruHasRef { fldI8 = 999999 }, ref rs2, out var os2);

			if (ret2.cls.fld != 42)
				return 7;
			if (ret2.fldI8 != -1000001)
				return 8;
			if (rs2.cls.fld != os2.cls.fld)
				return 9;
			if (rs2.cls.fld != ret2.cls.fld)
				return 10;
			if (ret2.fldI8 != rs2.fldI8)
				return 11;
			if (os2.fldI8 != -2)
				return 12;

			if (sfldI2 != 26)
				return 13;

			if (!TestInPlace())
				return 14;

			if (!TestLocalloc())
				return 15;

			StruT<int> stt = new StruT<int>()
			{
				aa = 123,
				bb = 456,
				cc = new List<int>()
			};
			int h1 = stt.GetHashCode();

			StruT<int> cmp1 = new StruT<int>()
			{
				aa = 123,
				bb = 456,
				cc = stt.cc
			};
			if (h1 != cmp1.GetHashCode())
				return 16;

			StruT<object> stt2 = new StruT<object>()
			{
				aa = 789,
				bb = 234,
				cc = new List<object>()
			};
			int h2 = stt2.GetHashCode();

			StruT<object> cmp2 = new StruT<object>()
			{
				aa = 789,
				bb = 234,
				cc = stt2.cc
			};
			if (h2 != cmp2.GetHashCode())
				return 17;

			if (!stt.Equals(cmp1))
				return 18;

			if (!stt2.Equals(cmp2))
				return 19;

			StruSelf sslf = new StruSelf();
			sslf.obj = sslf;

			StruSelf sslf2 = new StruSelf();
			sslf2.obj = sslf;

			if (!sslf.Equals(sslf))
				return 20;

			if (sslf.Equals(sslf2))
				return 21;

			if (!sslf2.Equals(sslf2))
				return 22;

			if (sslf2.Equals(sslf))
				return 23;

			StruSelf sslf3 = new StruSelf();
			sslf3.obj = sslf.obj;

			if (!sslf.Equals(sslf3))
				return 24;

			if (!sslf3.Equals(sslf))
				return 25;

			if (new StruNeq().Equals(new StruNeq()))
				return 26;

			StruNeq sneq = new StruNeq();
			if (sneq.Equals(sneq))
				return 27;

			StruCmp scmp = new StruCmp { aa = 123, bb = 456.789 };
			StruCmp scmp2 = new StruCmp { aa = 123, bb = 456.789 };

			if (!scmp.Equals(scmp2))
				return 28;

			StruNeq2 sneq2 = new StruNeq2();
			if (sneq2.Equals(sneq2))
				return 29;

			return 0;
		}
	}

	[CodeGen]
	static unsafe class TestExplicitLayout
	{
		[StructLayout(LayoutKind.Explicit)]
		struct Stru
		{
			[FieldOffset(0)] public char str;
			[FieldOffset(12)] public int num;
			[FieldOffset(12)] public short snum1;
			[FieldOffset(14)] public short snum2;
			[FieldOffset(16)] public float fnum;
		}

		[StructLayout(LayoutKind.Auto, Size = 120, Pack = 1)]
		struct StruSized
		{
			public float aa;
			public double bb;
		}

		[StructLayout(LayoutKind.Explicit, Size = 60)]
		struct StruExpSized
		{
			[FieldOffset(12)] public int num;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 2)]
		struct StruPacked
		{
			public byte b1;
			public byte b2;
			public int i3;
		}

		public static int Entry()
		{
			Stru s = new Stru();
			Stru* p = &s;

			if ((byte*)&p->num - (byte*)p != 12)
				return 1;

			if ((byte*)&p->snum1 - (byte*)p != 12)
				return 2;

			if ((byte*)&p->snum2 - (byte*)p != 14)
				return 3;

			if ((byte*)&p->fnum - (byte*)p != 16)
				return 4;

			if ((byte*)&p->str - (byte*)p != 0)
				return 5;

			if (sizeof(Stru) != 20)
				return 6;

			var s2 = new StruSized();
			s2.aa = 1.23f;
			s2.bb = 3.14;
			if (sizeof(StruSized) != 120)
				return 7;

			var s3 = new StruExpSized();
			s3.num = 666;
			if (sizeof(StruExpSized) != 60)
				return 8;

			StruPacked ex = new StruPacked();
			byte* addr = (byte*)&ex;
			if (sizeof(StruPacked) != 6)
				return 9;
			if (&ex.b1 - addr != 0)
				return 10;
			if (&ex.b2 - addr != 1)
				return 11;
			if ((byte*)&ex.i3 - addr != 2)
				return 12;

			return 0;
		}
	}

	[CodeGen]
	static class TestSZArray
	{
		struct Stru
		{
			public int aa;
			public double bb;
		}

		public static int Entry()
		{
			int[] iary = new int[10];
			for (int i = 0; i < iary.Length; ++i)
				iary[i] = (i + 1) * (i + 3);

			int isum = 0;
			for (int i = 0; i < iary.Length; ++i)
				isum += iary[i];

			if (isum != 495)
				return -1;

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

			Stru[] sary1 = new Stru[5];
			for (int i = 0; i < sary1.Length; ++i)
			{
				sary1[i].aa = 100 + i;
				sary1[i].bb = (i + 9) / 100.0;
			}
			Stru[] sary2 = new Stru[10];
			Array.Copy(sary1, 1, sary2, 4, 4);
			for (int i = 4, j = 1; j < sary1.Length; ++i, ++j)
			{
				if (sary2[i].aa != sary1[j].aa)
					return 17;
				if (sary2[i].bb != sary1[j].bb)
					return 18;
			}

			return 0;
		}
	}

	[CodeGen]
	static class TestSZArrayPerf
	{
		public static long Entry()
		{
			long sum = 0;
			for (int i = 0; i < 91234; ++i)
				sum += TestSZArray.Entry();
			return sum;
		}
	}

	[CodeGen]
	static class TestMDArray
	{
		struct Stru
		{
			public int aa;
			public double bb;

			public static bool operator ==(Stru lhs, Stru rhs)
			{
				return lhs.aa == rhs.aa &&
					lhs.bb == rhs.bb;
			}
			public static bool operator !=(Stru lhs, Stru rhs)
			{
				return !(lhs == rhs);
			}
		}

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

			short[,,] sary3d = new short[2, 3, 4]
				{
				{
					{ 1, 2, 3, 4 }, { 5, 6, 7, 8 }, { 9, 10, 11, 12 }
				},
				{
					{ 13, 14, 15, 16 }, { 17, 18, 19, 20 }, { 21, 22, 23, 24 }
				}
			};

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
						++num;
						if (sary3d[x, y, z] != num)
							return 31;
					}
				}
			}

			num = 0;
			foreach (short n in sary3d)
			{
				if (n != ++num)
					return 31;
			}

			Stru[,] sary1 = new Stru[3, 4];
			for (int i = 0; i < sary1.GetLength(0); ++i)
			{
				for (int j = 0; j < sary1.GetLength(1); ++j)
				{
					sary1[i, j] = new Stru { aa = j * 3 - i * 2, bb = (double)i / (j + 1) };
				}
			}
			Stru[,] sary2 = new Stru[4, 5];
			Array.Copy(sary1, 5, sary2, 6, 7);

			if (sary2[1, 1] != sary1[1, 1])
				return 32;

			int sum2 = 0;
			for (int i = 0; i < sary2.GetLength(0); ++i)
			{
				for (int j = 0; j < sary2.GetLength(1); ++j)
				{
					sum2 = (sum2 << 1) + sary2[i, j].aa;
				}
			}

			if (sum2 != 35456)
				return 33;

			return 0;
		}
	}

	[CodeGen]
	static class TestMDArrayPerf
	{
		public static long Entry()
		{
			long sum = 0;
			for (int i = 0; i < 91234; ++i)
				sum += TestMDArray.Entry();
			return sum;
		}
	}

	[CodeGen]
	static class TestArrayInterface
	{
		class Cls
		{
			public int num;
		}

		private static void FailCases()
		{
			{
				object[] oary = new Cls[10];
				object obj1 = oary[5];
				IList<object> olst = oary;
				object obj2 = olst[5];
			}

			{
				IList<object>[] cary = new Cls[2][];
				object obj3 = cary[0]?[0];
				var olst2 = new IList<object>[10];
				object obj4 = olst2[9];
			}

			{
				object[] oary = new Cls[10];
				object obj1 = oary[5];
				ICollection<object> olst = oary;
				object obj2 = olst.Count;
			}

			{
				ICollection<object>[] cary = new Cls[2][];
				var obj3 = cary[0]?.Count;
				var olst2 = new ICollection<object>[10];
				var obj4 = olst2[9]?.Count;
			}
		}

		public static int Entry()
		{
			FailCases();

			int[] ary = new int[10];
			for (int i = 0; i < ary.Length; ++i)
			{
				ary[i] = (i + 2) * (i + 1);
			}
			IList<int> ilst = ary;

			bool ro = ilst.IsReadOnly;
			if (!ro)
				return 1;

			var enu = ilst.GetEnumerator();
			int sum = 0;
			while (enu.MoveNext())
			{
				sum += enu.Current;
			}
			if (sum != 440)
				return 2;

			int[] ary2 = new int[11];
			ilst.CopyTo(ary2, 1);
			foreach (int item in ary2)
			{
				sum += item;
			}
			if (sum != 880)
				return 3;

			int cnt = ilst.Count;
			if (cnt != 10)
				return 4;

			ilst[9] = 123;
			int val = ilst[9];
			if (val != 123)
				return 5;

			/*bool con = ilst.Contains(123);
			if (!con)
				return 6;*/

			/*IList<int> ilst = new int[10];
			ilst.Contains(123);*/

			return 0;
		}
	}

	[CodeGen]
	static class TestValueTypeArray
	{
		struct MyStru
		{
			public int fldI4;
			public double fldR8;
		}

		public static int Entry()
		{
			MyStru[] sary = new MyStru[30];
			for (int i = 0; i < sary.Length; ++i)
			{
				sary[i].fldI4 = i + 1;
				sary[i].fldR8 = i * 100 + 0.1234567;
			}

			for (int i = sary.Length - 1; i >= 0; --i)
			{
				if (sary[i].fldI4 != i + 1)
					return 1;
				if (!sary[i].fldR8.IsEquals(i * 100 + 0.1234567))
					return 2;
			}

			MyStru[,,] sary3d = new MyStru[5, 4, 3];
			if (sary3d[4, 3, 2].fldI4 != 0)
				return 3;

			return 0;
		}
	}

	[CodeGen]
	static class TestEnum
	{
		enum MyEnum
		{
			AA,
			BB,
			CC = 123
		}

		enum MyEnumI8 : long
		{
			DD = 3,
			EE
		}

		enum MyEnum2
		{
			FF = 26,
			GG
		}

		class MyCls
		{
			public MyEnum enum1;
			public MyEnumI8 enum2;
		}

		static MyEnum senum1 = MyEnum.BB;
		static MyEnumI8 senum2 = MyEnumI8.EE;

		static MyEnumI8 Foo(MyEnum enu)
		{
			return (MyEnumI8)enu;
		}

		static int Foo2(MyEnum2 enu)
		{
			return (int)enu;
		}

		public static int Entry()
		{
			if (Foo2(MyEnum2.GG) != 27)
				return -1;

			var cls = new MyCls();
			if (cls.enum1 != 0)
				return 1;
			if (cls.enum2 != 0)
				return 2;

			cls.enum1 = senum1;
			cls.enum2 = senum2;

			if (cls.enum1 != MyEnum.BB)
				return 3;
			if (cls.enum2 != MyEnumI8.EE)
				return 4;

			if (cls.enum1 > (MyEnum)1)
				return 5;
			if (cls.enum2 > (MyEnumI8)4)
				return 6;

			MyEnum e = MyEnum.CC;
			MyEnumI8 e8 = Foo(e);

			if (e8 != (MyEnumI8)123)
				return 7;

			MyEnumI8 e9 = e8;
			if (e8.GetHashCode() != e9.GetHashCode())
				return 8;

			object oe = (object)e;
			if (!e.Equals(oe))
				return 9;

			if (e.GetHashCode() != oe.GetHashCode())
				return 10;

			return 0;
		}
	}

	[CodeGen]
	static class TestException
	{
		private static int testID = 0;
		private static int filterNum = 0;
		private static int catchNum = 0;
		private static int finallyNum = 0;

		class Except1 : Exception
		{
		}

		class Except2 : Exception
		{
		}

		class Except3 : Exception
		{
		}

		static void Foo()
		{
			switch (testID)
			{
				case 1:
					throw new Except1();

				case 2:
					throw new Except2();

				case 3:
					throw new Except3();
			}
		}

		static void Catch1()
		{
			catchNum += 1;
		}

		static void Catch2()
		{
			catchNum += 2;
		}

		static void Catch3()
		{
			catchNum += 3;
		}

		static void Catch4()
		{
			catchNum += 4;
		}

		static void Finally1()
		{
			finallyNum += 1;
		}

		static void Finally2()
		{
			finallyNum += 2;
		}

		static void Finally3()
		{
			finallyNum += 3;
		}

		private static void ExceptionFunc()
		{
			catchNum = 0;
			finallyNum = 0;
			try
			{
				try
				{
					try
					{
						Foo();
					}
					catch (Except1 ex)
					{
						Catch1();
					}
					finally
					{
						Finally1();
					}
				}
				finally
				{
					Finally2();
				}
			}
			catch (Except2 ex)
			{
				Catch2();
			}
			catch (Except3 ex) when (filterNum == 1)
			{
				Catch3();
			}
			catch (Except3 ex) when (filterNum == 2)
			{
				Catch3();
				catchNum += 10;
			}
			catch
			{
				Catch4();
			}
			finally
			{
				Finally3();
			}
		}

		private static int Test1()
		{
			int res = 0;
			try
			{
				throw new Exception();
			}
			catch (Exception ex)
			{
				res = 10;
			}
			finally
			{
				res -= 1;
			}
			return res;
		}

		private static int Test2()
		{
			int i = 0;
			try
			{
				for (; ; )
				{
					++i;
					if (i >= 123456)
						return i;
				}
			}
			finally
			{
				i = 0;
			}
		}

		private static int Test3()
		{
			int val = 0;
			try
			{
				try
				{
					val = 42;
					throw new Exception();
				}
				catch (Exception) when (val == 42)
				{
					val = 30;
					throw;
				}
			}
			catch
			{
				return val + 1;
			}
		}

		static int test4num = 0;
		static int Test4()
		{
			try
			{
				test4num = 2;
				DoIt();
			}
			catch
			{
				test4num *= 9;
			}
			finally
			{
				test4num *= 11;
			}
			return test4num;
		}
		static void DoIt()
		{
			try
			{
				test4num *= 3;
				int i = 0;
				throw new Exception();
			}
			catch (Exception e)
			{
				test4num *= 5;
				throw e;
			}
			finally
			{
				test4num *= 7;
			}
		}

		public static int Entry()
		{
			if (Test1() != 9)
				return -1;
			if (Test2() != 123456)
				return -2;
			if (Test3() != 31)
				return -3;
			if (Test4() != 20790)
				return -4;

			filterNum = 0;
			ExceptionFunc();
			if (catchNum != 0 ||
				finallyNum != 6)
				return 1;

			filterNum = 1;
			ExceptionFunc();
			if (catchNum != 0 ||
				finallyNum != 6)
				return 2;

			filterNum = 2;
			ExceptionFunc();
			if (catchNum != 0 ||
				finallyNum != 6)
				return 3;

			testID = 1;
			filterNum = 0;
			ExceptionFunc();
			if (catchNum != 1 ||
				finallyNum != 6)
				return 4;

			filterNum = 1;
			ExceptionFunc();
			if (catchNum != 1 ||
				finallyNum != 6)
				return 5;

			filterNum = 2;
			ExceptionFunc();
			if (catchNum != 1 ||
				finallyNum != 6)
				return 6;

			testID = 2;
			filterNum = 0;
			ExceptionFunc();
			if (catchNum != 2 ||
				finallyNum != 6)
				return 7;

			filterNum = 1;
			ExceptionFunc();
			if (catchNum != 2 ||
				finallyNum != 6)
				return 8;

			filterNum = 2;
			ExceptionFunc();
			if (catchNum != 2 ||
				finallyNum != 6)
				return 9;

			testID = 3;
			filterNum = 0;
			ExceptionFunc();
			if (catchNum != 4 ||
				finallyNum != 6)
				return 10;

			filterNum = 1;
			ExceptionFunc();
			if (catchNum != 3 ||
				finallyNum != 6)
				return 11;

			filterNum = 2;
			ExceptionFunc();
			if (catchNum != 13 ||
				finallyNum != 6)
				return 12;

			return 0;
		}
	}

	[CodeGen]
	static class TestBitwise
	{
		static List<int> ShiftLeft(int num)
		{
			List<int> results = new List<int>();
			while (num != 0)
			{
				num = num << 1;
				results.Add(num);
			}
			return results;
		}

		static List<int> ShiftLeftUn(int num)
		{
			List<int> results = new List<int>();
			while (num != 0)
			{
				num = (int)((uint)num << 1);
				results.Add(num);
			}
			return results;
		}

		static List<int> ShiftRight(int num)
		{
			List<int> results = new List<int>();
			while (num != 0 && num != -1)
			{
				num = num >> 1;
				results.Add(num);
			}
			return results;
		}

		static List<int> ShiftRightUn(int num)
		{
			List<int> results = new List<int>();
			while (num != 0)
			{
				num = (int)((uint)num >> 1);
				results.Add(num);
			}
			return results;
		}

		static bool CheckLeft(List<int> lst)
		{
			if (lst.Count != 31) return false;

			if (lst[0] != -2468) return false;
			if (lst[1] != -4936) return false;
			if (lst[2] != -9872) return false;
			if (lst[3] != -19744) return false;
			if (lst[4] != -39488) return false;
			if (lst[5] != -78976) return false;
			if (lst[6] != -157952) return false;
			if (lst[7] != -315904) return false;
			if (lst[8] != -631808) return false;
			if (lst[9] != -1263616) return false;
			if (lst[10] != -2527232) return false;
			if (lst[11] != -5054464) return false;
			if (lst[12] != -10108928) return false;
			if (lst[13] != -20217856) return false;
			if (lst[14] != -40435712) return false;
			if (lst[15] != -80871424) return false;
			if (lst[16] != -161742848) return false;
			if (lst[17] != -323485696) return false;
			if (lst[18] != -646971392) return false;
			if (lst[19] != -1293942784) return false;
			if (lst[20] != 1707081728) return false;
			if (lst[21] != -880803840) return false;
			if (lst[22] != -1761607680) return false;
			if (lst[23] != 771751936) return false;
			if (lst[24] != 1543503872) return false;
			if (lst[25] != -1207959552) return false;
			if (lst[26] != 1879048192) return false;
			if (lst[27] != -536870912) return false;
			if (lst[28] != -1073741824) return false;
			if (lst[29] != -2147483648) return false;
			if (lst[30] != 0) return false;

			return true;
		}

		static bool CheckRight(List<int> lst)
		{
			if (lst.Count != 11) return false;

			if (lst[0] != -617) return false;
			if (lst[1] != -309) return false;
			if (lst[2] != -155) return false;
			if (lst[3] != -78) return false;
			if (lst[4] != -39) return false;
			if (lst[5] != -20) return false;
			if (lst[6] != -10) return false;
			if (lst[7] != -5) return false;
			if (lst[8] != -3) return false;
			if (lst[9] != -2) return false;
			if (lst[10] != -1) return false;

			return true;
		}

		static bool CheckRightUn(List<int> lst)
		{
			if (lst.Count != 32) return false;

			if (lst[0] != 2147483031) return false;
			if (lst[1] != 1073741515) return false;
			if (lst[2] != 536870757) return false;
			if (lst[3] != 268435378) return false;
			if (lst[4] != 134217689) return false;
			if (lst[5] != 67108844) return false;
			if (lst[6] != 33554422) return false;
			if (lst[7] != 16777211) return false;
			if (lst[8] != 8388605) return false;
			if (lst[9] != 4194302) return false;
			if (lst[10] != 2097151) return false;
			if (lst[11] != 1048575) return false;
			if (lst[12] != 524287) return false;
			if (lst[13] != 262143) return false;
			if (lst[14] != 131071) return false;
			if (lst[15] != 65535) return false;
			if (lst[16] != 32767) return false;
			if (lst[17] != 16383) return false;
			if (lst[18] != 8191) return false;
			if (lst[19] != 4095) return false;
			if (lst[20] != 2047) return false;
			if (lst[21] != 1023) return false;
			if (lst[22] != 511) return false;
			if (lst[23] != 255) return false;
			if (lst[24] != 127) return false;
			if (lst[25] != 63) return false;
			if (lst[26] != 31) return false;
			if (lst[27] != 15) return false;
			if (lst[28] != 7) return false;
			if (lst[29] != 3) return false;
			if (lst[30] != 1) return false;
			if (lst[31] != 0) return false;

			return true;
		}

		public static int Entry()
		{
			var res = ShiftLeft(-1234);
			if (!CheckLeft(res))
				return 1;

			res = ShiftLeftUn(-1234);
			if (!CheckLeft(res))
				return 2;

			res = ShiftRight(-1234);
			if (!CheckRight(res))
				return 3;

			res = ShiftRightUn(-1234);
			if (!CheckRightUn(res))
				return 4;

			return 0;
		}
	}

	[CodeGen]
	static class TestNullable
	{
		struct MyStru
		{
			public int fld;
		}
		public static int Entry()
		{
			float? nf = 1.0f;
			float f = nf ?? 0;
			if (f != 1.0f)
				return 1;

			nf = null;
			f = nf ?? 0;
			if (f != 0)
				return 2;

			MyStru? ns = null;
			if (ns.HasValue)
				return 3;

			ns = new MyStru() { fld = 789 };
			if (ns == null)
				return 4;

			MyStru s = ns.Value;
			if (s.fld != 789)
				return 5;

			bool res = nf is float;
			if (res)
				return 6;

			nf = 78.9f;
			res = nf is float;
			if (!res)
				return 7;

			return 0;
		}
	}

	[CodeGen]
	static class TestBoxUnbox
	{
		public static int Entry()
		{
			int val = 123;
			object obj = (object)val;
			if (obj is int)
			{
				int unbox = (int)obj;

				if (unbox != 123)
					return 1;
			}
			else
				return 2;

			if (obj is uint)
				return 3;

			uint val2 = 456;
			obj = (object)val2;
			if (obj is int)
				return 4;

			if (obj is uint unbox2)
			{
				if (unbox2 != 456)
					return 5;
			}
			else return 6;

			return 0;
		}
	}

	[CodeGen]
	static class TestInPlace
	{
		class Cls
		{
			public int fld1, fld2;

			public Cls()
			{
			}

			public Cls(Cls other)
			{
				fld1 = 0;
				fld2 = 0;
				fld1 += other.fld1;
				fld2 += other.fld2;
			}
		}

		public static int Entry()
		{
			Cls cls = new Cls() { fld1 = 123, fld2 = 456 };
			cls = new Cls(cls);
			if (cls.fld1 != 123 || cls.fld2 != 456)
				return 1;
			return 0;
		}
	}

	[CodeGen]
	static class TestDelegate
	{
		delegate int FooFunc(int a, int b);

		class Cls
		{
			public int num;
			public int Foo(int a, int b)
			{
				return (a - b) * num;
			}
		}

		interface Inf
		{
			int Foo(int a, int b);
		}

		class ClsA : Inf
		{
			public int num;
			public int Foo(int a, int b)
			{
				return a + b + num;
			}
		}

		class ClsB : Inf
		{
			public int num;
			public int Foo(int a, int b)
			{
				return a - b - num;
			}
		}

		private static int SMethod(bool flag)
		{
			var cls = new Cls() { num = 34 };
			FooFunc pfn = cls.Foo;
			int result = pfn(789, 734);
			if (result != 1870)
				return 1;

			Inf inf;
			int cmp;
			if (flag)
			{
				inf = new ClsA { num = 88 };
				cmp = 339;
			}
			else
			{
				inf = new ClsB { num = 99 };
				cmp = -86;
			}
			pfn = inf.Foo;
			result = pfn(132, 119);
			if (result != cmp)
				return 2;

			return 0;
		}

		static int FuncCall(Func<bool, int> func, bool b)
		{
			return func(b);
		}

		private static int actres = -1;
		static void Act()
		{
			actres = SMethod(true);
			if (actres == 0)
				actres = SMethod(false);
		}

		static void ActAA()
		{
			++actres;
		}

		static void ActBB()
		{
			actres *= 10;
		}

		delegate int SFunc(bool b);
		public static int Entry()
		{
			SFunc pfn = SMethod;

			int res = pfn(true);
			if (res != 0)
				return res;

			pfn(false);
			if (res != 0)
				return res;

			Func<bool, int> lambda = bl =>
			{
				return TestDelegate.SMethod(bl);
			};
			res = FuncCall(lambda, true);
			if (res != 0)
				return res;

			res = FuncCall(lambda, false);
			if (res != 0)
				return res;

			Action act = Act;
			act();
			if (actres != 0)
				return actres;

			/*act -= Act;
			act += ActAA;
			act += ActBB;
			act();

			if (actres != 10)
				return actres;

			act -= ActBB;
			act();

			if (actres != 11)
				return actres;*/

			return 0;
		}
	}

	[CodeGen]
	static class TestConstrained
	{
		struct Stru
		{
			public int num;
			public override int GetHashCode()
			{
				return 1234 + num;
			}
		}

		struct Stru2
		{
			public int aa;
			public int bb;
		}

		struct Stru3
		{
			public Stru2 aa;
			public int bb;
		}

		class Cls
		{ }

		public static int Entry()
		{
			Stru s = new Stru();
			s.num = 4567;
			int code = s.GetHashCode();
			if (code != 5801)
				return 1;

			int numi4 = 123;
			code = numi4.GetHashCode();
			if (code != 123)
				return 2;

			float numr4 = 1.23f;
			code = numr4.GetHashCode();
			if (code != 1067282596)
				return 3;

			double numr8 = 1.23;
			code = numr8.GetHashCode();
			if (code != 1158867386)
				return 4;

			if (new Stru2 { aa = 123, bb = 456 }.GetHashCode() !=
				new Stru2 { aa = 123, bb = 456 }.GetHashCode())
				return 5;

			if (new Stru2 { aa = 124, bb = 456 }.GetHashCode() ==
				new Stru2 { aa = 123, bb = 456 }.GetHashCode())
				return 6;

			code = new Stru2 { aa = 123, bb = 457 }.GetHashCode();
			int code2 = new Stru2 { aa = 123, bb = 456 }.GetHashCode();
			if (code == code2)
				return 7;

			Stru2 stru = new Stru2();
			stru.aa = 789;
			stru.bb = 321;

			Stru3 stru2 = new Stru3();
			stru2.aa = stru;
			if (stru2.GetHashCode() == stru.GetHashCode())
				return 8;

			if (new Stru2 { aa = 123, bb = 456 }.GetHashCode() ==
				new Stru2 { aa = 456, bb = 123 }.GetHashCode())
				return 9;

			if (new Cls().GetHashCode() == new Cls().GetHashCode())
				return 10;

			return 0;
		}
	}

	[CodeGen]
	static class TestNoRef
	{
		class NoRefCls
		{
			public int aa = 1;
			public float bb = 1;
		}

		class RefCls
		{
			public int aa = 1;
			public string bb = "test";
		}

		class RefCls2
		{
			public int aa = 1;
			public NoRefCls bb = new NoRefCls();
		}

		struct NoRefStru
		{
			public int aa;
			public float bb;
		}

		struct NoRefStru2
		{
			public int aa;
			public NoRefStru bb;
		}

		struct RefStru
		{
			public int aa;
			public string bb;
		}

		struct RefStru2
		{
			public int aa;
			public RefStru bb;
		}

		public static int Entry()
		{
			var a = new NoRefCls();
			var b = new RefCls();
			var c = new RefCls2();
			object d = new NoRefStru() { aa = 1, bb = 2 };
			object e = new NoRefStru2() { aa = 1, bb = new NoRefStru() };
			object f = new RefStru() { aa = 1, bb = "test2" };
			object g = new RefStru2() { aa = 1, bb = new RefStru() };

			return 0;
		}
	}

	[CodeGen]
	static class TestContainer
	{
		static int TestList()
		{
			List<int> ilst = new List<int>() { 1, 2, 3 };
			ilst.Add(123);
			ilst.Add(456);

			int sum = 0;
			foreach (int n in ilst)
				sum += n;

			if (sum != 585)
				return 1;

			if (ilst.Count != 5)
				return 2;

			if (ilst[0] != 1 || ilst[4] != 456)
				return 3;

			ilst.Clear();
			if (ilst.Count != 0)
				return 4;

			ilst.Insert(0, 99);
			ilst.Insert(0, 88);
			ilst.Insert(0, 77);

			if (ilst[0] != 77 || ilst[1] != 88 || ilst[2] != 99)
				return 5;

			ilst.InsertRange(1, new[] { 11, 22 });
			if (ilst[0] != 77 || ilst[1] != 11 || ilst[2] != 22 || ilst[3] != 88 || ilst[4] != 99)
				return 6;

			/*if (ilst.IndexOf(99) != 4)
				return 9;
			if (ilst.LastIndexOf(11) != 1)
				return 10;*/

			ilst.RemoveRange(1, 3);
			if (ilst[0] != 77 || ilst[1] != 99)
				return 7;

			ilst.RemoveRange(0, 2);
			if (ilst.Count != 0)
				return 8;

			return 0;
		}

		public static int Entry()
		{
			int res = TestList();
			if (res != 0)
				return res;

			return 0;
		}
	}

	[CodeGen]
	static class TestContainer2
	{
		class ObjEqualityComparer<T> : EqualityComparer<T>
		{
			public override bool Equals(T x, T y)
			{
				if (x != null)
				{
					if (y != null) return x.Equals(y);
					return false;
				}
				if (y != null) return false;
				return true;
			}

			public override int GetHashCode(T obj) => obj?.GetHashCode() ?? 0;

			public override int GetHashCode() => 0;
		}

		static int TestObjEqualityComparer()
		{
			{
				ObjEqualityComparer<int> cint = new ObjEqualityComparer<int>();
				int int1 = 123;
				int int2 = 456;

				if (cint.Equals(int1, int2))
					return 1;

				if (!cint.Equals(int1, int1))
					return 2;
			}

			{
				ObjEqualityComparer<byte> cbyte = new ObjEqualityComparer<byte>();
				byte byte1 = 0xFF;
				byte byte2 = 0x12;

				if (cbyte.Equals(byte1, byte2))
					return 3;

				if (!cbyte.Equals(byte1, byte1))
					return 4;
			}

			{
				ObjEqualityComparer<object> cobj = new ObjEqualityComparer<object>();
				object obj1 = "hello";
				object obj2 = "world";

				if (!cobj.Equals(obj1, "hello"))
					return 5;

				if (cobj.Equals(obj1, obj2))
					return 6;
			}

			{
				ObjEqualityComparer<int?> cnul = new ObjEqualityComparer<int?>();
				int? n1 = 123;
				int? n2 = 456;
				int? n3 = null;

				if (!cnul.Equals(n1, 122 + 1))
					return 7;

				if (cnul.Equals(n1, n2))
					return 8;

				if (!cnul.Equals(n3, null))
					return 9;
			}

			return 0;
		}

		public static int Entry()
		{
			var dict = new Dictionary<int, int>();
			dict.Add(1, 123);
			dict.Add(2, 456);
			dict.Add(3, 789);
			dict[4] = 120;
			if (dict.Count != 4)
				return 1;

			if (!dict.ContainsKey(1))
				return 2;
			if (!dict.ContainsKey(2))
				return 3;
			if (!dict.ContainsKey(3))
				return 4;
			if (!dict.ContainsKey(4))
				return 5;
			if (dict.ContainsKey(0))
				return 6;

			if (dict[1] != 123)
				return 7;
			if (dict[2] != 456)
				return 8;
			if (dict[3] != 789)
				return 9;
			if (dict[4] != 120)
				return 10;

			int sumKey = 0;
			int sumVal = 0;
			foreach (var kv in dict)
			{
				sumKey += kv.Key;
				sumVal += kv.Value;
			}
			if (sumKey != 10)
				return 11;
			if (sumVal != 1488)
				return 12;

			dict.Remove(2);
			dict.Remove(3);
			if (dict.Count != 2)
				return 13;

			if (dict[1] != 123 || dict[4] != 120)
				return 14;

			dict.Clear();
			if (dict.Count != 0)
				return 15;

			int res = TestObjEqualityComparer();
			if (res != 0)
				return 20 + res;

			Dictionary<string, string> dict2 = new Dictionary<string, string>
			{
				{ "cat", "feline" },
				{ "dog", "canine" }
			};

			if (!dict2.TryGetValue("cat", out var test) ||
				test != "feline")
				return 30;

			if (dict2.TryGetValue("bird", out test))
				return 31;

			foreach (var pair in dict2)
			{
				string val = pair.Value;
				switch (pair.Key)
				{
					case "cat":
						if (val != "feline")
							return 32;
						break;

					case "dog":
						if (val != "canine")
							return 33;
						break;
				}
			}

			List<string> list = new List<string>(dict2.Keys);
			int b1 = 0, b2 = 0;
			foreach (var item in list)
			{
				switch (item)
				{
					case "cat":
						++b1;
						break;

					case "dog":
						++b2;
						break;
				}
			}

			if (b1 != 1 || b2 != 1)
				return 34;

			dict2["foo"] = "blabla";
			dict2.Add("123", "4567");

			if (!dict2.ContainsValue("feline") ||
				!dict2.ContainsValue("canine") ||
				!dict2.ContainsValue("blabla") ||
				!dict2.ContainsValue("4567"))
				return 35;

			var lst = dict2.ToList();
			int b3 = 0, b4 = 0;
			b1 = b2 = 0;
			foreach (var item in lst)
			{
				string val = item.Value;
				switch (item.Key)
				{
					case "cat":
						if (val != "feline")
							return 36;
						++b1;
						break;

					case "dog":
						if (val != "canine")
							return 37;
						++b2;
						break;

					case "foo":
						if (val != "blabla")
							return 38;
						++b3;
						break;

					case "123":
						if (val != "4567")
							return 39;
						++b4;
						break;
				}
			}

			if (b1 != 1 || b2 != 1 || b3 != 1 || b4 != 1)
				return 40;

			dict2.Remove("foo");
			dict2.Remove("123");
			if (dict2.ContainsValue("blabla") ||
				dict2.ContainsValue("4567"))
				return 41;

			/*string[] arr = new string[]
			{
				"abc",
				"defg"
			};
			var dict3 = arr.ToDictionary(item => item, item => item.Length);
			if (dict3.Count != 2)
				return 35;

			if (!dict3.TryGetValue("abc", out var value) ||
				value != 3)
				return 36;

			if (!dict3.TryGetValue("defg", out value) ||
				value != 4)
				return 37;*/


			return 0;
		}
	}

	[CodeGen]
	static class TestContainerPerf
	{
		public static int Entry()
		{
			const int amount = 50000000;
			Dictionary<int, int> dict = new Dictionary<int, int>();
			for (int i = 0; i < amount; ++i)
			{
				dict.Add(i, i);
				if (!dict.TryGetValue(i, out var oi) || oi != i)
					return 1;
			}
			return 0;
		}
	}

	//[CodeGen]
	static class TestYield
	{
		static IEnumerable<int> MyRange(int start, int end)
		{
			for (var i = start; i <= end; i++)
				yield return i;
		}

		static int MySum(this IEnumerable<int> numbers)
		{
			var sum = 0;
			foreach (var i in numbers)
				sum += i;
			return sum;
		}

		public static int Entry()
		{
			int a = MyRange(1, 100).MySum();

			return a;
		}
	}

	[CodeGen]
	static class TestReflection
	{
		public static int Entry()
		{
			float[] ary = { 1, 2, 3, 4, 5 };

			if (ary[0] != 1 ||
				ary[1] != 2 ||
				ary[2] != 3 ||
				ary[3] != 4 ||
				ary[4] != 5)
				return 1;

			float sum = 0;
			foreach (var n in ary)
				sum += n;
			if (!sum.IsEquals(15))
				return 2;

			return 0;
		}
	}

	[CodeGen]
	static class TestGarbageCollection
	{
		private const int Capacity = 50000;
		private static List<int> s_AA = new List<int>(Capacity);
		private static List<float> s_BB = new List<float>(Capacity);
		private static List<object> s_CC = new List<object>(Capacity);

		class Node
		{
			public Node next;
			public byte[] payload = new byte[64];
		}

		static TestGarbageCollection()
		{
			for (int i = 0; i < Capacity; ++i)
			{
				s_AA.Add(123);
				s_BB.Add(4.56f);
				s_CC.Add(null);
			}
		}

		private static int ProbeIsStaticAlive()
		{
			if (s_AA.Count != Capacity)
				return 1;

			foreach (var item in s_AA)
			{
				if (item != 123)
					return 2;
			}

			if (s_BB.Count != Capacity)
				return 3;

			foreach (var item in s_BB)
			{
				if (item != 4.56f)
					return 4;
			}

			if (s_CC.Count != Capacity)
				return 5;

			foreach (var item in s_CC)
			{
				if (item != null)
					return 6;
			}

			return 0;
		}

		public static int Entry()
		{
			Node curr = new Node();

			for (int i = 0; i < 10000; ++i)
			{
				curr.next = new Node();
				curr = curr.next;

				GC.Collect();

				int res = ProbeIsStaticAlive();
				if (res != 0)
					return 10 + res;
			}

			return 0;
		}
	}

	internal class Program
	{
		/*private static void MainRayTrace()
		{
			var tw = new Stopwatch();
			tw.Start();
			var c = TestRayTrace2.RenderEntry();
			tw.Stop();
			Console.WriteLine("Elapsed: {0}", tw.ElapsedMilliseconds);

			using (StreamWriter sw = new StreamWriter("imageCS.ppm"),
				sw2 = new StreamWriter("imageCS.ppm.d"))
			{
				sw.Write("P3\r\n{0} {1}\r\n{2}\r\n", 256, 256, 255);
				for (int i = 0; i < 256 * 256; i++)
				{
					sw.Write("{0} {1} {2}\r\n", TestRayTrace2.toInt(c[i].x), TestRayTrace2.toInt(c[i].y), TestRayTrace2.toInt(c[i].z));

					sw2.Write("{0:x}\r\n", TestRayTrace2.CalcHash(c[i]));
				}
			}
		}*/

		private static void Main()
		{
			var tw = new Stopwatch();
			tw.Start();

			var result = BindingTests.Entry();

			tw.Stop();
			Console.WriteLine("Result: {0}, Elapsed: {1}ms", result, tw.ElapsedMilliseconds);
		}
	}
}
