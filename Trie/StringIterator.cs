using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace CompactTrie
{
	/// <summary>
	/// Iterate over all strings stored in a StringTrie.
	/// </summary>
	/// <description>
	/// This class is designed for extracting full strings from partial matches
	/// </description>
	public class StringIterator
	{
		// Holds all chars in the already visited path
		readonly StringBuilder _builder;
		// Stack of CharIterators visited. Each CharIterator points to a char after the already visited path
		// stack.Peek() points to the next char that will be added by down
		readonly List<CharIterator> _stack;

		public StringIterator(StringIterator other)
		{
			_builder = new StringBuilder(other.GetString());
			_stack = new List<CharIterator>(other._stack.Select(ci => ci.Clone()));
		}

		public StringIterator(StringBuilder builder, CharIterator root)
		{
			_builder = builder;
			_stack = new List<CharIterator>();
			_stack.Push(root);
		}

		public StringIterator(string prefix, CharIterator root) : this(new StringBuilder(prefix ?? string.Empty), root) { }

		public StringIterator(Trie trie, string prefix = null) : this(prefix, new CharIterator(trie)) { }

		/// <summary>
		/// Return the full string at this point
		/// </summary>
		public string GetString() => _builder.ToString();

		/// <summary>
		/// Return the current CharIterator
		/// </summary>
		private CharIterator Current => _stack.Peek();

		/// <summary>
		/// Add one more char to string
		/// </summary>
		/// <returns>false, if we reached end of string</returns>
		public bool Down()
		{
			var c = NextChar();
			if (c == '\u0000')
				return false;

			_builder.Append(c);

			var next = Current.Clone();
			next.Down();
			_stack.Push(next);

			return true;
		}

		public bool Follow(char c)
		{
			var it = Current.Clone();
			do
			{
				if (it.GetChar() != c)
					continue;

				_stack.Pop();
				_stack.Push(it);
				return Down();
			}
			while (it.Alt());

			return false;
		}

		public int Follow(string s)
		{
			int idx = 0;
			for (; idx < s.Length; ++idx)
				if (!Follow(s[idx]))
					return idx;

			return idx;
		}

		public bool Unique()
		{
			if (!HasAlt())
				return true; // shortcut
			
			if (_stack.Count < 2)
				return !HasAlt(); // corner case.. not handled perfect :-/

			var it = _stack.Peek(1).Clone();
			it.Down(); // move to first sibling
			return !it.HasAlt();
		}

		public int FollowUnique(bool down = true)
		{
			int length = 0;
			while (Unique() && (down ? Down() : Up()))
				length++;

			return length;
		}

		/// <summary>
		/// Revert previous call to Down
		/// </summary>
		public bool Up()
		{
			if (_stack.Count == 1)
				return false;

			_builder.Length -= 1;
			_stack.Pop();

			return true;
		}

		/// <summary>
		/// Revert previous call to Down
		/// </summary>
		public int Up(int steps)
		{
			var remaining = steps;
			while (remaining > 0 && Up())
				remaining--;

			return steps - remaining;
		}

		public bool HasAlt() => Current.HasAlt();

		/// <summary>
		/// Replace current char with its alternative
		/// </summary>
		/// <returns>false, if no more alternatives available</returns>
		public bool Alt() => Current.Alt();

		/// <summary>
		/// Returns the next char to add to the string. Call Down to add it.
		/// </summary>
		/// <returns>The char.</returns>
		public char NextChar() => Current.GetChar();

		public StringIterator Clone() => new StringIterator(this);
	}
}
