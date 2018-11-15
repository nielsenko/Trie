using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompactTrie
{
	/// <summary>
	/// Perform Aproximate String Matching using the Levenshtein distance (edit distance).
	/// </summary>
	public class FuzzyMatcher
	{
		Trie trie;
		public int NodesSearched;
		public int MaxDistance;
		public int InsertWeight = 1;
		public int DeleteWeight = 1;
		public int ReplaceWeight = 1;
		public IComparer<Char> Comparer = Comparer<char>.Default;

		public FuzzyMatcher(Trie validStrings)
		{
			trie = validStrings;
			MaxDistance = 10;
		}

		/// <summary>
		/// Find the best match in the set, as well as the distance needed
		/// </summary>
		/// <remarks>The code is converted from Python to C#. Source is 
		/// <see ref="http://stevehanov.ca/blog/index.php?id=114">Steve 
		/// Hanov's Blog about Levenshtein distance using a Trie</see></remarks>
		/// <param name="target">Target string we are trying to match</param>
		/// <param name="distance">Distance from found string to target string.</param>
		/// <result>The best match to the target string found</result>
		public string FindBestMatch(string target, out int distance)
		{
#if DEBUG
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
#endif

			string bestMatch = null;

			// Set distance to "infinity"
			distance = MaxDistance + 1;

			// Build first row
			var sb = new StringBuilder();
			var currentRow = new int[target.Length + 1];
			for (int i = 0; i < target.Length + 1; i++)
				currentRow[i] = i; // all insertions

			var it = new CharIterator(trie);
			NodesSearched = 0;
			SearchRecursive(it, target, currentRow, ref sb, ref bestMatch, ref distance);

#if DEBUG
			sw.Stop();
#endif

			return bestMatch;
		}

		/// <summary>
		/// This recursive helper is used by the search function above. It assumes that
		/// the previousRow has been filled in already.
		/// </summary>
		void SearchRecursive(
			CharIterator it,
			string word, // target
			int[] previousRow,
			ref StringBuilder sb,
			ref string bestMatch,
			ref int bestDistance)
		{
			NodesSearched++;

			var pos = sb.Length;
			var fast = it.Clone();

			if (pos < word.Length)
			{
				var exact = word[sb.Length];
				do
				{
					if (Comparer.Compare(fast.GetChar(), exact) == 0)
						break; // exact match found
				} while (fast.Alt());
			}

			// Search exact match child first ..
			SearchAlternative(fast, word, previousRow, ref sb, ref bestMatch, ref bestDistance);
			do
			{
				if (Comparer.Compare(it.GetChar(), fast.GetChar()) != 0)
				{
					// .. then search other children next
					SearchAlternative(it, word, previousRow, ref sb, ref bestMatch, ref bestDistance);
				}
			} while (it.Alt());
		}

		void SearchAlternative(
			CharIterator it,
			string word,
			int[] previousRow,
			ref StringBuilder sb,
			ref string bestMatch,
			ref int bestDistance)
		{
			if (it == null)
				return;
			var letter = it.GetChar();

			// if there is a word in this trie node, and the last entry in the previous row 
			// indicates the optimal cost is less than the best cost so far, then use it as best match.
			if (letter == '\u0000' && previousRow[word.Length] < bestDistance)
			{
				bestMatch = sb.ToString();
				bestDistance = previousRow[word.Length];
				return;
			}

			sb.Append(letter);

			int columns = word.Length + 1;
			var currentRow = new int[columns];

			// Build one row for the letter, with a column for each letter in the target
			// word, plus one for the empty string at column 0
			currentRow[0] = previousRow[0] + 1;
			for (int col = 1; col < columns; col++)
			{
				var insertCost = currentRow[col - 1] + InsertWeight;
				var deleteCost = previousRow[col] + DeleteWeight;
				var replaceCost = previousRow[col - 1];

				if (Comparer.Compare(word[col - 1], letter) != 0)
					replaceCost = previousRow[col - 1] + ReplaceWeight;

				currentRow[col] = Math.Min(insertCost, Math.Min(deleteCost, replaceCost));
			}

			// If any entries in the row are less than the maximum cost, then 
			// recursively search each branch of the trie.
			if (currentRow.Min() < bestDistance)
			{
				var next = it.Clone();
				next.Down();
				SearchRecursive(next, word, currentRow, ref sb, ref bestMatch, ref bestDistance);
			}

			sb.Remove(sb.Length - 1, 1);
		}
	}
}
