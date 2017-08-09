using System.Collections.Generic;

namespace il2cpp
{
	class StringGenerator
	{
		private readonly TypeGenerator TypeGen;
		private readonly Dictionary<string, int> StringMap = new Dictionary<string, int>();
		private int Counter;

		public StringGenerator(TypeGenerator typeGen)
		{
			TypeGen = typeGen;
		}

		public void Reset()
		{
			StringMap.Clear();
			Counter = 0;
		}

		public int AddString(string str)
		{
			if (!StringMap.ContainsKey(str))
			{
				int cnt = ++Counter;
				StringMap.Add(str, cnt);
				return cnt;
			}
			return 0;
		}
	}
}
