using System.Text;

namespace il2cpp
{
	internal static class NameHelper
	{
		public static uint NameCounter;

		public static string GetCppName(this MethodX metX, bool isVirt)
		{
			if (metX.CppName_ == null)
				metX.CppName_ = ToCppName(metX.FullFuncName);

			return (isVirt ? "vmet_" : "met_") + metX.CppName_;
		}

		private static string ToCppName(string fullName)
		{
			StringBuilder sb = new StringBuilder();

			string hash = ToRadix(NameCounter++, (uint)DigMap.Length);
			sb.Append(hash + "_");

			for (int i = 0; i < fullName.Length; ++i)
			{
				if (IsLegalIdentChar(fullName[i]))
				{
					sb.Append(fullName[i]);
				}
				else
				{
					sb.Append('_');
				}
			}
			return sb.ToString();
		}

		private static bool IsLegalIdentChar(char ch)
		{
			return ch >= 'a' && ch <= 'z' ||
				   ch >= 'A' && ch <= 'Z' ||
				   ch >= '0' && ch <= '9' ||
				   ch == '_';
		}

		private static string DigMap = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
		private static string ToRadix(uint value, uint radix)
		{
			StringBuilder sb = new StringBuilder();
			do
			{
				uint dig = value % radix;
				value /= radix;
				sb.Append(DigMap[(int)dig]);
			} while (value != 0);

			return sb.ToString();
		}
	}
}
