// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the ManifestResource table
	/// </summary>
	[DebuggerDisplay("{Offset} {Name.String} {Implementation}")]
	public abstract class ManifestResource : IHasCustomAttribute {
	    /// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.ManifestResource, Rid);

	    /// <inheritdoc/>
		public uint Rid { get; set; }

	    /// <inheritdoc/>
		public int HasCustomAttributeTag => 18;

	    /// <summary>
		/// From column ManifestResource.Offset
		/// </summary>
		public uint Offset {
			get => offset;
	        set => offset = value;
	    }
		/// <summary/>
		protected uint offset;

		/// <summary>
		/// From column ManifestResource.Flags
		/// </summary>
		public ManifestResourceAttributes Flags {
			get => (ManifestResourceAttributes)attributes;
		    set => attributes = (int)value;
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column ManifestResource.Name
		/// </summary>
		public UTF8String Name { get; set; }

	    /// <summary>
		/// From column ManifestResource.Implementation
		/// </summary>
		public IImplementation Implementation { get; set; }

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

	    /// <summary>
		/// Modify <see cref="attributes"/> property: <see cref="attributes"/> =
		/// (<see cref="attributes"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(ManifestResourceAttributes andMask, ManifestResourceAttributes orMask) {
#if THREAD_SAFE
			int origVal, newVal;
			do {
				origVal = attributes;
				newVal = (origVal & (int)andMask) | (int)orMask;
			} while (Interlocked.CompareExchange(ref attributes, newVal, origVal) != origVal);
#else
			attributes = (attributes & (int)andMask) | (int)orMask;
#endif
		}

		/// <summary>
		/// Gets/sets the visibility
		/// </summary>
		public ManifestResourceAttributes Visibility {
			get => (ManifestResourceAttributes)attributes & ManifestResourceAttributes.VisibilityMask;
		    set => ModifyAttributes(~ManifestResourceAttributes.VisibilityMask, value & ManifestResourceAttributes.VisibilityMask);
		}

		/// <summary>
		/// <c>true</c> if <see cref="ManifestResourceAttributes.Public"/> is set
		/// </summary>
		public bool IsPublic => ((ManifestResourceAttributes)attributes & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Public;

	    /// <summary>
		/// <c>true</c> if <see cref="ManifestResourceAttributes.Private"/> is set
		/// </summary>
		public bool IsPrivate => ((ManifestResourceAttributes)attributes & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Private;
	}

	/// <summary>
	/// A ManifestResource row created by the user and not present in the original .NET file
	/// </summary>
	public class ManifestResourceUser : ManifestResource {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ManifestResourceUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="implementation">Implementation</param>
		public ManifestResourceUser(UTF8String name, IImplementation implementation)
			: this(name, implementation, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="implementation">Implementation</param>
		/// <param name="flags">Flags</param>
		public ManifestResourceUser(UTF8String name, IImplementation implementation, ManifestResourceAttributes flags)
			: this(name, implementation, flags, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="implementation">Implementation</param>
		/// <param name="flags">Flags</param>
		/// <param name="offset">Offset</param>
		public ManifestResourceUser(UTF8String name, IImplementation implementation, ManifestResourceAttributes flags, uint offset) {
			this.Name = name;
			this.Implementation = implementation;
			this.attributes = (int)flags;
			this.offset = offset;
		}
	}

	/// <summary>
	/// Created from a row in the ManifestResource table
	/// </summary>
	sealed class ManifestResourceMD : ManifestResource, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

	    /// <inheritdoc/>
		public uint OrigRid { get; }

	    /// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.ManifestResource, OrigRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ManifestResource</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ManifestResourceMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException(nameof(readerModule));
			if (readerModule.TablesStream.ManifestResourceTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"ManifestResource rid {rid} does not exist");
#endif
			this.OrigRid = rid;
			this.Rid = rid;
			this.readerModule = readerModule;
			uint implementation = readerModule.TablesStream.ReadManifestResourceRow(OrigRid, out this.offset, out this.attributes, out uint name);
			this.Name = readerModule.StringsStream.ReadNoNull(name);
			this.Implementation = readerModule.ResolveImplementation(implementation);
		}
	}
}
