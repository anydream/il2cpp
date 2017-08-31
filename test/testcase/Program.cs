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

namespace group8
{
	interface Inf
	{
		int Foo(int n);
	}

	class Base : Inf
	{
		int Inf.Foo(int n)
		{
			return n;
		}
	}

	class Sub1 : Base
	{
		public int Foo(int n)
		{
			return n;
		}
	}

	class Sub2 : Base, Inf
	{
		public int Foo(int n)
		{
			return n;
		}
	}
}

namespace group9
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

namespace group10
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

namespace group11
{
	interface Inf
	{
		T Foo<T>();
		void Foo<T>(T t);
		void Foo<T, T2>(T t, T2 t2);
	}

	interface Res
	{
	}

	interface Res2
	{
	}

	class Cls : Inf
	{
		T Inf.Foo<T>()
		{
			return default(T);
		}

		void Inf.Foo<T>(T t)
		{
		}

		void Inf.Foo<T, T2>(T t, T2 t2)
		{
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
	static class GenOverride8
	{
		interface Inf<T1, T2>
		{
			T1 Foo(T2 n);
		}

		class Base
		{
			public int Foo(short n)
			{
				return n;
			}
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

	[Test]
	static class GenOverride9
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
				return n;
			}

			public override unsafe TCF2 Foo<TCF2>(TCF2 n, short*[] i)
			{
				return n;
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

	[Test]
	static class GenOverride10
	{
		interface Inf<T>
		{
			void Foo(T t);
		}

		class Cls<T> : Inf<int>
		{
			public int field1;
			public int field2;

			public void Foo(T t)
			{
				field1 = 1;
			}

			public void Foo(int t)
			{
				field2 = 2;
			}
		}

		public static unsafe void Entry()
		{
			Inf<int> inf = new Cls<int>();
			inf.Foo(123);
		}
	}

	[Test]
	static class GenOverride11
	{
		interface Inf<T>
		{
			void Foo(int t);
			void Foo(T t);
		}

		class Cls<T> : Inf<int>
		{
			public int field1;
			public int field2;

			public void Foo(T t)
			{
				field1 = 1;
			}

			public void Foo(int t)
			{
				field2 = 2;
			}
		}

		public static unsafe void Entry()
		{
			Inf<int> inf = new Cls<int>();
			inf.Foo(123);
		}
	}

	[Test]
	static class GenOverride12
	{
		interface Inf0<T>
		{
			void Foo(T t);
		}

		interface Inf<T> : Inf0<T>
		{
			void Foo(int t);
			void Foo(T t);
		}

		class Cls<T> : Inf<int>
		{
			public int field1;
			public int field2;

			public void Foo(T t)
			{
				field1 = 1;
			}

			public void Foo(int t)
			{
				field2 = 2;
			}
		}

		public static unsafe void Entry()
		{
			Inf0<int> inf = new Cls<int>();
			inf.Foo(123);
		}
	}

	[Test]
	static class GenOverride13
	{
		interface Inf<T>
		{
			void Foo(int t);
			void Foo(T t);
		}

		class Cls<T> : Inf<T>
		{
			public int field1;
			public int field2;

			public void Foo(T t)
			{
				field1 = 1;
			}

			public void Foo(int t)
			{
				field2 = 2;
			}
		}

		public static unsafe void Entry()
		{
			Inf<int> inf = new Cls<int>();
			inf.Foo(123);
		}
	}

	[Test]
	static class GenOverride14
	{
		interface Inf0<T>
		{
			void Foo(T t);
		}

		interface Inf<T> : Inf0<T>
		{
			void Foo(int t);
			void Foo(T t);
		}

		class Cls<T> : Inf<T>
		{
			public int field1;
			public int field2;

			public void Foo(T t)
			{
				field1 = 1;
			}

			public void Foo(int t)
			{
				field2 = 2;
			}
		}

		public static unsafe void Entry()
		{
			Inf0<int> inf = new Cls<int>();
			inf.Foo(123);
		}
	}

	[Test]
	static class GenOverride15
	{
		interface Inf<T>
		{
			void Foo(T t);
		}

		class Cls<T> : Inf<T>
		{
			public int field1;
			public int field2;

			public void Foo(T t)
			{
				field1 = 1;
			}

			public void Foo(int t)
			{
				field2 = 2;
			}
		}

		public static unsafe void Entry()
		{
			Inf<int> inf = new Cls<int>();
			inf.Foo(123);
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
	static class ExpOverride9
	{
		public static void Entry()
		{
			group8.Inf inf = new group8.Sub1();
			inf.Foo(123);
		}
	}

	[Test]
	static class ExpOverride10
	{
		public static void Entry()
		{
			group8.Inf inf = new group8.Sub2();
			inf.Foo(123);
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
	static class GenExpOverride4
	{
		interface Inf
		{
			int Foo(int n);
		}

		interface Inf<TI, TI2>
		{
			TI Foo(TI n);
			TI Foo<TF>(TI n, TI2 n2, TF n3);
			TI Foo<TF>(TF n3, TI2 n2, TI n);
		}

		class Base<TB0, TB> : Inf<int, TB>
		{
			public virtual int Foo(int n, long n1)
			{
				return n;
			}

			int Inf<int, TB>.Foo(int n)
			{
				return n;
			}

			int Inf<int, TB>.Foo<TF>(TF n3, TB n2, int n)
			{
				return n;
			}

			int Inf<int, TB>.Foo<TF>(int n, TB n2, TF n3)
			{
				return n;
			}
		}

		class Derived<TD, TD1> : Base<TD1, TD>, Inf<int, TD>
		{
			public override int Foo(int n, long n1)
			{
				return n;
			}
		}

		private static T EntryT<T>(T bla)
		{
			var cls = new Derived<T, ushort>();
			Inf<int, T> inf = cls;
			T t = bla;
			inf.Foo(123);
			inf.Foo(123, t, "");
			return bla;
		}

		public static void Entry()
		{
			EntryT('c');
		}
	}

	[Test]
	static class GenExpOverride5
	{
		interface Inf<TI, TI2>
		{
			TI Foo(TI n);
			TI Foo(TI n, TI2 n2);
		}

		class Base<TB> : Inf<int, TB>
		{
			int Inf<int, TB>.Foo(int n)
			{
				return n;
			}

			int Inf<int, TB>.Foo(int n, TB n2)
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
			inf.Foo(123);
			inf.Foo(123, 456L);
		}
	}

	[Test]
	static class GenExpOverride6
	{
		public static void Entry()
		{
			var cls = new group10.Cls();
			cls.Foo();
		}
	}

	[Test]
	static class GenExpOverride7
	{
		public static void Entry()
		{
			var cls = new group10.Cls();
			group10.Inf inf = cls;
			inf.Foo();
		}
	}

	[Test]
	static class GenExpOverride8
	{
		public static void Entry()
		{
			var cls = new group10.Cls();
			group10.Inf<short> inf = cls;
			inf.Foo();
		}
	}

	[Test]
	static class GenExpOverride9
	{
		public static void Entry()
		{
			var cls = new group10.Sub1();
			group10.Inf<short> inf = cls;
			inf.Foo();
			group10.Inf inf2 = cls;
			inf2.Foo();
		}
	}

	[Test]
	static class GenExpOverride10
	{
		public static void Entry()
		{
			var cls = new group10.Sub1();
			group10.Inf<uint> inf = cls;
			inf.Foo();
		}
	}

	[Test]
	static class GenExpOverride11
	{
		public static void Entry()
		{
			var cls = new group10.Sub2();
			group10.Inf<uint> inf = cls;
			inf.Foo();

			group10.Inf<short> inf2 = cls;
			inf2.Foo();
		}
	}

	[Test]
	static class GenExpOverride12
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

	[Test]
	static class GenExpOverride13
	{
		public static void Entry()
		{
			group11.Inf i = new group11.Cls();
			i.Foo("nice");
		}
	}

	[Test]
	static class GenExpOverride14
	{
		public static void Entry()
		{
			group11.Inf i = new group11.Cls();
			group11.Res res = i.Foo<group11.Res>();
			group11.Res2 res2 = i.Foo<group11.Res2>();
			i.Foo(res, res2);
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
	static class Interface9
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

	[Test]
	static class Override1
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

	[Test]
	static class Override2
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

	[Test]
	static class Override3
	{
		public static void Entry()
		{
			group9.BaseCls b = new group9.Sub1();

			var cls = new group9.Sub5();
			b = cls;
			b.Foo(123);
			b.Bla(456);
		}
	}

	[Test]
	static class Override4
	{
		public static void Entry()
		{
			new group9.Sub1();

			var cls = new group9.Sub5();

			group9.Sub2 s2 = cls;
			s2.Foo(123);
		}
	}

	[Test]
	static class Override5
	{
		public static void Entry()
		{
			new group9.Sub1();

			var cls = new group9.Sub5();

			group9.Sub4 s4 = cls;
			s4.Foo(123);
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

	[Test]
	static class StaticCctor2
	{
		class Base
		{
			public static int fld;
			static Base()
			{
				fld = 123;
			}
		}

		class Cls : Base
		{
			public new static int fld;
			public static int fld2;
			static Cls()
			{
				fld = 456;
			}
		}

		public static void Entry()
		{
			Cls.fld2 = 0;
		}
	}

	[Test]
	static class vsw593884
	{
		interface ITest
		{
			int Test { get; }
			int Test2 { get; }
		}

		class Level1 : ITest
		{
			public int Test { get { return 11; } }
			public int Test2 { get { return 12; } }
		}

		class Level2 : Level1, ITest
		{
			int ITest.Test { get { return 21; } }
			int ITest.Test2 { get { return 22; } }
		}

		class Level3 : Level2, ITest
		{
			int ITest.Test2 { get { return 32; } }
		}

		public static void Entry()
		{
			ITest test = new Level3();

			int ret1 = test.Test;
			int ret2 = test.Test2;
		}
	}

	[Test]
	static class vsw577403
	{
		interface ITest
		{
			int Test { get; }
			int Test2 { get; }
		}

		class Level1 : ITest
		{
			public int Test { get { return 11; } }
			public int Test2 { get { return 12; } }
		}

		class Level2 : Level1, ITest
		{
			int ITest.Test { get { return 21; } }
			int ITest.Test2 { get { return 22; } }
		}

		class Level3 : Level2, ITest
		{
			int ITest.Test2 { get { return 32; } }
		}

		class GenericLevel2<T> : Level1, ITest
		{
			int ITest.Test { get { return 21; } }
			int ITest.Test2 { get { return 22; } }
		}

		class GenericLevel3 : GenericLevel2<int>
		{
		}

		class GenericLevel4 : GenericLevel3, ITest
		{
			int ITest.Test2 { get { return 32; } }
		}

		public static void Entry()
		{
			ITest test = new Level3();
			ITest gen_test = new GenericLevel4();

			int ret1 = test.Test;
			int ret2 = test.Test2;

			int gen_ret1 = gen_test.Test;
			int gen_ret2 = gen_test.Test2;
		}
	}

	[Test]
	static class GenSameSig1
	{
		class Base<T, P>
		{
			public virtual void Foo(T t)
			{

			}
			public virtual void Foo(P p)
			{

			}

			public void CallT(T t)
			{
				Foo(t);
			}

			public void CallP(P p)
			{
				Foo(p);
			}
		}

		class Derived : Base<int, int>
		{
		}

		public static void Entry()
		{
			Base<int, int> b = new Derived();
			b.CallT(123);
		}
	}

	[Test]
	static class InterfaceImplementation
	{
		class A<T, U>
		{
			public virtual string Print(T t) { return "A.Print(T)"; }
			public virtual string Print(U u) { return "A.Print(U)"; }
		}

		interface I
		{
			string Print(int i);
		}

		interface J<T>
		{
			string Print(T t);
		}

		class A2_IntInt : A<int, int>, I, J<int>
		{
		}

		class A2_StringString : A<string, string>, J<string>
		{
		}

		public static void Entry()
		{
			I i = (I)new A2_IntInt();
			string res1 = i.Print(1);
			J<int> ji = (J<int>)new A2_IntInt();
			string res2 = ji.Print(1);
			J<string> js = (J<string>)new A2_StringString();
			string res3 = js.Print("");
		}
	}

	[Test]
	static class RecursiveGeneric
	{
		static object M<T>(long n) where T : class
		{
			if (n == 1)
			{
				return new T[1];
			}
			else
			{
				return M<T[]>(n - 1);
			}
		}

		public static void Entry()
		{
			var a = M<object>(9);
		}
	}

	internal class Program
	{
		private static void Main()
		{
		}
	}
}
