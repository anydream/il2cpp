// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolReader : ISymbolReader {
		readonly ISymUnmanagedReader reader;

		const int E_FAIL = unchecked((int)0x80004005);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">An unmanaged symbol reader</param>
		public SymbolReader(ISymUnmanagedReader reader) {
		    this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
		}

		public SymbolToken UserEntryPoint {
			get {
                int hr = reader.GetUserEntryPoint(out uint token);
                if (hr == E_FAIL)
					token = 0;
				else
					Marshal.ThrowExceptionForHR(hr);
				return new SymbolToken((int)token);
			}
		}

		public ISymbolDocument GetDocument(string url, Guid language, Guid languageVendor, Guid documentType) {
            reader.GetDocument(url, language, languageVendor, documentType, out ISymUnmanagedDocument document);
            return document == null ? null : new SymbolDocument(document);
		}

		public ISymbolDocument[] GetDocuments() {
            reader.GetDocuments(0, out uint numDocs, null);
            var unDocs = new ISymUnmanagedDocument[numDocs];
			reader.GetDocuments((uint)unDocs.Length, out numDocs, unDocs);
			var docs = new ISymbolDocument[numDocs];
			for (uint i = 0; i < numDocs; i++)
				docs[i] = new SymbolDocument(unDocs[i]);
			return docs;
		}

		public ISymbolVariable[] GetGlobalVariables() {
            reader.GetGlobalVariables(0, out uint numVars, null);
            var unVars = new ISymUnmanagedVariable[numVars];
			reader.GetGlobalVariables((uint)unVars.Length, out numVars, unVars);
			var vars = new ISymbolVariable[numVars];
			for (uint i = 0; i < numVars; i++)
				vars[i] = new SymbolVariable(unVars[i]);
			return vars;
		}

		public ISymbolMethod GetMethod(SymbolToken method) {
            int hr = reader.GetMethod((uint)method.GetToken(), out ISymUnmanagedMethod unMethod);
            if (hr == E_FAIL)
				return null;
			Marshal.ThrowExceptionForHR(hr);
			return unMethod == null ? null : new SymbolMethod(unMethod);
		}

		public ISymbolMethod GetMethod(SymbolToken method, int version) {
            int hr = reader.GetMethodByVersion((uint)method.GetToken(), version, out ISymUnmanagedMethod unMethod);
            if (hr == E_FAIL)
				return null;
			Marshal.ThrowExceptionForHR(hr);
			return unMethod == null ? null : new SymbolMethod(unMethod);
		}

		public ISymbolMethod GetMethodFromDocumentPosition(ISymbolDocument document, int line, int column) {
			var symDoc = document as SymbolDocument;
			if (symDoc == null)
				throw new ArgumentException("document is not a non-null SymbolDocument instance");
            int hr = reader.GetMethodFromDocumentPosition(symDoc.SymUnmanagedDocument, (uint)line, (uint)column, out ISymUnmanagedMethod unMethod);
            if (hr == E_FAIL)
				return null;
			Marshal.ThrowExceptionForHR(hr);
			return unMethod == null ? null : new SymbolMethod(unMethod);
		}

		public ISymbolNamespace[] GetNamespaces() {
            reader.GetNamespaces(0, out uint numNss, null);
            var unNss = new ISymUnmanagedNamespace[numNss];
			reader.GetNamespaces((uint)unNss.Length, out numNss, unNss);
			var nss = new ISymbolNamespace[numNss];
			for (uint i = 0; i < numNss; i++)
				nss[i] = new SymbolNamespace(unNss[i]);
			return nss;
		}

		public byte[] GetSymAttribute(SymbolToken parent, string name) {
            reader.GetSymAttribute((uint)parent.GetToken(), name, 0, out uint bufSize, null);
            var buffer = new byte[bufSize];
			reader.GetSymAttribute((uint)parent.GetToken(), name, (uint)buffer.Length, out bufSize, buffer);
			return buffer;
		}

		public ISymbolVariable[] GetVariables(SymbolToken parent) {
            reader.GetVariables((uint)parent.GetToken(), 0, out uint numVars, null);
            var unVars = new ISymUnmanagedVariable[numVars];
			reader.GetVariables((uint)parent.GetToken(), (uint)unVars.Length, out numVars, unVars);
			var vars = new ISymbolVariable[numVars];
			for (uint i = 0; i < numVars; i++)
				vars[i] = new SymbolVariable(unVars[i]);
			return vars;
		}
	}
}
