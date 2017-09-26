using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace il2cpp
{
	internal static class InternalImpls
	{
		public static readonly string SystemArrayImpl;

		static InternalImpls()
		{
			SystemArrayImpl = ResourceString("il2cpp.InternalImpls.SystemArray.cpp");
		}

		private static string ResourceString(string resKey)
		{
			return StreamToString(Assembly.GetExecutingAssembly().GetManifestResourceStream(resKey));
		}

		private static string StreamToString(Stream s)
		{
			byte[] buf = new byte[s.Length];
			s.Read(buf, 0, buf.Length);
			Debug.Assert(
				buf != null &&
				buf.Length > 3 &&
				buf[0] == 0xEF && buf[1] == 0xBB && buf[2] == 0xBF);
			return Encoding.UTF8.GetString(buf);
		}
	}
}
