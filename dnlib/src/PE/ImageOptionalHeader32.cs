// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_OPTIONAL_HEADER (32-bit) PE section
	/// </summary>
	public sealed class ImageOptionalHeader32 : FileSection, IImageOptionalHeader {
	    readonly uint imageBase;
	    readonly uint sizeOfStackReserve;
		readonly uint sizeOfStackCommit;
		readonly uint sizeOfHeapReserve;
		readonly uint sizeOfHeapCommit;

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.Magic field
		/// </summary>
		public ushort Magic { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MajorLinkerVersion field
		/// </summary>
		public byte MajorLinkerVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MinorLinkerVersion field
		/// </summary>
		public byte MinorLinkerVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfCode field
		/// </summary>
		public uint SizeOfCode { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfInitializedData field
		/// </summary>
		public uint SizeOfInitializedData { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfUninitializedData field
		/// </summary>
		public uint SizeOfUninitializedData { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.AddressOfEntryPoint field
		/// </summary>
		public RVA AddressOfEntryPoint { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.BaseOfCode field
		/// </summary>
		public RVA BaseOfCode { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.BaseOfData field
		/// </summary>
		public RVA BaseOfData { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.ImageBase field
		/// </summary>
		public ulong ImageBase => imageBase;

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SectionAlignment field
		/// </summary>
		public uint SectionAlignment { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.FileAlignment field
		/// </summary>
		public uint FileAlignment { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MajorOperatingSystemVersion field
		/// </summary>
		public ushort MajorOperatingSystemVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MinorOperatingSystemVersion field
		/// </summary>
		public ushort MinorOperatingSystemVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MajorImageVersion field
		/// </summary>
		public ushort MajorImageVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MinorImageVersion field
		/// </summary>
		public ushort MinorImageVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MajorSubsystemVersion field
		/// </summary>
		public ushort MajorSubsystemVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.MinorSubsystemVersion field
		/// </summary>
		public ushort MinorSubsystemVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.Win32VersionValue field
		/// </summary>
		public uint Win32VersionValue { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfImage field
		/// </summary>
		public uint SizeOfImage { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfHeaders field
		/// </summary>
		public uint SizeOfHeaders { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.CheckSum field
		/// </summary>
		public uint CheckSum { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.Subsystem field
		/// </summary>
		public Subsystem Subsystem { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.DllCharacteristics field
		/// </summary>
		public DllCharacteristics DllCharacteristics { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfStackReserve field
		/// </summary>
		public ulong SizeOfStackReserve => sizeOfStackReserve;

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfStackCommit field
		/// </summary>
		public ulong SizeOfStackCommit => sizeOfStackCommit;

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfHeapReserve field
		/// </summary>
		public ulong SizeOfHeapReserve => sizeOfHeapReserve;

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.SizeOfHeapCommit field
		/// </summary>
		public ulong SizeOfHeapCommit => sizeOfHeapCommit;

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.LoaderFlags field
		/// </summary>
		public uint LoaderFlags { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.NumberOfRvaAndSizes field
		/// </summary>
		public uint NumberOfRvaAndSizes { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER.DataDirectories field
		/// </summary>
		public ImageDataDirectory[] DataDirectories { get; } = new ImageDataDirectory[16];

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="totalSize">Total size of this optional header (from the file header)</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageOptionalHeader32(IImageStream reader, uint totalSize, bool verify) {
			if (totalSize < 0x60)
				throw new BadImageFormatException("Invalid optional header size");
			if (verify && reader.Position + totalSize > reader.Length)
				throw new BadImageFormatException("Invalid optional header size");
			SetStartOffset(reader);
			this.Magic = reader.ReadUInt16();
			this.MajorLinkerVersion = reader.ReadByte();
			this.MinorLinkerVersion = reader.ReadByte();
			this.SizeOfCode = reader.ReadUInt32();
			this.SizeOfInitializedData = reader.ReadUInt32();
			this.SizeOfUninitializedData = reader.ReadUInt32();
			this.AddressOfEntryPoint = (RVA)reader.ReadUInt32();
			this.BaseOfCode = (RVA)reader.ReadUInt32();
			this.BaseOfData = (RVA)reader.ReadUInt32();
			this.imageBase = reader.ReadUInt32();
			this.SectionAlignment = reader.ReadUInt32();
			this.FileAlignment = reader.ReadUInt32();
			this.MajorOperatingSystemVersion = reader.ReadUInt16();
			this.MinorOperatingSystemVersion = reader.ReadUInt16();
			this.MajorImageVersion = reader.ReadUInt16();
			this.MinorImageVersion = reader.ReadUInt16();
			this.MajorSubsystemVersion = reader.ReadUInt16();
			this.MinorSubsystemVersion = reader.ReadUInt16();
			this.Win32VersionValue = reader.ReadUInt32();
			this.SizeOfImage = reader.ReadUInt32();
			this.SizeOfHeaders = reader.ReadUInt32();
			this.CheckSum = reader.ReadUInt32();
			this.Subsystem = (Subsystem)reader.ReadUInt16();
			this.DllCharacteristics = (DllCharacteristics)reader.ReadUInt16();
			this.sizeOfStackReserve = reader.ReadUInt32();
			this.sizeOfStackCommit = reader.ReadUInt32();
			this.sizeOfHeapReserve = reader.ReadUInt32();
			this.sizeOfHeapCommit = reader.ReadUInt32();
			this.LoaderFlags = reader.ReadUInt32();
			this.NumberOfRvaAndSizes = reader.ReadUInt32();
			for (int i = 0; i < DataDirectories.Length; i++) {
				uint len = (uint)(reader.Position - startOffset);
				if (len + 8 <= totalSize)
					DataDirectories[i] = new ImageDataDirectory(reader, verify);
				else
					DataDirectories[i] = new ImageDataDirectory();
			}
			reader.Position = (long)startOffset + totalSize;
			SetEndoffset(reader);
		}
	}
}
