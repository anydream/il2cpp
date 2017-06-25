// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Reads all PE sections from a PE stream
	/// </summary>
	sealed class PEInfo {
	    /// <summary>
		/// Returns the DOS header
		/// </summary>
		public ImageDosHeader ImageDosHeader { get; }

	    /// <summary>
		/// Returns the NT headers
		/// </summary>
		public ImageNTHeaders ImageNTHeaders { get; }

	    /// <summary>
		/// Returns the section headers
		/// </summary>
		public ImageSectionHeader[] ImageSectionHeaders { get; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify sections</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public PEInfo(IImageStream reader, bool verify) {
			reader.Position = 0;
			this.ImageDosHeader = new ImageDosHeader(reader, verify);

			if (verify && this.ImageDosHeader.NTHeadersOffset == 0)
				throw new BadImageFormatException("Invalid NT headers offset");
			reader.Position = this.ImageDosHeader.NTHeadersOffset;
			this.ImageNTHeaders = new ImageNTHeaders(reader, verify);

			reader.Position = (long)this.ImageNTHeaders.OptionalHeader.StartOffset + this.ImageNTHeaders.FileHeader.SizeOfOptionalHeader;
			this.ImageSectionHeaders = new ImageSectionHeader[this.ImageNTHeaders.FileHeader.NumberOfSections];
			for (int i = 0; i < this.ImageSectionHeaders.Length; i++)
				this.ImageSectionHeaders[i] = new ImageSectionHeader(reader, verify);
		}

		/// <summary>
		/// Returns the first <see cref="ImageSectionHeader"/> that has data at file offset
		/// <paramref name="offset"/>
		/// </summary>
		/// <param name="offset">The file offset</param>
		/// <returns></returns>
		public ImageSectionHeader ToImageSectionHeader(FileOffset offset) {
			foreach (var section in ImageSectionHeaders) {
				if ((long)offset >= section.PointerToRawData && (long)offset < section.PointerToRawData + section.SizeOfRawData)
					return section;
			}
			return null;
		}

		/// <summary>
		/// Returns the first <see cref="ImageSectionHeader"/> that has data at RVA
		/// <paramref name="rva"/>
		/// </summary>
		/// <param name="rva">The RVA</param>
		/// <returns></returns>
		public ImageSectionHeader ToImageSectionHeader(RVA rva) {
			foreach (var section in ImageSectionHeaders) {
				if (rva >= section.VirtualAddress && rva < section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData))
					return section;
			}
			return null;
		}

		/// <summary>
		/// Converts a <see cref="FileOffset"/> to an <see cref="RVA"/>
		/// </summary>
		/// <param name="offset">The file offset to convert</param>
		/// <returns>The RVA</returns>
		public RVA ToRVA(FileOffset offset) {
			var section = ToImageSectionHeader(offset);
			if (section != null)
				return (uint)(offset - section.PointerToRawData) + section.VirtualAddress;
			return (RVA)offset;
		}

		/// <summary>
		/// Converts an <see cref="RVA"/> to a <see cref="FileOffset"/>
		/// </summary>
		/// <param name="rva">The RVA to convert</param>
		/// <returns>The file offset</returns>
		public FileOffset ToFileOffset(RVA rva) {
			var section = ToImageSectionHeader(rva);
			if (section != null)
				return (FileOffset)((long)(rva - section.VirtualAddress) + section.PointerToRawData);
			return (FileOffset)rva;
		}

		static ulong alignUp(ulong val, uint alignment) {
			return (val + alignment - 1) & ~(ulong)(alignment - 1);
		}

		/// <summary>
		/// Returns size of image rounded up to <see cref="IImageOptionalHeader.SectionAlignment"/>
		/// </summary>
		/// <remarks>It calculates the size itself, and does not return <see cref="IImageOptionalHeader.SizeOfImage"/></remarks>
		/// <returns>Size of image in bytes</returns>
		public long GetImageSize() {
			var optHdr = ImageNTHeaders.OptionalHeader;
			uint alignment = optHdr.SectionAlignment;
			ulong len = alignUp(optHdr.SizeOfHeaders, alignment);
			foreach (var section in ImageSectionHeaders) {
				ulong len2 = alignUp((ulong)section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData), alignment);
				if (len2 > len)
					len = len2;
			}
			return (long)len;
		}
	}
}
