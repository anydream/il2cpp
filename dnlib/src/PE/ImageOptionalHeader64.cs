// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Represents the IMAGE_OPTIONAL_HEADER64 PE section
	/// </summary>
	public sealed class ImageOptionalHeader64 : FileSection, IImageOptionalHeader {
	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.Magic field
		/// </summary>
		public ushort Magic { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MajorLinkerVersion field
		/// </summary>
		public byte MajorLinkerVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MinorLinkerVersion field
		/// </summary>
		public byte MinorLinkerVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfCode field
		/// </summary>
		public uint SizeOfCode { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfInitializedData field
		/// </summary>
		public uint SizeOfInitializedData { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfUninitializedData field
		/// </summary>
		public uint SizeOfUninitializedData { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.AddressOfEntryPoint field
		/// </summary>
		public RVA AddressOfEntryPoint { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.BaseOfCode field
		/// </summary>
		public RVA BaseOfCode { get; }

	    /// <summary>
		/// Returns 0 since BaseOfData is not present in IMAGE_OPTIONAL_HEADER64
		/// </summary>
		public RVA BaseOfData => 0;

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.ImageBase field
		/// </summary>
		public ulong ImageBase { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SectionAlignment field
		/// </summary>
		public uint SectionAlignment { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.FileAlignment field
		/// </summary>
		public uint FileAlignment { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MajorOperatingSystemVersion field
		/// </summary>
		public ushort MajorOperatingSystemVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MinorOperatingSystemVersion field
		/// </summary>
		public ushort MinorOperatingSystemVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MajorImageVersion field
		/// </summary>
		public ushort MajorImageVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MinorImageVersion field
		/// </summary>
		public ushort MinorImageVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MajorSubsystemVersion field
		/// </summary>
		public ushort MajorSubsystemVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.MinorSubsystemVersion field
		/// </summary>
		public ushort MinorSubsystemVersion { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.Win32VersionValue field
		/// </summary>
		public uint Win32VersionValue { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfImage field
		/// </summary>
		public uint SizeOfImage { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfHeaders field
		/// </summary>
		public uint SizeOfHeaders { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.CheckSum field
		/// </summary>
		public uint CheckSum { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.Subsystem field
		/// </summary>
		public Subsystem Subsystem { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.DllCharacteristics field
		/// </summary>
		public DllCharacteristics DllCharacteristics { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfStackReserve field
		/// </summary>
		public ulong SizeOfStackReserve { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfStackCommit field
		/// </summary>
		public ulong SizeOfStackCommit { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfHeapReserve field
		/// </summary>
		public ulong SizeOfHeapReserve { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.SizeOfHeapCommit field
		/// </summary>
		public ulong SizeOfHeapCommit { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.LoaderFlags field
		/// </summary>
		public uint LoaderFlags { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.NumberOfRvaAndSizes field
		/// </summary>
		public uint NumberOfRvaAndSizes { get; }

	    /// <summary>
		/// Returns the IMAGE_OPTIONAL_HEADER64.DataDirectories field
		/// </summary>
		public ImageDataDirectory[] DataDirectories { get; } = new ImageDataDirectory[16];

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="totalSize">Total size of this optional header (from the file header)</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public ImageOptionalHeader64(IImageStream reader, uint totalSize, bool verify) {
			if (totalSize < 0x70)
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
			this.ImageBase = reader.ReadUInt64();
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
			this.SizeOfStackReserve = reader.ReadUInt64();
			this.SizeOfStackCommit = reader.ReadUInt64();
			this.SizeOfHeapReserve = reader.ReadUInt64();
			this.SizeOfHeapCommit = reader.ReadUInt64();
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
