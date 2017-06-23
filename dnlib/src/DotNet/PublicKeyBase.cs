// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet {
	/// <summary>
	/// Public key / public key token base class
	/// </summary>
	public abstract class PublicKeyBase {
		/// <summary>
		/// The key data
		/// </summary>
		protected byte[] data;

		/// <summary>
		/// Returns <c>true</c> if <see cref="Data"/> is <c>null</c> or empty
		/// </summary>
		public bool IsNullOrEmpty => IsNullOrEmpty_NoLock;

	    /// <summary>
		/// The unlocked version of <see cref="IsNullOrEmpty"/>.
		/// </summary>
		protected bool IsNullOrEmpty_NoLock => data == null || data.Length == 0;

	    /// <summary>
		/// Returns <c>true</c> if <see cref="Data"/> is <c>null</c>
		/// </summary>
		public bool IsNull => Data == null;

	    /// <summary>
		/// Gets/sets key data
		/// </summary>
		public virtual byte[] Data {
			get => data;
	        set => data = value;
	    }

		/// <summary>
		/// Gets the <see cref="PublicKeyToken"/>
		/// </summary>
		public abstract PublicKeyToken Token { get; }

		/// <summary>
		/// Default constructor
		/// </summary>
		protected PublicKeyBase() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Key data</param>
		protected PublicKeyBase(byte[] data) {
			this.data = data;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hexString">Key data as a hex string or the string <c>"null"</c>
		/// to set key data to <c>null</c></param>
		protected PublicKeyBase(string hexString) {
			this.data = Parse(hexString);
		}

		static byte[] Parse(string hexString) {
			if (hexString == null || hexString == "null")
				return null;
			return Utils.ParseBytes(hexString);
		}

		/// <summary>
		/// Checks whether a public key or token is null or empty
		/// </summary>
		/// <param name="a">Public key or token instance</param>
		public static bool IsNullOrEmpty2(PublicKeyBase a) {
			return a == null || a.IsNullOrEmpty;
		}

		/// <summary>
		/// Returns a <see cref="PublicKeyToken"/>
		/// </summary>
		/// <param name="pkb">A <see cref="PublicKey"/> or a <see cref="PublicKeyToken"/> instance</param>
		public static PublicKeyToken ToPublicKeyToken(PublicKeyBase pkb) {
            if (pkb is PublicKeyToken pkt)
                return pkt;
            var pk = pkb as PublicKey;
		    return pk?.Token;
		}

		/// <summary>
		/// Compares two <see cref="PublicKeyBase"/>s as <see cref="PublicKeyToken"/>s
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		public static int TokenCompareTo(PublicKeyBase a, PublicKeyBase b)
		{
		    return a == b ? 0 : TokenCompareTo(ToPublicKeyToken(a), ToPublicKeyToken(b));
		}

		/// <summary>
		/// Checks whether two public key tokens are equal
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public static bool TokenEquals(PublicKeyBase a, PublicKeyBase b) {
			return TokenCompareTo(a, b) == 0;
		}

		static readonly byte[] EmptyByteArray = new byte[0];
		/// <summary>
		/// Compares two <see cref="PublicKeyToken"/>s
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		public static int TokenCompareTo(PublicKeyToken a, PublicKeyToken b)
		{
		    return Equals(a, b) ? 0 : TokenCompareTo(a?.Data, b?.Data);
		}

		static int TokenCompareTo(byte[] a, byte[] b) {
			return Utils.CompareTo(a ?? EmptyByteArray, b ?? EmptyByteArray);
		}

		/// <summary>
		/// Checks whether two public key tokens are equal
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns><c>true</c> if same, <c>false</c> otherwise</returns>
		public static bool TokenEquals(PublicKeyToken a, PublicKeyToken b) {
			return TokenCompareTo(a, b) == 0;
		}

		/// <summary>
		/// Gets the public key token hash code
		/// </summary>
		/// <param name="a">Public key or token</param>
		/// <returns>The hash code</returns>
		public static int GetHashCodeToken(PublicKeyBase a) {
			return GetHashCode(ToPublicKeyToken(a));
		}

		/// <summary>
		/// Gets the public key token hash code
		/// </summary>
		/// <param name="a">Public key token</param>
		/// <returns>The hash code</returns>
		public static int GetHashCode(PublicKeyToken a)
		{
		    return a == null ? 0 : Utils.GetHashCode(a.Data);
		}

		/// <summary>
		/// Creates a <see cref="PublicKey"/>
		/// </summary>
		/// <param name="data">Public key data or <c>null</c></param>
		/// <returns>A new <see cref="PublicKey"/> instance or <c>null</c> if <paramref name="data"/>
		/// was <c>null</c></returns>
		public static PublicKey CreatePublicKey(byte[] data)
		{
		    return data == null ? null : new PublicKey(data);
		}

		/// <summary>
		/// Creates a <see cref="PublicKeyToken"/>
		/// </summary>
		/// <param name="data">Public key token data or <c>null</c></param>
		/// <returns>A new <see cref="PublicKeyToken"/> instance or <c>null</c> if <paramref name="data"/>
		/// was <c>null</c></returns>
		public static PublicKeyToken CreatePublicKeyToken(byte[] data)
		{
		    return data == null ? null : new PublicKeyToken(data);
		}

		/// <summary>
		/// Gets the raw public key / public key token byte array
		/// </summary>
		/// <param name="pkb">The instance or <c>null</c></param>
		/// <returns>Raw public key / public key token data or <c>null</c></returns>
		public static byte[] GetRawData(PublicKeyBase pkb) {
		    return pkb?.Data;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
		    var d = Data;
		    return d == null || d.Length == 0 ? "null" : Utils.ToHex(d, false);
		}
	}
}
