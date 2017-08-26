using System;

namespace group1
{
	class Base<T, P>
	{
		private T fld;
		public virtual void Foo(T a, P b)
		{
			fld = a;
		}
	}

	class Derived<T, P> : Base<P, T>
	{
		private T fld;
		public override void Foo(P a, T b)
		{
			fld = b;
		}
	}
}

namespace group2
{
	interface Inf<T, P>
	{
		void Foo(T a, P b);
	}

	class Base<T, P> : Inf<T, P>
	{
		private T fld;
		private int fldInt;
		public virtual void Foo(T a, P b)
		{
			fld = a;
		}

		public virtual void Foo(int a, float b)
		{
			fldInt = a;
		}
	}

	class Derived<T, P> : Base<P, T>
	{
		private T fld;
		private int fldInt;
		public override void Foo(P a, T b)
		{
			fld = b;
		}

		public override void Foo(int a, float b)
		{
			fldInt = a;
		}
	}

	class DerivedX2<T> : Derived<T, int>
	{
		private T fld;
		public override void Foo(int a, T b)
		{
			fld = b;
		}
	}
}

namespace group3
{
	class Base<T, P>
	{
		private T fld;
		private P fld2;
		public virtual MT Foo<MT, MP>(T t, P p, MT mt, MP mp)
		{
			fld = t;
			return mt;
		}

		public virtual MT Foo<MT, MP>(T t, P p, MP mp, MT mt)
		{
			fld2 = p;
			return mt;
		}
	}

	class Derived<T, P> : Base<P, T>
	{
		private T fld;
		private P fld2;
		public override MT Foo<MT, MP>(P t, T p, MT mt, MP mp)
		{
			fld = p;
			return mt;
		}

		public override MT Foo<MT, MP>(P t, T p, MP mp, MT mt)
		{
			fld2 = t;
			return mt;
		}
	}
}

namespace group4
{
	interface Inf
	{
		void Foo();
		void Foo(int n);
	}

	class Base : Inf
	{
		private int fld;

		public virtual void Foo()
		{

		}

		void Inf.Foo(int n)
		{
			fld = n;
		}
	}

	class ClsA : Base
	{
		private int fld;
		private float fld2;

		public override void Foo()
		{
			fld2 = 1.0f;
		}

		public void Foo(int n)
		{
			fld = n;
		}
	}

	class ClsB : Base, Inf
	{
		private int fld;
		private float fld2;

		public override void Foo()
		{
			fld2 = 1.0f;
		}

		public void Foo(int n)
		{
			fld = n;
		}
	}
}

namespace group5
{
	interface InfA
	{
		int Foo(int n);
	}

	interface InfB
	{
		int Foo(int n);
	}

	interface InfC : InfA
	{
		int Foo(int n);
	}

	class Cls : InfA, InfB, InfC
	{
		private int fld;

		public int Foo(int n)
		{
			fld = n;
			return n;
		}
	}

	class Base : InfA
	{
		private int fld;

		public int Foo(int n)
		{
			fld = n;
			return n;
		}
	}

	class Derived : Base, InfB
	{
		private int fld;

		public int Foo(int n)
		{
			fld = n;
			return n;
		}
	}

	class DerivedX2 : Derived, InfC
	{ }
}

namespace group6
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
}

namespace group7
{
	interface Inf
	{
		int Foo();
	}

	class Base : Inf
	{
		private int field1;
		public virtual int Foo()
		{
			return field1;
		}
	}

	class Middle : Base, Inf
	{
		private int field2;
		int Inf.Foo()
		{
			return field2;
		}
	}

	class Sub : Middle
	{
		private int field3;
		public override int Foo()
		{
			return field3;
		}
	}
}

namespace testcase
{
	class TestAttribute : Attribute
	{
	}

	[Test]
	static class GenOverride1
	{
		public static void Entry()
		{
			group1.Base<int, float> b = new group1.Derived<float, int>();
			b.Foo(1, 1.2f);
		}
	}

	[Test]
	static class GenOverride2
	{
		public static void Entry()
		{
			group1.Base<int, float> b = null;
			b.Foo(1, 1.2f);
		}
	}

	[Test]
	static class GenOverride3
	{
		public static void Entry()
		{
			group1.Base<int, float> b = new group1.Base<int, float>();
			b.Foo(1, 1.2f);
			var d = new group1.Derived<float, int>();
		}
	}

	[Test]
	static class GenOverride4
	{
		public static void Entry()
		{
			group2.Inf<int, float> i = new group2.DerivedX2<float>();
			i.Foo(1, 1.2f);
			group2.Inf<float, int> i2 = null;
			i2.Foo(1.2f, 1);
		}
	}

	[Test]
	static class GenOverride5
	{
		public static void Entry()
		{
			group2.Base<int, float> b = new group2.DerivedX2<float>();
			b.Foo(1, 1.2f);
		}
	}

	[Test]
	static class GenOverride6
	{
		public static void Entry()
		{
			group3.Base<int, float> b = new group3.Derived<float, int>();
			b.Foo<short, long>(1, 1.2f, (short)12345, (long)999999);
		}
	}

	[Test]
	static class GenOverride7
	{
		public static void Entry()
		{
			group3.Base<int, float> b = new group3.Derived<float, int>();
			b.Foo<short, long>(1, 1.2f, (long)999999, (short)12345);
		}
	}

