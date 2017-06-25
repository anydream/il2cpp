// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the ImplMap table
	/// </summary>
	[DebuggerDisplay("{Module} {Name}")]
	public abstract class ImplMap : IMDTokenProvider {
	    /// <inheritdoc/>
		public MDToken MDToken => new MDToken(Table.ImplMap, Rid);

	    /// <inheritdoc/>
		public uint Rid { get; set; }

	    /// <summary>
		/// From column ImplMap.MappingFlags
		/// </summary>
		public PInvokeAttributes Attributes {
			get => (PInvokeAttributes)attributes;
	        set => attributes = (int)value;
	    }
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column ImplMap.ImportName
		/// </summary>
		public UTF8String Name { get; set; }

	    /// <summary>
		/// From column ImplMap.ImportScope
		/// </summary>
		public ModuleRef Module { get; set; }

	    /// <summary>
		/// Modify <see cref="attributes"/> property: <see cref="attributes"/> =
		/// (<see cref="attributes"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(PInvokeAttributes andMask, PInvokeAttributes orMask) {
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
		/// Set or clear flags in <see cref="attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, PInvokeAttributes flags) {
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
		/// Gets/sets the <see cref="PInvokeAttributes.NoMangle"/> bit
		/// </summary>
		public bool IsNoMangle {
			get => ((PInvokeAttributes)attributes & PInvokeAttributes.NoMangle) != 0;
		    set => ModifyAttributes(value, PInvokeAttributes.NoMangle);
		}

		/// <summary>
		/// Gets/sets the char set
		/// </summary>
		public PInvokeAttributes CharSet {
			get => (PInvokeAttributes)attributes & PInvokeAttributes.CharSetMask;
		    set => ModifyAttributes(~PInvokeAttributes.CharSetMask, value & PInvokeAttributes.CharSetMask);
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CharSetNotSpec"/> is set
		/// </summary>
		public bool IsCharSetNotSpec => ((PInvokeAttributes)attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetNotSpec;

	    /// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CharSetAnsi"/> is set
		/// </summary>
		public bool IsCharSetAnsi => ((PInvokeAttributes)attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetAnsi;

	    /// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CharSetUnicode"/> is set
		/// </summary>
		public bool IsCharSetUnicode => ((PInvokeAttributes)attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetUnicode;

	    /// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CharSetAuto"/> is set
		/// </summary>
		public bool IsCharSetAuto => ((PInvokeAttributes)attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetAuto;

	    /// <summary>
		/// Gets/sets best fit
		/// </summary>
		public PInvokeAttributes BestFit {
			get => (PInvokeAttributes)attributes & PInvokeAttributes.BestFitMask;
	        set => ModifyAttributes(~PInvokeAttributes.BestFitMask, value & PInvokeAttributes.BestFitMask);
	    }

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.BestFitUseAssem"/> is set
		/// </summary>
		public bool IsBestFitUseAssem => ((PInvokeAttributes)attributes & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitUseAssem;

	    /// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.BestFitEnabled"/> is set
		/// </summary>
		public bool IsBestFitEnabled => ((PInvokeAttributes)attributes & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitEnabled;

	    /// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.BestFitDisabled"/> is set
		/// </summary>
		public bool IsBestFitDisabled => ((PInvokeAttributes)attributes & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitDisabled;

	    /// <summary>
		/// Gets/sets throw on unmappable char
		/// </summary>
		public PInvokeAttributes ThrowOnUnmappableChar {
			get => (PInvokeAttributes)attributes & PInvokeAttributes.ThrowOnUnmappableCharMask;
	        set => ModifyAttributes(~PInvokeAttributes.ThrowOnUnmappableCharMask, value & PInvokeAttributes.ThrowOnUnmappableCharMask);
	    }

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.ThrowOnUnmappableCharUseAssem"/> is set
		/// </summary>
		public bool IsThrowOnUnmappableCharUseAssem => ((PInvokeAttributes)attributes & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharUseAssem;

	    /// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.ThrowOnUnmappableCharEnabled"/> is set
		/// </summary>
		public bool IsThrowOnUnmappableCharEnabled => ((PInvokeAttributes)attributes & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharEnabled;

	    /// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.ThrowOnUnmappableCharDisabled"/> is set
		/// </summary>
		public bool IsThrowOnUnmappableCharDisabled => ((PInvokeAttributes)attributes & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharDisabled;

	    /// <summary>
		/// Gets/sets the <see cref="PInvokeAttributes.SupportsLastError"/> bit
		/// </summary>
		public bool SupportsLastError {
			get => ((PInvokeAttributes)attributes & PInvokeAttributes.SupportsLastError) != 0;
	        set => ModifyAttributes(value, PInvokeAttributes.SupportsLastError);
	    }

		/// <summary>
		/// Gets/sets calling convention
		/// </summary>
		public PInvokeAttributes CallConv {
			get => (PInvokeAttributes)attributes & PInvokeAttributes.CallConvMask;
		    set => ModifyAttributes(~PInvokeAttributes.CallConvMask, value & PInvokeAttributes.CallConvMask);
		}

		/// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvWinapi"/> is set
		/// </summary>
		public bool IsCallConvWinapi => ((PInvokeAttributes)attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvWinapi;

	    /// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvCdecl"/> is set
		/// </summary>
		public bool IsCallConvCdecl => ((PInvokeAttributes)attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvCdecl;

	    /// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvStdcall"/> is set
		/// </summary>
		public bool IsCallConvStdcall => ((PInvokeAttributes)attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvStdcall;

	    /// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvThiscall"/> is set
		/// </summary>
		public bool IsCallConvThiscall => ((PInvokeAttributes)attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvThiscall;

	    /// <summary>
		/// <c>true</c> if <see cref="PInvokeAttributes.CallConvFastcall"/> is set
		/// </summary>
		public bool IsCallConvFastcall => ((PInvokeAttributes)attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvFastcall;

	    /// <summary>
		/// Checks whether this <see cref="ImplMap"/> is a certain P/Invoke method
		/// </summary>
		/// <param name="dllName">Name of the DLL</param>
		/// <param name="funcName">Name of the function within the DLL</param>
		/// <returns><c>true</c> if it's the specified P/Invoke method, else <c>false</c></returns>
		public bool IsPinvokeMethod(string dllName, string funcName) {
			if (Name != funcName)
				return false;
			var mod = Module;
			if (mod == null)
				return false;
			return GetDllName(dllName).Equals(GetDllName(mod.Name), StringComparison.OrdinalIgnoreCase);
		}

		static string GetDllName(string dllName) {
			if (dllName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				return dllName.Substring(0, dllName.Length - 4);
			return dllName;
		}
	}

	/// <summary>
	/// An ImplMap row created by the user and not present in the original .NET file
	/// </summary>
	public class ImplMapUser : ImplMap {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ImplMapUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="scope">Scope</param>
		/// <param name="name">Name</param>
		/// <param name="flags">Flags</param>
		public ImplMapUser(ModuleRef scope, UTF8String name, PInvokeAttributes flags) {
			this.Module = scope;
			this.Name = name;
			this.attributes = (int)flags;
		}
	}

	/// <summary>
	/// Created from a row in the ImplMap table
	/// </summary>
	sealed class ImplMapMD : ImplMap, IMDTokenProviderMD {
	    /// <inheritdoc/>
		public uint OrigRid { get; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ImplMap</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ImplMapMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException(nameof(readerModule));
			if (readerModule.TablesStream.ImplMapTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"ImplMap rid {rid} does not exist");
#endif
			this.OrigRid = rid;
			this.Rid = rid;
			uint scope = readerModule.TablesStream.ReadImplMapRow(OrigRid, out this.attributes, out uint name);
			this.Name = readerModule.StringsStream.ReadNoNull(name);
			this.Module = readerModule.ResolveModuleRef(scope);
		}
	}
}
