using System.Collections.Generic;
using System.Text;

namespace CompactTrie
{
	/// <summary>
	/// Facade that makes it simpler to use AltNode and NextNode without worrying
	/// about implementation details.
	/// </summary>
	/// <description>
	/// The Trie uses a node structure that has an explicit and an implicit 
	/// pointer. Every node is stored in an array for compactness. The explicit 
	/// pointer is an index into the array, the implicit pointer is the following 
	/// node in the array.
	/// 
	/// The AltNode has an explicit alternative and an implicit next pointer.
	/// The NextNode has an explicit next and an implicit alternative pointer.
	/// The former makes insertions efficient, the latter makes it possible to 
	/// build a DAWG.
	/// 
	/// Users of Trie is provided a CharIterator, that will hide which of the
	/// two representations are used internally.
	/// </description>
	public class Trie
	{
		internal static Encoding utf8 = new UTF8Encoding(false);

		public IStore Store { get; set; }

		public Trie() : this(new ArrayStore<AltNode>()) { }

		public Trie(IStore store)
		{
			Store = store;
		}

		/// <summary>
		/// Matches as much as possible of prefix, and returns a CharIterator that will let you iterate through
		/// all strings with the matched prefix.
		/// </summary>
		/// <remarks>If you want to append data you should use FindLongestMatch instead</remarks>
		/// <returns><c>true</c>, if all chars in prefix was found (even if this is just a prefix of a valid string), <c>false</c> otherwise.</returns>
		/// <param name="prefix">Prefix.</param>
		/// <param name="ci">CharIterator after matched prefix</param>
		/// <param name="matchLength">Number of chars in prefix matched</param>
		public bool FindLongestPrefix(string prefix, out CharIterator ci, out int matchLength, bool before = true)
		{
			matchLength = 0;
			ci = new CharIterator(this);
			if (!ci.IsValid())
				return false;

			while (true)
			{
				// Check if all of prefix is matched
				if (matchLength == prefix.Length)
					return true;

				// Find correct alternative at this length
				var prevCi = ci.Clone();
				var letter = prefix[matchLength];
				while (ci.GetChar() != letter)
				{
					if (!ci.Alt())
					{
						if (before) 
							ci = prevCi;
						return false;
					}
				}

				// Increase length
				matchLength++;
				bool moreChars = ci.Down();

				// There will always be more chars after a matching char, 
				// since strings in StringTrie are null terminated, and str is not.
				System.Diagnostics.Debug.Assert(moreChars);
			}
		}

		/// <summary>
		/// Tries to match as many chars of str as possible. Returns an iterator that points at the node after the
		/// longest matching prefix. 
		/// In case of a full match, the char pointed at is '\u0000', 
		/// in case of a mismatch, the iterator points at the node that should have added an alternate node.
		/// </summary>
		/// <remarks>This function cannot be used to find all strings that has str as prefix. For that purpose
		/// you should use FindLongestPrefix</remarks>
		/// <returns>True, if str was found as a string in StringTrie (not just as a substring of a longer string)</returns>
		/// <param name="str">String to search for.</param>
		/// <param name="ci">Iterator pointing at last node. Use this to insert remaining substring</param>
		/// <param name="matchLength">Number of chars from str matched</param>
		public bool FindLongestMatch(string str, out CharIterator ci, out int matchLength)
		{
			//			return FindLongestPrefix (str, out ci, out matchLength, false);
			matchLength = 0;
			ci = new CharIterator(this);
			if (!ci.IsValid())
				return false;

			while (true)
			{
				// Check for end of string
				if (matchLength == str.Length)
				{
					// check for end marker
					while (ci.GetChar() != '\u0000')
					{
						if (!ci.Alt())
							return false;
					}
					return true;
				}

				// Find correct alternative at this length
				while (ci.GetChar() != str[matchLength])
				{
					if (!ci.Alt())
						return false;
				}

				// Increase length
				matchLength++;
				bool moreChars = ci.Down();

				// There will always be more chars after a matching char, 
				// since strings in StringTrie are null terminated, and str is not.
				System.Diagnostics.Debug.Assert(moreChars);
			}
		}

		/// <summary>
		/// Test if an exact match can be found for a given string.
		/// </summary>
		/// <returns><c>true</c>, if exact match was found, <c>false</c> otherwise.</returns>
		/// <param name="str">String to search for.</param>
		public bool FindExactMatch(string str) 
			=> FindLongestMatch(str, out CharIterator ci, out int matchLength);

		/// <summary>
		/// Add a string to the trie.
		/// </summary>
		/// <param name="str">the string to add</param>
		/// <description>Note that we add alternatives at the start of UTF-8 char sequences.
		/// This means that a char that is more than 1 byte long will always have alternative values at the first byte.
		/// This will waste a tiny amount of space, but make the CharIterator code simpler.
		/// Example: The Danish letters Æ and Ø are UTF-8 encoded as C3 86 and C3 98. This would normally be stored 
		/// as 3 bytes by the trie, since the first byte is the same. However, since we are using CharIterator
		/// the internal ByteIterator will point at the first byte, not the second, when it finds the mismatch.
		/// Thus, the data is stored as 4 bytes.
		/// </description>
		/// <returns>
		/// 	<c>true</c>, if string was added, otherwise 
		/// 	<c>false</c> if string was already in trie
		/// </returns>
		public bool Add(string str)
		{
			var altStore = Store as IStore<AltNode>;

			if (altStore == null)
				throw new System.InvalidOperationException(
					"You cannot add a string to a StringTrie that has been compactified");

			if (FindLongestMatch(str, out CharIterator ci, out int matchLength))
				return false;

			uint curInx = ((AltNodeIterator)ci.bi).inx;
			uint newInx = (uint)altStore.Length;
			var altData = utf8.GetBytes(str.Substring(matchLength));

			// Mark end of string
			altStore[newInx + (uint)altData.Length] = new AltNode { payload = 0 };
			// altStore.Length grows as we assign values to more and more of the array

			// Add pointer to new alternative
			var n = altStore[curInx];
			n.alt = newInx;
			altStore[curInx] = n;

			// Add mismatched string suffix
			for (uint i = 0; i < altData.Length; i++)
			{
				n = altStore[newInx + i];
				n.payload = altData[i];
				altStore[newInx + i] = n;
			}

			return true;
		}

