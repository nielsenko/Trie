using System.Collections.Generic;
using System.Text;
namespace CompactTrie
{
	/// <summary>
	/// Iterator that returns chars, decoded from UTF-8 data 
	/// </summary>
	/// <description>This iterator has a ByteIterator inside, which does the acctual iteration. 
	/// This class performs convertion from bytes to chars.</description>
	public class CharIterator
	{
		internal IByteIterator bi;

		public CharIterator(IByteIterator bi)
		{
			this.bi = bi;
		}

		public CharIterator(Trie trie) : this(trie.Store.CreateIterator()) { }

		/// <summary>
		/// Returns the char at the current index.
		/// </summary>
		/// <param name="byteCount">number of bytes used for char</param>
		/// <description>Since the char is encoded in UTF-8, 
		/// multiple nodes may be needed for one char. End of string is marked with char '\u0000'</description>
		/// <returns>The char at the current index.</returns>
		private char GetChar(out uint byteCount)
		{
			Decoder dec = Trie.utf8.GetDecoder();
			byteCount = 0;
			var bi2 = bi.Clone();
			int charCount = 0;
			byte[] b = new byte[1];
			char[] c = new char[1];

			// Decoder.GetChars has an internal state where it remembers the 
			// bytes it has previously seen. Thus, as soon as it reports
			// that 1 char has been produced, we are done.
			do
			{
				b[0] = bi2.GetByte();
				try
				{
					charCount = dec.GetChars(b, 0, 1, c, 0);
				}
				catch (System.Exception)
				{
					throw;
				}
				var hasNext = bi2.Next();
				System.Diagnostics.Debug.Assert(hasNext || charCount > 0);
				byteCount++;
				System.Diagnostics.Debug.Assert(byteCount <= 4);
			} while (charCount == 0);

			return c[0];
		}

		/// <summary>
		/// Returns the char at the current index.
		/// </summary>
		/// <description>Since the char is encoded in UTF-8, 
		/// multiple nodes may be needed for one char. End of string is marked with char '\u0000'</description>
		/// <returns>The char.</returns>
		public char GetChar()
		{
			uint bytesRequired;
			return GetChar(out bytesRequired);
		}

		/// <summary>
		/// Follow the string to the next char. The user should look at GetChar to determine end of string.
		/// </summary>
		public bool Down()
		{
			if (bi.GetByte() == 0)
				return false;
			uint bytesRequired;
			GetChar(out bytesRequired);
			while (bytesRequired-- > 0)
			{
				bi.Next();
			}
			return true;
		}

		public bool HasAlt()
		{
			return bi.HasAlt();
		}

		/// <summary>
		/// Go to the alternative continuation of the string. This will replace the current char, and GetChar will return something different.
		/// </summary>
		/// <returns>false, if no more alternatives are available here</returns>
		public bool Alt()
		{
			return bi.Alt();
		}

		public bool IsValid()
		{
			return bi.IsValid();
		}

		/// <summary>
		/// </summary>
		public CharIterator Clone()
		{
			return new CharIterator(bi.Clone());
		}

		/// <summary>
		/// Determines whether the subtree starting at this iterator contains the same strings as the subtree starting
		/// at the other iterator, in the same order.
		/// </summary>
		/// <returns><c>true</c> if this instance is equal to the specified other; otherwise, <c>false</c>.</returns>
		/// <param name="other">Other subtree</param>
		public bool IsEqualTo(CharIterator other)
		{
			CharIterator i1 = Clone();
			CharIterator i2 = other.Clone();

			// Compare all alternatives
			bool moreAlternatives = true;
			while (moreAlternatives)
			{
				// Compare root char
				if (i1.GetChar() != i2.GetChar())
					return false;

				// Compare subtrees
				CharIterator n1 = i1.Clone();
				CharIterator n2 = i2.Clone();
				bool more1 = n1.Down();
				bool more2 = n2.Down();
				if (more1 != more2)
					return false;
				if (more1 && !n1.IsEqualTo(n2))
					return false;

				// Go to alternative root
				more1 = i1.Alt();
				more2 = i2.Alt();
				if (more1 != more2)
					return false;
				moreAlternatives = more1;
			}
			return true;
		}

		/// <summary>
		/// Determines whether the subtree starting at this iterator contains the same strings as the subtree starting
		/// at the other iterator, even if the sequence is not the same.
		/// </summary>
		/// <returns><c>true</c> if this instance is equal to the specified other; otherwise, <c>false</c>.</returns>
		/// <param name="other">Other subtree</param>
		public bool HasSameContent(CharIterator other)
		{
			CharIterator i1 = Clone();
			CharIterator i2 = other.Clone();

			// Find all alternatives
			var d1 = new Dictionary<char, CharIterator>();
			var d2 = new Dictionary<char, CharIterator>();
			do
			{
				d1[i1.GetChar()] = i1.Clone();
			} while (i1.Alt());
			do
			{
				d2[i2.GetChar()] = i2.Clone();
			} while (i2.Alt());
			if (d1.Count != d2.Count)
				return false;

			// Compare all alternatives
			foreach (var alt1 in d1)
			{
				if (!d2.ContainsKey(alt1.Key))
					return false;
				CharIterator n1 = alt1.Value;
				CharIterator n2 = d2[alt1.Key];
				bool more1 = n1.Down();
				bool more2 = n2.Down();
				if (more1 != more2)
					return false;
				if (more1 && !n1.HasSameContent(n2))
					return false;
			}
			return true;
		}
	}
}
