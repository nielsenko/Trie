using System;

namespace CompactTrie
{
	public class ArrayStore<TNode> : IStore<TNode> where TNode : struct, INode
	{
		TNode[] _data;

		public ArrayStore()
		{
			_data = new TNode[1]; // ensure space for at least one node. Will grow on demand.
			Length = 0;
		}

		void ResizeIfNeeded(uint inx)
		{
			int l = _data.Length;
			if (inx >= l)
			{
				while (inx >= l)
					l *= 2;
				Array.Resize(ref _data, l);
			}
		}

		public TNode this[uint inx]
		{
			get
			{
				if (inx < Length)
					return _data[inx];

				throw new IndexOutOfRangeException(string.Format("inx: {0} >= {1} :Length", inx, Length));
			}
			set
			{
				ResizeIfNeeded(inx);
				_data[inx] = value;
				Length = (int)Math.Max(Length, inx + 1);
			}
		}

		public int Length { get; private set; }

		public IByteIterator CreateIterator()
		{
			return _data[0].CreateIterator(this);
		}

		public void Commit()
		{
			// no-op
		}
	}
}
