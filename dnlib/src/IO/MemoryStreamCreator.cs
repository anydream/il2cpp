// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.IO;

namespace dnlib.IO {
	/// <summary>
	/// Creates <see cref="MemoryStream"/>s to partially access a byte[]
	/// </summary>
	/// <seealso cref="UnmanagedMemoryStreamCreator"/>
	[DebuggerDisplay("byte[]: O:{dataOffset} L:{dataLength} {FileName}")]
	sealed class MemoryStreamCreator : IImageStreamCreator {
		byte[] data;
		int dataOffset;
		int dataLength;

	    /// <summary>
		/// The file name
		/// </summary>
		public string FileName { get; set; }

	    /// <inheritdoc/>
		public long Length => dataLength;

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The data</param>
		public MemoryStreamCreator(byte[] data)
			: this(data, 0, data.Length) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The data</param>
		/// <param name="offset">Start offset in <paramref name="data"/></param>
		/// <param name="length">Length of data starting from <paramref name="offset"/></param>
		/// <exception cref="ArgumentOutOfRangeException">If one of the args is invalid</exception>
		public MemoryStreamCreator(byte[] data, int offset, int length) {
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (length < 0 || offset + length < offset)
				throw new ArgumentOutOfRangeException(nameof(length));
			if (offset + length > data.Length)
				throw new ArgumentOutOfRangeException(nameof(length));
			this.data = data;
			this.dataOffset = offset;
			this.dataLength = length;
		}

		/// <inheritdoc/>
		public IImageStream Create(FileOffset offset, long length) {
			if (offset < 0 || length < 0)
				return MemoryImageStream.CreateEmpty();

			int offs = (int)Math.Min((long)dataLength, (long)offset);
			int len = (int)Math.Min((long)dataLength - offs, length);
			return new MemoryImageStream(offset, data, dataOffset + offs, len);
		}

		/// <inheritdoc/>
		public IImageStream CreateFull() {
			return new MemoryImageStream(0, data, dataOffset, dataLength);
		}

		/// <inheritdoc/>
		public void Dispose() {
			data = null;
			dataOffset = 0;
			dataLength = 0;
			FileName = null;
		}
	}
}
