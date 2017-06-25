// dnlib: See LICENSE.txt for more info

using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolVariable : ISymbolVariable {
		readonly ISymUnmanagedVariable variable;

		public SymbolVariable(ISymUnmanagedVariable variable) {
			this.variable = variable;
		}

		public int AddressField1 {
			get {
				variable.GetAddressField1(out uint result);
				return (int)result;
			}
		}

		public int AddressField2 {
			get {
                variable.GetAddressField2(out uint result);
                return (int)result;
			}
		}

		public int AddressField3 {
			get {
                variable.GetAddressField3(out uint result);
                return (int)result;
			}
		}

		public SymAddressKind AddressKind {
			get {
                variable.GetAddressKind(out uint result);
                return (SymAddressKind)result;
			}
		}

		public object Attributes {
			get {
                variable.GetAttributes(out uint result);
                return (int)result;
			}
		}

		public int EndOffset {
			get {
                variable.GetEndOffset(out uint result);
                return (int)result;
			}
		}

		public string Name {
			get {
                variable.GetName(0, out uint count, null);
                var chars = new char[count];
				variable.GetName((uint)chars.Length, out count, chars);
				return chars.Length == 0 ? string.Empty : new string(chars, 0, chars.Length - 1);
			}
		}

		public int StartOffset {
			get {
                variable.GetStartOffset(out uint result);
                return (int)result;
			}
		}

		public byte[] GetSignature() {
            variable.GetSignature(0, out uint bufSize, null);
            var buffer = new byte[bufSize];
			variable.GetSignature((uint)buffer.Length, out bufSize, buffer);
			return buffer;
		}
	}
}
