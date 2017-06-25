// dnlib: See LICENSE.txt for more info

﻿using System;

namespace dnlib.DotNet {
	/// <summary>
	/// Recursion counter
	/// </summary>
	public struct RecursionCounter {
		/// <summary>
		/// Max recursion count. If this is reached, we won't continue, and will use a default value.
		/// </summary>
		public const int MAX_RECURSION_COUNT = 100;

	    /// <summary>
		/// Gets the recursion counter
		/// </summary>
		public int Counter { get; private set; }

	    /// <summary>
		/// Increments <see cref="Counter"/> if it's not too high. <c>ALL</c> instance methods
		/// that can be called recursively must call this method and <see cref="Decrement"/>
		/// (if this method returns <c>true</c>)
		/// </summary>
		/// <returns><c>true</c> if it was incremented and caller can continue, <c>false</c> if
		/// it was <c>not</c> incremented and the caller must return to its caller.</returns>
		public bool Increment() {
			if (Counter >= MAX_RECURSION_COUNT)
				return false;
			Counter++;
			return true;
		}

		/// <summary>
		/// Must be called before returning to caller if <see cref="Increment"/>
		/// returned <c>true</c>.
		/// </summary>
		public void Decrement() {
#if DEBUG
			if (Counter <= 0)
				throw new InvalidOperationException("recursionCounter <= 0");
#endif
			Counter--;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return Counter.ToString();
		}
	}
}
