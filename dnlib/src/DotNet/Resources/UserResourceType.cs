// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// User resource type
	/// </summary>
	public sealed class UserResourceType {
	    /// <summary>
		/// Full name including assembly of type
		/// </summary>
		public string Name { get; }

	    /// <summary>
		/// User type code
		/// </summary>
		public ResourceTypeCode Code { get; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Full name including assembly of type</param>
		/// <param name="code">User type code</param>
		public UserResourceType(string name, ResourceTypeCode code) {
			this.Name = name;
			this.Code = code;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return $"{(int) Code:X2} {Name}";
		}
	}
}
