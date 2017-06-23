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

	namespace SelectInfImpl
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
TestIL.TestSelectInfImpl1
-> void Entry()

object
-> void .ctor()

TestIL.SelectInfImpl.Cls
-> void .ctor()
-> void Foo()
   \ void Foo(): TestIL.SelectInfImpl.Cls
--> int field1

======
void
System.ValueType
TestIL.SelectInfImpl.Inf
short
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<short>
System.IEquatable`1<short>
TestIL.SelectInfImpl.Inf`1<short>
uint
System.IComparable`1<uint>
System.IEquatable`1<uint>
TestIL.SelectInfImpl.Inf`1<uint>
int
System.IComparable`1<int>
System.IEquatable`1<int>
")]
	static class TestSelectInfImpl1
	{
		public static void Entry()
		{
			var cls = new SelectInfImpl.Cls();
			cls.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestSelectInfImpl2
-> void Entry()

object
-> void .ctor()

TestIL.SelectInfImpl.Cls
-> void .ctor()
-> void TestIL.SelectInfImpl.Inf.Foo()
--> int field2

TestIL.SelectInfImpl.Inf
-> void Foo() = 0
   \ void TestIL.SelectInfImpl.Inf.Foo(): TestIL.SelectInfImpl.Cls

======
void
System.ValueType
short
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<short>
System.IEquatable`1<short>
TestIL.SelectInfImpl.Inf`1<short>
uint
System.IComparable`1<uint>
System.IEquatable`1<uint>
TestIL.SelectInfImpl.Inf`1<uint>
int
System.IComparable`1<int>
System.IEquatable`1<int>
")]
	static class TestSelectInfImpl2
	{
		public static void Entry()
		{
			var cls = new SelectInfImpl.Cls();
			SelectInfImpl.Inf inf = cls;
			inf.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestSelectInfImpl3
-> void Entry()

object
-> void .ctor()

TestIL.SelectInfImpl.Cls
-> void .ctor()
-> void TestIL.SelectInfImpl.Inf<System.Int16>.Foo()
--> int field3

TestIL.SelectInfImpl.Inf`1<short>
-> void Foo() = 0
   \ void TestIL.SelectInfImpl.Inf<System.Int16>.Foo(): TestIL.SelectInfImpl.Cls

======
void
System.ValueType
TestIL.SelectInfImpl.Inf
short
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<short>
System.IEquatable`1<short>
uint
System.IComparable`1<uint>
System.IEquatable`1<uint>
TestIL.SelectInfImpl.Inf`1<uint>
int
System.IComparable`1<int>
System.IEquatable`1<int>
")]
	static class TestSelectInfImpl3
	{
		public static void Entry()
		{
			var cls = new SelectInfImpl.Cls();
			SelectInfImpl.Inf<short> inf = cls;
			inf.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestSelectInfImpl4
-> void Entry()

object
-> void .ctor()

TestIL.SelectInfImpl.Sub1
-> void .ctor()
-> void TestIL.SelectInfImpl.Inf<System.Int16>.Foo()

TestIL.SelectInfImpl.Cls
-> void .ctor()
-> void TestIL.SelectInfImpl.Inf.Foo()
--> int field2

TestIL.SelectInfImpl.Inf
-> void Foo() = 0
   \ void TestIL.SelectInfImpl.Inf.Foo(): TestIL.SelectInfImpl.Cls

TestIL.SelectInfImpl.Inf`1<short>
-> void Foo() = 0
   \ void TestIL.SelectInfImpl.Inf<System.Int16>.Foo(): TestIL.SelectInfImpl.Sub1

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
TestIL.SelectInfImpl.Inf`1<uint>
int
System.IComparable`1<int>
System.IEquatable`1<int>
")]
	static class TestSelectInfImpl4
	{
		public static void Entry()
		{
			var cls = new SelectInfImpl.Sub1();
			SelectInfImpl.Inf<short> inf = cls;
			inf.Foo();
			SelectInfImpl.Inf inf2 = cls;
			inf2.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestSelectInfImpl5
-> void Entry()

object
-> void .ctor()

TestIL.SelectInfImpl.Sub1
-> void .ctor()

TestIL.SelectInfImpl.Cls
-> void .ctor()
-> void Foo()
--> int field1

TestIL.SelectInfImpl.Inf`1<uint>
-> void Foo() = 0
   \ void Foo(): TestIL.SelectInfImpl.Cls

======
void
System.ValueType
TestIL.SelectInfImpl.Inf
short
System.IComparable
System.IFormattable
System.IConvertible
System.IComparable`1<short>
System.IEquatable`1<short>
TestIL.SelectInfImpl.Inf`1<short>
uint
System.IComparable`1<uint>
System.IEquatable`1<uint>
int
System.IComparable`1<int>
System.IEquatable`1<int>
")]
	static class TestSelectInfImpl5
	{
		public static void Entry()
		{
			var cls = new SelectInfImpl.Sub1();
			SelectInfImpl.Inf<uint> inf = cls;
			inf.Foo();
		}
	}

	[TestClass(@"======
TestIL.TestSelectInfImpl6
-> void Entry()

object
-> void .ctor()

TestIL.SelectInfImpl.Sub2
-> void .ctor()
-> void TestIL.SelectInfImpl.Inf<System.UInt32>.Foo()

TestIL.SelectInfImpl.Cls
-> void .ctor()
-> void TestIL.SelectInfImpl.Inf<System.Int16>.Foo()
--> int field3

TestIL.SelectInfImpl.Inf`1<short>
-> void Foo() = 0
   \ void TestIL.SelectInfImpl.Inf<System.Int16>.Foo(): TestIL.SelectInfImpl.Cls

TestIL.SelectInfImpl.Inf`1<uint>
-> void Foo() = 0
   \ void TestIL.SelectInfImpl.Inf<System.UInt32>.Foo(): TestIL.SelectInfImpl.Sub2

======
void
System.ValueType
TestIL.SelectInfImpl.Inf
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
	static class TestSelectInfImpl6
	{
		public static void Entry()
		{
			var cls = new SelectInfImpl.Sub2();
			SelectInfImpl.Inf<uint> inf = cls;
			inf.Foo();

			SelectInfImpl.Inf<short> inf2 = cls;
			inf2.Foo();
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

	class Program
	{
		static void Main()
		{
		}
	}
}
