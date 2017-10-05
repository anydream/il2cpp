using System.Collections.Generic;
using System.Text;

namespace il2cpp
{
	internal class StringGenerator
	{
		private class StringProp
		{
			public int ConstIndex;
			public int UnitIndex;
		}

		private readonly Dictionary<string, StringProp> StringMap = new Dictionary<string, StringProp>();

		private int CurrConstIndex;
		private int CurrUnitIndex = 1;
		private int CurrAccLength;

		public string AddString(string str)
		{
			if (!StringMap.TryGetValue(str, out var prop))
			{
				prop = new StringProp();
				prop.ConstIndex = ++CurrConstIndex;

				StringMap.Add(str, prop);
			}
			return GetConstName(prop.ConstIndex);
		}

		public void Generate(Dictionary<string, CompileUnit> unitMap, uint strTypeID)
		{
			foreach (var unit in unitMap.Values)
			{
				foreach (string strDep in unit.StringDepends)
				{
					unit.ImplDepends.Add(StringToUnitName(strDep));
				}
			}

			Dictionary<int, List<string>> unitStrMap = new Dictionary<int, List<string>>();
			foreach (var kv in StringMap)
			{
				int unitIdx = kv.Value.UnitIndex;
				if (!unitStrMap.TryGetValue(unitIdx, out var strList))
				{
					strList = new List<string>();
					unitStrMap.Add(unitIdx, strList);
				}
				strList.Add(kv.Key);
			}

			foreach (var kv in unitStrMap)
			{
				var unit = new CompileUnit();

				unit.Name = GetUnitName(kv.Key);
				unit.DeclCode = GenStringCode(kv.Value, strTypeID);

				unitMap[unit.Name] = unit;
			}
		}

		private string StringToUnitName(string str)
		{
			StringProp prop = StringMap[str];
			if (prop.UnitIndex == 0)
			{
				prop.UnitIndex = CurrUnitIndex;
				CurrAccLength += str.Length;

				if (CurrAccLength > 3000)
				{
					CurrAccLength = 0;
					++CurrUnitIndex;
				}
			}

			return GetUnitName(prop.UnitIndex);
		}

		private string GenStringCode(List<string> strList, uint strTypeID)
		{
			CodePrinter prt = new CodePrinter();

			foreach (string str in strList)
			{
				StringProp prop = StringMap[str];

				string strAry = StringToArrayOrRaw(str, out bool isRaw);

				prt.AppendFormatLine("// {0}", EscapeString(str));
				prt.AppendFormatLine("static const struct {{ cls_Object obj; int32_t len; {5} str[{0}]; }} {1} {{ {{{2}}}, {3}, {4} }};",
					str.Length + 1,
					GetConstName(prop.ConstIndex),
					strTypeID,
					str.Length,
					strAry,
					isRaw ? "char16_t" : "uint16_t");
			}
			return prt.ToString();
		}

		private static string StringToArrayOrRaw(string str, out bool isRaw)
		{
			isRaw = true;
			StringBuilder sbRaw = new StringBuilder();
			foreach (char c in str)
			{
				if (c >= 0x21 && c <= 0x7E)
				{
					switch (c)
					{
						case '\\':
							sbRaw.Append("\\\\");
							break;
						case '"':
							sbRaw.Append("\\\"");
							break;
						default:
							sbRaw.Append(c);
							break;
					}
				}
				else
				{
					isRaw = false;
					break;
				}
			}

			if (isRaw)
				return "u\"" + sbRaw + "\"";
			return StringToArray(str);
		}

		private static string StringToArray(string str)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append('{');
			for (int i = 0; i < str.Length; ++i)
			{
				if (i != 0)
					sb.Append(',');
				sb.Append((ushort)str[i]);
			}
			sb.Append(",0}");
			return sb.ToString();
		}

		private static string EscapeString(string str)
		{
			return str.Replace("\n", "\\n");
		}

		private static string GetConstName(int idx)
		{
			return "il2cppConstStr_" + idx;
		}

		private static string GetUnitName(int idx)
		{
			return "il2cppString_" + idx;
		}
	}
}
