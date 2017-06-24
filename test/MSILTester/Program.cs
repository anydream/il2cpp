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
-> void Entry()

object
-> void .ctor()

TestIL.TestInfImpl/Cls
-> void .ctor()
-> void Foo()
--> int field1

TestIL.TestInfImpl/Inf
-> void Foo() = 0
   \ void Foo(): TestIL.TestInfImpl/Cls

======
void
System.ValueType
int
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<int>
System.IEquatable`1<int>
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
-> void Entry()

object
-> void .ctor()

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
void
System.ValueType
int
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<int>
System.IEquatable`1<int>
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
-> void Entry()

object
-> void .ctor()

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
void
System.ValueType
int
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<int>
System.IEquatable`1<int>
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
-> void Entry()

object
-> void .ctor()

TestIL.TestGenInfImpl/Sub2
-> void .ctor()
-> char*[] Foo<char*[]>(char*[],int*[])
-> char*[] Foo<char*[]>(char*[],short*[])

TestIL.TestGenInfImpl/Sub1`1<int*[]>
-> void .ctor()

TestIL.TestGenInfImpl/Cls`1<int*[]>
-> char*[] Foo<char*[]>(char*[],int*[])
   | char*[] Foo<char*[]>(char*[],int*[]): TestIL.TestGenInfImpl/Sub2
   \ char*[] Foo<char*[]>(char*[],int*[]): TestIL.TestGenInfImpl/Cls`1<int*[]>
-> char*[] Foo<char*[]>(char*[],short*[])
   | char*[] Foo<char*[]>(char*[],short*[]): TestIL.TestGenInfImpl/Sub2
   \ char*[] Foo<char*[]>(char*[],short*[]): TestIL.TestGenInfImpl/Cls`1<int*[]>
-> void .ctor()

TestIL.TestGenInfImpl/Inf`1<int*[]>
-> char*[] Foo<char*[]>(char*[],int*[]) = 0
   | char*[] Foo<char*[]>(char*[],int*[]): TestIL.TestGenInfImpl/Sub2
   \ char*[] Foo<char*[]>(char*[],int*[]): TestIL.TestGenInfImpl/Cls`1<int*[]>

TestIL.TestGenInfImpl/Inf2`1<short*[]>
-> char*[] Foo<char*[]>(char*[],short*[]) = 0
   | char*[] Foo<char*[]>(char*[],short*[]): TestIL.TestGenInfImpl/Sub2
   | char*[] Foo<char*[]>(char*[],short*[]): TestIL.TestGenInfImpl/Cls`1<long*[]>
   \ char*[] Foo<char*[]>(char*[],short*[]): TestIL.TestGenInfImpl/Cls`1<int*[]>

TestIL.TestGenInfImpl/Cls`1<long*[]>
-> void .ctor()
-> char*[] Foo<char*[]>(char*[],short*[])

======
void
System.ValueType
int*[]
short*[]
char*[]
long*[]
TestIL.TestGenInfImpl/Inf`1<long*[]>
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
-> void Entry()

object
-> void .ctor()

TestIL.ExplicitOverride.Cls
-> void .ctor()
-> void Foo()
   \ void Foo(): TestIL.ExplicitOverride.Cls
--> int field1

======
void
System.ValueType
TestIL.ExplicitOverride.Inf
short
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<short>
System.IEquatable`1<short>
TestIL.ExplicitOverride.Inf`1<short>
uint
System.IComparable`1<uint>
System.IEquatable`1<uint>
TestIL.ExplicitOverride.Inf`1<uint>
int
System.IComparable`1<int>
System.IEquatable`1<int>
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
-> void Entry()

object
-> void .ctor()

TestIL.ExplicitOverride.Cls
-> void .ctor()
-> void TestIL.ExplicitOverride.Inf.Foo()
--> int field2

TestIL.ExplicitOverride.Inf
-> void Foo() = 0
   \ void TestIL.ExplicitOverride.Inf.Foo(): TestIL.ExplicitOverride.Cls

======
void
System.ValueType
short
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<short>
System.IEquatable`1<short>
TestIL.ExplicitOverride.Inf`1<short>
uint
System.IComparable`1<uint>
System.IEquatable`1<uint>
TestIL.ExplicitOverride.Inf`1<uint>
int
System.IComparable`1<int>
System.IEquatable`1<int>
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
-> void Entry()

