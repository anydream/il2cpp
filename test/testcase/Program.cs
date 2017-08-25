using System;

namespace testcase
{
	class TestAttribute : Attribute
	{
	}

	[Test]
	static class GenOverride1
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

		public static void Entry()
		{
			Base<int, float> b = new Derived<float, int>();
			b.Foo(1, 1.2f);
		}
	}

	internal class Program
	{
		private static void Main()
		{
		}
	}
}
