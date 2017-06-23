// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet {
	/// <summary>
	/// Default implementation of <see cref="ICorLibTypes"/>
	/// </summary>
	public sealed class CorLibTypes : ICorLibTypes {
		readonly ModuleDef module;

	    /// <inheritdoc/>
		public CorLibTypeSig Void { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig Boolean { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig Char { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig SByte { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig Byte { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig Int16 { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig UInt16 { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig Int32 { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig UInt32 { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig Int64 { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig UInt64 { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig Single { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig Double { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig String { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig TypedReference { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig IntPtr { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig UIntPtr { get; private set; }

	    /// <inheritdoc/>
		public CorLibTypeSig Object { get; private set; }

	    /// <inheritdoc/>
		public AssemblyRef AssemblyRef { get; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The owner module</param>
		public CorLibTypes(ModuleDef module)
			: this(module, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The owner module</param>
		/// <param name="corLibAssemblyRef">Corlib assembly reference or <c>null</c> if a default
		/// assembly reference should be created</param>
		public CorLibTypes(ModuleDef module, AssemblyRef corLibAssemblyRef) {
			this.module = module;
			this.AssemblyRef = corLibAssemblyRef ?? CreateCorLibAssemblyRef();
			Initialize();
		}

		AssemblyRef CreateCorLibAssemblyRef() {
			return module.UpdateRowId(AssemblyRefUser.CreateMscorlibReferenceCLR20());
		}

		void Initialize() {
			bool isCorLib = module.Assembly.IsCorLib();
			Void	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Void"),		ElementType.Void);
			Boolean	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Boolean"),	ElementType.Boolean);
			Char	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Char"),		ElementType.Char);
			SByte	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "SByte"),		ElementType.I1);
			Byte	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Byte"),		ElementType.U1);
			Int16	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Int16"),		ElementType.I2);
			UInt16	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "UInt16"),	ElementType.U2);
			Int32	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Int32"),		ElementType.I4);
			UInt32	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "UInt32"),	ElementType.U4);
			Int64	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Int64"),		ElementType.I8);
			UInt64	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "UInt64"),	ElementType.U8);
			Single	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Single"),	ElementType.R4);
			Double	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Double"),	ElementType.R8);
			String	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "String"),	ElementType.String);
			TypedReference = new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "TypedReference"), ElementType.TypedByRef);
			IntPtr	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "IntPtr"),	ElementType.I);
			UIntPtr	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "UIntPtr"),	ElementType.U);
			Object	= new CorLibTypeSig(CreateCorLibTypeRef(isCorLib, "Object"),	ElementType.Object);
		}

		ITypeDefOrRef CreateCorLibTypeRef(bool isCorLib, string name) {
			var tr = new TypeRefUser(module, "System", name, AssemblyRef);
			if (isCorLib) {
				var td = module.Find(tr);
				if (td != null)
					return td;
			}
			return module.UpdateRowId(tr);
		}

		/// <inheritdoc/>
		public TypeRef GetTypeRef(string @namespace, string name) {
			return module.UpdateRowId(new TypeRefUser(module, @namespace, name, AssemblyRef));
		}
	}
}
