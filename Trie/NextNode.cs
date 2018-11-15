using System;
using System.Runtime.InteropServices;

// Allow unit tests to access internal parts
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("StringMatchTest")]

namespace CompactTrie
{
	/// <summary>
	/// The node structure used by NextTrie.
	/// </summary>
	/// <description>
	/// This struct takes 5 bytes per 'char-byte' stored. Since chars are encoded in UTF-8
	/// One char may take multiple NextNodes to store.
	/// </description>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct NextNode : INode
	{
		static uint _noAltMask = 1u << 31; // high bit for noAlt, ..
		static uint _nextMask = ~_noAltMask; // .. remaining bits for next

		uint _nextAndNoAlt; // storage for both next and noAlt

		public byte payload;

		public uint next
		{
			get { return _nextAndNoAlt & _nextMask; }
			set
			{
				if ((value & _nextMask) != value)
					throw new ArgumentOutOfRangeException(string.Format("value: {0}", value));

				_nextAndNoAlt = (_nextAndNoAlt & ~_nextMask) | value;
			}
		}

		public bool noAlt
		{
			get { return (_nextAndNoAlt & _noAltMask) > 0; }
			set { _nextAndNoAlt = (_nextAndNoAlt & ~_noAltMask) | (value ? _noAltMask : 0); }
		}

		#region INode implementation

		public IByteIterator CreateIterator(IStore store)
		{
			return new NextNodeIterator(store as IStore<NextNode>);
		}

		#endregion
	}
}
