// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;
using dnlib.PE;
using dnlib.Threading;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// .NET metadata tables stream
	/// </summary>
	public sealed partial class TablesStream : DotNetStream {
		bool initialized;
	    byte majorVersion;
		byte minorVersion;

	    HotTableStream hotTableStream;

#pragma warning disable 1591	// XML doc comment
		public MDTable ModuleTable { get; private set; }
		public MDTable TypeRefTable { get; private set; }
		public MDTable TypeDefTable { get; private set; }
		public MDTable FieldPtrTable { get; private set; }
		public MDTable FieldTable { get; private set; }
		public MDTable MethodPtrTable { get; private set; }
		public MDTable MethodTable { get; private set; }
		public MDTable ParamPtrTable { get; private set; }
		public MDTable ParamTable { get; private set; }
		public MDTable InterfaceImplTable { get; private set; }
		public MDTable MemberRefTable { get; private set; }
		public MDTable ConstantTable { get; private set; }
		public MDTable CustomAttributeTable { get; private set; }
		public MDTable FieldMarshalTable { get; private set; }
		public MDTable DeclSecurityTable { get; private set; }
		public MDTable ClassLayoutTable { get; private set; }
		public MDTable FieldLayoutTable { get; private set; }
		public MDTable StandAloneSigTable { get; private set; }
		public MDTable EventMapTable { get; private set; }
		public MDTable EventPtrTable { get; private set; }
		public MDTable EventTable { get; private set; }
		public MDTable PropertyMapTable { get; private set; }
		public MDTable PropertyPtrTable { get; private set; }
		public MDTable PropertyTable { get; private set; }
		public MDTable MethodSemanticsTable { get; private set; }
		public MDTable MethodImplTable { get; private set; }
		public MDTable ModuleRefTable { get; private set; }
		public MDTable TypeSpecTable { get; private set; }
		public MDTable ImplMapTable { get; private set; }
		public MDTable FieldRVATable { get; private set; }
		public MDTable ENCLogTable { get; private set; }
		public MDTable ENCMapTable { get; private set; }
		public MDTable AssemblyTable { get; private set; }
		public MDTable AssemblyProcessorTable { get; private set; }
		public MDTable AssemblyOSTable { get; private set; }
		public MDTable AssemblyRefTable { get; private set; }
		public MDTable AssemblyRefProcessorTable { get; private set; }
		public MDTable AssemblyRefOSTable { get; private set; }
		public MDTable FileTable { get; private set; }
		public MDTable ExportedTypeTable { get; private set; }
		public MDTable ManifestResourceTable { get; private set; }
		public MDTable NestedClassTable { get; private set; }
		public MDTable GenericParamTable { get; private set; }
		public MDTable MethodSpecTable { get; private set; }
		public MDTable GenericParamConstraintTable { get; private set; }
		public MDTable Document { get; private set; }
		public MDTable MethodDebugInformation { get; private set; }
		public MDTable LocalScope { get; private set; }
		public MDTable LocalVariable { get; private set; }
		public MDTable LocalConstant { get; private set; }
		public MDTable ImportScope { get; private set; }
		public MDTable StateMachineMethod { get; private set; }
		public MDTable CustomDebugInformation { get; private set; }
#pragma warning restore

#if THREAD_SAFE
		internal readonly Lock theLock = Lock.Create();
#endif

		internal HotTableStream HotTableStream {
			set => hotTableStream = value;
		}

		/// <summary>
		/// Gets/sets the column reader
		/// </summary>
		public IColumnReader ColumnReader { get; set; }

	    /// <summary>
		/// Gets/sets the <c>Method</c> table reader
		/// </summary>
		public IRowReader<RawMethodRow> MethodRowReader { get; set; }

	    /// <summary>
		/// Gets the reserved field
		/// </summary>
		public uint Reserved1 { get; private set; }

	    /// <summary>
		/// Gets the version. The major version is in the upper 8 bits, and the minor version
		/// is in the lower 8 bits.
		/// </summary>
		public ushort Version => (ushort)((majorVersion << 8) | minorVersion);

	    /// <summary>
		/// Gets <see cref="MDStreamFlags"/>
		/// </summary>
		public MDStreamFlags Flags { get; private set; }

	    /// <summary>
		/// Gets the reserved log2 rid field
		/// </summary>
		public byte Log2Rid { get; private set; }

	    /// <summary>
		/// Gets the valid mask
		/// </summary>
		public ulong ValidMask { get; private set; }

	    /// <summary>
		/// Gets the sorted mask
		/// </summary>
		public ulong SortedMask { get; private set; }

	    /// <summary>
		/// Gets the extra data
		/// </summary>
		public uint ExtraData { get; private set; }

	    /// <summary>
		/// Gets the MD tables
		/// </summary>
		public MDTable[] MDTables { get; private set; }

	    /// <summary>
		/// Gets the <see cref="MDStreamFlags.BigStrings"/> bit
		/// </summary>
		public bool HasBigStrings => (Flags & MDStreamFlags.BigStrings) != 0;

	    /// <summary>
		/// Gets the <see cref="MDStreamFlags.BigGUID"/> bit
		/// </summary>
		public bool HasBigGUID => (Flags & MDStreamFlags.BigGUID) != 0;

	    /// <summary>
		/// Gets the <see cref="MDStreamFlags.BigBlob"/> bit
		/// </summary>
		public bool HasBigBlob => (Flags & MDStreamFlags.BigBlob) != 0;

	    /// <summary>
		/// Gets the <see cref="MDStreamFlags.Padding"/> bit
		/// </summary>
		public bool HasPadding => (Flags & MDStreamFlags.Padding) != 0;

	    /// <summary>
		/// Gets the <see cref="MDStreamFlags.DeltaOnly"/> bit
		/// </summary>
		public bool HasDeltaOnly => (Flags & MDStreamFlags.DeltaOnly) != 0;

	    /// <summary>
		/// Gets the <see cref="MDStreamFlags.ExtraData"/> bit
		/// </summary>
		public bool HasExtraData => (Flags & MDStreamFlags.ExtraData) != 0;

	    /// <summary>
		/// Gets the <see cref="MDStreamFlags.HasDelete"/> bit
		/// </summary>
		public bool HasDelete => (Flags & MDStreamFlags.HasDelete) != 0;

	    /// <inheritdoc/>
		public TablesStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}

		/// <summary>
		/// Initializes MD tables
		/// </summary>
		/// <param name="peImage">The PEImage</param>
		public void Initialize(IPEImage peImage) {
			if (initialized)
				throw new Exception("Initialize() has already been called");
			initialized = true;

			Reserved1 = imageStream.ReadUInt32();
			majorVersion = imageStream.ReadByte();
			minorVersion = imageStream.ReadByte();
			Flags = (MDStreamFlags)imageStream.ReadByte();
			Log2Rid = imageStream.ReadByte();
			ValidMask = imageStream.ReadUInt64();
			SortedMask = imageStream.ReadUInt64();

			var dnTableSizes = new DotNetTableSizes();
			var tableInfos = dnTableSizes.CreateTables(majorVersion, minorVersion, out int maxPresentTables);
			MDTables = new MDTable[tableInfos.Length];

			ulong valid = ValidMask;
			var sizes = new uint[64];
			for (int i = 0; i < 64; valid >>= 1, i++) {
				uint rows = (valid & 1) == 0 ? 0 : imageStream.ReadUInt32();
				if (i >= maxPresentTables)
					rows = 0;
				sizes[i] = rows;
				if (i < MDTables.Length)
					MDTables[i] = new MDTable((Table)i, rows, tableInfos[i]);
			}

			if (HasExtraData)
				ExtraData = imageStream.ReadUInt32();

			dnTableSizes.InitializeSizes(HasBigStrings, HasBigGUID, HasBigBlob, sizes);

			var currentRva = peImage.ToRVA(imageStream.FileOffset) + (uint)imageStream.Position;
			foreach (var mdTable in MDTables) {
				var dataLen = (long)mdTable.TableInfo.RowSize * (long)mdTable.Rows;
				mdTable.ImageStream = peImage.CreateStream(currentRva, dataLen);
				var newRva = currentRva + (uint)dataLen;
				if (newRva < currentRva)
					throw new BadImageFormatException("Too big MD table");
				currentRva = newRva;
			}

			InitializeTables();
		}

		void InitializeTables() {
			ModuleTable = MDTables[(int)Table.Module];
			TypeRefTable = MDTables[(int)Table.TypeRef];
			TypeDefTable = MDTables[(int)Table.TypeDef];
			FieldPtrTable = MDTables[(int)Table.FieldPtr];
			FieldTable = MDTables[(int)Table.Field];
			MethodPtrTable = MDTables[(int)Table.MethodPtr];
			MethodTable = MDTables[(int)Table.Method];
			ParamPtrTable = MDTables[(int)Table.ParamPtr];
			ParamTable = MDTables[(int)Table.Param];
			InterfaceImplTable = MDTables[(int)Table.InterfaceImpl];
			MemberRefTable = MDTables[(int)Table.MemberRef];
			ConstantTable = MDTables[(int)Table.Constant];
			CustomAttributeTable = MDTables[(int)Table.CustomAttribute];
			FieldMarshalTable = MDTables[(int)Table.FieldMarshal];
			DeclSecurityTable = MDTables[(int)Table.DeclSecurity];
			ClassLayoutTable = MDTables[(int)Table.ClassLayout];
			FieldLayoutTable = MDTables[(int)Table.FieldLayout];
			StandAloneSigTable = MDTables[(int)Table.StandAloneSig];
			EventMapTable = MDTables[(int)Table.EventMap];
			EventPtrTable = MDTables[(int)Table.EventPtr];
			EventTable = MDTables[(int)Table.Event];
			PropertyMapTable = MDTables[(int)Table.PropertyMap];
			PropertyPtrTable = MDTables[(int)Table.PropertyPtr];
			PropertyTable = MDTables[(int)Table.Property];
			MethodSemanticsTable = MDTables[(int)Table.MethodSemantics];
			MethodImplTable = MDTables[(int)Table.MethodImpl];
			ModuleRefTable = MDTables[(int)Table.ModuleRef];
			TypeSpecTable = MDTables[(int)Table.TypeSpec];
			ImplMapTable = MDTables[(int)Table.ImplMap];
			FieldRVATable = MDTables[(int)Table.FieldRVA];
			ENCLogTable = MDTables[(int)Table.ENCLog];
			ENCMapTable = MDTables[(int)Table.ENCMap];
			AssemblyTable = MDTables[(int)Table.Assembly];
			AssemblyProcessorTable = MDTables[(int)Table.AssemblyProcessor];
			AssemblyOSTable = MDTables[(int)Table.AssemblyOS];
			AssemblyRefTable = MDTables[(int)Table.AssemblyRef];
			AssemblyRefProcessorTable = MDTables[(int)Table.AssemblyRefProcessor];
			AssemblyRefOSTable = MDTables[(int)Table.AssemblyRefOS];
			FileTable = MDTables[(int)Table.File];
			ExportedTypeTable = MDTables[(int)Table.ExportedType];
			ManifestResourceTable = MDTables[(int)Table.ManifestResource];
			NestedClassTable = MDTables[(int)Table.NestedClass];
			GenericParamTable = MDTables[(int)Table.GenericParam];
			MethodSpecTable = MDTables[(int)Table.MethodSpec];
			GenericParamConstraintTable = MDTables[(int)Table.GenericParamConstraint];
			Document = MDTables[(int)Table.Document];
			MethodDebugInformation = MDTables[(int)Table.MethodDebugInformation];
			LocalScope = MDTables[(int)Table.LocalScope];
			LocalVariable = MDTables[(int)Table.LocalVariable];
			LocalConstant = MDTables[(int)Table.LocalConstant];
			ImportScope = MDTables[(int)Table.ImportScope];
			StateMachineMethod = MDTables[(int)Table.StateMachineMethod];
			CustomDebugInformation = MDTables[(int)Table.CustomDebugInformation];
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				var mt = MDTables;
				if (mt != null) {
					foreach (var mdTable in mt)
					{
					    mdTable?.Dispose();
					}
					MDTables = null;
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Returns a MD table
		/// </summary>
		/// <param name="table">The table type</param>
		/// <returns>A <see cref="MDTable"/> or <c>null</c> if table doesn't exist</returns>
		public MDTable Get(Table table) {
			int index = (int)table;
			if ((uint)index >= (uint)MDTables.Length)
				return null;
			return MDTables[index];
		}

		/// <summary>
		/// Checks whether a table exists
		/// </summary>
		/// <param name="table">The table type</param>
		/// <returns><c>true</c> if the table exists</returns>
		public bool HasTable(Table table) {
			return (uint)table < (uint)MDTables.Length;
		}

		/// <summary>
		/// Checks whether table <paramref name="table"/> is sorted
		/// </summary>
		/// <param name="table">The table</param>
		public bool IsSorted(MDTable table) {
			int index = (int)table.Table;
			if ((uint)index >= 64)
				return false;
			return (SortedMask & (1UL << index)) != 0;
		}
	}
}
