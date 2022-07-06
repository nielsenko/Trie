using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CompactTrie
{
	public class PrefixMatcher : IEnumerable<string>
	{
		StringIterator _iterator;
		char[] _stop;
		bool _expandIfUnique;
		string _appendIfPartial;

		public PrefixMatcher(StringIterator iterator, char[] stop = null, bool expandIfUnique = false, string appendIfPartial = null)
		{
			_iterator = iterator;
			_stop = stop;
			_expandIfUnique = expandIfUnique;
			_appendIfPartial = appendIfPartial;
		}

		public PrefixMatcher
		(
			Trie trie,
			string prefix,
			char[] stop = null,
			bool expandIfUnique = false,
			string appendIfPartial = null,
			string expandedPrefix = null
		)
			: this(BuildStringIterator(trie, prefix, expandedPrefix), stop, expandIfUnique, appendIfPartial) { }

		static StringIterator BuildStringIterator(Trie trie, string prefix, string expandedPrefix = null)
		{
			if (trie != null && trie.FindLongestPrefix(prefix, out CharIterator ci, out int matchLength))
				return new StringIterator(expandedPrefix ?? prefix, ci);

			return null;
		}

		bool IsStopChar(char c) => _stop?.Contains(c) ?? false;

		#region IEnumerable implementation

		public IEnumerator<string> GetEnumerator()
		{
			if (this._iterator == null)
				yield break;

			var iterator = _iterator.Clone();

			while (true)
			{
				do
				{
					// Continue string as long as possible, stop at stop chars
					while (true)
					{
						if (IsStopChar(iterator.NextChar()))
						{
							int extension = 0;

							bool more;
							while (more = iterator.Down())
							{
								// extent until alternation ..
								++extension;
								if (!_expandIfUnique || iterator.HasAlt())
									break;
							}

							var result = iterator.GetString();
							if (_appendIfPartial != null && more)
								result += _appendIfPartial;

							while (extension-- > 0)
								iterator.Up(); // .. and remember to remove it again, before actually stopping!

							yield return result;
							break;
						}
						if (!iterator.Down())
						{
							// at the end
							yield return iterator.GetString();
							break;
						}
					}
				}
				while (iterator.Alt()); // loop over alternatives

				// Go up until we find next alternative
				while (!iterator.Alt())
				{
					if (!iterator.Up())
						yield break;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		#endregion
	}
}
