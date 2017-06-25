// dnlib: See LICENSE.txt for more info

using System;
using System.Threading;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the MethodSpec table
	/// </summary>
	public abstract class MethodSpec : IHasCustomAttribute, IMethod, IContainsGenericParameter {
	    /// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.MethodSpec, Rid);

	    /// <inheritdoc/>
		public uint Rid { get; set; }

	    /// <inheritdoc/>
		public int HasCustomAttributeTag => 21;

	    /// <summary>
		/// From column MethodSpec.Method
		/// </summary>
		public IMethodDefOrRef Method { get; set; }

	    /// <summary>
		/// From column MethodSpec.Instantiation
		/// </summary>
		public CallingConventionSig Instantiation { get; set; }

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
		MethodSig IMethod.MethodSig {
			get {
				var m = Method;
				return m?.MethodSig;
			}
			set {
				var m = Method;
				if (m != null)
					m.MethodSig = value;
			}
		}

		/// <inheritdoc/>
		public UTF8String Name {
			get {
				var m = Method;
				return m == null ? UTF8String.Empty : m.Name;
			}
			set {
				var m = Method;
				if (m != null)
					m.Name = value;
			}
		}

		/// <inheritdoc/>
		public ITypeDefOrRef DeclaringType {
			get {
				var m = Method;
				return m?.DeclaringType;
			}
		}

		/// <summary>
		/// Gets/sets the generic instance method sig
		/// </summary>
		public GenericInstMethodSig GenericInstMethodSig {
			get => Instantiation as GenericInstMethodSig;
		    set => Instantiation = value;
		}

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get {
				var sig = GenericInstMethodSig;
				return sig?.GenericArguments.Count ?? 0;
			}
		}

		/// <inheritdoc/>
		public ModuleDef Module {
			get {
				var m = Method;
				return m?.Module;
			}
		}

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string FullName {
			get {
				var gims = GenericInstMethodSig;
				var methodGenArgs = gims?.GenericArguments;
				var m = Method;
                if (m is MethodDef methodDef)
                {
                    var declaringType = methodDef.DeclaringType;
                    return FullNameCreator.MethodFullName(declaringType?.FullName, methodDef.Name, methodDef.MethodSig, null, methodGenArgs);
                }

                var memberRef = m as MemberRef;
			    var methodSig = memberRef?.MethodSig;
			    if (methodSig != null) {
			        var tsOwner = memberRef.Class as TypeSpec;
			        var gis = tsOwner?.TypeSig as GenericInstSig;
			        var typeGenArgs = gis?.GenericArguments;
			        return FullNameCreator.MethodFullName(memberRef.GetDeclaringTypeFullName(), memberRef.Name, methodSig, typeGenArgs, methodGenArgs);
			    }

			    return string.Empty;
			}
		}

		bool IIsTypeOrMethod.IsType => false;

	    bool IIsTypeOrMethod.IsMethod => true;

	    bool IMemberRef.IsField => false;

	    bool IMemberRef.IsTypeSpec => false;

	    bool IMemberRef.IsTypeRef => false;

	    bool IMemberRef.IsTypeDef => false;

	    bool IMemberRef.IsMethodSpec => true;

	    bool IMemberRef.IsMethodDef => false;

	    bool IMemberRef.IsMemberRef => false;

	    bool IMemberRef.IsFieldDef => false;

	    bool IMemberRef.IsPropertyDef => false;

	    bool IMemberRef.IsEventDef => false;

	    bool IMemberRef.IsGenericParam => false;

	    bool IContainsGenericParameter.ContainsGenericParameter => TypeHelper.ContainsGenericParameter(this);

	    /// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A MethodSpec row created by the user and not present in the original .NET file
	/// </summary>
	public class MethodSpecUser : MethodSpec {
		/// <summary>
		/// Default constructor
		/// </summary>
		public MethodSpecUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">The generic method</param>
		public MethodSpecUser(IMethodDefOrRef method)
			: this(method, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">The generic method</param>
		/// <param name="sig">The instantiated method sig</param>
		public MethodSpecUser(IMethodDefOrRef method, GenericInstMethodSig sig) {
			this.Method = method;
			this.Instantiation = sig;
		}
	}

	/// <summary>
	/// Created from a row in the MethodSpec table
	/// </summary>
	sealed class MethodSpecMD : MethodSpec, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

	    /// <inheritdoc/>
		public uint OrigRid { get; }

	    /// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.MethodSpec, OrigRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>MethodSpec</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public MethodSpecMD(ModuleDefMD readerModule, uint rid, GenericParamContext gpContext) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException(nameof(readerModule));
			if (readerModule.TablesStream.MethodSpecTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"MethodSpec rid {rid} does not exist");
#endif
			this.OrigRid = rid;
			this.Rid = rid;
			this.readerModule = readerModule;
			uint instantiation = readerModule.TablesStream.ReadMethodSpecRow(OrigRid, out uint method);
			this.Method = readerModule.ResolveMethodDefOrRef(method, gpContext);
			this.Instantiation = readerModule.ReadSignature(instantiation, gpContext);
		}
	}
}
