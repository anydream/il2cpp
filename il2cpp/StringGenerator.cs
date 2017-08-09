using System.Collections.Generic;
using System.Text;

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
			if (!StringMap.TryGetValue(str, out int idx))
			{
				idx = ++Counter;
				StringMap.Add(str, idx);
			}
			return idx;
		}

		public void GenDefineCode(
			int splitLines,
			out Dictionary<string, int> strSplitMap,
			out Dictionary<int, StringBuilder> codeMap,
			out string strTypeDefs)
		{
			List<string> sortedStrs = GetSortedStrings();
			strSplitMap = new Dictionary<string, int>();
			codeMap = new Dictionary<int, StringBuilder>();
			HashSet<int> lenSet = new HashSet<int>();

			uint strTypeID = 0;
			TypeX strType = TypeGen.TypeMgr.GetNamedType("System.String");
			if (strType != null)
				strTypeID = strType.GetCppTypeID();

			int index = 0;
			int counter = 0;

			StringBuilder currSb = new StringBuilder();
			codeMap.Add(index, currSb);

			foreach (var str in sortedStrs)
			{
				lenSet.Add(str.Length);
				strSplitMap.Add(str, index);
				currSb.Append(GenStringCode(str, strTypeID));

				++counter;
				if (counter >= splitLines)
				{
					counter = 0;
					++index;

					currSb = new StringBuilder();
					codeMap.Add(index, currSb);
				}
			}

			Reset();

			var prt = new CodePrinter();
			foreach (var len in lenSet)
			{
				prt.AppendFormatLine("struct il2cppString_{0}\n{{",
					len + 1);
				++prt.Indents;
				prt.AppendLine("IL2CPP_OBJECT_BODY;");
				prt.AppendFormatLine("int len;\nuint16_t str[{0}];",
					len + 1);
				--prt.Indents;
				prt.AppendLine("};");
			}
			strTypeDefs = prt.ToString();
		}

		private List<string> GetSortedStrings()
		{
			List<string> sortedStr = new List<string>(StringMap.Keys);
			sortedStr.Sort((x, y) => StringMap[x].CompareTo(StringMap[y]));
			return sortedStr;
		}

		private string GenStringCode(string str, uint strTypeID)
		{
			string charArray = null;
			for (int i = 0; i < str.Length; ++i)
			{
				char ch = str[i];
				if (ch >= 0x21 &&
					ch <= 0x7E &&
					ch != '\\' &&
					ch != '\'' &&
					ch != '"')
				{
					charArray += "'" + ch + "'";
				}
				else
				{
					charArray += "0x" + ((ushort)ch).ToString("X");
				}
				charArray += ',';
			}
			charArray += '0';

			return string.Format("const il2cppString_{0} str_{1} {{ {2}, {3}, {{{4}}} }};\n",
				str.Length + 1,
				StringMap[str],
				strTypeID,
				str.Length,
				charArray);
		}
	}
}
