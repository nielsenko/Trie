namespace CompactTrie
{
	public interface ITrieLoader
	{
		string RootPath { set; get; }
		Trie Load(string path);
	}
}
