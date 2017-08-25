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

	internal class Program
	{
		private static void Main()
		{
		}
	}
}
