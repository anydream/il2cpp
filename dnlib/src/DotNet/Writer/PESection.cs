// dnlib: See LICENSE.txt for more info

﻿using System.IO;
using System.Text;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// A PE section
	/// </summary>
	public sealed class PESection : ChunkList<IChunk> {
	    /// <summary>
		/// Gets the name
		/// </summary>
		public string Name { get; set; }

	    /// <summary>
		/// Gets the Characteristics
		/// </summary>
		public uint Characteristics { get; set; }

	    /// <summary>
		/// <c>true</c> if this is a code section
		/// </summary>
		public bool IsCode => (Characteristics & 0x20) != 0;

	    /// <summary>
		/// <c>true</c> if this is an initialized data section
		/// </summary>
		public bool IsInitializedData => (Characteristics & 0x40) != 0;

	    /// <summary>
		/// <c>true</c> if this is an uninitialized data section
		/// </summary>
		public bool IsUninitializedData => (Characteristics & 0x80) != 0;

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Section name</param>
		/// <param name="characteristics">Section characteristics</param>
		public PESection(string name, uint characteristics) {
			this.Name = name;
			this.Characteristics = characteristics;
		}

		/// <summary>
		/// Writes the section header to <paramref name="writer"/> at its current position.
		/// Returns aligned virtual size (aligned to <paramref name="sectionAlignment"/>)
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="fileAlignment">File alignment</param>
		/// <param name="sectionAlignment">Section alignment</param>
		/// <param name="rva">Current <see cref="RVA"/></param>
		public uint WriteHeaderTo(BinaryWriter writer, uint fileAlignment, uint sectionAlignment, uint rva) {
			uint vs = GetVirtualSize();
			uint fileLen = GetFileLength();
			uint alignedVs = Utils.AlignUp(vs, sectionAlignment);
			uint rawSize = Utils.AlignUp(fileLen, fileAlignment);
			uint dataOffset = (uint)FileOffset;

			writer.Write(Encoding.UTF8.GetBytes(Name + "\0\0\0\0\0\0\0\0"), 0, 8);
			writer.Write(vs);			// VirtualSize
			writer.Write((uint)rva);	// VirtualAddress
			writer.Write(rawSize);		// SizeOfRawData
			writer.Write(dataOffset);	// PointerToRawData
			writer.Write(0);			// PointerToRelocations
			writer.Write(0);			// PointerToLinenumbers
			writer.Write((ushort)0);	// NumberOfRelocations
			writer.Write((ushort)0);	// NumberOfLinenumbers
			writer.Write(Characteristics);

			return alignedVs;
		}
	}
}
