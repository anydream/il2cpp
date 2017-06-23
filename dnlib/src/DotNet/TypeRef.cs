// dnlib: See LICENSE.txt for more info

using System;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the TypeRef table
	/// </summary>
	public abstract class TypeRef : ITypeDefOrRef, IHasCustomAttribute, IMemberRefParent, IResolutionScope {
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.TypeRef, Rid);

	    /// <inheritdoc/>
		public uint Rid { get; set; }

	    /// <inheritdoc/>
		public int TypeDefOrRefTag => 1;

	    /// <inheritdoc/>
		public int HasCustomAttributeTag => 2;

	    /// <inheritdoc/>
		public int MemberRefParentTag => 1;

	    /// <inheritdoc/>
		public int ResolutionScopeTag => 3;

	    /// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters => 0;

	    /// <inheritdoc/>
		string IType.TypeName => FullNameCreator.Name(this, false, null);

	    /// <inheritdoc/>
		public string ReflectionName => FullNameCreator.Name(this, true, null);

	    /// <inheritdoc/>
		string IType.Namespace => FullNameCreator.Namespace(this, false, null);

	    /// <inheritdoc/>
		public string ReflectionNamespace => FullNameCreator.Namespace(this, true, null);

	    /// <inheritdoc/>
		public string FullName => FullNameCreator.FullName(this, false, null, null);

	    /// <inheritdoc/>
		public string ReflectionFullName => FullNameCreator.FullName(this, true, null, null);

	    /// <inheritdoc/>
		public string AssemblyQualifiedName => FullNameCreator.AssemblyQualifiedName(this, null, null);

	    /// <inheritdoc/>
		public IAssembly DefinitionAssembly => FullNameCreator.DefinitionAssembly(this);

	    /// <inheritdoc/>
		public IScope Scope => FullNameCreator.Scope(this);

	    /// <inheritdoc/>
		public ITypeDefOrRef ScopeType => this;

	    /// <summary>
		/// Always returns <c>false</c> since a <see cref="TypeRef"/> does not contain any
		/// <see cref="GenericVar"/> or <see cref="GenericMVar"/>.
		/// </summary>
		public bool ContainsGenericParameter => false;

	    /// <inheritdoc/>
		public ModuleDef Module { get; protected set; }

	    /// <summary>
		/// From column TypeRef.ResolutionScope
		/// </summary>
		public IResolutionScope ResolutionScope {
			get {
				if (!resolutionScope_isInitialized)
					InitializeResolutionScope();
				return resolutionScope;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				resolutionScope = value;
				resolutionScope_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected IResolutionScope resolutionScope;
		/// <summary/>
		protected bool resolutionScope_isInitialized;

		void InitializeResolutionScope() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (resolutionScope_isInitialized)
				return;
			resolutionScope = GetResolutionScope_NoLock();
			resolutionScope_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="resolutionScope"/></summary>
		protected virtual IResolutionScope GetResolutionScope_NoLock() {
			return null;
		}

		/// <summary>
		/// From column TypeRef.Name
		/// </summary>
		public UTF8String Name { get; set; }

	    /// <summary>
		/// From column TypeRef.Namespace
		/// </summary>
		public UTF8String Namespace { get; set; }

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
		/// <c>true</c> if it's nested within another <see cref="TypeRef"/>
		/// </summary>
		public bool IsNested => DeclaringType != null;

	    /// <inheritdoc/>
		public bool IsValueType {
			get {
				var td = Resolve();
				return td != null && td.IsValueType;
			}
		}

		/// <inheritdoc/>
		public bool IsPrimitive => this.IsPrimitive();

	    /// <summary>
		/// Gets the declaring type, if any
		/// </summary>
		public TypeRef DeclaringType => ResolutionScope as TypeRef;

	    /// <inheritdoc/>
		ITypeDefOrRef IMemberRef.DeclaringType => DeclaringType;

	    bool IIsTypeOrMethod.IsType => true;

	    bool IIsTypeOrMethod.IsMethod => false;

	    bool IMemberRef.IsField => false;

	    bool IMemberRef.IsTypeSpec => false;

	    bool IMemberRef.IsTypeRef => true;

	    bool IMemberRef.IsTypeDef => false;

	    bool IMemberRef.IsMethodSpec => false;

	    bool IMemberRef.IsMethodDef => false;

	    bool IMemberRef.IsMemberRef => false;

	    bool IMemberRef.IsFieldDef => false;

	    bool IMemberRef.IsPropertyDef => false;

	    bool IMemberRef.IsEventDef => false;

	    bool IMemberRef.IsGenericParam => false;

	    /// <summary>
		/// Resolves the type
		/// </summary>
		/// <returns>A <see cref="TypeDef"/> instance or <c>null</c> if it couldn't be resolved</returns>
		public TypeDef Resolve() {
			return Resolve(null);
		}

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <param name="sourceModule">The module that needs to resolve the type or <c>null</c></param>
		/// <returns>A <see cref="TypeDef"/> instance or <c>null</c> if it couldn't be resolved</returns>
		public TypeDef Resolve(ModuleDef sourceModule) {
		    return Module?.Context.Resolver.Resolve(this, sourceModule ?? Module);
		}

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <returns>A <see cref="TypeDef"/> instance</returns>
		/// <exception cref="TypeResolveException">If the type couldn't be resolved</exception>
		public TypeDef ResolveThrow() {
			return ResolveThrow(null);
		}

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <param name="sourceModule">The module that needs to resolve the type or <c>null</c></param>
		/// <returns>A <see cref="TypeDef"/> instance</returns>
		/// <exception cref="TypeResolveException">If the type couldn't be resolved</exception>
		public TypeDef ResolveThrow(ModuleDef sourceModule) {
			var type = Resolve(sourceModule);
			if (type != null)
				return type;
			throw new TypeResolveException($"Could not resolve type: {this} ({DefinitionAssembly})");
		}

		/// <summary>
		/// Gets the top-most (non-nested) <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">Input</param>
		/// <returns>The non-nested <see cref="TypeRef"/> or <c>null</c></returns>
		internal static TypeRef GetNonNestedTypeRef(TypeRef typeRef) {
			if (typeRef == null)
				return null;
			for (int i = 0; i < 1000; i++) {
				var next = typeRef.ResolutionScope as TypeRef;
				if (next == null)
					return typeRef;
				typeRef = next;
			}
			return null;	// Here if eg. the TypeRef has an infinite loop
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A TypeRef row created by the user and not present in the original .NET file
	/// </summary>
	public class TypeRefUser : TypeRef {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Type name</param>
		public TypeRefUser(ModuleDef module, UTF8String name)
			: this(module, UTF8String.Empty, name) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="namespace">Type namespace</param>
		/// <param name="name">Type name</param>
		public TypeRefUser(ModuleDef module, UTF8String @namespace, UTF8String name)
			: this(module, @namespace, name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="namespace">Type namespace</param>
		/// <param name="name">Type name</param>
		/// <param name="resolutionScope">Resolution scope (a <see cref="ModuleDef"/>,
		/// <see cref="ModuleRef"/>, <see cref="AssemblyRef"/> or <see cref="TypeRef"/>)</param>
		public TypeRefUser(ModuleDef module, UTF8String @namespace, UTF8String name, IResolutionScope resolutionScope) {
			this.Module = module;
			this.resolutionScope = resolutionScope;
			this.resolutionScope_isInitialized = true;
			this.Name = name;
			this.Namespace = @namespace;
		}
	}

	/// <summary>
	/// Created from a row in the TypeRef table
	/// </summary>
	sealed class TypeRefMD : TypeRef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

	    readonly uint resolutionScopeCodedToken;

		/// <inheritdoc/>
		public uint OrigRid { get; }

	    /// <inheritdoc/>
		protected override IResolutionScope GetResolutionScope_NoLock() {
			return readerModule.ResolveResolutionScope(resolutionScopeCodedToken);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.TypeRef, OrigRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>TypeRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public TypeRefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException(nameof(readerModule));
			if (readerModule.TablesStream.TypeRefTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"TypeRef rid {rid} does not exist");
#endif
			this.OrigRid = rid;
			this.Rid = rid;
			this.readerModule = readerModule;
			this.Module = readerModule;
            uint @namespace = readerModule.TablesStream.ReadTypeRefRow(OrigRid, out uint resolutionScope, out uint name);
            this.Name = readerModule.StringsStream.ReadNoNull(name);
			this.Namespace = readerModule.StringsStream.ReadNoNull(@namespace);
			this.resolutionScopeCodedToken = resolutionScope;
		}
	}
}
