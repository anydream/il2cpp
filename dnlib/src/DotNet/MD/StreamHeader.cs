// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Text;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// A metadata stream header
	/// </summary>
	[DebuggerDisplay("O:{Offset} L:{StreamSize} {Name}")]
	public sealed class StreamHeader : FileSection {
	    /// <summary>
		/// The offset of the stream relative to the start of the MetaData header
		/// </summary>
		public uint Offset { get; }

	    /// <summary>
		/// The size of the stream
		/// </summary>
		public uint StreamSize { get; }

	    /// <summary>
		/// The name of the stream
		/// </summary>
		public string Name { get; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public StreamHeader(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			this.Offset = reader.ReadUInt32();
			this.StreamSize = reader.ReadUInt32();
			this.Name = ReadString(reader, 32, verify);
			SetEndoffset(reader);
			if (verify && Offset + size < Offset)
				throw new BadImageFormatException("Invalid stream header");
		}

		static string ReadString(IImageStream reader, int maxLen, bool verify) {
			var origPos = reader.Position;
			var sb = new StringBuilder(maxLen);
			int i;
			for (i = 0; i < maxLen; i++) {
				byte b = reader.ReadByte();
				if (b == 0)
					break;
				sb.Append((char)b);
			}
			if (verify && i == maxLen)
				throw new BadImageFormatException("Invalid stream name string");
			if (i != maxLen)
				reader.Position = origPos + ((i + 1 + 3) & ~3);
			return sb.ToString();
		}
	}
}
