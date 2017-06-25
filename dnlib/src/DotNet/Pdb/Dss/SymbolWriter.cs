﻿// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolWriter : ISymbolWriter2 {
		readonly ISymUnmanagedWriter2 writer;
		readonly string pdbFileName;
		readonly Stream pdbStream;
		bool closeCalled;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="pdbFileName">PDB file name</param>
		public SymbolWriter(ISymUnmanagedWriter2 writer, string pdbFileName) {
		    this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
			this.pdbFileName = pdbFileName ?? throw new ArgumentNullException(nameof(pdbFileName));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="pdbFileName">PDB file name</param>
		/// <param name="pdbStream">PDB output stream</param>
		public SymbolWriter(ISymUnmanagedWriter2 writer, string pdbFileName, Stream pdbStream) {
		    this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
			this.pdbStream = pdbStream ?? throw new ArgumentNullException(nameof(pdbStream));
			this.pdbFileName = pdbFileName;
		}

		public void Close() {
			if (closeCalled)
				return;
			closeCalled = true;
			writer.Close();
		}

		public void CloseMethod() {
			writer.CloseMethod();
		}

		public void CloseNamespace() {
			writer.CloseNamespace();
		}

		public void CloseScope(int endOffset) {
			writer.CloseScope((uint)endOffset);
		}

		public ISymbolDocumentWriter DefineDocument(string url, Guid language, Guid languageVendor, Guid documentType) {
            writer.DefineDocument(url, ref language, ref languageVendor, ref documentType, out ISymUnmanagedDocumentWriter unDocWriter);
            return unDocWriter == null ? null : new SymbolDocumentWriter(unDocWriter);
		}

		public void DefineField(SymbolToken parent, string name, System.Reflection.FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3) {
			writer.DefineField((uint)parent.GetToken(), name, (uint)attributes, (uint)signature.Length, signature, (uint)addrKind, (uint)addr1, (uint)addr2, (uint)addr3);
		}

		public void DefineGlobalVariable(string name, System.Reflection.FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3) {
			writer.DefineGlobalVariable(name, (uint)attributes, (uint)signature.Length, signature, (uint)addrKind, (uint)addr1, (uint)addr2, (uint)addr3);
		}

		public void DefineLocalVariable(string name, System.Reflection.FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset) {
			writer.DefineLocalVariable(name, (uint)attributes, (uint)signature.Length, signature, (uint)addrKind, (uint)addr1, (uint)addr2, (uint)addr3, (uint)startOffset, (uint)endOffset);
		}

		public void DefineParameter(string name, ParameterAttributes attributes, int sequence, SymAddressKind addrKind, int addr1, int addr2, int addr3) {
			writer.DefineParameter(name, (uint)attributes, (uint)sequence, (uint)addrKind, (uint)addr1, (uint)addr2, (uint)addr3);
		}

		public void DefineSequencePoints(ISymbolDocumentWriter document, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns) {
			var doc = document as SymbolDocumentWriter;
			if (doc == null)
				throw new ArgumentException("document isn't a non-null SymbolDocumentWriter instance");
			if (offsets == null || lines == null || columns == null ||
				endLines == null || endColumns == null ||
				offsets.Length != lines.Length ||
				offsets.Length != columns.Length ||
				offsets.Length != endLines.Length ||
				offsets.Length != endColumns.Length)
				throw new ArgumentException("Invalid arrays");
			writer.DefineSequencePoints(doc.SymUnmanagedDocumentWriter, (uint)offsets.Length, offsets, lines, columns, endLines, endColumns);
		}

		public void DefineSequencePoints(ISymbolDocumentWriter document, uint arraySize, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns) {
			var doc = document as SymbolDocumentWriter;
			if (doc == null)
				throw new ArgumentException("document isn't a non-null SymbolDocumentWriter instance");
			writer.DefineSequencePoints(doc.SymUnmanagedDocumentWriter, arraySize, offsets, lines, columns, endLines, endColumns);
		}

		public void Initialize(IntPtr emitter, string filename, bool fFullBuild) {
			writer.Initialize(emitter, filename, null, fFullBuild);
		}

		public void OpenMethod(SymbolToken method) {
			writer.OpenMethod((uint)method.GetToken());
		}

		public void OpenNamespace(string name) {
			writer.OpenNamespace(name);
		}

		public int OpenScope(int startOffset) {
            writer.OpenScope((uint)startOffset, out uint result);
            return (int)result;
		}

		public void SetMethodSourceRange(ISymbolDocumentWriter startDoc, int startLine, int startColumn, ISymbolDocumentWriter endDoc, int endLine, int endColumn) {
			var sdoc = startDoc as SymbolDocumentWriter;
			if (sdoc == null)
				throw new ArgumentException("startDoc isn't a non-null SymbolDocumentWriter instance");
			var edoc = endDoc as SymbolDocumentWriter;
			if (edoc == null)
				throw new ArgumentException("endDoc isn't a non-null SymbolDocumentWriter instance");
			writer.SetMethodSourceRange(sdoc.SymUnmanagedDocumentWriter, (uint)startLine, (uint)startColumn, edoc.SymUnmanagedDocumentWriter, (uint)endLine, (uint)endColumn);
		}

		public void SetScopeRange(int scopeID, int startOffset, int endOffset) {
			writer.SetScopeRange((uint)scopeID, (uint)startOffset, (uint)endOffset);
		}

		public void SetSymAttribute(SymbolToken parent, string name, byte[] data) {
			writer.SetSymAttribute((uint)parent.GetToken(), name, (uint)data.Length, data);
		}

		public void SetUnderlyingWriter(IntPtr underlyingWriter) {
			throw new NotSupportedException();
		}

		public void SetUserEntryPoint(SymbolToken entryMethod) {
			writer.SetUserEntryPoint((uint)entryMethod.GetToken());
		}

		public void UsingNamespace(string fullName) {
			writer.UsingNamespace(fullName);
		}

		public byte[] GetDebugInfo(out IMAGE_DEBUG_DIRECTORY pIDD) {
            writer.GetDebugInfo(out pIDD, 0, out uint size, null);
            var buffer = new byte[size];
			writer.GetDebugInfo(out pIDD, size, out size, buffer);
			return buffer;
		}

		public void DefineLocalVariable2(string name, uint attributes, uint sigToken, uint addrKind, uint addr1, uint addr2, uint addr3, uint startOffset, uint endOffset) {
			writer.DefineLocalVariable2(name, attributes, sigToken, addrKind, addr1, addr2, addr3, startOffset, endOffset);
		}

		public void Initialize(MetaData metaData) {
			if (pdbStream != null)
				writer.Initialize(new MDEmitter(metaData), pdbFileName, new StreamIStream(pdbStream), true);
			else if (!string.IsNullOrEmpty(pdbFileName))
				writer.Initialize(new MDEmitter(metaData), pdbFileName, null, true);
			else
				throw new InvalidOperationException();
		}

		public void Dispose() {
			Marshal.FinalReleaseComObject(writer);
		}
	}
}
