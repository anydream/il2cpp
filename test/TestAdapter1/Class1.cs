namespace TestAdapter1
{
	public static class Test
	{
		class Cls
		{
			public override int GetHashCode()
			{
				return 1;
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
