// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_FILE_HEADER PE section
	/// </summary>
	public sealed class ImageFileHeader : FileSection {
	    readonly ushort numberOfSections;
	    readonly ushort sizeOfOptionalHeader;

	    /// <summary>
		/// Returns the IMAGE_FILE_HEADER.Machine field
		/// </summary>
		public Machine Machine { get; }

	    /// <summary>
		/// Returns the IMAGE_FILE_HEADER.NumberOfSections field
		/// </summary>
		public int NumberOfSections => numberOfSections;

	    /// <summary>
		/// Returns the IMAGE_FILE_HEADER.TimeDateStamp field
		/// </summary>
		public uint TimeDateStamp { get; }

	    /// <summary>
		/// Returns the IMAGE_FILE_HEADER.PointerToSymbolTable field
		/// </summary>
		public uint PointerToSymbolTable { get; }

	    /// <summary>
		/// Returns the IMAGE_FILE_HEADER.NumberOfSymbols field
		/// </summary>
		public uint NumberOfSymbols { get; }

	    /// <summary>
		/// Returns the IMAGE_FILE_HEADER.SizeOfOptionalHeader field
		/// </summary>
		public uint SizeOfOptionalHeader => sizeOfOptionalHeader;

	    /// <summary>
		/// Returns the IMAGE_FILE_HEADER.Characteristics field
		/// </summary>
		public Characteristics Characteristics { get; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageFileHeader(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			this.Machine = (Machine)reader.ReadUInt16();
			this.numberOfSections = reader.ReadUInt16();
			this.TimeDateStamp = reader.ReadUInt32();
			this.PointerToSymbolTable = reader.ReadUInt32();
			this.NumberOfSymbols = reader.ReadUInt32();
			this.sizeOfOptionalHeader = reader.ReadUInt16();
			this.Characteristics = (Characteristics)reader.ReadUInt16();
			SetEndoffset(reader);
			if (verify && this.sizeOfOptionalHeader == 0)
				throw new BadImageFormatException("Invalid SizeOfOptionalHeader");
		}
	}
}
