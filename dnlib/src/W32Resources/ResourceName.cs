// dnlib: See LICENSE.txt for more info

﻿using System;

namespace dnlib.W32Resources {
	/// <summary>
	/// A Win32 resource name. It can be either an integer or a string.
	/// </summary>
	public struct ResourceName : IComparable<ResourceName>, IEquatable<ResourceName> {
	    /// <summary>
		/// <c>true</c> if <see cref="Id"/> is valid
		/// </summary>
		public bool HasId => Name == null;

	    /// <summary>
		/// <c>true</c> if <see cref="Name"/> is valid
		/// </summary>
		public bool HasName => Name != null;

	    /// <summary>
		/// The ID. It's only valid if <see cref="HasId"/> is <c>true</c>
		/// </summary>
		public int Id { get; }

	    /// <summary>
		/// The name. It's only valid if <see cref="HasName"/> is <c>true</c>
		/// </summary>
		public string Name { get; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="id">ID</param>
		public ResourceName(int id) {
			this.Id = id;
			this.Name = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public ResourceName(string name) {
			this.Id = 0;
			this.Name = name;
		}

		/// <summary>Converts input to a <see cref="ResourceName"/></summary>
		public static implicit operator ResourceName(int id) {
			return new ResourceName(id);
		}

		/// <summary>Converts input to a <see cref="ResourceName"/></summary>
		public static implicit operator ResourceName(string name) {
			return new ResourceName(name);
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <(ResourceName left, ResourceName right) {
			return left.CompareTo(right) < 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator <=(ResourceName left, ResourceName right) {
			return left.CompareTo(right) <= 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >(ResourceName left, ResourceName right) {
			return left.CompareTo(right) > 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator >=(ResourceName left, ResourceName right) {
			return left.CompareTo(right) >= 0;
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator ==(ResourceName left, ResourceName right) {
			return left.Equals(right);
		}

		/// <summary>Overloaded operator</summary>
		public static bool operator !=(ResourceName left, ResourceName right) {
			return !left.Equals(right);
		}

		/// <inheritdoc/>
		public int CompareTo(ResourceName other) {
			if (HasId != other.HasId) {
				// Sort names before ids
				return HasName ? -1 : 1;
			}
			if (HasId)
				return Id.CompareTo(other.Id);
			else
				return Name.ToUpperInvariant().CompareTo(other.Name.ToUpperInvariant());
		}

		/// <inheritdoc/>
		public bool Equals(ResourceName other) {
			return CompareTo(other) == 0;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if (!(obj is ResourceName))
				return false;
			return Equals((ResourceName)obj);
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			if (HasId)
				return Id;
			return Name.GetHashCode();
		}

		/// <inheritdoc/>
		public override string ToString() {
			return HasId ? Id.ToString() : Name;
		}
	}
}
