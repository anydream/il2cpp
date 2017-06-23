// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Text;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Represents the .NET metadata header
	/// </summary>
	/// <remarks><c>IMAGE_COR20_HEADER.MetaData</c> points to this header</remarks>
	public sealed class MetaDataHeader : FileSection {
	    /// <summary>
		/// Returns the signature (should be 0x424A5342)
		/// </summary>
		public uint Signature { get; }

	    /// <summary>
		/// Returns the major version
		/// </summary>
		public ushort MajorVersion { get; }

	    /// <summary>
		/// Returns the minor version
		/// </summary>
		public ushort MinorVersion { get; }

	    /// <summary>
		/// Returns the reserved dword (pointer to extra header data)
		/// </summary>
		public uint Reserved1 { get; }

	    /// <summary>
		/// Returns the version string length value
		/// </summary>
		public uint StringLength { get; }

	    /// <summary>
		/// Returns the version string
		/// </summary>
		public string VersionString { get; }

	    /// <summary>
		/// Returns the offset of <c>STORAGEHEADER</c>
		/// </summary>
		public FileOffset StorageHeaderOffset { get; }

	    /// <summary>
		/// Returns the flags (reserved)
		/// </summary>
		public StorageFlags Flags { get; }

	    /// <summary>
		/// Returns the reserved byte (padding)
		/// </summary>
		public byte Reserved2 { get; }

	    /// <summary>
		/// Returns the number of streams
		/// </summary>
		public ushort Streams { get; }

	    /// <summary>
		/// Returns all stream headers
		/// </summary>
		public IList<StreamHeader> StreamHeaders { get; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public MetaDataHeader(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			this.Signature = reader.ReadUInt32();
			if (verify && this.Signature != 0x424A5342)
				throw new BadImageFormatException("Invalid MetaData header signature");
			this.MajorVersion = reader.ReadUInt16();
			this.MinorVersion = reader.ReadUInt16();
			if (verify && !((MajorVersion == 1 && MinorVersion == 1) || (MajorVersion == 0 && MinorVersion >= 19)))
				throw new BadImageFormatException($"Unknown MetaData header version: {MajorVersion}.{MinorVersion}");
			this.Reserved1 = reader.ReadUInt32();
			this.StringLength = reader.ReadUInt32();
			this.VersionString = ReadString(reader, StringLength);
			this.StorageHeaderOffset = reader.FileOffset + reader.Position;
			this.Flags = (StorageFlags)reader.ReadByte();
			this.Reserved2 = reader.ReadByte();
			this.Streams = reader.ReadUInt16();
			this.StreamHeaders = new StreamHeader[Streams];
			for (int i = 0; i < StreamHeaders.Count; i++)
				StreamHeaders[i] = new StreamHeader(reader, verify);
			SetEndoffset(reader);
		}

		static string ReadString(IImageStream reader, uint maxLength) {
			long endPos = reader.Position + maxLength;
			if (endPos < reader.Position || endPos > reader.Length)
				throw new BadImageFormatException("Invalid MD version string");
			byte[] utf8Bytes = new byte[maxLength];
			uint i;
			for (i = 0; i < maxLength; i++) {
				byte b = reader.ReadByte();
				if (b == 0)
					break;
				utf8Bytes[i] = b;
			}
			reader.Position = endPos;
			return Encoding.UTF8.GetString(utf8Bytes, 0, (int)i);
		}
	}
}
