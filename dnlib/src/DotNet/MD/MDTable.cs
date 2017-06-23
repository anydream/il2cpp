// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// A MD table (eg. Method table)
	/// </summary>
	[DebuggerDisplay("DL:{imageStream.Length} R:{Rows} RS:{TableInfo.RowSize} C:{Count} {tableInfo.Name}")]
	public sealed class MDTable : IDisposable, IFileSection {
	    private IImageStream imageStream;

		// Fix for VS2015 expression evaluator: "The debugger is unable to evaluate this expression"
	    private int Count => TableInfo.Columns.Count;

	    /// <inheritdoc/>
		public FileOffset StartOffset => imageStream.FileOffset;

	    /// <inheritdoc/>
		public FileOffset EndOffset => imageStream.FileOffset + imageStream.Length;

	    /// <summary>
		/// Gets the table
		/// </summary>
		public Table Table { get; }

	    /// <summary>
		/// Gets the name of this table
		/// </summary>
		public string Name => TableInfo.Name;

	    /// <summary>
		/// Returns total number of rows
		/// </summary>
		public uint Rows { get; private set; }

	    /// <summary>
		/// Gets the total size in bytes of one row in this table
		/// </summary>
		public uint RowSize => (uint)TableInfo.RowSize;

	    /// <summary>
		/// Returns all the columns
		/// </summary>
		public IList<ColumnInfo> Columns => TableInfo.Columns;

	    /// <summary>
		/// Returns <c>true</c> if there are no valid rows
		/// </summary>
		public bool IsEmpty => Rows == 0;

	    /// <summary>
		/// Returns info about this table
		/// </summary>
		public TableInfo TableInfo { get; private set; }

	    /// <summary>
		/// The stream that can access all the rows in this table
		/// </summary>
		internal IImageStream ImageStream {
			get => imageStream;
	        set {
				var ims = imageStream;
				if (ims == value)
					return;
	            ims?.Dispose();
	            imageStream = value;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="table">The table</param>
		/// <param name="numRows">Number of rows in this table</param>
		/// <param name="tableInfo">Info about this table</param>
		internal MDTable(Table table, uint numRows, TableInfo tableInfo) {
			this.Table = table;
			this.Rows = numRows;
			this.TableInfo = tableInfo;
		}

		internal IImageStream CloneImageStream() {
			return imageStream.Clone();
		}

		/// <summary>
		/// Checks whether the row <paramref name="rid"/> exists
		/// </summary>
		/// <param name="rid">Row ID</param>
		public bool IsValidRID(uint rid) {
			return rid != 0 && rid <= Rows;
		}

		/// <summary>
		/// Checks whether the row <paramref name="rid"/> does not exist
		/// </summary>
		/// <param name="rid">Row ID</param>
		public bool IsInvalidRID(uint rid) {
			return rid == 0 || rid > Rows;
		}

		/// <inheritdoc/>
		public void Dispose() {
			var ims = imageStream;
		    ims?.Dispose();
		    Rows = 0;
			TableInfo = null;
			imageStream = null;
		}
	}
}