		private string Rep(SortedDictionary<char, uint> children)
		{
			var sb = new StringBuilder();
			foreach (var k in children.Keys)
			{
				sb.AppendFormat("{0}{1:X}#", k, children[k]);
			}
			return sb.ToString();
		}

		private string Rep(NextNodeIterator subtree)
		{
			var i = subtree.Clone() as NextNodeIterator;
			var d = new SortedDictionary<char, uint>();
			do
			{
				d[new CharIterator(i).GetChar()] = i.inx;
			} while (i.Alt());
			return Rep(d);
		}

		/// <summary>
		/// Minimize a subtree
		/// </summary>
		/// <returns>The compacted subtree</returns>
		/// <param name="src">Source.</param>
		/// <param name="contentIndex">Index of subtrees already minimized</param>
		/// <param name="tgtStore">The target store</param>
		/// <param name="comparer">The comparer to use for sorting alternations during minimization</param>
		/// <description>
		/// foreach alternative continuation
		/// 	step down,
		/// 	recurse
		/// 	result from recursion is added to my compacted children
		/// compute by representation from my compacted children
		/// if I'm already in contentIndex, return that up
		/// if not, create new data and return pointer to that
		/// </description>
		private uint MinimizeSubtree(
			CharIterator src,
			IStore<NextNode> tgtStore,
			Dictionary<string, uint> contentIndex,
			IComparer<char> comparer)
		{
			// Compact each subtree and find their location in tgtStore
			var alt = src.Clone();
			var subtree = new SortedDictionary<char, uint>(comparer);
			uint subtreeInx = 0;
			do
			{
				char key = alt.GetChar();
				var down = alt.Clone();
				if (down.Down())
					subtreeInx = MinimizeSubtree(down, tgtStore, contentIndex, comparer);

				subtree[key] = subtreeInx;
			} while (alt.Alt());

			// Check if entire subtree is a repetition, if yes don't store it again
			if (contentIndex.TryGetValue(Rep(subtree), out uint altInx))
				return altInx;

			//
			// Not a repetition, so create new subtree-root
			//
			uint firstInx = altInx = (uint)tgtStore.Length;

			// Append all subtrees as one continuous alt-block
			uint payloadInx = altInx + (uint)subtree.Count; // >1 byte payload is allocated after alt-block
			foreach (var kv in subtree)
			{
				//
				// Insert payload char in UTF-8 encoding
				//
				var payloadData = Trie.utf8.GetBytes(new[] { kv.Key });
				var payloadLeft = payloadData.Length;

				uint prevInx = altInx;

				// Insert first node with start of payload, this is part of the alt-block
				var node = new NextNode
				{
					payload = payloadData[0],
					noAlt = prevInx == firstInx
				};
				tgtStore[altInx++] = node;

				// Payload longer than 1 byte needs extra allocation behind the alt-block
				while (--payloadLeft > 0)
				{
					// Update next pointer to allocated payload
					node = tgtStore[prevInx];
					node.next = payloadInx;
					tgtStore[prevInx] = node;

					prevInx = payloadInx;

					// Append more payload
					tgtStore[payloadInx++] = new NextNode { payload = payloadData[payloadData.Length - payloadLeft] };
				}

				// Update final next pointer to point to subtree
				node = tgtStore[prevInx];
				node.next = kv.Value; // index of subtree
				tgtStore[prevInx] = node;
			}

			subtreeInx = altInx - 1;
			contentIndex[Rep(subtree)] = subtreeInx;
			return subtreeInx;
		}

		/// <summary>
		/// Copy Minimize the specified tgtStore.
		/// </summary>
		/// <description>
		/// Compress, Dawgify .. dear child has many names
		/// </description>
		/// <param name="tgtStore">Tgt store.</param>
		public void Minimize(IStore<NextNode> tgtStore, IComparer<char> comparer)
		{
			var srcStore = Store as IStore<AltNode>;
			if (srcStore == null)
				throw new System.InvalidOperationException("You can only Minimize a StringTrie once");

			var contentIndex = new Dictionary<string, uint>();
			uint tgtInx = 0;

			var it = new CharIterator(this);
			if (it.IsValid())
			{
				// Insert termination char
				tgtStore[tgtInx] = new NextNode { payload = 0, noAlt = true };
				var ni = new NextNodeIterator(tgtStore, tgtInx);
				contentIndex[Rep(ni)] = ni.inx;
				System.Diagnostics.Debug.Assert(tgtStore.Length == 1);

				// Traverse srcStore trie in post order
				MinimizeSubtree(it, tgtStore, contentIndex, comparer);
			}

			// Replace store with minimized version
			Store = tgtStore;
		}

		public void Minimize(IStore<NextNode> tgtStore) 
			=> Minimize(tgtStore, new DanishCharComparer().Reverse());

		public void Minimize() 
			=> Minimize(new ArrayStore<NextNode>(), new DanishCharComparer().Reverse());
	}
}
