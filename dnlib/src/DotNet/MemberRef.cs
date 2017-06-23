// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Threading;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the MemberRef table
	/// </summary>
	public abstract class MemberRef : IHasCustomAttribute, IMethodDefOrRef, ICustomAttributeType, IField, IContainsGenericParameter {
	    /// <summary>
		/// The owner module
		/// </summary>
		protected ModuleDef module;

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.MemberRef, Rid);

	    /// <inheritdoc/>
		public uint Rid { get; set; }

	    /// <inheritdoc/>
		public int HasCustomAttributeTag => 6;

	    /// <inheritdoc/>
		public int MethodDefOrRefTag => 1;

	    /// <inheritdoc/>
		public int CustomAttributeTypeTag => 3;

	    /// <summary>
		/// From column MemberRef.Class
		/// </summary>
		public IMemberRefParent Class { get; set; }

	    /// <summary>
		/// From column MemberRef.Name
		/// </summary>
		public UTF8String Name { get; set; }

	    /// <summary>
		/// From column MemberRef.Signature
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

	    /// <inheritdoc/>
		public ITypeDefOrRef DeclaringType {
			get {
				var owner = Class;

                if (owner is ITypeDefOrRef tdr)
                    return tdr;

                if (owner is MethodDef method)
                    return method.DeclaringType;

                if (owner is ModuleRef mr)
                {
                    var tr = GetGlobalTypeRef(mr);
                    if (module != null)
                        return module.UpdateRowId(tr);
                    return tr;
                }

                return null;
			}
		}

		TypeRefUser GetGlobalTypeRef(ModuleRef mr) {
			if (module == null)
				return CreateDefaultGlobalTypeRef(mr);
			var globalType = module.GlobalType;
			if (globalType != null && new SigComparer().Equals(module, mr))
				return new TypeRefUser(module, globalType.Namespace, globalType.Name, mr);
			var asm = module.Assembly;
			if (asm == null)
				return CreateDefaultGlobalTypeRef(mr);
			var mod = asm.FindModule(mr.Name);
			if (mod == null)
				return CreateDefaultGlobalTypeRef(mr);
			globalType = mod.GlobalType;
			if (globalType == null)
				return CreateDefaultGlobalTypeRef(mr);
			return new TypeRefUser(module, globalType.Namespace, globalType.Name, mr);
		}

		TypeRefUser CreateDefaultGlobalTypeRef(ModuleRef mr) {
			var tr = new TypeRefUser(module, string.Empty, "<Module>", mr);
		    module?.UpdateRowId(tr);
		    return tr;
		}

		bool IIsTypeOrMethod.IsType => false;

	    bool IIsTypeOrMethod.IsMethod => IsMethodRef;

	    bool IMemberRef.IsField => IsFieldRef;

	    bool IMemberRef.IsTypeSpec => false;

	    bool IMemberRef.IsTypeRef => false;

	    bool IMemberRef.IsTypeDef => false;

	    bool IMemberRef.IsMethodSpec => false;

	    bool IMemberRef.IsMethodDef => false;

	    bool IMemberRef.IsMemberRef => true;

	    bool IMemberRef.IsFieldDef => false;

	    bool IMemberRef.IsPropertyDef => false;

	    bool IMemberRef.IsEventDef => false;

	    bool IMemberRef.IsGenericParam => false;

	    /// <summary>
		/// <c>true</c> if this is a method reference (<see cref="MethodSig"/> != <c>null</c>)
		/// </summary>
		public bool IsMethodRef => MethodSig != null;

	    /// <summary>
		/// <c>true</c> if this is a field reference (<see cref="FieldSig"/> != <c>null</c>)
		/// </summary>
		public bool IsFieldRef => FieldSig != null;

	    /// <summary>
		/// Gets/sets the method sig
		/// </summary>
		public MethodSig MethodSig {
			get => Signature as MethodSig;
	        set => Signature = value;
	    }

		/// <summary>
		/// Gets/sets the field sig
		/// </summary>
		public FieldSig FieldSig {
			get => Signature as FieldSig;
		    set => Signature = value;
		}

		/// <inheritdoc/>
		public ModuleDef Module => module;

	    /// <summary>
		/// <c>true</c> if the method has a hidden 'this' parameter
		/// </summary>
		public bool HasThis {
			get {
				var ms = MethodSig;
				return ms?.HasThis ?? false;
			}
		}

		/// <summary>
		/// <c>true</c> if the method has an explicit 'this' parameter
		/// </summary>
		public bool ExplicitThis {
			get {
				var ms = MethodSig;
				return ms?.ExplicitThis ?? false;
			}
		}

		/// <summary>
		/// Gets the calling convention
		/// </summary>
		public CallingConvention CallingConvention {
			get {
				var ms = MethodSig;
				return ms?.CallingConvention & CallingConvention.Mask ?? 0;
			}
		}

		/// <summary>
		/// Gets/sets the method return type
		/// </summary>
		public TypeSig ReturnType {
			get {
				var ms = MethodSig;
				return ms?.RetType;
			}
			set {
				var ms = MethodSig;
				if (ms != null)
					ms.RetType = value;
			}
		}

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get {
				var sig = MethodSig;
				return sig == null ? 0 : (int)sig.GenParamCount;
			}
		}

		/// <summary>
		/// Gets the full name
		/// </summary>
		public string FullName {
			get {
				var parent = Class;
				IList<TypeSig> typeGenArgs = null;
				if (parent is TypeSpec typeSpec) {
                    if (typeSpec.TypeSig is GenericInstSig sig)
                        typeGenArgs = sig.GenericArguments;
                }
				var methodSig = MethodSig;
				if (methodSig != null)
					return FullNameCreator.MethodFullName(GetDeclaringTypeFullName(parent), Name, methodSig, typeGenArgs, null, null, null);
				var fieldSig = FieldSig;
				if (fieldSig != null)
					return FullNameCreator.FieldFullName(GetDeclaringTypeFullName(parent), Name, fieldSig, typeGenArgs, null);
				return string.Empty;
			}
		}

		/// <summary>
		/// Get the declaring type's full name
		/// </summary>
		/// <returns>Full name or <c>null</c> if there's no declaring type</returns>
		public string GetDeclaringTypeFullName() {
			return GetDeclaringTypeFullName(Class);
		}

		string GetDeclaringTypeFullName(IMemberRefParent parent) {
			if (parent == null)
				return null;
			if (parent is ITypeDefOrRef typeDefOrRef)
				return typeDefOrRef.FullName;
			if (parent is ModuleRef moduleRef)
				return $"[module:{moduleRef}]<Module>";
			if (parent is MethodDef methodDef) {
				var declaringType = methodDef.DeclaringType;
				return declaringType?.FullName;
			}
			return null;	// Should never be reached
		}

		/// <summary>
		/// Resolves the method/field
		/// </summary>
		/// <returns>A <see cref="MethodDef"/> or a <see cref="FieldDef"/> instance or <c>null</c>
		/// if it couldn't be resolved.</returns>
		public IMemberForwarded Resolve() {
		    return module?.Context.Resolver.Resolve(this);
		}

		/// <summary>
		/// Resolves the method/field
		/// </summary>
		/// <returns>A <see cref="MethodDef"/> or a <see cref="FieldDef"/> instance</returns>
		/// <exception cref="MemberRefResolveException">If the method/field couldn't be resolved</exception>
		public IMemberForwarded ResolveThrow() {
			var memberDef = Resolve();
			if (memberDef != null)
				return memberDef;
			throw new MemberRefResolveException($"Could not resolve method/field: {this} ({this.GetDefinitionAssembly()})");
		}

		/// <summary>
		/// Resolves the field
		/// </summary>
		/// <returns>A <see cref="FieldDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		public FieldDef ResolveField() {
			return Resolve() as FieldDef;
		}

		/// <summary>
		/// Resolves the field
		/// </summary>
		/// <returns>A <see cref="FieldDef"/> instance</returns>
		/// <exception cref="MemberRefResolveException">If the field couldn't be resolved</exception>
		public FieldDef ResolveFieldThrow() {
			var field = ResolveField();
			if (field != null)
				return field;
			throw new MemberRefResolveException($"Could not resolve field: {this} ({this.GetDefinitionAssembly()})");
		}

		/// <summary>
		/// Resolves the method
		/// </summary>
		/// <returns>A <see cref="MethodDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		public MethodDef ResolveMethod() {
			return Resolve() as MethodDef;
		}

		/// <summary>
		/// Resolves the method
		/// </summary>
		/// <returns>A <see cref="MethodDef"/> instance</returns>
		/// <exception cref="MemberRefResolveException">If the method couldn't be resolved</exception>
		public MethodDef ResolveMethodThrow() {
			var method = ResolveMethod();
			if (method != null)
				return method;
			throw new MemberRefResolveException($"Could not resolve method: {this} ({this.GetDefinitionAssembly()})");
		}

		bool IContainsGenericParameter.ContainsGenericParameter => TypeHelper.ContainsGenericParameter(this);

	    /// <summary>
		/// Gets a <see cref="GenericParamContext"/> that can be used as signature context
		/// </summary>
		/// <param name="gpContext">Context passed to the constructor</param>
		/// <param name="class">Field/method class owner</param>
		/// <returns></returns>
		protected static GenericParamContext GetSignatureGenericParamContext(GenericParamContext gpContext, IMemberRefParent @class) {
			TypeDef type = null;
			MethodDef method = gpContext.Method;

			var ts = @class as TypeSpec;
            if (ts?.TypeSig is GenericInstSig gis)
                type = gis.GenericType.ToTypeDefOrRef().ResolveTypeDef();

            return new GenericParamContext(type, method);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A MemberRef row created by the user and not present in the original .NET file
	/// </summary>
	public class MemberRefUser : MemberRef {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		public MemberRefUser(ModuleDef module) {
			this.module = module;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Name of ref</param>
		public MemberRefUser(ModuleDef module, UTF8String name) {
			this.module = module;
			this.Name = name;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Name of field ref</param>
		/// <param name="sig">Field sig</param>
		public MemberRefUser(ModuleDef module, UTF8String name, FieldSig sig)
			: this(module, name, sig, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Name of field ref</param>
		/// <param name="sig">Field sig</param>
		/// <param name="class">Owner of field</param>
		public MemberRefUser(ModuleDef module, UTF8String name, FieldSig sig, IMemberRefParent @class) {
			this.module = module;
			this.Name = name;
			this.Class = @class;
			this.Signature = sig;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Name of method ref</param>
		/// <param name="sig">Method sig</param>
		public MemberRefUser(ModuleDef module, UTF8String name, MethodSig sig)
			: this(module, name, sig, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Name of method ref</param>
		/// <param name="sig">Method sig</param>
		/// <param name="class">Owner of method</param>
		public MemberRefUser(ModuleDef module, UTF8String name, MethodSig sig, IMemberRefParent @class) {
			this.module = module;
			this.Name = name;
			this.Class = @class;
			this.Signature = sig;
		}
	}

	/// <summary>
	/// Created from a row in the MemberRef table
	/// </summary>
	sealed class MemberRefMD : MemberRef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

	    /// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.MemberRef, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>MemberRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public MemberRefMD(ModuleDefMD readerModule, uint rid, GenericParamContext gpContext) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException(nameof(readerModule));
			if (readerModule.TablesStream.MemberRefTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"MemberRef rid {rid} does not exist");
#endif
			this.origRid = rid;
			this.Rid = rid;
			this.readerModule = readerModule;
			this.module = readerModule;
            uint signature = readerModule.TablesStream.ReadMemberRefRow(origRid, out uint @class, out uint name);
            this.Name = readerModule.StringsStream.ReadNoNull(name);
			this.Class = readerModule.ResolveMemberRefParent(@class, gpContext);
			this.Signature = readerModule.ReadSignature(signature, GetSignatureGenericParamContext(gpContext, this.Class));
		}
	}
}
