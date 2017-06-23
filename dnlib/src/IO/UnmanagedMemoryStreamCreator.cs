// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.IO;

namespace dnlib.IO {
	/// <summary>
	/// Creates <see cref="UnmanagedMemoryStream"/>s to partially access an
	/// unmanaged memory range
	/// </summary>
	/// <seealso cref="MemoryStreamCreator"/>
	[DebuggerDisplay("mem: D:{data} L:{Length} {FileName}")]
	class UnmanagedMemoryStreamCreator : IImageStreamCreator {
		/// <summary>
		/// Address of data
		/// </summary>
		protected IntPtr data;

	    /// <summary>
		/// The file name
		/// </summary>
		public string FileName { get; set; }

	    /// <summary>
		/// Size of the data
		/// </summary>
		public long Length { get; set; }

	    /// <summary>
		/// Returns the base address of the data
		/// </summary>
		public IntPtr Address => data;

	    public IntPtr UnsafeUseAddress {
			get {
				unsafeUseAddress = true;
				return data;
			}
		}
		protected bool unsafeUseAddress;

		/// <summary>
		/// Default constructor
		/// </summary>
		protected UnmanagedMemoryStreamCreator() {
		}

		/// <summary>
		/// Constructor for 0 bytes of data
		/// </summary>
		/// <param name="data">Pointer to the data</param>
		public UnmanagedMemoryStreamCreator(IntPtr data)
			: this(data, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Pointer to the data</param>
		/// <param name="dataLength">Length of data</param>
		/// <exception cref="ArgumentOutOfRangeException">If one of the args is invalid</exception>
		public UnmanagedMemoryStreamCreator(IntPtr data, long dataLength) {
			if (dataLength < 0)
				throw new ArgumentOutOfRangeException(nameof(dataLength));
			this.data = data;
			this.Length = dataLength;
		}

		/// <inheritdoc/>
		public unsafe IImageStream Create(FileOffset offset, long length) {
			if (offset < 0 || length < 0)
				return MemoryImageStream.CreateEmpty();

			long offs = Math.Min((long)Length, (long)offset);
			long len = Math.Min((long)Length - offs, length);
			return new UnmanagedMemoryImageStream(this, offset, offs, len);
		}

		/// <inheritdoc/>
		public IImageStream CreateFull() {
			return new UnmanagedMemoryImageStream(this, 0, 0, Length);
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing"><c>true</c> if called by <see cref="Dispose()"/></param>
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				data = IntPtr.Zero;
				Length = 0;
				FileName = null;
			}
		}
	}
}
