namespace TestAdapter2
{
	public static class Test
	{
		class Cls
		{
			public override int GetHashCode()
			{
				return 2;
			}
		}

		public static void Accept(object inf)
		{
			int n = inf.GetHashCode();
		}

		public static object Create()
		{
			return new Cls();
		}
	}
}
