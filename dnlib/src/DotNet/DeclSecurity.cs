// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the DeclSecurity table
	/// </summary>
	[DebuggerDisplay("{Action} Count={SecurityAttributes.Count}")]
	public abstract class DeclSecurity : IHasCustomAttribute {
	    /// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.DeclSecurity, Rid);

	    /// <inheritdoc/>
		public uint Rid { get; set; }

	    /// <inheritdoc/>
		public int HasCustomAttributeTag => 8;

	    /// <summary>
		/// From column DeclSecurity.Action
		/// </summary>
		public SecurityAction Action {
			get => action;
	        set => action = value;
	    }
		/// <summary/>
		protected SecurityAction action;

		/// <summary>
		/// From column DeclSecurity.PermissionSet
		/// </summary>
		public ThreadSafe.IList<SecurityAttribute> SecurityAttributes {
			get {
				if (securityAttributes == null)
					InitializeSecurityAttributes();
				return securityAttributes;
			}
		}
		/// <summary/>
		protected ThreadSafe.IList<SecurityAttribute> securityAttributes;
		/// <summary>Initializes <see cref="securityAttributes"/></summary>
		protected virtual void InitializeSecurityAttributes() {
			Interlocked.CompareExchange(ref securityAttributes, ThreadSafeListCreator.Create<SecurityAttribute>(), null);
		}

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
		/// <c>true</c> if <see cref="SecurityAttributes"/> is not empty
		/// </summary>
		public bool HasSecurityAttributes => SecurityAttributes.Count > 0;

	    /// <summary>
		/// Gets the blob data or <c>null</c> if there's none
		/// </summary>
		/// <returns>Blob data or <c>null</c></returns>
		public abstract byte[] GetBlob();

		/// <summary>
		/// Returns the .NET 1.x XML string or null if it's not a .NET 1.x format
		/// </summary>
		/// <returns></returns>
		public string GetNet1xXmlString() {
			return GetNet1xXmlStringInternal(SecurityAttributes);
		}

		internal static string GetNet1xXmlStringInternal(IList<SecurityAttribute> secAttrs) {
			if (secAttrs == null || secAttrs.Count != 1)
				return null;
			var sa = secAttrs[0];
			if (sa == null || sa.TypeFullName != "System.Security.Permissions.PermissionSetAttribute")
				return null;
			if (sa.NamedArguments.Count != 1)
				return null;
			var na = sa.NamedArguments[0];
			if (na == null || !na.IsProperty || na.Name != "XML")
				return null;
			if (na.ArgumentType.GetElementType() != ElementType.String)
				return null;
			var arg = na.Argument;
			if (arg.Type.GetElementType() != ElementType.String)
				return null;
			var utf8 = arg.Value as UTF8String;
			if ((object)utf8 != null)
				return utf8;
			var s = arg.Value as string;
		    return s;
		}
	}

	/// <summary>
	/// A DeclSecurity row created by the user and not present in the original .NET file
	/// </summary>
	public class DeclSecurityUser : DeclSecurity {
		/// <summary>
		/// Default constructor
		/// </summary>
		public DeclSecurityUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="action">The security action</param>
		/// <param name="securityAttrs">The security attributes (now owned by this)</param>
		public DeclSecurityUser(SecurityAction action, IList<SecurityAttribute> securityAttrs) {
			this.action = action;
			this.securityAttributes = ThreadSafeListCreator.MakeThreadSafe(securityAttrs);
		}

		/// <inheritdoc/>
		public override byte[] GetBlob() {
			return null;
		}
	}

	/// <summary>
	/// Created from a row in the DeclSecurity table
	/// </summary>
	sealed class DeclSecurityMD : DeclSecurity, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

	    readonly uint permissionSet;

		/// <inheritdoc/>
		public uint OrigRid { get; }

	    /// <inheritdoc/>
		protected override void InitializeSecurityAttributes() {
			var gpContext = new GenericParamContext();
			var tmp = DeclSecurityReader.Read(readerModule, permissionSet, gpContext);
			Interlocked.CompareExchange(ref securityAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.DeclSecurity, OrigRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>DeclSecurity</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public DeclSecurityMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException(nameof(readerModule));
			if (readerModule.TablesStream.DeclSecurityTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"DeclSecurity rid {rid} does not exist");
#endif
			this.OrigRid = rid;
			this.Rid = rid;
			this.readerModule = readerModule;
			this.permissionSet = readerModule.TablesStream.ReadDeclSecurityRow(OrigRid, out this.action);
		}

		/// <inheritdoc/>
		public override byte[] GetBlob() {
			return readerModule.BlobStream.Read(permissionSet);
		}
	}
}
