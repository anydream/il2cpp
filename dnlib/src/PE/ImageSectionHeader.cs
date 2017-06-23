// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Text;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_SECTION_HEADER PE section
	/// </summary>
	[DebuggerDisplay("RVA:{VirtualAddress} VS:{VirtualSize} FO:{PointerToRawData} FS:{SizeOfRawData} {DisplayName}")]
	public sealed class ImageSectionHeader : FileSection {
	    /// <summary>
		/// Returns the human readable section name, ignoring everything after
		/// the first nul byte
		/// </summary>
		public string DisplayName { get; }

	    /// <summary>
		/// Returns the IMAGE_SECTION_HEADER.Name field
		/// </summary>
		public byte[] Name { get; }

	    /// <summary>
		/// Returns the IMAGE_SECTION_HEADER.VirtualSize field
		/// </summary>
		public uint VirtualSize { get; }

	    /// <summary>
		/// Returns the IMAGE_SECTION_HEADER.VirtualAddress field
		/// </summary>
		public RVA VirtualAddress { get; }

	    /// <summary>
		/// Returns the IMAGE_SECTION_HEADER.SizeOfRawData field
		/// </summary>
		public uint SizeOfRawData { get; }

	    /// <summary>
		/// Returns the IMAGE_SECTION_HEADER.PointerToRawData field
		/// </summary>
		public uint PointerToRawData { get; }

	    /// <summary>
		/// Returns the IMAGE_SECTION_HEADER.PointerToRelocations field
		/// </summary>
		public uint PointerToRelocations { get; }

	    /// <summary>
		/// Returns the IMAGE_SECTION_HEADER.PointerToLinenumbers field
		/// </summary>
		public uint PointerToLinenumbers { get; }

	    /// <summary>
		/// Returns the IMAGE_SECTION_HEADER.NumberOfRelocations field
		/// </summary>
		public ushort NumberOfRelocations { get; }

	    /// <summary>
		/// Returns the IMAGE_SECTION_HEADER.NumberOfLinenumbers field
		/// </summary>
		public ushort NumberOfLinenumbers { get; }

	    /// <summary>
		/// Returns the IMAGE_SECTION_HEADER.Characteristics field
		/// </summary>
		public uint Characteristics { get; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageSectionHeader(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			this.Name = reader.ReadBytes(8);
			this.VirtualSize = reader.ReadUInt32();
			this.VirtualAddress = (RVA)reader.ReadUInt32();
			this.SizeOfRawData = reader.ReadUInt32();
			this.PointerToRawData = reader.ReadUInt32();
			this.PointerToRelocations = reader.ReadUInt32();
			this.PointerToLinenumbers = reader.ReadUInt32();
			this.NumberOfRelocations = reader.ReadUInt16();
			this.NumberOfLinenumbers = reader.ReadUInt16();
			this.Characteristics = reader.ReadUInt32();
			SetEndoffset(reader);
			DisplayName = ToString(Name);
		}

		static string ToString(byte[] name) {
			var sb = new StringBuilder(name.Length);
			foreach (var b in name) {
				if (b == 0)
					break;
				sb.Append((char)b);
			}
			return sb.ToString();
		}
	}
}
