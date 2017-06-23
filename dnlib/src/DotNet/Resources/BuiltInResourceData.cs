// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Runtime.Serialization;
using dnlib.IO;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Built-in resource data
	/// </summary>
	public sealed class BuiltInResourceData : IResourceData {
	    /// <summary>
		/// Gets the data
		/// </summary>
		public object Data { get; }

	    /// <inheritdoc/>
		public ResourceTypeCode Code { get; }

	    /// <inheritdoc/>
		public FileOffset StartOffset { get; set; }

		/// <inheritdoc/>
		public FileOffset EndOffset { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">Type of data</param>
		/// <param name="data">Data</param>
		public BuiltInResourceData(ResourceTypeCode code, object data) {
			this.Code = code;
			this.Data = data;
		}

		/// <inheritdoc/>
		public void WriteData(BinaryWriter writer, IFormatter formatter) {
			switch (Code) {
			case ResourceTypeCode.Null:
				break;

			case ResourceTypeCode.String:
				writer.Write((string)Data);
				break;

			case ResourceTypeCode.Boolean:
				writer.Write((bool)Data);
				break;

			case ResourceTypeCode.Char:
				writer.Write((ushort)(char)Data);
				break;

			case ResourceTypeCode.Byte:
				writer.Write((byte)Data);
				break;

			case ResourceTypeCode.SByte:
				writer.Write((sbyte)Data);
				break;

			case ResourceTypeCode.Int16:
				writer.Write((short)Data);
				break;

			case ResourceTypeCode.UInt16:
				writer.Write((ushort)Data);
				break;

			case ResourceTypeCode.Int32:
				writer.Write((int)Data);
				break;

			case ResourceTypeCode.UInt32:
				writer.Write((uint)Data);
				break;

			case ResourceTypeCode.Int64:
				writer.Write((long)Data);
				break;

			case ResourceTypeCode.UInt64:
				writer.Write((ulong)Data);
				break;

			case ResourceTypeCode.Single:
				writer.Write((float)Data);
				break;

			case ResourceTypeCode.Double:
				writer.Write((double)Data);
				break;

			case ResourceTypeCode.Decimal:
				writer.Write((decimal)Data);
				break;

			case ResourceTypeCode.DateTime:
				writer.Write(((DateTime)Data).ToBinary());
				break;

			case ResourceTypeCode.TimeSpan:
				writer.Write(((TimeSpan)Data).Ticks);
				break;

			case ResourceTypeCode.ByteArray:
			case ResourceTypeCode.Stream:
				var ary = (byte[])Data;
				writer.Write(ary.Length);
				writer.Write(ary);
				break;

			default:
				throw new InvalidOperationException("Unknown resource type code");
			}
		}

		/// <inheritdoc/>
		public override string ToString() {
			switch (Code) {
			case ResourceTypeCode.Null:
				return "null";

			case ResourceTypeCode.String:
			case ResourceTypeCode.Boolean:
			case ResourceTypeCode.Char:
			case ResourceTypeCode.Byte:
			case ResourceTypeCode.SByte:
			case ResourceTypeCode.Int16:
			case ResourceTypeCode.UInt16:
			case ResourceTypeCode.Int32:
			case ResourceTypeCode.UInt32:
			case ResourceTypeCode.Int64:
			case ResourceTypeCode.UInt64:
			case ResourceTypeCode.Single:
			case ResourceTypeCode.Double:
			case ResourceTypeCode.Decimal:
			case ResourceTypeCode.DateTime:
			case ResourceTypeCode.TimeSpan:
				return $"{Code}: '{Data}'";

			case ResourceTypeCode.ByteArray:
			case ResourceTypeCode.Stream:
				var ary = Data as byte[];
				if (ary != null)
					return $"{Code}: Length: {ary.Length}";
				return $"{Code}: '{Data}'";

			default:
				return $"{Code}: '{Data}'";
			}
		}
	}
}
