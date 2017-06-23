// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
 using System.Linq;
 using dnlib.DotNet.MD;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// MD table interface
	/// </summary>
	public interface IMDTable {
		/// <summary>
		/// Gets the table type
		/// </summary>
		Table Table { get; }

		/// <summary>
		/// <c>true</c> if the table is empty
		/// </summary>
		bool IsEmpty { get; }

		/// <summary>
		/// Gets the number of rows in this table
		/// </summary>
		int Rows { get; }

		/// <summary>
		/// Gets/sets a value indicating whether it's sorted
		/// </summary>
		bool IsSorted { get; set; }

		/// <summary>
		/// <c>true</c> if <see cref="SetReadOnly()"/> has been called
		/// </summary>
		bool IsReadOnly { get; }

		/// <summary>
		/// Gets/sets the <see cref="TableInfo"/>
		/// </summary>
		TableInfo TableInfo { get; set; }

		/// <summary>
		/// Called when the table can't be modified any more
		/// </summary>
		void SetReadOnly();

		/// <summary>
		/// Gets a raw row
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>The raw row</returns>
		IRawRow Get(uint rid);

		/// <summary>
		/// Gets all raw rows
		/// </summary>
		IEnumerable<IRawRow> GetRawRows();
	}

	/// <summary>
	/// Creates rows in a table. Rows can optionally be shared to create a compact table.
	/// </summary>
	/// <typeparam name="T">The raw row type</typeparam>
	public sealed class MDTable<T> : IMDTable, IEnumerable<T> where T : IRawRow {
	    readonly Dictionary<T, uint> cachedDict;
		readonly List<T> cached;

	    /// <inheritdoc/>
		public Table Table { get; }

	    /// <inheritdoc/>
		public bool IsEmpty => cached.Count == 0;

	    /// <inheritdoc/>
		public int Rows => cached.Count;

	    /// <inheritdoc/>
		public bool IsSorted { get; set; }

	    /// <inheritdoc/>
		public bool IsReadOnly { get; private set; }

	    /// <inheritdoc/>
		public TableInfo TableInfo { get; set; }

	    /// <summary>
		/// Gets the value with rid <paramref name="rid"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		public T this[uint rid] => cached[(int)rid - 1];

	    /// <inheritdoc/>
		public IRawRow Get(uint rid) {
			return this[rid];
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="table">The table type</param>
		/// <param name="equalityComparer">Equality comparer</param>
		public MDTable(Table table, IEqualityComparer<T> equalityComparer) {
			this.Table = table;
			this.cachedDict = new Dictionary<T, uint>(equalityComparer);
			this.cached = new List<T>();
		}

		/// <inheritdoc/>
		public void SetReadOnly() {
			IsReadOnly = true;
		}

		/// <summary>
		/// Adds a row. If the row already exists, returns a rid to the existing one, else
		/// it's created and a new rid is returned.
		/// </summary>
		/// <param name="row">The row. It's now owned by us and must NOT be modified by the caller.</param>
		/// <returns>The RID (row ID) of the row</returns>
		public uint Add(T row) {
			if (IsReadOnly)
				throw new ModuleWriterException($"Trying to modify table {Table} after it's been set to read-only");
            return cachedDict.TryGetValue(row, out uint rid) ? rid : Create(row);
        }

		/// <summary>
		/// Creates a new row even if this row already exists.
		/// </summary>
		/// <param name="row">The row. It's now owned by us and must NOT be modified by the caller.</param>
		/// <returns>The RID (row ID) of the row</returns>
		public uint Create(T row) {
			if (IsReadOnly)
				throw new ModuleWriterException($"Trying to modify table {Table} after it's been set to read-only");
			uint rid = (uint)cached.Count + 1;
			if (!cachedDict.ContainsKey(row))
				cachedDict[row] = rid;
			cached.Add(row);
			return rid;
		}

		/// <summary>
		/// Re-adds all added rows. Should be called if rows have been modified after being
		/// inserted.
		/// </summary>
		public void ReAddRows() {
			if (IsReadOnly)
				throw new ModuleWriterException($"Trying to modify table {Table} after it's been set to read-only");
			cachedDict.Clear();
			for (int i = 0; i < cached.Count; i++) {
				uint rid = (uint)i + 1;
				var row = cached[i];
				if (!cachedDict.ContainsKey(row))
					cachedDict[row] = rid;
			}
		}

		/// <summary>
		/// Reset the table.
		/// </summary>
		public void Reset() {
			if (IsReadOnly)
				throw new ModuleWriterException($"Trying to modify table {Table} after it's been set to read-only");
			cachedDict.Clear();
			cached.Clear();
		}

		/// <inheritdoc/>
		public IEnumerator<T> GetEnumerator() {
			return cached.GetEnumerator();
		}

		/// <inheritdoc/>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		/// <inheritdoc/>
		public IEnumerable<IRawRow> GetRawRows()
		{
		    return cached.Cast<IRawRow>();
		}
	}
}
