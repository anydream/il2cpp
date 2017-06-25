// dnlib: See LICENSE.txt for more info

using System.Diagnostics;
using dnlib.DotNet.Emit;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// A PDB scope
	/// </summary>
	[DebuggerDisplay("{Start} - {End}")]
	public sealed class PdbScope {
	    /// <summary>
		/// Gets/sets the first instruction
		/// </summary>
		public Instruction Start { get; set; }

		/// <summary>
		/// Gets/sets the last instruction. It's <c>null</c> if it ends at the end of the method.
		/// </summary>
		public Instruction End { get; set; }

		/// <summary>
		/// Gets all child scopes
		/// </summary>
		public ThreadSafe.IList<PdbScope> Scopes { get; } = ThreadSafeListCreator.Create<PdbScope>();

	    /// <summary>
		/// <c>true</c> if <see cref="Scopes"/> is not empty
		/// </summary>
		public bool HasScopes => Scopes.Count > 0;

	    /// <summary>
		/// Gets all locals in this scope
		/// </summary>
		public ThreadSafe.IList<Local> Variables { get; } = ThreadSafeListCreator.Create<Local>();

	    /// <summary>
		/// <c>true</c> if <see cref="Variables"/> is not empty
		/// </summary>
		public bool HasVariables => Variables.Count > 0;

	    /// <summary>
		/// Gets all namespaces
		/// </summary>
		public ThreadSafe.IList<string> Namespaces { get; } = ThreadSafeListCreator.Create<string>();

	    /// <summary>
		/// <c>true</c> if <see cref="Namespaces"/> is not empty
		/// </summary>
		public bool HasNamespaces => Namespaces.Count > 0;
	}
}
