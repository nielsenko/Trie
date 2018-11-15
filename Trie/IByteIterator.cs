namespace CompactTrie
{
	/// <summary>
	/// Returns the bytes stored in the trie / dawg.
	/// </summary>
	/// <remarks>You probably want to use CharIterator, which interpets the bytes as UTF-8 chars.</remarks>
	public interface IByteIterator
	{
		/// <summary>
		/// Returns the byte at the current index.
		/// </summary>
		byte GetByte();

		/// <summary>
		/// Determines whether this instance has next.
		/// </summary>
		/// <returns><c>true</c> if this instance has next; otherwise, <c>false</c>.</returns>
		bool HasNext();

		/// <summary>
		/// Follow the string to the next byte.
		/// </summary>
		/// <description>Warning: There is no end of string marker, so the user must determine when a string is 
		/// complete.</description>
		bool Next();

		/// <summary>
		/// Determines whether this instance has alternate.
		/// </summary>
		/// <returns><c>true</c> if this instance has alternate; otherwise, <c>false</c>.</returns>
		bool HasAlt();

		/// <summary>
		/// Go to the alternative continuation of the string, if any. 
		/// This will replace the current char, and GetChar will return something different.
		/// </summary>
		/// <returns>false, if no more alternatives are available here</returns>
		bool Alt();

		/// <summary>
		/// Determines whether this instance is valid.
		/// </summary>
		/// <returns><c>true</c> if this instance is valid; otherwise, <c>false</c>.</returns>
		bool IsValid();

		/// <summary>
		/// Clone this instance.
		/// </summary>
		IByteIterator Clone();
	}
}
