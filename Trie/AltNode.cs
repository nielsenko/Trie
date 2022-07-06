using System.Runtime.InteropServices;

// Allow unit tests to access internal parts
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("StringMatchTest")]

namespace CompactTrie
{
	/// <summary>
	/// The node structure used by AltTrie.
	/// </summary>
	/// <description>
	/// This struct takes 5 bytes per 'char-byte' stored. Since chars are encoded in UTF-8
	/// One char may take multiple AltNodes to store.
	/// </description>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct AltNode : INode
	{
		public byte payload;
		public uint alt { get; set; }

		#region INode implementation

		public IByteIterator CreateIterator(IStore store)
		{
			return new AltNodeIterator(store as IStore<AltNode>);
		}

		#endregion
	}
	
}