	[Test]
	static class ExpOverride1
	{
		public static void Entry()
		{
			group4.Inf i = new group4.ClsA();
			i.Foo(123);
		}
	}

	[Test]
	static class ExpOverride2
	{
		public static void Entry()
		{
			group4.Inf i = new group4.ClsA();
			i.Foo();
		}
	}

	[Test]
	static class ExpOverride3
	{
		public static void Entry()
		{
			group4.Inf i = new group4.ClsB();
			i.Foo(123);
		}
	}

	[Test]
	static class ExpOverride4
	{
		public static void Entry()
		{
			group4.Inf i = new group4.ClsB();
			i.Foo();
		}
	}

	[Test]
	static class ExpOverride5
	{
		public static void Entry()
		{
			group6.Inf inf = new group6.Sub3();
			inf.Foo();
		}
	}

	[Test]
	static class ExpOverride6
	{
		public static void Entry()
		{
			group6.Inf inf = new group6.Sub2();
			inf.Foo();
		}
	}

	[Test]
	static class ExpOverride7
	{
		public static void Entry()
		{
			group7.Inf inf = new group7.Sub();
			inf.Foo();
		}
	}

	[Test]
	static class ExpOverride8
	{
		public static void Entry()
		{
			group7.Base b = new group7.Sub();
			b.Foo();
		}
	}

	[Test]
	static class GenExpOverride1
	{
		interface Inf<T>
		{
			void Foo(T t);
		}

		class Cls : Inf<float>
		{
			private float fld;
			void Inf<float>.Foo(float t)
			{
				fld = t;
			}
		}

		public static void Entry()
		{
			Inf<float> i = new Cls();
			i.Foo(1.2f);
		}
	}

	[Test]
	static class GenExpOverride2
	{
		interface Inf
		{
			T Foo<T>(T t);
		}

		class Cls : Inf
		{
			T Inf.Foo<T>(T t)
			{
				return t;
			}
		}

		public static void Entry()
		{
			Inf i = new Cls();
			i.Foo(1.2f);
		}
	}

	[Test]
	static class GenExpOverride3
	{
		interface Inf<T>
		{
			MT Foo<MT>(MT mt, T t);
		}

		class Cls : Inf<long>
		{
			MT Inf<long>.Foo<MT>(MT mt, long t)
			{
				return mt;
			}
		}

		public static void Entry()
		{
			Inf<long> i = new Cls();
			i.Foo(1.2f, (long)999999);
			Inf<char> i2 = null;
			i2.Foo(1234, 'a');
		}
	}

	[Test]
	static class Interface1
	{
		public static void Entry()
		{
			group5.InfA i = new group5.Cls();
			i.Foo(123);
		}
	}

	[Test]
	static class Interface2
	{
		public static void Entry()
		{
			group5.InfC i = new group5.Cls();
			i.Foo(123);
		}
	}

	[Test]
	static class Interface3
	{
		public static void Entry()
		{
			group5.InfA i = new group5.Derived();
			i.Foo(123);
		}
	}

	[Test]
	static class Interface4
	{
		public static void Entry()
		{
			group5.InfB i = new group5.Derived();
			i.Foo(123);
		}
	}

	[Test]
	static class Interface5
	{
		public static void Entry()
		{
			group5.InfA i = new group5.DerivedX2();
			i.Foo(123);
		}
	}

	[Test]
	static class Interface6
	{
		public static void Entry()
		{
			group5.InfC i = new group5.DerivedX2();
			i.Foo(123);
		}
	}

	[Test]
	static class Interface7
	{
		interface Inf
		{
			void Foo();
		}

		class Base
		{
			private int fld;
			public void Foo()
			{
				fld = 1;
			}
		}

		class Derived : Base, Inf
		{ }

		public static void Entry()
		{
			Inf i = new Derived();
			i.Foo();
		}
	}

	[Test]
	static class Interface8
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

	[Test]
	static class CycleType1
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

	[Test]
	static class CycleType2
	{
		class A<T> : C<B<T>>
		{
			private int fld;
			private int fld2;
			public virtual T Foo(T t)
			{
				fld = 1;
				return t;
			}

			public override B<T> Foo(B<T> t)
			{
				fld2 = 2;
				return t;
			}
		}

		class B<T> : C<A<T>>
		{
			private int fld;
			private int fld2;
			public virtual T Foo(T t)
			{
				fld = 1;
				return t;
			}

			public override A<T> Foo(A<T> t)
			{
				fld2 = 2;
				return t;
			}
		}

		class C<T>
		{
			private int fld;
			public virtual T Foo(T t)
			{
				fld = 1;
				return t;
			}
		}

		public static void Entry()
		{
			C<B<int>> i1 = new A<int>();
			C<A<int>> i2 = new B<int>();
			i1.Foo(i1.Foo(null));
			i2.Foo(i2.Foo(null));
		}
	}

	[Test]
	static class CrossRuntimeVersion1
	{
		public static void Entry()
		{
			int hash = 0;
			object obj = net20x1.Class.NewObj(ref hash);
			hash ^= obj.GetHashCode();
		}
	}

	[Test]
	static class Finalizer1
	{
		class Cls
		{
			static int fld;
			static int fld2;

			static Cls()
			{
				fld = 456;
			}

			~Cls()
			{
				fld2 = 789;
			}
		}

		public static void Entry()
		{
			new Cls();
		}
	}

	[Test]
	static class StaticCctor1
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

	internal class Program
	{
		private static void Main()
		{
		}
	}
}
