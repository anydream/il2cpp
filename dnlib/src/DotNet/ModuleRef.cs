// dnlib: See LICENSE.txt for more info

using System;
using System.Threading;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the ModuleRef table
	/// </summary>
	public abstract class ModuleRef : IHasCustomAttribute, IMemberRefParent, IResolutionScope, IModule, IOwnerModule {
	    /// <summary>
		/// The owner module
		/// </summary>
		protected ModuleDef module;

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.ModuleRef, Rid);

	    /// <inheritdoc/>
		public uint Rid { get; set; }

	    /// <inheritdoc/>
		public int HasCustomAttributeTag => 12;

	    /// <inheritdoc/>
		public int MemberRefParentTag => 2;

	    /// <inheritdoc/>
		public int ResolutionScopeTag => 1;

	    /// <inheritdoc/>
		public ScopeType ScopeType => ScopeType.ModuleRef;

	    /// <inheritdoc/>
		public string ScopeName => FullName;

	    /// <summary>
		/// From column ModuleRef.Name
		/// </summary>
		public UTF8String Name { get; set; }

	    /// <summary>
		/// Gets all custom attributes
		/// </summary>
		public CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributes == null)
					InitializeCustomAttributes();
				return customAttributes;
			}
		}
		/// <summary/>
		protected CustomAttributeCollection customAttributes;
		/// <summary>Initializes <see cref="customAttributes"/></summary>
		protected virtual void InitializeCustomAttributes() {
			Interlocked.CompareExchange(ref customAttributes, new CustomAttributeCollection(), null);
		}

		/// <inheritdoc/>
		public bool HasCustomAttributes => CustomAttributes.Count > 0;

	    /// <inheritdoc/>
		public ModuleDef Module => module;

	    /// <summary>
		/// Gets the definition module, i.e., the module which it references, or <c>null</c>
		/// if the module can't be found.
		/// </summary>
		public ModuleDef DefinitionModule {
			get {
				if (module == null)
					return null;
				var n = Name;
				if (UTF8String.CaseInsensitiveEquals(n, module.Name))
					return module;
				var asm = DefinitionAssembly;
				return asm?.FindModule(n);
			}
		}

		/// <summary>
		/// Gets the definition assembly, i.e., the assembly of the module it references, or
		/// <c>null</c> if the assembly can't be found.
		/// </summary>
		public AssemblyDef DefinitionAssembly => module?.Assembly;

	    /// <inheritdoc/>
		public string FullName => UTF8String.ToSystemStringOrEmpty(Name);

	    /// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A ModuleRef row created by the user and not present in the original .NET file
	/// </summary>
	public class ModuleRefUser : ModuleRef {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		public ModuleRefUser(ModuleDef module)
			: this(module, UTF8String.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Module name</param>
		public ModuleRefUser(ModuleDef module, UTF8String name) {
			this.module = module;
			this.Name = name;
		}
	}

	/// <summary>
	/// Created from a row in the ModuleRef table
	/// </summary>
	sealed class ModuleRefMD : ModuleRef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

	    /// <inheritdoc/>
		public uint OrigRid { get; }

	    /// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.ModuleRef, OrigRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ModuleRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ModuleRefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException(nameof(readerModule));
			if (readerModule.TablesStream.ModuleRefTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"ModuleRef rid {rid} does not exist");
#endif
			this.OrigRid = rid;
			this.Rid = rid;
			this.readerModule = readerModule;
			this.module = readerModule;
			uint name = readerModule.TablesStream.ReadModuleRefRow2(OrigRid);
			this.Name = readerModule.StringsStream.ReadNoNull(name);
		}
	}
}
