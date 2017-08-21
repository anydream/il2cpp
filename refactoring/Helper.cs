using System.Diagnostics;

namespace il2cpp
{
	// 单向布尔
	internal struct OnceBool
	{
		private bool Value;

		public static implicit operator OnceBool(bool b)
		{
			Debug.Assert(b);
			return new OnceBool { Value = true };
		}

		public static implicit operator bool(OnceBool ob)
		{
			return ob.Value;
		}
	}

	// 辅助扩展方法
	internal static class Helper
	{
	}
}
