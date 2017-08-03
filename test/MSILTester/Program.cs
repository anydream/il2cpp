using System;

namespace TestIL
{
	class TestClassAttribute : Attribute
	{
		public string Result;

		public TestClassAttribute(string result)
		{
			Result = result;
		}
	}

	[TestClass(@"======
TestIL.TestInfImpl
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestInfImpl/Cls
-> void .ctor()
-> void Foo()
--> int field1

TestIL.TestInfImpl/Inf
-> void Foo() = 0
   \ void Foo(): TestIL.TestInfImpl/Cls

======
")]
	static class TestInfImpl
	{
		interface Inf
		{
			void Foo();
		}

		class Cls : Inf
		{
			private int field1;
			private int field2;

			public void Foo()
			{
				field1 = 0;
			}
		}

		public static void Entry()
		{
			Inf inf = new Cls();
			inf.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestClsImpl
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestClsImpl/SubCls
-> void .ctor()
-> int Foo(int)

TestIL.TestClsImpl/BaseCls
-> int Foo(int)
   | int Foo(int): TestIL.TestClsImpl/SubCls
   \ int Foo(int): TestIL.TestClsImpl/SubCls2
-> void .ctor()
--> int field

TestIL.TestClsImpl/SubCls2
-> void .ctor()
-> int Foo(int)

======
")]
	static class TestClsImpl
	{
		class BaseCls
		{
			public int field;
			public virtual int Foo(int n)
			{
				return n + field;
			}
		}

		class SubCls : BaseCls
		{
			public override int Foo(int n)
			{
				return n;
			}
		}

		class SubCls2 : BaseCls
		{
			public override int Foo(int n)
			{
				return base.Foo(n) + n;
			}
		}

		public static void Entry()
		{
			BaseCls b = new SubCls();
			b.Foo(123);

			b = new SubCls2();
			b.Foo(456);
		}
	}

	namespace Chain
	{
		abstract class BaseCls
		{
			private int field0;
			public abstract int Foo(int n);
			public virtual int Bla(int n)
			{
				return n + field0;
			}
		}

		class Sub1 : BaseCls
		{
			private int field1;
			public override int Foo(int n)
			{
				return field1 + n;
			}
			public override int Bla(int n)
			{
				return n;
			}
		}

		class Sub2 : Sub1
		{
			private int field2;
			public new virtual int Foo(int n)
			{
				return field2 + n;
			}
			public override int Bla(int n)
			{
				return n;
			}
		}

		class Sub3 : Sub2
		{
			private int field3;
			public override int Foo(int n)
			{
				return n + field3;
			}
			public override int Bla(int n)
			{
				return n;
			}
		}

		class Sub4 : Sub3
		{
			private int field4;
			public new int Foo(int n)
			{
				return field4 + n;
			}
			public override int Bla(int n)
			{
				return n;
			}
		}

		class Sub5 : Sub4
		{
			private int field5;
			private int field5_2;
			public virtual int Foo(int n)
			{
				return n + field5;
			}
			public override int Bla(int n)
			{
				return n + field5_2;
			}
		}
	}

	[TestClass(@"======
TestIL.TestNewOverrides
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.Chain.Sub1
-> void .ctor()
-> int Foo(int)
-> int Bla(int)
--> int field1

TestIL.Chain.BaseCls
-> int Foo(int) = 0
   \ int Foo(int): TestIL.Chain.Sub1
-> int Bla(int) = 0
   | int Bla(int): TestIL.Chain.Sub1
   \ int Bla(int): TestIL.Chain.Sub5
-> void .ctor()

TestIL.Chain.Sub5
-> void .ctor()
-> int Bla(int)
--> int field5_2

TestIL.Chain.Sub4
-> int Foo(int)
-> void .ctor()
--> int field4

TestIL.Chain.Sub3
-> void .ctor()
-> int Foo(int)
--> int field3

TestIL.Chain.Sub2
-> int Foo(int) = 0
   \ int Foo(int): TestIL.Chain.Sub3
-> void .ctor()

======
")]
	static class TestNewOverrides
	{
		public static void Entry()
		{
			new Chain.Sub1();

			var cls = new Chain.Sub5();
			Chain.BaseCls b = cls;
			b.Foo(123);
			b.Bla(456);

			Chain.Sub2 s2 = cls;
			s2.Foo(123);

			Chain.Sub4 s4 = cls;
			s4.Foo(123);
		}
	}

	[TestClass(@"======
TestIL.TestGenInfImpl
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestGenInfImpl/Sub2
-> void .ctor()
-> char*[] Foo<char*[]>(char*[],int*[])
-> char*[] Foo<char*[]>(char*[],short*[])

TestIL.TestGenInfImpl/Sub1`1<int*[]>
-> void .ctor()

TestIL.TestGenInfImpl/Cls`1<int*[]>
-> char*[] Foo<char*[]>(char*[],int*[])
   | char*[] Foo<char*[]>(char*[],int*[]): TestIL.TestGenInfImpl/Cls`1<int*[]>
   \ char*[] Foo<char*[]>(char*[],int*[]): TestIL.TestGenInfImpl/Sub2
-> char*[] Foo<char*[]>(char*[],short*[])
   | char*[] Foo<char*[]>(char*[],short*[]): TestIL.TestGenInfImpl/Cls`1<int*[]>
   \ char*[] Foo<char*[]>(char*[],short*[]): TestIL.TestGenInfImpl/Sub2
-> void .ctor()

TestIL.TestGenInfImpl/Inf`1<int*[]>
-> char*[] Foo<char*[]>(char*[],int*[]) = 0
   | char*[] Foo<char*[]>(char*[],int*[]): TestIL.TestGenInfImpl/Cls`1<int*[]>
   \ char*[] Foo<char*[]>(char*[],int*[]): TestIL.TestGenInfImpl/Sub2

TestIL.TestGenInfImpl/Inf2`1<short*[]>
-> char*[] Foo<char*[]>(char*[],short*[]) = 0
   | char*[] Foo<char*[]>(char*[],short*[]): TestIL.TestGenInfImpl/Cls`1<int*[]>
   | char*[] Foo<char*[]>(char*[],short*[]): TestIL.TestGenInfImpl/Sub2
   \ char*[] Foo<char*[]>(char*[],short*[]): TestIL.TestGenInfImpl/Cls`1<long*[]>

TestIL.TestGenInfImpl/Cls`1<long*[]>
-> void .ctor()
-> char*[] Foo<char*[]>(char*[],short*[])

======
")]
	static class TestGenInfImpl
	{
		interface Inf<TI>
		{
			TF Foo<TF>(TF n, TI i);
		}

		interface Inf2<TI>
		{
			TF Foo<TF>(TF n, TI i);
		}

		unsafe class Cls<TC> : Inf<TC>, Inf2<short*[]>
		{
			public virtual TCF Foo<TCF>(TCF n, TC i)
			{
				return n;
			}

			public virtual TF Foo<TF>(TF n, short*[] i)
			{
				return n;
			}
		}

		unsafe class Sub1<TC1> : Cls<TC1>
		{
			private TC1 field1;
			private short*[] field2;

			public override TCF1 Foo<TCF1>(TCF1 n, TC1 i)
			{
				field1 = i;
				return n;
			}

			public override TCF1 Foo<TCF1>(TCF1 n, short*[] i)
			{
				field2 = i;
				return n;
			}
		}

		unsafe class Sub2 : Sub1<int*[]>
		{
			public override TCF2 Foo<TCF2>(TCF2 n, int*[] i)
			{
				return n;//base.Foo(n, i);
			}

			public override unsafe TCF2 Foo<TCF2>(TCF2 n, short*[] i)
			{
				return n;//base.Foo(n, i);
			}
		}

		public static unsafe void Entry()
		{
			char*[] cc = null;
			int*[] ii = null;
			short*[] ss = null;

			var cls = new Sub2();

			cls.Foo(cc, ii);
			cls.Foo(cc, ss);

			Inf<int*[]> inf = cls;
			inf.Foo(cc, ii);

			Inf2<short*[]> inf2 = cls;
			inf2.Foo(cc, ss);

			var cls2 = new Cls<long*[]>();

			Sub1<int*[]> s1 = cls;
			s1.Foo(cc, ii);
			s1.Foo(cc, ss);

			new Cls<int*[]>();
		}
	}

	namespace ExplicitOverride
	{
		interface Inf<T>
		{
			void Foo();
		}

		interface Inf
		{
			void Foo();
		}

		class Cls : Inf, Inf<short>, Inf<uint>
		{
			private int field1;
			private int field2;
			private int field3;

			public void Foo()
			{
				field1 = 0;
			}

			void Inf.Foo()
			{
				field2 = 0;
			}

			void Inf<short>.Foo()
			{
				field3 = 0;
			}
		}

		class Sub1 : Cls, Inf<short>
		{
			void Inf<short>.Foo()
			{

			}
		}

		class Sub2 : Cls, Inf<uint>
		{
			void Inf<uint>.Foo()
			{

			}
		}
	}

	[TestClass(@"======
TestIL.TestExplicitOverride1
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.ExplicitOverride.Cls
-> void .ctor()
-> void Foo()
   \ void Foo(): TestIL.ExplicitOverride.Cls
--> int field1

======
")]
	static class TestExplicitOverride1
	{
		public static void Entry()
		{
			var cls = new ExplicitOverride.Cls();
			cls.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestExplicitOverride2
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.ExplicitOverride.Cls
-> void .ctor()
-> void TestIL.ExplicitOverride.Inf.Foo()
--> int field2

TestIL.ExplicitOverride.Inf
-> void Foo() = 0
   \ void TestIL.ExplicitOverride.Inf.Foo(): TestIL.ExplicitOverride.Cls

======
")]
	static class TestExplicitOverride2
	{
		public static void Entry()
		{
			var cls = new ExplicitOverride.Cls();
			ExplicitOverride.Inf inf = cls;
			inf.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestExplicitOverride3
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.ExplicitOverride.Cls
-> void .ctor()
-> void TestIL.ExplicitOverride.Inf<System.Int16>.Foo()
--> int field3

TestIL.ExplicitOverride.Inf`1<short>
-> void Foo() = 0
   \ void TestIL.ExplicitOverride.Inf<System.Int16>.Foo(): TestIL.ExplicitOverride.Cls

======
")]
	static class TestExplicitOverride3
	{
		public static void Entry()
		{
			var cls = new ExplicitOverride.Cls();
			ExplicitOverride.Inf<short> inf = cls;
			inf.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestExplicitOverride4
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.ExplicitOverride.Sub1
-> void .ctor()
-> void TestIL.ExplicitOverride.Inf<System.Int16>.Foo()

TestIL.ExplicitOverride.Cls
-> void .ctor()
-> void TestIL.ExplicitOverride.Inf.Foo()
--> int field2

TestIL.ExplicitOverride.Inf
-> void Foo() = 0
   \ void TestIL.ExplicitOverride.Inf.Foo(): TestIL.ExplicitOverride.Cls

TestIL.ExplicitOverride.Inf`1<short>
-> void Foo() = 0
   \ void TestIL.ExplicitOverride.Inf<System.Int16>.Foo(): TestIL.ExplicitOverride.Sub1

======
")]
	static class TestExplicitOverride4
	{
		public static void Entry()
		{
			var cls = new ExplicitOverride.Sub1();
			ExplicitOverride.Inf<short> inf = cls;
			inf.Foo();
			ExplicitOverride.Inf inf2 = cls;
			inf2.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestExplicitOverride5
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.ExplicitOverride.Sub1
-> void .ctor()

TestIL.ExplicitOverride.Cls
-> void .ctor()
-> void Foo()
--> int field1

TestIL.ExplicitOverride.Inf`1<uint>
-> void Foo() = 0
   \ void Foo(): TestIL.ExplicitOverride.Cls

======
")]
	static class TestExplicitOverride5
	{
		public static void Entry()
		{
			var cls = new ExplicitOverride.Sub1();
			ExplicitOverride.Inf<uint> inf = cls;
			inf.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestExplicitOverride6
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.ExplicitOverride.Sub2
-> void .ctor()
-> void TestIL.ExplicitOverride.Inf<System.UInt32>.Foo()

TestIL.ExplicitOverride.Cls
-> void .ctor()
-> void TestIL.ExplicitOverride.Inf<System.Int16>.Foo()
--> int field3

TestIL.ExplicitOverride.Inf`1<short>
-> void Foo() = 0
   \ void TestIL.ExplicitOverride.Inf<System.Int16>.Foo(): TestIL.ExplicitOverride.Cls

TestIL.ExplicitOverride.Inf`1<uint>
-> void Foo() = 0
   \ void TestIL.ExplicitOverride.Inf<System.UInt32>.Foo(): TestIL.ExplicitOverride.Sub2

======
")]
	static class TestExplicitOverride6
	{
		public static void Entry()
		{
			var cls = new ExplicitOverride.Sub2();
			ExplicitOverride.Inf<uint> inf = cls;
			inf.Foo();

			ExplicitOverride.Inf<short> inf2 = cls;
			inf2.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestExplicitOverride7
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestExplicitOverride7/Cls`1<TestIL.TestExplicitOverride7/Elem>
-> void .ctor()
-> TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem> TestIL.TestExplicitOverride7.Inf<TC>.Foo(TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem>)

TestIL.TestExplicitOverride7/Inf`1<TestIL.TestExplicitOverride7/Elem>
-> TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem> Foo(TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem>) = 0
   \ TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem> TestIL.TestExplicitOverride7.Inf<TC>.Foo(TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem>): TestIL.TestExplicitOverride7/Cls`1<TestIL.TestExplicitOverride7/Elem>

======
")]
	static class TestExplicitOverride7
	{
		interface IBla<TB>
		{ }

		interface Inf<TI>
		{
			IBla<TI> Foo(IBla<TI> n);
		}

		class Cls<TC> : Inf<TC>
		{
			IBla<TC> Inf<TC>.Foo(IBla<TC> n)
			{
				return n;
			}

			public IBla<TC> Foo(IBla<TC> n)
			{
				return n;
			}
		}

		class Elem
		{ }

		public static void Entry()
		{
			Inf<Elem> inf = new Cls<Elem>();
			inf.Foo(null);
		}
	}

	[TestClass(@"======
TestIL.TestCrossOverride
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestCrossOverride/Sub3
-> void .ctor()

TestIL.TestCrossOverride/Sub2
-> void .ctor()
-> int getWidth()
--> int field2

TestIL.TestCrossOverride/Sub1
-> int getWidth() = 0
   \ int getWidth(): TestIL.TestCrossOverride/Sub2
-> int getHeight()
   \ int getHeight(): TestIL.TestCrossOverride/Sub1
-> void .ctor()

TestIL.TestCrossOverride/Inf
-> int getWidth() = 0
   \ int getWidth(): TestIL.TestCrossOverride/Sub2
-> int getHeight() = 0
   \ int getHeight(): TestIL.TestCrossOverride/Sub1

======
")]
	static class TestCrossOverride
	{
		interface Inf
		{
			int getWidth();
			int getHeight();
		}

		class Sub1
		{
			public virtual int getWidth()
			{
				return 0;
			}

			public int getHeight()
			{
				return 0;
			}
		}

		class Sub2 : Sub1
		{
			private int field2;
			public override int getWidth()
			{
				return field2;
			}
		}

		class Sub3 : Sub2, Inf
		{
			//public int Width { get; }
		}

		public static void Entry()
		{
			var cls = new Sub3();
			Inf inf = cls;
			int a = inf.getWidth();
			a = inf.getHeight();

			Sub1 t2d = cls;
			a = t2d.getWidth();
			a = t2d.getHeight();
		}
	}

	[TestClass(@"======
TestIL.TestCrossOverride2
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestCrossOverride2/Cls
-> void .ctor()

TestIL.TestCrossOverride2/Base
-> void .ctor()
-> int Foo(short)

TestIL.TestCrossOverride2/Inf`2<int,short>
-> int Foo(short) = 0
   \ int Foo(short): TestIL.TestCrossOverride2/Base

======
")]
	static class TestCrossOverride2
	{
		class Base
		{
			public int Foo(short n)
			{
				return n;
			}
		}

		interface Inf<T1, T2>
		{
			T1 Foo(T2 n);
		}

		class Cls : Base, Inf<int, short>
		{
		}

		public static void Entry()
		{
			Inf<int, short> inf = new Cls();
			int n = inf.Foo(123);
		}
	}

	[TestClass(@"======
TestIL.TestCrossOverride3
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestCrossOverride3/Derived
-> void .ctor()

TestIL.TestCrossOverride3/Base
-> void .ctor()
-> void TestIL.TestCrossOverride3.Inf.fun()

TestIL.TestCrossOverride3/Inf
-> void fun() = 0
   \ void TestIL.TestCrossOverride3.Inf.fun(): TestIL.TestCrossOverride3/Base

======
")]
	static class TestCrossOverride3
	{
		interface Inf
		{
			void fun();
		}

		class Base : Inf
		{
			void Inf.fun() { }
		}

		class Derived : Base, Inf
		{
		}

		public static void Entry()
		{
			(new Derived() as Inf).fun();
		}
	}

	[TestClass(@"======
TestIL.TestCrossOverride4
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestCrossOverride4/Derived`1<long>
-> void .ctor()

TestIL.TestCrossOverride4/Base`1<long>
-> void .ctor()
-> int TestIL.TestCrossOverride4.Inf<System.Int32,TB>.fun(int)
-> int TestIL.TestCrossOverride4.Inf<System.Int32,TB>.fun(int,long)

TestIL.TestCrossOverride4/Inf`2<int,long>
-> int fun(int) = 0
   \ int TestIL.TestCrossOverride4.Inf<System.Int32,TB>.fun(int): TestIL.TestCrossOverride4/Base`1<long>
-> int fun(int,long) = 0
   \ int TestIL.TestCrossOverride4.Inf<System.Int32,TB>.fun(int,long): TestIL.TestCrossOverride4/Base`1<long>

======
")]
	static class TestCrossOverride4
	{
		interface Inf
		{
			int fun(int n);
		}

		interface Inf<TI, TI2>
		{
			TI fun(TI n);
			TI fun(TI n, TI2 n2);
		}

		class Base<TB> : Inf<int, TB>
		{
			int Inf<int, TB>.fun(int n)
			{
				return n;
			}

			int Inf<int, TB>.fun(int n, TB n2)
			{
				return n;
			}
		}

		class Derived<TD> : Base<TD>, Inf<int, TD>
		{
		}

		public static void Entry()
		{
			var cls = new Derived<long>();
			Inf<int, long> inf = cls;
			inf.fun(123);
			inf.fun(123, 456L);
		}
	}

	[TestClass(@"======
TestIL.TestCrossOverride5
-> static void Entry()
-> static char EntryT<char>(char)

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestCrossOverride5/Derived`2<char,ushort>
-> void .ctor()

TestIL.TestCrossOverride5/Base`2<ushort,char>
-> void .ctor()
-> int TestIL.TestCrossOverride5.Inf<System.Int32,TB>.fun(int)
-> int TestIL.TestCrossOverride5.Inf<System.Int32,TB>.fun<string>(int,char,string)

TestIL.TestCrossOverride5/Inf`2<int,char>
-> int fun(int) = 0
   \ int TestIL.TestCrossOverride5.Inf<System.Int32,TB>.fun(int): TestIL.TestCrossOverride5/Base`2<ushort,char>
-> int fun<string>(int,char,string) = 0
   \ int TestIL.TestCrossOverride5.Inf<System.Int32,TB>.fun<string>(int,char,string): TestIL.TestCrossOverride5/Base`2<ushort,char>

======
")]
	static class TestCrossOverride5
	{
		interface Inf
		{
			int fun(int n);
		}

		interface Inf<TI, TI2>
		{
			TI fun(TI n);
			TI fun<TF>(TI n, TI2 n2, TF n3);
			TI fun<TF>(TF n3, TI2 n2, TI n);
		}

		class Base<TB0, TB> : Inf<int, TB>
		{
			public virtual int fun(int n, long n1)
			{
				return n;
			}

			int Inf<int, TB>.fun(int n)
			{
				return n;
			}

			int Inf<int, TB>.fun<TF>(TF n3, TB n2, int n)
			{
				return n;
			}

#if true
			int Inf<int, TB>.fun<TF>(int n, TB n2, TF n3)
			{
				return n;
			}
#else
			public int fun<TF>(int n, TB n2, TF n3)
			{
				return n;
			}
#endif
		}

		class Derived<TD, TD1> : Base<TD1, TD>, Inf<int, TD>
		{
			public override int fun(int n, long n1)
			{
				return n;
			}
		}

		private static T EntryT<T>(T bla)
		{
			var cls = new Derived<T, ushort>();
			Inf<int, T> inf = cls;
			T t = bla;
			inf.fun(123);
			inf.fun(123, t, "");
			return bla;
		}

		public static void Entry()
		{
			EntryT('c');
		}
	}

	[TestClass(@"======
TestIL.TestCrossOverride6
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestCrossOverride6/Sub1
-> void .ctor()

TestIL.TestCrossOverride6/Base
-> void .ctor()
-> int TestIL.TestCrossOverride6.IInf.foo(int)

TestIL.TestCrossOverride6/IInf
-> int foo(int) = 0
   | int TestIL.TestCrossOverride6.IInf.foo(int): TestIL.TestCrossOverride6/Base
   \ int foo(int): TestIL.TestCrossOverride6/Sub2

TestIL.TestCrossOverride6/Sub2
-> void .ctor()
-> int foo(int)

======
")]
	static class TestCrossOverride6
	{
		interface IInf
		{
			int foo(int n);
		}

		class Base : IInf
		{
			int IInf.foo(int n)
			{
				return n;
			}
		}

		class Sub1 : Base
		{
			public int foo(int n)
			{
				return n;
			}
		}

		class Sub2 : Base, IInf
		{
			public int foo(int n)
			{
				return n;
			}
		}

		public static void Entry()
		{
			IInf inf = new Sub1();
			inf.foo(123);

			inf = new Sub2();
			inf.foo(456);
		}
	}

	[TestClass(@"======
TestIL.TestCrossOverride7
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestCrossOverride7/Sub
-> void .ctor()

TestIL.TestCrossOverride7/Middle
-> void .ctor()
-> int TestIL.TestCrossOverride7.Inf.foo()
--> int field2

TestIL.TestCrossOverride7/Base
-> void .ctor()

TestIL.TestCrossOverride7/Inf
-> int foo() = 0
   \ int TestIL.TestCrossOverride7.Inf.foo(): TestIL.TestCrossOverride7/Middle

======
")]
	static class TestCrossOverride7
	{
		interface Inf
		{
			int foo();
		}

		class Base : Inf
		{
			private int field1;
			public virtual int foo()
			{
				return field1;
			}
		}

		class Middle : Base, Inf
		{
			private int field2;
			int Inf.foo()
			{
				return field2;
			}
		}

		class Sub : Middle
		{
			private int field3;
			public override int foo()
			{
				return field3;
			}
		}

		public static void Entry()
		{
			var cls = new Sub();
			Inf inf = cls;
			inf.foo();
		}
	}

	[TestClass(@"======
TestIL.TestCrossOverride8
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestCrossOverride8/Sub2
-> void .ctor()

TestIL.TestCrossOverride8/Sub
-> void .ctor()
-> void TestIL.TestCrossOverride8.Inf.Foo()

TestIL.TestCrossOverride8/Base
-> void .ctor()

TestIL.TestCrossOverride8/Inf
-> void Foo() = 0
   \ void TestIL.TestCrossOverride8.Inf.Foo(): TestIL.TestCrossOverride8/Sub

======
")]
	static class TestCrossOverride8
	{
		interface Inf
		{
			void Foo();
		}

		class Base : Inf
		{
			public virtual void Foo()
			{

			}
		}

		class Sub : Base, Inf
		{
			void Inf.Foo()
			{

			}
		}

		class Sub2 : Sub, Inf
		{
		}

		public static void Entry()
		{
			Inf inf = new Sub2();
			inf.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestCrossOverride9
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestCrossOverride9/Sub3
-> void .ctor()

TestIL.TestCrossOverride9/Sub2
-> void .ctor()

TestIL.TestCrossOverride9/Sub
-> void .ctor()
-> void TestIL.TestCrossOverride9.Inf.Foo()

TestIL.TestCrossOverride9/Base
-> void .ctor()

TestIL.TestCrossOverride9/Inf
-> void Foo() = 0
   \ void TestIL.TestCrossOverride9.Inf.Foo(): TestIL.TestCrossOverride9/Sub

======
")]
	static class TestCrossOverride9
	{
		interface Inf
		{
			void Foo();
		}

		class Base : Inf
		{
			public virtual void Foo()
			{

			}
		}

		class Sub : Base, Inf
		{
			void Inf.Foo()
			{

			}
		}

		class Sub2 : Sub, Inf
		{
		}

		class Sub3 : Sub2
		{
			public override void Foo()
			{

			}
		}

		public static void Entry()
		{
			Inf inf = new Sub3();
			inf.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestStaticCctor
-> static void Entry()

TestIL.TestStaticCctor/Cls
-> static void .cctor()
--> static int field
--> static int field2

======
")]
	static class TestStaticCctor
	{
		class Cls
		{
			public static int field;
			private static int field2;
			private static int field3;

			static Cls()
			{
				field2 = 456;
			}

			~Cls()
			{
			}
		}

		public static void Entry()
		{
			Cls.field = 123;
		}
	}

	[TestClass(@"======
TestIL.TestFinalizer
-> static void Entry()

object
-> void .ctor()
-> void Finalize() = 0
   \ void Finalize(): TestIL.TestFinalizer/Cls

TestIL.TestFinalizer/Cls
-> void .ctor()
-> static void .cctor()
-> void Finalize()
--> static int field2

======
")]
	static class TestFinalizer
	{
		class Cls
		{
			public static int field;
			private static int field2;
			private static int field3;

			static Cls()
			{
				field2 = 456;
			}

			~Cls()
			{
			}
		}

		public static void Entry()
		{
			object obj = new Cls();
		}
	}

#if false
	[TestClass(@"======
TestIL.TestDifferentBCL
-> static void Entry()

object
-> int GetHashCode() = 0
   | int GetHashCode(): TestAdapter2.Test/Cls
   \ int GetHashCode(): TestAdapter1.Test/Cls
-> void .ctor()
-> void Finalize()
   | void Finalize(): object
   \ void Finalize(): object

TestAdapter2.Test
-> static object Create()
-> static void Accept(object)

TestAdapter1.Test
-> static void Accept(object)
-> static object Create()

object
-> int GetHashCode() = 0
   | int GetHashCode(): TestAdapter2.Test/Cls
   \ int GetHashCode(): TestAdapter1.Test/Cls
-> void .ctor()
-> void Finalize()
   | void Finalize(): object
   \ void Finalize(): object

TestAdapter2.Test/Cls
-> void .ctor()
-> int GetHashCode()

TestAdapter1.Test/Cls
-> void .ctor()
-> int GetHashCode()

======
")]
	static class TestDifferentBCL
	{
		public static void Entry()
		{
			TestAdapter1.Test.Accept(TestAdapter2.Test.Create());
			TestAdapter2.Test.Accept(TestAdapter1.Test.Create());
		}
	}
#endif

	[TestClass(@"======
TestIL.TestNullCall
-> static void Entry()

TestIL.TestNullCall/Inf
-> void Foo() = 0

======
")]
	static class TestNullCall
	{
		interface Inf
		{
			void Foo();
		}

		public static void Entry()
		{
			Inf inf = null;
			inf.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestCycleTypeRef
-> static void Entry()

object
-> void .ctor()
-> void Finalize()
   \ void Finalize(): object

TestIL.TestCycleTypeRef/A`1<int>
-> void .ctor()

TestIL.TestCycleTypeRef/C`1<TestIL.TestCycleTypeRef/B`1<int>>
-> void .ctor()

TestIL.TestCycleTypeRef/B`1<int>
-> void .ctor()

TestIL.TestCycleTypeRef/C`1<TestIL.TestCycleTypeRef/A`1<int>>
-> void .ctor()

======
")]
	static class TestCycleTypeRef
	{
		class A<T> : C<B<T>>
		{ }

		class B<T> : C<A<T>>
		{ }

		class C<T>
		{ }

		public static void Entry()
		{
			var aa = new A<int>();
			var bb = new B<int>();
		}
	}

	class Program
	{
		static void Main()
		{
		}
	}
}
