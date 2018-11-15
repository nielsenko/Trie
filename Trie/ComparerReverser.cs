using System;
using System.Collections.Generic;

namespace CompactTrie
{
	public sealed class ComparerReverser<T> : IComparer<T>
	{
		readonly IComparer<T> _wrappedComparer;

		// Initializes an instance of a ComparerReverser that takes a wrapped comparer
		// and returns the inverse of the comparison.
		public ComparerReverser(IComparer<T> wrappedComparer)
		{
			_wrappedComparer = wrappedComparer ?? throw new ArgumentNullException(nameof(wrappedComparer));
		}

		// Compares two objects and returns a value indicating whether
		// one is less than, equal to, or greater than the other.
		public int Compare(T x, T y)
		{
			// To reverse compare, just invert the operands.
			return _wrappedComparer.Compare(y, x);
		}
	}
}
