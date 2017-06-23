// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet.MD;
using dnlib.W32Resources;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// <see cref="ModuleWriter"/> options
	/// </summary>
	public sealed class ModuleWriterOptions : ModuleWriterOptionsBase {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ModuleWriterOptions() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		public ModuleWriterOptions(ModuleDef module)
			: this(module, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		/// <param name="listener">Module writer listener</param>
		public ModuleWriterOptions(ModuleDef module, IModuleWriterListener listener)
			: base(module, listener) {
		}
	}

	/// <summary>
	/// Writes a .NET PE file. See also <see cref="NativeModuleWriter"/>
	/// </summary>
	public sealed class ModuleWriter : ModuleWriterBase {
		const uint DEFAULT_IAT_ALIGNMENT = 4;
		const uint DEFAULT_IMPORTDIRECTORY_ALIGNMENT = 4;
		const uint DEFAULT_STARTUPSTUB_ALIGNMENT = 1;
		const uint DEFAULT_RELOC_ALIGNMENT = 4;

		readonly ModuleDef module;
		ModuleWriterOptions options;

		List<PESection> sections;
		PESection textSection;
		PESection rsrcSection;

	    /// <inheritdoc/>
		public override ModuleDef Module => module;

	    /// <inheritdoc/>
		public override ModuleWriterOptionsBase TheOptions => Options;

	    /// <summary>
		/// Gets/sets the writer options. This is never <c>null</c>
		/// </summary>
		public ModuleWriterOptions Options {
			get => options ?? (options = new ModuleWriterOptions(module));
	        set => options = value;
	    }

		/// <summary>
		/// Gets all <see cref="PESection"/>s
		/// </summary>
		public override List<PESection> Sections => sections;

	    /// <summary>
		/// Gets the <c>.text</c> section
		/// </summary>
		public override PESection TextSection => textSection;

	    /// <summary>
		/// Gets the <c>.rsrc</c> section or <c>null</c> if there's none
		/// </summary>
		public override PESection RsrcSection => rsrcSection;

	    /// <summary>
		/// Gets the <c>.reloc</c> section or <c>null</c> if there's none
		/// </summary>
		public PESection RelocSection { get; private set; }

	    /// <summary>
		/// Gets the PE headers
		/// </summary>
		public PEHeaders PEHeaders { get; private set; }

	    /// <summary>
		/// Gets the IAT or <c>null</c> if there's none
		/// </summary>
		public ImportAddressTable ImportAddressTable { get; private set; }

	    /// <summary>
		/// Gets the .NET header
		/// </summary>
		public ImageCor20Header ImageCor20Header { get; private set; }

	    /// <summary>
		/// Gets the import directory or <c>null</c> if there's none
		/// </summary>
		public ImportDirectory ImportDirectory { get; private set; }

	    /// <summary>
		/// Gets the startup stub or <c>null</c> if there's none
		/// </summary>
		public StartupStub StartupStub { get; private set; }

	    /// <summary>
		/// Gets the reloc directory or <c>null</c> if there's none
		/// </summary>
		public RelocDirectory RelocDirectory { get; private set; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		public ModuleWriter(ModuleDef module)
			: this(module, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		/// <param name="options">Options or <c>null</c></param>
		public ModuleWriter(ModuleDef module, ModuleWriterOptions options) {
			this.module = module;
			this.options = options;
		}

		/// <inheritdoc/>
		protected override long WriteImpl() {
			Initialize();
			metaData.CreateTables();
			return WriteFile();
		}

		void Initialize() {
			CreateSections();
			Listener.OnWriterEvent(this, ModuleWriterEvent.PESectionsCreated);

			CreateChunks();
			Listener.OnWriterEvent(this, ModuleWriterEvent.ChunksCreated);

			AddChunksToSections();
			Listener.OnWriterEvent(this, ModuleWriterEvent.ChunksAddedToSections);
		}

		/// <inheritdoc/>
		protected override Win32Resources GetWin32Resources() {
			return Options.Win32Resources ?? module.Win32Resources;
		}

		void CreateSections() {
			sections = new List<PESection>();
			sections.Add(textSection = new PESection(".text", 0x60000020));
			if (GetWin32Resources() != null)
				sections.Add(rsrcSection = new PESection(".rsrc", 0x40000040));
			if (!Options.Is64Bit)
				sections.Add(RelocSection = new PESection(".reloc", 0x42000040));
		}

		void CreateChunks() {
			PEHeaders = new PEHeaders(Options.PEHeadersOptions);

			if (!Options.Is64Bit) {
				ImportAddressTable = new ImportAddressTable();
				ImportDirectory = new ImportDirectory();
				StartupStub = new StartupStub();
				RelocDirectory = new RelocDirectory();
			}

			CreateStrongNameSignature();

			ImageCor20Header = new ImageCor20Header(Options.Cor20HeaderOptions);
			CreateMetaDataChunks(module);

			CreateDebugDirectory();

			if (ImportDirectory != null)
				ImportDirectory.IsExeFile = Options.IsExeFile;

			PEHeaders.IsExeFile = Options.IsExeFile;
		}

		void AddChunksToSections() {
			textSection.Add(ImportAddressTable, DEFAULT_IAT_ALIGNMENT);
			textSection.Add(ImageCor20Header, DEFAULT_COR20HEADER_ALIGNMENT);
			textSection.Add(strongNameSignature, DEFAULT_STRONGNAMESIG_ALIGNMENT);
			textSection.Add(constants, DEFAULT_CONSTANTS_ALIGNMENT);
			textSection.Add(methodBodies, DEFAULT_METHODBODIES_ALIGNMENT);
			textSection.Add(netResources, DEFAULT_NETRESOURCES_ALIGNMENT);
			textSection.Add(metaData, DEFAULT_METADATA_ALIGNMENT);
			textSection.Add(debugDirectory, DEFAULT_DEBUGDIRECTORY_ALIGNMENT);
			textSection.Add(ImportDirectory, DEFAULT_IMPORTDIRECTORY_ALIGNMENT);
			textSection.Add(StartupStub, DEFAULT_STARTUPSTUB_ALIGNMENT);
		    rsrcSection?.Add(win32Resources, DEFAULT_WIN32_RESOURCES_ALIGNMENT);
		    RelocSection?.Add(RelocDirectory, DEFAULT_RELOC_ALIGNMENT);
		}

		long WriteFile() {
			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginWritePdb);
			WritePdbFile();
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndWritePdb);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginCalculateRvasAndFileOffsets);
		    var chunks = new List<IChunk> {PEHeaders};
		    chunks.AddRange(sections);
		    PEHeaders.PESections = sections;
			CalculateRvasAndFileOffsets(chunks, 0, 0, PEHeaders.FileAlignment, PEHeaders.SectionAlignment);
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndCalculateRvasAndFileOffsets);

			InitializeChunkProperties();

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginWriteChunks);
			var writer = new BinaryWriter(destStream);
			WriteChunks(writer, chunks, 0, PEHeaders.FileAlignment);
			long imageLength = writer.BaseStream.Position - destStreamBaseOffset;
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndWriteChunks);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginStrongNameSign);
			if (Options.StrongNameKey != null)
				StrongNameSign((long)strongNameSignature.FileOffset);
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndStrongNameSign);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginWritePEChecksum);
			if (Options.AddCheckSum)
				PEHeaders.WriteCheckSum(writer, imageLength);
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndWritePEChecksum);

			return imageLength;
		}

		void InitializeChunkProperties() {
			Options.Cor20HeaderOptions.EntryPoint = GetEntryPoint();

			if (ImportAddressTable != null) {
				ImportAddressTable.ImportDirectory = ImportDirectory;
				ImportDirectory.ImportAddressTable = ImportAddressTable;
				StartupStub.ImportDirectory = ImportDirectory;
				StartupStub.PEHeaders = PEHeaders;
				RelocDirectory.StartupStub = StartupStub;
			}
			PEHeaders.StartupStub = StartupStub;
			PEHeaders.ImageCor20Header = ImageCor20Header;
			PEHeaders.ImportAddressTable = ImportAddressTable;
			PEHeaders.ImportDirectory = ImportDirectory;
			PEHeaders.Win32Resources = win32Resources;
			PEHeaders.RelocDirectory = RelocDirectory;
			PEHeaders.DebugDirectory = debugDirectory;
			ImageCor20Header.MetaData = metaData;
			ImageCor20Header.NetResources = netResources;
			ImageCor20Header.StrongNameSignature = strongNameSignature;
		}

		uint GetEntryPoint() {
            if (module.ManagedEntryPoint is MethodDef methodEntryPoint)
                return new MDToken(Table.Method, metaData.GetRid(methodEntryPoint)).Raw;

            if (module.ManagedEntryPoint is FileDef fileEntryPoint)
                return new MDToken(Table.File, metaData.GetRid(fileEntryPoint)).Raw;

            uint nativeEntryPoint = (uint)module.NativeEntryPoint;
			if (nativeEntryPoint != 0)
				return nativeEntryPoint;

			return 0;
		}
	}
}
