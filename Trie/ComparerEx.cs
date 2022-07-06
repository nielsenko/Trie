using System.Collections.Generic;

namespace CompactTrie
{
	public static class ComparerEx
	{
		public static IComparer<T> Reverse<T>(this IComparer<T> self)
		{
			return new ComparerReverser<T>(self);
		}
	}
}
