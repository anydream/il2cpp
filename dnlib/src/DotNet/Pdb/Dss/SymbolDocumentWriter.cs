// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolDocumentWriter : ISymbolDocumentWriter {
	    public ISymUnmanagedDocumentWriter SymUnmanagedDocumentWriter { get; }

	    public SymbolDocumentWriter(ISymUnmanagedDocumentWriter writer) {
			this.SymUnmanagedDocumentWriter = writer;
		}

		public void SetCheckSum(Guid algorithmId, byte[] checkSum) {
			SymUnmanagedDocumentWriter.SetCheckSum(algorithmId, (uint)(checkSum?.Length ?? 0), checkSum);
		}

		public void SetSource(byte[] source) {
			SymUnmanagedDocumentWriter.SetSource((uint)source.Length, source);
		}
	}
}
