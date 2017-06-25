// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.PE;
using dnlib.IO;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// All native vtables
	/// </summary>
	[DebuggerDisplay("RVA = {RVA}, Count = {VTables.Count}")]
	public sealed class VTableFixups : IEnumerable<VTable> {
	    /// <summary>
		/// Gets/sets the RVA of the vtable fixups
		/// </summary>
		public RVA RVA { get; set; }

	    /// <summary>
		/// Gets all <see cref="VTable"/>s
		/// </summary>
		public ThreadSafe.IList<VTable> VTables { get; private set; }

	    /// <summary>
		/// Default constructor
		/// </summary>
		public VTableFixups() {
			this.VTables = ThreadSafeListCreator.Create<VTable>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		public VTableFixups(ModuleDefMD module) {
			Initialize(module);
		}

		void Initialize(ModuleDefMD module) {
			var info = module.MetaData.ImageCor20Header.VTableFixups;
			if (info.VirtualAddress == 0 || info.Size == 0) {
				this.VTables = ThreadSafeListCreator.Create<VTable>();
				return;
			}
			this.RVA = info.VirtualAddress;
			this.VTables = ThreadSafeListCreator.Create<VTable>((int)info.Size / 8);

			var peImage = module.MetaData.PEImage;
			using (var reader = peImage.CreateFullStream()) {
				reader.Position = (long)peImage.ToFileOffset(info.VirtualAddress);
				long endPos = reader.Position + info.Size;
				while (reader.Position + 8 <= endPos && reader.CanRead(8)) {
					RVA tableRva = (RVA)reader.ReadUInt32();
					int numSlots = reader.ReadUInt16();
					var flags = (VTableFlags)reader.ReadUInt16();
					var vtable = new VTable(tableRva, flags, numSlots);
					VTables.Add(vtable);

					var pos = reader.Position;
					reader.Position = (long)peImage.ToFileOffset(tableRva);
					int slotSize = vtable.Is64Bit ? 8 : 4;
					while (numSlots-- > 0 && reader.CanRead(slotSize)) {
						vtable.Methods.Add(module.ResolveToken(reader.ReadUInt32()) as IMethod);
						if (slotSize == 8)
							reader.ReadUInt32();
					}
					reader.Position = pos;
				}
			}
		}

		/// <inheritdoc/>
		public IEnumerator<VTable> GetEnumerator() {
			return VTables.GetEnumerator();
		}

		/// <inheritdoc/>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}

	/// <summary>
	/// See COR_VTABLE_XXX in CorHdr.h
	/// </summary>
	[Flags]
	public enum VTableFlags : ushort {
		/// <summary>
		/// 32-bit vtable slots
		/// </summary>
		_32Bit				= 0x01,

		/// <summary>
		/// 64-bit vtable slots
		/// </summary>
		_64Bit				= 0x02,

		/// <summary>
		/// Transition from unmanaged code
		/// </summary>
		FromUnmanaged		= 0x04,

		/// <summary>
		/// Also retain app domain
		/// </summary>
		FromUnmanagedRetainAppDomain = 0x08,

		/// <summary>
		/// Call most derived method
		/// </summary>
		CallMostDerived		= 0x10,
	}

	/// <summary>
	/// One VTable accessed by native code
	/// </summary>
	public sealed class VTable : IEnumerable<IMethod> {
	    /// <summary>
		/// Gets/sets the <see cref="RVA"/> of this vtable
		/// </summary>
		public RVA RVA { get; set; }

	    /// <summary>
		/// Gets/sets the flags
		/// </summary>
		public VTableFlags Flags { get; set; }

	    /// <summary>
		/// <c>true</c> if each vtable slot is 32 bits in size
		/// </summary>
		public bool Is32Bit => (Flags & VTableFlags._32Bit) != 0;

	    /// <summary>
		/// <c>true</c> if each vtable slot is 64 bits in size
		/// </summary>
		public bool Is64Bit => (Flags & VTableFlags._64Bit) != 0;

	    /// <summary>
		/// Gets the vtable methods
		/// </summary>
		public ThreadSafe.IList<IMethod> Methods { get; }

	    /// <summary>
		/// Default constructor
		/// </summary>
		public VTable() {
			this.Methods = ThreadSafeListCreator.Create<IMethod>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		public VTable(VTableFlags flags) {
			this.Flags = flags;
			this.Methods = ThreadSafeListCreator.Create<IMethod>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rva">RVA of this vtable</param>
		/// <param name="flags">Flgas</param>
		/// <param name="numSlots">Number of methods in vtable</param>
		public VTable(RVA rva, VTableFlags flags, int numSlots) {
			this.RVA = rva;
			this.Flags = flags;
			this.Methods = ThreadSafeListCreator.Create<IMethod>(numSlots);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rva">RVA of this vtable</param>
		/// <param name="flags">Flgas</param>
		/// <param name="methods">Vtable methods</param>
		public VTable(RVA rva, VTableFlags flags, IEnumerable<IMethod> methods) {
			this.RVA = rva;
			this.Flags = flags;
			this.Methods = ThreadSafeListCreator.Create<IMethod>(methods);
		}

		/// <inheritdoc/>
		public IEnumerator<IMethod> GetEnumerator() {
			return Methods.GetEnumerator();
		}

		/// <inheritdoc/>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		/// <inheritdoc/>
		public override string ToString()
		{
		    return Methods.Count == 0 ? $"{Methods.Count} {(uint) RVA:X8}" : $"{Methods.Count} {(uint) RVA:X8} {Methods.Get(0, null)}";
		}
	}
}