object
-> void .ctor()

TestIL.ExplicitOverride.Cls
-> void .ctor()
-> void TestIL.ExplicitOverride.Inf<System.Int16>.Foo()
--> int field3

TestIL.ExplicitOverride.Inf`1<short>
-> void Foo() = 0
   \ void TestIL.ExplicitOverride.Inf<System.Int16>.Foo(): TestIL.ExplicitOverride.Cls

======
void
System.ValueType
TestIL.ExplicitOverride.Inf
short
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<short>
System.IEquatable`1<short>
uint
System.IComparable`1<uint>
System.IEquatable`1<uint>
TestIL.ExplicitOverride.Inf`1<uint>
int
System.IComparable`1<int>
System.IEquatable`1<int>
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
-> void Entry()

object
-> void .ctor()

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
void
System.ValueType
short
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<short>
System.IEquatable`1<short>
uint
System.IComparable`1<uint>
System.IEquatable`1<uint>
TestIL.ExplicitOverride.Inf`1<uint>
int
System.IComparable`1<int>
System.IEquatable`1<int>
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
-> void Entry()

object
-> void .ctor()

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
void
System.ValueType
TestIL.ExplicitOverride.Inf
short
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<short>
System.IEquatable`1<short>
TestIL.ExplicitOverride.Inf`1<short>
uint
System.IComparable`1<uint>
System.IEquatable`1<uint>
int
System.IComparable`1<int>
System.IEquatable`1<int>
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
-> void Entry()

object
-> void .ctor()

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
void
System.ValueType
TestIL.ExplicitOverride.Inf
short
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<short>
System.IEquatable`1<short>
uint
System.IComparable`1<uint>
System.IEquatable`1<uint>
int
System.IComparable`1<int>
System.IEquatable`1<int>
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
-> void Entry()

object
-> void .ctor()

TestIL.TestExplicitOverride7/Cls`1<TestIL.TestExplicitOverride7/Elem>
-> void .ctor()
-> TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem> TestIL.TestExplicitOverride7.Inf<TC>.Foo(TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem>)

TestIL.TestExplicitOverride7/Inf`1<TestIL.TestExplicitOverride7/Elem>
-> TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem> Foo(TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem>) = 0
   \ TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem> TestIL.TestExplicitOverride7.Inf<TC>.Foo(TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem>): TestIL.TestExplicitOverride7/Cls`1<TestIL.TestExplicitOverride7/Elem>

======
void
System.ValueType
TestIL.TestExplicitOverride7/Elem
TestIL.TestExplicitOverride7/IBla`1<TestIL.TestExplicitOverride7/Elem>
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
-> void Entry()

object
-> void .ctor()

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
void
System.ValueType
int
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<int>
System.IEquatable`1<int>
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
-> void Entry()

object
-> void .ctor()

TestIL.TestCrossOverride2/Cls
-> void .ctor()

TestIL.TestCrossOverride2/Base
-> void .ctor()
-> int Foo(short)

TestIL.TestCrossOverride2/Inf`2<int,short>
-> int Foo(short) = 0
   \ int Foo(short): TestIL.TestCrossOverride2/Base

======
void
System.ValueType
int
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<int>
System.IEquatable`1<int>
short
System.IComparable`1<short>
System.IEquatable`1<short>
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
-> void Entry()

object
-> void .ctor()

TestIL.TestCrossOverride3/Derived
-> void .ctor()

TestIL.TestCrossOverride3/Base
-> void .ctor()
-> void TestIL.TestCrossOverride3.Inf.fun()

TestIL.TestCrossOverride3/Inf
-> void fun() = 0
   \ void TestIL.TestCrossOverride3.Inf.fun(): TestIL.TestCrossOverride3/Base

======
void
System.ValueType
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

	class Program
	{
		static void Main()
		{
			TestExplicitOverride7.Entry();
		}
	}
}
