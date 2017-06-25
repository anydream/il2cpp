// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace dnlib.IO {
	/// <summary>
	/// IImageStream for unmanaged memory
	/// </summary>
	[DebuggerDisplay("FO:{fileOffset} S:{Length} A:{startAddr}")]
	sealed unsafe class UnmanagedMemoryImageStream : IImageStream {
		FileOffset fileOffset;
		long startAddr;
		long endAddr;
		long currentAddr;
		UnmanagedMemoryStreamCreator owner;
		bool ownOwner;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="owner">Owner of memory</param>
		/// <param name="fileOffset">File offset of data</param>
		/// <param name="baseAddr">Address of data</param>
		/// <param name="length">Length of data</param>
		public UnmanagedMemoryImageStream(UnmanagedMemoryStreamCreator owner, FileOffset fileOffset, long baseAddr, long length) {
			this.fileOffset = fileOffset;
			this.startAddr = baseAddr;
			this.endAddr = baseAddr + length;
			this.currentAddr = this.startAddr;
			this.owner = owner;
		}

		/// <summary>
		/// Saves <paramref name="creator"/> in this instance so it doesn't get garbage collected.
		/// </summary>
		/// <param name="creator">A <see cref="UnmanagedMemoryStreamCreator"/> instance</param>
		internal UnmanagedMemoryImageStream(UnmanagedMemoryStreamCreator creator)
			: this(creator, 0, 0, creator.Length) {
			this.ownOwner = true;
		}

		/// <inheritdoc/>
		public FileOffset FileOffset => fileOffset;

	    /// <summary>
		/// Gets the start address of the memory this instance uses
		/// </summary>
		internal IntPtr StartAddress => new IntPtr((byte*)owner.UnsafeUseAddress + startAddr);

	    /// <inheritdoc/>
		public long Length => endAddr - startAddr;

	    /// <inheritdoc/>
		public long Position {
			get => currentAddr - startAddr;
	        set {
				if (IntPtr.Size == 4 && (ulong)value > int.MaxValue)
					value = int.MaxValue;
				long newAddr = startAddr + value;
				if (newAddr < startAddr)
					newAddr = endAddr;
				currentAddr = newAddr;
			}
		}

		/// <inheritdoc/>
		public IImageStream Create(FileOffset offset, long length) {
			if ((long)offset < 0 || length < 0)
				return MemoryImageStream.CreateEmpty();

			long offs = Math.Min(Length, (long)offset);
			long len = Math.Min(Length - offs, length);
			return new UnmanagedMemoryImageStream(owner, (FileOffset)((long)fileOffset + (long)offset), startAddr + (long)offs, len);
		}

		/// <inheritdoc/>
		public byte[] ReadBytes(int size) {
			if (size < 0)
				throw new IOException("Invalid size");
			size = (int)Math.Min(size, Length - Math.Min(Length, Position));
			var newData = new byte[size];
			Marshal.Copy(new IntPtr((byte*)owner.Address + currentAddr), newData, 0, size);
			currentAddr += size;
			return newData;
		}

		/// <inheritdoc/>
		public int Read(byte[] buffer, int offset, int length) {
			if (length < 0)
				throw new IOException("Invalid size");
			length = (int)Math.Min(length, Length - Math.Min(Length, Position));
			Marshal.Copy(new IntPtr((byte*)owner.Address + currentAddr), buffer, offset, length);
			currentAddr += length;
			return length;
		}

		/// <inheritdoc/>
		public byte[] ReadBytesUntilByte(byte b) {
			long pos = GetPositionOf(b);
			if (pos == -1)
				return null;
			return ReadBytes((int)(pos - currentAddr));
		}

	    long GetPositionOf(byte b) {
			byte* pos = (byte*)owner.Address + currentAddr;
			byte* posStart = pos;
			var endPos = (byte*)owner.Address + endAddr;
			while (pos < endPos) {
				if (*pos == b)
					return currentAddr + (pos - posStart);
				pos++;
			}
			return -1;
		}

		/// <inheritdoc/>
		public sbyte ReadSByte() {
			if (currentAddr >= endAddr)
				throw new IOException("Can't read one SByte");
			return (sbyte)*((byte*)owner.Address + currentAddr++);
		}

		/// <inheritdoc/>
		public byte ReadByte() {
			if (currentAddr >= endAddr)
				throw new IOException("Can't read one Byte");
			return *((byte*)owner.Address + currentAddr++);
		}

		/// <inheritdoc/>
		public short ReadInt16() {
			if (currentAddr + 1 >= endAddr)
				throw new IOException("Can't read one Int16");
			short val = *(short*)((byte*)owner.Address + currentAddr);
			currentAddr += 2;
			return val;
		}

		/// <inheritdoc/>
		public ushort ReadUInt16() {
			if (currentAddr + 1 >= endAddr)
				throw new IOException("Can't read one UInt16");
			ushort val = *(ushort*)((byte*)owner.Address + currentAddr);
			currentAddr += 2;
			return val;
		}

		/// <inheritdoc/>
		public int ReadInt32() {
			if (currentAddr + 3 >= endAddr)
				throw new IOException("Can't read one Int32");
			int val = *(int*)((byte*)owner.Address + currentAddr);
			currentAddr += 4;
			return val;
		}

		/// <inheritdoc/>
		public uint ReadUInt32() {
			if (currentAddr + 3 >= endAddr)
				throw new IOException("Can't read one UInt32");
			uint val = *(uint*)((byte*)owner.Address + currentAddr);
			currentAddr += 4;
			return val;
		}

		/// <inheritdoc/>
		public long ReadInt64() {
			if (currentAddr + 7 >= endAddr)
				throw new IOException("Can't read one Int64");
			long val = *(long*)((byte*)owner.Address + currentAddr);
			currentAddr += 8;
			return val;
		}

		/// <inheritdoc/>
		public ulong ReadUInt64() {
			if (currentAddr + 7 >= endAddr)
				throw new IOException("Can't read one UInt64");
			ulong val = *(ulong*)((byte*)owner.Address + currentAddr);
			currentAddr += 8;
			return val;
		}

		/// <inheritdoc/>
		public float ReadSingle() {
			if (currentAddr + 3 >= endAddr)
				throw new IOException("Can't read one Single");
			var val = *(float*)((byte*)owner.Address + currentAddr);
			currentAddr += 4;
			return val;
		}

		/// <inheritdoc/>
		public double ReadDouble() {
			if (currentAddr + 7 >= endAddr)
				throw new IOException("Can't read one Double");
			var val = *(double*)((byte*)owner.Address + currentAddr);
			currentAddr += 8;
			return val;
		}

		/// <inheritdoc/>
		public string ReadString(int chars) {
			if (IntPtr.Size == 4 && (uint)chars > (uint)int.MaxValue)
				throw new IOException("Not enough space to read the string");
			if (currentAddr + chars * 2 < currentAddr || (chars != 0 && currentAddr + chars * 2 - 1 >= endAddr))
				throw new IOException("Not enough space to read the string");
			var s = new string((char*)((byte*)owner.Address + currentAddr), 0, chars);
			currentAddr += chars * 2;
			return s;
		}

		/// <inheritdoc/>
		public void Dispose() {
			fileOffset = 0;
			startAddr = 0;
			endAddr = 0;
			currentAddr = 0;
			if (ownOwner)
				owner?.Dispose();
			owner = null;
		}
	}
}
