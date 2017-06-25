// dnlib: See LICENSE.txt for more info

using System;
using System.Threading;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the GenericParamConstraint table
	/// </summary>
	public abstract class GenericParamConstraint : IHasCustomAttribute, IContainsGenericParameter {
	    /// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.GenericParamConstraint, Rid);

	    /// <inheritdoc/>
		public uint Rid { get; set; }

	    /// <inheritdoc/>
		public int HasCustomAttributeTag => 20;

	    /// <summary>
		/// Gets the owner generic param
		/// </summary>
		public GenericParam Owner { get; protected internal set; }

	    /// <summary>
		/// From column GenericParamConstraint.Constraint
		/// </summary>
		public ITypeDefOrRef Constraint { get; set; }

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

	    bool IContainsGenericParameter.ContainsGenericParameter => TypeHelper.ContainsGenericParameter(this);
	}

	/// <summary>
	/// A GenericParamConstraintAssembly row created by the user and not present in the original .NET file
	/// </summary>
	public class GenericParamConstraintUser : GenericParamConstraint {
		/// <summary>
		/// Default constructor
		/// </summary>
		public GenericParamConstraintUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="constraint">The constraint</param>
		public GenericParamConstraintUser(ITypeDefOrRef constraint) {
			this.Constraint = constraint;
		}
	}

	/// <summary>
	/// Created from a row in the GenericParamConstraint table
	/// </summary>
	sealed class GenericParamConstraintMD : GenericParamConstraint, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

	    /// <inheritdoc/>
		public uint OrigRid { get; }

	    /// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.GenericParamConstraint, OrigRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>GenericParamConstraint</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public GenericParamConstraintMD(ModuleDefMD readerModule, uint rid, GenericParamContext gpContext) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException(nameof(readerModule));
			if (readerModule.TablesStream.GenericParamConstraintTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"GenericParamConstraint rid {rid} does not exist");
#endif
			this.OrigRid = rid;
			this.Rid = rid;
			this.readerModule = readerModule;
			uint constraint = readerModule.TablesStream.ReadGenericParamConstraintRow2(OrigRid);
			this.Constraint = readerModule.ResolveTypeDefOrRef(constraint, gpContext);
			this.Owner = readerModule.GetOwner(this);
		}

		internal GenericParamConstraintMD InitializeAll() {
			MemberMDInitializer.Initialize(Owner);
			MemberMDInitializer.Initialize(Constraint);
			MemberMDInitializer.Initialize(CustomAttributes);
			return this;
		}
	}
}
