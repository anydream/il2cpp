// dnlib: See LICENSE.txt for more info

using System;
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
	/// A high-level representation of a row in the Event table
	/// </summary>
	public abstract class EventDef : IHasCustomAttribute, IHasSemantic, IFullName, IMemberDef {
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.Event, Rid);

	    /// <inheritdoc/>
		public uint Rid { get; set; }

	    /// <inheritdoc/>
		public int HasCustomAttributeTag => 10;

	    /// <inheritdoc/>
		public int HasSemanticTag => 0;

	    /// <summary>
		/// From column Event.EventFlags
		/// </summary>
		public EventAttributes Attributes {
			get => (EventAttributes)attributes;
	        set => attributes = (int)value;
	    }
		/// <summary/>
		protected int attributes;

		/// <summary>
		/// From column Event.Name
		/// </summary>
		public UTF8String Name { get; set; }

	    /// <summary>
		/// From column Event.EventType
		/// </summary>
		public ITypeDefOrRef EventType {
			get => eventType;
	        set => eventType = value;
	    }
		/// <summary/>
		protected ITypeDefOrRef eventType;

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

		/// <summary>
		/// Gets/sets the adder method
		/// </summary>
		public MethodDef AddMethod {
			get {
				if (otherMethods == null)
					InitializeEventMethods();
				return addMethod;
			}
			set {
				if (otherMethods == null)
					InitializeEventMethods();
				addMethod = value;
			}
		}

		/// <summary>
		/// Gets/sets the invoker method
		/// </summary>
		public MethodDef InvokeMethod {
			get {
				if (otherMethods == null)
					InitializeEventMethods();
				return invokeMethod;
			}
			set {
				if (otherMethods == null)
					InitializeEventMethods();
				invokeMethod = value;
			}
		}

		/// <summary>
		/// Gets/sets the remover method
		/// </summary>
		public MethodDef RemoveMethod {
			get {
				if (otherMethods == null)
					InitializeEventMethods();
				return removeMethod;
			}
			set {
				if (otherMethods == null)
					InitializeEventMethods();
				removeMethod = value;
			}
		}

		/// <summary>
		/// Gets the other methods
		/// </summary>
		public ThreadSafe.IList<MethodDef> OtherMethods {
			get {
				if (otherMethods == null)
					InitializeEventMethods();
				return otherMethods;
			}
		}

		void InitializeEventMethods() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (otherMethods == null)
				InitializeEventMethods_NoLock();
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Initializes <see cref="otherMethods"/>, <see cref="addMethod"/>,
		/// <see cref="invokeMethod"/> and <see cref="removeMethod"/>.
		/// </summary>
		protected virtual void InitializeEventMethods_NoLock() {
			otherMethods = ThreadSafeListCreator.Create<MethodDef>();
		}

		/// <summary/>
		protected MethodDef addMethod;
		/// <summary/>
		protected MethodDef invokeMethod;
		/// <summary/>
		protected MethodDef removeMethod;
		/// <summary/>
		protected ThreadSafe.IList<MethodDef> otherMethods;

		/// <summary>Reset <see cref="AddMethod"/>, <see cref="InvokeMethod"/>, <see cref="RemoveMethod"/>, <see cref="OtherMethods"/></summary>
		protected void ResetMethods() {
			otherMethods = null;
		}

		/// <summary>
		/// <c>true</c> if there are no methods attached to this event
		/// </summary>
		public bool IsEmpty => AddMethod == null &&
		                       removeMethod == null &&
		                       invokeMethod == null &&
		                       otherMethods.Count == 0;

	    /// <inheritdoc/>
		public bool HasCustomAttributes => CustomAttributes.Count > 0;

	    /// <summary>
		/// <c>true</c> if <see cref="OtherMethods"/> is not empty
		/// </summary>
		public bool HasOtherMethods => OtherMethods.Count > 0;

	    /// <summary>
		/// Gets/sets the declaring type (owner type)
		/// </summary>
		public TypeDef DeclaringType {
			get => DeclaringType2;
	        set {
				var currentDeclaringType = DeclaringType2;
				if (currentDeclaringType == value)
					return;
	            currentDeclaringType?.Events.Remove(this);	// Will set DeclaringType2 = null
	            value?.Events.Add(this);	// Will set DeclaringType2 = value
	        }
		}

		/// <inheritdoc/>
		ITypeDefOrRef IMemberRef.DeclaringType => DeclaringType2;

	    /// <summary>
		/// Called by <see cref="DeclaringType"/> and should normally not be called by any user
		/// code. Use <see cref="DeclaringType"/> instead. Only call this if you must set the
		/// declaring type without inserting it in the declaring type's method list.
		/// </summary>
		public TypeDef DeclaringType2 { get; set; }

	    /// <inheritdoc/>
		public ModuleDef Module {
			get {
				var dt = DeclaringType2;
				return dt?.Module;
			}
		}

		/// <summary>
		/// Gets the full name of the event
		/// </summary>
		public string FullName {
			get {
				var dt = DeclaringType2;
				return FullNameCreator.EventFullName(dt?.FullName, Name, eventType);
			}
		}

		bool IIsTypeOrMethod.IsType => false;

	    bool IIsTypeOrMethod.IsMethod => false;

	    bool IMemberRef.IsField => false;

	    bool IMemberRef.IsTypeSpec => false;

	    bool IMemberRef.IsTypeRef => false;

	    bool IMemberRef.IsTypeDef => false;

	    bool IMemberRef.IsMethodSpec => false;

	    bool IMemberRef.IsMethodDef => false;

	    bool IMemberRef.IsMemberRef => false;

	    bool IMemberRef.IsFieldDef => false;

	    bool IMemberRef.IsPropertyDef => false;

	    bool IMemberRef.IsEventDef => true;

	    bool IMemberRef.IsGenericParam => false;

	    /// <summary>
		/// Set or clear flags in <see cref="attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, EventAttributes flags) {
