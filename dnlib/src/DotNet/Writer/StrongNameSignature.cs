// dnlib: See LICENSE.txt for more info

﻿using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Strong name signature chunk
	/// </summary>
	public sealed class StrongNameSignature : IChunk {
	    int size;

		/// <inheritdoc/>
		public FileOffset FileOffset { get; private set; }

	    /// <inheritdoc/>
		public RVA RVA { get; private set; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="size">Size of strong name signature</param>
		public StrongNameSignature(int size) {
			this.size = size;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.FileOffset = offset;
			this.RVA = rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return (uint)this.size;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			writer.WriteZeros(size);
		}
	}
}
