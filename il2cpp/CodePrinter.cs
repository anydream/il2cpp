using System.Text;

namespace il2cpp
{
	class CodePrinter
	{
		public int Indents;
		public int LineCount { get; private set; }
		public int Length => Builder.Length;
		private readonly StringBuilder Builder = new StringBuilder();

		public override string ToString()
		{
			return Builder.ToString();
		}

		public void Append(string str)
		{
			if (str == null)
				return;

			bool isNewLine = IsNewLine();

			foreach (char ch in str)
			{
				if (ch == '\r')
					continue;

				if (ch == '\n')
				{
					isNewLine = true;
					++LineCount;
				}
				else if (isNewLine)
				{
					isNewLine = false;
					AppendIndent();
				}
				Builder.Append(ch);
			}
		}

		public void AppendLine(string str)
		{
			Append(str);
			Builder.Append('\n');
			++LineCount;
		}

		public void AppendLine()
		{
			Builder.Append('\n');
			++LineCount;
		}

		public void AppendFormat(string fmt, params object[] args)
		{
			Append(string.Format(fmt, args));
		}

		public void AppendFormatLine(string fmt, params object[] args)
		{
			AppendLine(string.Format(fmt, args));
		}

		private void AppendIndent()
		{
			for (int i = 0; i < Indents; ++i)
				Builder.Append("    ");
		}

		private bool IsNewLine()
		{
			return Builder.Length == 0 ||
				   Builder[Builder.Length - 1] == '\n';
		}
	}
}
