// Allow unit tests to access internal parts
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("StringMatchTest")]

namespace CompactTrie
{
	public class NextNodeIterator : IByteIterator
	{
		internal IStore<NextNode> _store;

		internal uint inx { get; private set; }

		private NextNode CurrentNode => _store[inx];

		internal NextNodeIterator(IStore<NextNode> store, uint inx)
		{
			_store = store;
			this.inx = inx;
			System.Diagnostics.Debug.Assert(IsValid() || _store.Length == 0);
		}

		public NextNodeIterator(IStore<NextNode> store) : this(store, Root(store)) { }

		static private uint Root(IStore<NextNode> store)
		{
			var i = (uint)store.Length;
			do --i; while (i < store.Length && (store[i].payload & 0xc0) == 0x80); // skip extension bytes (0b11xxxxxx)
			return i;
		}

		/// <summary>
		/// Returns the byte at the current index.
		/// </summary>
		/// <description>Since the char is encoded in UTF-8,
		/// multiple nodes may be needed for one char. 
		/// End of string is marked with char '\u0000'
		/// </description>
		/// <returns>The char.</returns>
		public byte GetByte() => CurrentNode.payload;

		public bool HasNext() => inx != 0; // next node dawgs share the same termination node, ie. 0

		/// <summary>
		/// Follow the string to the next byte. The user should look at GetByte to determine end of string.
		/// </summary>
		public bool Next()
		{
			if (!HasNext())
				return false;

			inx = CurrentNode.next;
			System.Diagnostics.Debug.Assert(IsValid());
			return true;
		}

		public bool HasAlt() => !CurrentNode.noAlt;

		/// <summary>
		/// Go to the alternative continuation of the string.
		/// </summary>
		/// <returns>false, if no more alternatives are available here</returns>
		public bool Alt()
		{
			if (CurrentNode.noAlt)
				return false;

			--inx;
			return true;
		}

		public bool IsValid() => inx < _store.Length;

		public IByteIterator Clone() => new NextNodeIterator(_store, inx);
	}
}
