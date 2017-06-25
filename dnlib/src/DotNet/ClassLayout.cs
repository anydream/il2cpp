// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the ClassLayout table
	/// </summary>
	public abstract class ClassLayout : IMDTokenProvider {
	    /// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.ClassLayout, Rid);

	    /// <inheritdoc/>
		public uint Rid { get; set; }

	    /// <summary>
		/// From column ClassLayout.PackingSize
		/// </summary>
		public ushort PackingSize {
			get => packingSize;
	        set => packingSize = value;
	    }
		/// <summary/>
		protected ushort packingSize;

		/// <summary>
		/// From column ClassLayout.ClassSize
		/// </summary>
		public uint ClassSize { get; set; }
	}

	/// <summary>
	/// A ClassLayout row created by the user and not present in the original .NET file
	/// </summary>
	public class ClassLayoutUser : ClassLayout {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ClassLayoutUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="packingSize">PackingSize</param>
		/// <param name="classSize">ClassSize</param>
		public ClassLayoutUser(ushort packingSize, uint classSize) {
			this.packingSize = packingSize;
			this.ClassSize = classSize;
		}
	}

	/// <summary>
	/// Created from a row in the ClassLayout table
	/// </summary>
	sealed class ClassLayoutMD : ClassLayout, IMDTokenProviderMD {
	    /// <inheritdoc/>
		public uint OrigRid { get; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ClassLayout</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ClassLayoutMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException(nameof(readerModule));
			if (readerModule.TablesStream.ClassLayoutTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"ClassLayout rid {rid} does not exist");
#endif
			this.OrigRid = rid;
			this.Rid = rid;
			this.ClassSize = readerModule.TablesStream.ReadClassLayoutRow(OrigRid, out this.packingSize);
		}
	}
}
