// dnlib: See LICENSE.txt for more info

using System;
using System.Threading;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the StandAloneSig table
	/// </summary>
	public abstract class StandAloneSig : IHasCustomAttribute, IContainsGenericParameter {
	    /// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.StandAloneSig, Rid);

	    /// <inheritdoc/>
		public uint Rid { get; set; }

	    /// <inheritdoc/>
		public int HasCustomAttributeTag => 11;

	    /// <summary>
		/// From column StandAloneSig.Signature
		/// </summary>
		public CallingConventionSig Signature { get; set; }

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
		/// Gets/sets the method sig
		/// </summary>
		public MethodSig MethodSig {
			get => Signature as MethodSig;
	        set => Signature = value;
	    }

		/// <summary>
		/// Gets/sets the locals sig
		/// </summary>
		public LocalSig LocalSig {
			get => Signature as LocalSig;
		    set => Signature = value;
		}

		/// <inheritdoc/>
		public bool ContainsGenericParameter => TypeHelper.ContainsGenericParameter(this);
	}

	/// <summary>
	/// A StandAloneSig row created by the user and not present in the original .NET file
	/// </summary>
	public class StandAloneSigUser : StandAloneSig {
		/// <summary>
		/// Default constructor
		/// </summary>
		public StandAloneSigUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="localSig">A locals sig</param>
		public StandAloneSigUser(LocalSig localSig) {
			this.Signature = localSig;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="methodSig">A method sig</param>
		public StandAloneSigUser(MethodSig methodSig) {
			this.Signature = methodSig;
		}
	}

	/// <summary>
	/// Created from a row in the StandAloneSig table
	/// </summary>
	sealed class StandAloneSigMD : StandAloneSig, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

	    /// <inheritdoc/>
		public uint OrigRid { get; }

	    /// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.StandAloneSig, OrigRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>StandAloneSig</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public StandAloneSigMD(ModuleDefMD readerModule, uint rid, GenericParamContext gpContext) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException(nameof(readerModule));
			if (readerModule.TablesStream.StandAloneSigTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"StandAloneSig rid {rid} does not exist");
#endif
			this.OrigRid = rid;
			this.Rid = rid;
			this.readerModule = readerModule;
			uint signature = readerModule.TablesStream.ReadStandAloneSigRow2(OrigRid);
			this.Signature = readerModule.ReadSignature(signature, gpContext);
		}
	}
}
