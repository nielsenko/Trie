using System.Collections.Generic;

namespace CompactTrie
{
	public sealed class DanishCharComparer : IComparer<char>
	{
		readonly IComparer<char> _default = Comparer<char>.Default;
		const string _orderOfDanishLetters = "æÆøØåÅ";

		#region IComparer implementation

		public int Compare(char x, char y)
		{
			switch (x)
			{
				case 'æ':
				case 'Æ':
				case 'å':
				case 'Å':
				case 'ø':
				case 'Ø':
					switch (y)
					{
						case 'æ':
						case 'ø':
						case 'å':
						case 'Æ':
						case 'Ø':
						case 'Å':
							return _orderOfDanishLetters.IndexOf(x) - _orderOfDanishLetters.IndexOf(y);
						default:
							return 1;
					}

				default:
					switch (y)
					{
						case 'æ':
						case 'ø':
						case 'å':
						case 'Æ':
						case 'Ø':
						case 'Å':
							return -1;
						default:
							return _default.Compare(x, y);
					}
			}
		}

		#endregion
	}
}
