// dnlib: See LICENSE.txt for more info

﻿using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Relocations directory
	/// </summary>
	public sealed class RelocDirectory : IChunk {
	    /// <summary>
		/// Gets/sets the <see cref="StartupStub"/>
		/// </summary>
		public StartupStub StartupStub { get; set; }

		/// <inheritdoc/>
		public FileOffset FileOffset { get; private set; }

	    /// <inheritdoc/>
		public RVA RVA { get; private set; }

	    /// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.FileOffset = offset;
			this.RVA = rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return 12;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			uint rva = (uint)StartupStub.RelocRVA;
			writer.Write(rva & ~0xFFFU);
			writer.Write(12);
			writer.Write((ushort)(0x3000 | (rva & 0xFFF)));
			writer.Write((ushort)0);
		}
	}
}
