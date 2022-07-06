using System.Collections.Generic;

namespace CompactTrie
{
	public sealed class CaseInsensitiveCharComparer : IComparer<char>
	{
		IComparer<char> Comparer { get; set; } = Comparer<char>.Default;

		public int Compare(char x, char y)
			=> Comparer.Compare(char.ToLower(x), char.ToLower(y));
	}
}
