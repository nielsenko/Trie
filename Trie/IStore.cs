namespace CompactTrie
{
	public interface INode
	{
		IByteIterator CreateIterator(IStore store);
	}

	public interface IStore
	{
		IByteIterator CreateIterator();
		int Length { get; }
	}

	/// <summary>
	/// A compact trie store. This interface defines the operations needed by the
	/// AltTrie and NextTrie classes, in order to store internal nodes. It is basically an
	/// abstraction over a TNode[]. 
	/// </summary>
	public interface IStore<TNode> : IStore where TNode : struct, INode
	{
		TNode this[uint inx] { get; set; }
	}

	public static class IStoreEx
	{
		public static void CopyTo<TNode>(this IStore<TNode> self, IStore<TNode> target) where TNode : struct, INode
		{
			for (uint i = 0; i < self.Length; ++i) 
			{
				target[i] = self[i];
			}
		}
	}
}
