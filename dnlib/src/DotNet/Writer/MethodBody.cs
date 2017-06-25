// dnlib: See LICENSE.txt for more info

﻿using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Method body chunk
	/// </summary>
	public sealed class MethodBody : IChunk {
		const uint EXTRA_SECTIONS_ALIGNMENT = 4;

	    uint length;

	    /// <inheritdoc/>
		public FileOffset FileOffset { get; private set; }

	    /// <inheritdoc/>
		public RVA RVA { get; private set; }

	    /// <summary>
		/// Gets the code
		/// </summary>
		public byte[] Code { get; }

	    /// <summary>
		/// Gets the extra sections (exception handlers) or <c>null</c>
		/// </summary>
		public byte[] ExtraSections { get; }

	    /// <summary>
		/// Gets the token of the locals
		/// </summary>
		public uint LocalVarSigTok { get; }

	    /// <summary>
		/// <c>true</c> if it's a fat body
		/// </summary>
		public bool IsFat => !IsTiny;

	    /// <summary>
		/// <c>true</c> if it's a tiny body
		/// </summary>
		public bool IsTiny { get; }

	    /// <summary>
		/// <c>true</c> if there's an extra section
		/// </summary>
		public bool HasExtraSections => ExtraSections != null && ExtraSections.Length > 0;

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Code</param>
		public MethodBody(byte[] code)
			: this(code, null, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Code</param>
		/// <param name="extraSections">Extra sections or <c>null</c></param>
		public MethodBody(byte[] code, byte[] extraSections)
			: this(code, extraSections, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Code</param>
		/// <param name="extraSections">Extra sections or <c>null</c></param>
		/// <param name="localVarSigTok">Token of locals</param>
		public MethodBody(byte[] code, byte[] extraSections, uint localVarSigTok) {
			this.IsTiny = (code[0] & 3) == 2;
			this.Code = code;
			this.ExtraSections = extraSections;
			this.LocalVarSigTok = localVarSigTok;
		}

		/// <summary>
		/// Gets the approximate size of the method body (code + exception handlers)
		/// </summary>
		public int GetSizeOfMethodBody() {
			int len = Code.Length;
			if (ExtraSections != null) {
				len = Utils.AlignUp(len, EXTRA_SECTIONS_ALIGNMENT);
				len += ExtraSections.Length;
				len = Utils.AlignUp(len, EXTRA_SECTIONS_ALIGNMENT);
			}
			return len;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.FileOffset = offset;
			this.RVA = rva;
			if (HasExtraSections) {
				RVA rva2 = rva + (uint)Code.Length;
				rva2 = rva2.AlignUp(EXTRA_SECTIONS_ALIGNMENT);
				rva2 += (uint)ExtraSections.Length;
				length = (uint)rva2 - (uint)rva;
			}
			else
				length = (uint)Code.Length;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			writer.Write(Code);
			if (HasExtraSections) {
				RVA rva2 = RVA + (uint)Code.Length;
				writer.WriteZeros((int)rva2.AlignUp(EXTRA_SECTIONS_ALIGNMENT) - (int)rva2);
				writer.Write(ExtraSections);
			}
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return Utils.GetHashCode(Code) + Utils.GetHashCode(ExtraSections);
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			var other = obj as MethodBody;
			if (other == null)
				return false;
			return Utils.Equals(Code, other.Code) &&
				Utils.Equals(ExtraSections, other.ExtraSections);
		}
	}
}
