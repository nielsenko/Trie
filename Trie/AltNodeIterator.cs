// Allow unit tests to access internal parts
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("StringMatchTest")]

namespace CompactTrie
{
	/// <summary>
	/// Returns the bytes stored in the trie.
	/// </summary>
	/// <remarks>You probably want to use CharIterator, which interpets the bytes as UTF-8 chars.</remarks>
	class AltNodeIterator : IByteIterator
	{
		internal IStore<AltNode> _store;

		/// <summary>Index to the current node in the AltNode array in _trie</summary>
		internal uint inx { get; private set; }

		private AltNode CurrentNode
		{
			get { return _store[inx]; }
		}

		internal AltNodeIterator(IStore<AltNode> store, uint inx)
		{
			_store = store;
			this.inx = inx;
			System.Diagnostics.Debug.Assert(IsValid() || _store.Length == 0);
		}

		public AltNodeIterator(IStore<AltNode> store) : this(store, 0) { }

		/// <summary>
		/// Returns the char at the current index.
		/// </summary>
		/// <description>Since the char is encoded in UTF-8,
		/// multiple nodes may be needed for one char. End of string is marked with char '\u0000'</description>
		/// <returns>The char.</returns>
		public byte GetByte()
		{
			return CurrentNode.payload;
		}

		public bool HasNext()
		{
			return GetByte() != 0;
		}
		/// <summary>
		/// Follow the string to the next char. The user should look at GetChar to determine end of string.
		/// </summary>
		/// <returns>true, when a full string has been read. </returns>
		/// <remarks>It may still be possible to have alternatives, even at the end of a string</remarks>
		public bool Next()
		{
			if (!HasNext())
				return false;

			inx++;
			System.Diagnostics.Debug.Assert(IsValid());
			return true;
		}

		public bool HasAlt()
		{
			return CurrentNode.alt != 0;
		}

		/// <summary>
		/// Go to the alternative continuation of the string. This will replace the current char, and GetChar will return something different.
		/// </summary>
		/// <returns>false, if no more alternatives are available here</returns>
		public bool Alt()
		{
			if (!HasAlt())
				return false;

			inx = CurrentNode.alt;
			System.Diagnostics.Debug.Assert(IsValid());
			return true;
		}

		public bool IsValid()
		{
			return inx < _store.Length;
		}

		public IByteIterator Clone()
		{
			return new AltNodeIterator(_store, inx);
		}
	}
}
