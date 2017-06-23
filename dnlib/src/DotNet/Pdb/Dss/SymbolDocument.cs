// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolDocument : ISymbolDocument {
		readonly ISymUnmanagedDocument document;

		public ISymUnmanagedDocument SymUnmanagedDocument => document;

	    public SymbolDocument(ISymUnmanagedDocument document) {
			this.document = document;
		}

		public Guid CheckSumAlgorithmId {
			get {
				document.GetCheckSumAlgorithmId(out Guid guid);
				return guid;
			}
		}

		public Guid DocumentType {
			get {
                document.GetDocumentType(out Guid guid);
                return guid;
			}
		}

		public bool HasEmbeddedSource {
			get {
                document.HasEmbeddedSource(out bool result);
                return result;
			}
		}

		public Guid Language {
			get {
                document.GetLanguage(out Guid guid);
                return guid;
			}
		}

		public Guid LanguageVendor {
			get {
                document.GetLanguageVendor(out Guid guid);
                return guid;
			}
		}

		public int SourceLength {
			get {
                document.GetSourceLength(out uint result);
                return (int)result;
			}
		}

		public string URL {
			get {
                document.GetURL(0, out uint count, null);
                var chars = new char[count];
				document.GetURL((uint)chars.Length, out count, chars);
				if (chars.Length == 0)
					return string.Empty;
				return new string(chars, 0, chars.Length - 1);
			}
		}

		public int FindClosestLine(int line) {
            document.FindClosestLine((uint)line, out uint result);
            return (int)result;
		}

		public byte[] GetCheckSum() {
            document.GetCheckSum(0, out uint bufSize, null);
            var buffer = new byte[bufSize];
			document.GetCheckSum((uint)buffer.Length, out bufSize, buffer);
			return buffer;
		}

		public byte[] GetSourceRange(int startLine, int startColumn, int endLine, int endColumn) {
            document.GetSourceRange((uint)startLine, (uint)startColumn, (uint)endLine, (uint)endColumn, 0, out uint bufSize, null);
            var buffer = new byte[bufSize];
			document.GetSourceRange((uint)startLine, (uint)startColumn, (uint)endLine, (uint)endColumn, (uint)buffer.Length, out bufSize, buffer);
			return buffer;
		}
	}
}