#if THREAD_SAFE
			int origVal, newVal;
			do {
				origVal = attributes;
				if (set)
					newVal = origVal | (int)flags;
				else
					newVal = origVal & ~(int)flags;
			} while (Interlocked.CompareExchange(ref attributes, newVal, origVal) != origVal);
#else
			if (set)
				attributes |= (int)flags;
			else
				attributes &= ~(int)flags;
#endif
		}

		/// <summary>
		/// Gets/sets the <see cref="EventAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get => ((EventAttributes)attributes & EventAttributes.SpecialName) != 0;
		    set => ModifyAttributes(value, EventAttributes.SpecialName);
		}

		/// <summary>
		/// Gets/sets the <see cref="EventAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRuntimeSpecialName {
			get => ((EventAttributes)attributes & EventAttributes.RTSpecialName) != 0;
		    set => ModifyAttributes(value, EventAttributes.RTSpecialName);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// An Event row created by the user and not present in the original .NET file
	/// </summary>
	public class EventDefUser : EventDef {
		/// <summary>
		/// Default constructor
		/// </summary>
		public EventDefUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public EventDefUser(UTF8String name)
			: this(name, null, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="type">Type</param>
		public EventDefUser(UTF8String name, ITypeDefOrRef type)
			: this(name, type, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="type">Type</param>
		/// <param name="flags">Flags</param>
		public EventDefUser(UTF8String name, ITypeDefOrRef type, EventAttributes flags) {
			this.Name = name;
			this.eventType = type;
			this.attributes = (int)flags;
		}
	}

	/// <summary>
	/// Created from a row in the Event table
	/// </summary>
	sealed class EventDefMD : EventDef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

	    /// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.Event, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Event</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public EventDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException(nameof(readerModule));
			if (readerModule.TablesStream.EventTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"Event rid {rid} does not exist");
#endif
			this.origRid = rid;
			this.Rid = rid;
			this.readerModule = readerModule;
			uint eventType = readerModule.TablesStream.ReadEventRow(origRid, out this.attributes, out uint name);
			this.Name = readerModule.StringsStream.ReadNoNull(name);
			this.DeclaringType2 = readerModule.GetOwnerType(this);
			this.eventType = readerModule.ResolveTypeDefOrRef(eventType, new GenericParamContext(DeclaringType2));
		}

		internal EventDefMD InitializeAll() {
			MemberMDInitializer.Initialize(Attributes);
			MemberMDInitializer.Initialize(Name);
			MemberMDInitializer.Initialize(EventType);
			MemberMDInitializer.Initialize(CustomAttributes);
			MemberMDInitializer.Initialize(AddMethod);
			MemberMDInitializer.Initialize(InvokeMethod);
			MemberMDInitializer.Initialize(RemoveMethod);
			MemberMDInitializer.Initialize(OtherMethods);
			MemberMDInitializer.Initialize(DeclaringType);
			return this;
		}

		/// <inheritdoc/>
		protected override void InitializeEventMethods_NoLock() {
			ThreadSafe.IList<MethodDef> newOtherMethods;
			var dt = DeclaringType2 as TypeDefMD;
			if (dt == null)
				newOtherMethods = ThreadSafeListCreator.Create<MethodDef>();
			else
				dt.InitializeEvent(this, out addMethod, out invokeMethod, out removeMethod, out newOtherMethods);
			otherMethods = newOtherMethods;
		}
	}
}
