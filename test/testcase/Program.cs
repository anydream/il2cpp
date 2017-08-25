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

	internal class Program
	{
		private static void Main()
		{
		}
	}
}
