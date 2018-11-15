using System.Collections.Generic;

namespace CompactTrie
{
	public static class ListEx
	{
		public static T Pop<T>(this List<T> list)
		{
			int idx = list.Count - 1;
			var result = list[idx];
			list.RemoveAt(idx);
			return result;
		}

		public static void Push<T>(this List<T> list, T item)
		{
			list.Add(item);
		}

		public static T Peek<T>(this List<T> list, int idx = 0)
		{
			return list[list.Count - 1 - idx];
		}
	}
}
