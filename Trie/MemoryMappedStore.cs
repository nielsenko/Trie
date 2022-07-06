using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;

namespace CompactTrie
{
	/// <summary>
	/// A Memory mapped store allows you to work with memory mapped regions almost like an array.
	/// </summary>
	public class MemoryMappedStore<TNode> : IStore<TNode>, IDisposable where TNode : struct, INode
	{
		static readonly MD5 Md5 = MD5.Create();

		//
		// using typeof(TNode).GUID does not work across runtimes. Instead we base the guid on an md5 hash of
		// the fullname of the node type.
		//
		public static readonly Guid NodeType = new Guid(Md5.ComputeHash(Encoding.UTF8.GetBytes(typeof(TNode).FullName)));
		public static readonly int NodeSize = Marshal.SizeOf(typeof(TNode));

		const int NodeSizeOffset = 0;
		const int NodeTypeOffset = sizeof(int);
		const int NodeTypeLength = 16; // Md5 hash is 16 bytes
		const int LengthOffset = NodeTypeOffset + NodeTypeLength;
		const int DataOffset = LengthOffset + sizeof(int);

		MemoryMappedViewAccessor _accessor;

		public MemoryMappedStore(string path, int capacity, FileMode mode = FileMode.OpenOrCreate) : this(MemoryMappedFile.CreateFromFile(path, mode, null, capacity)) { }

		public MemoryMappedStore(MemoryMappedFile db)
		{
			_accessor = db.CreateViewAccessor();
			try
			{
				if (_accessor.Capacity < DataOffset)
					throw new InvalidDataException($"Capacity must be larger than header ({DataOffset})");

				var nodeSize = _accessor.ReadInt32(NodeSizeOffset);
				if (nodeSize > 0)
				{
					if (NodeSize != nodeSize)
						throw new InvalidDataException($"Node size (expected) {NodeSize} != {nodeSize} (actual)");

					var buffer = new byte[NodeTypeLength];
					_accessor.ReadArray(NodeTypeOffset, buffer, 0, buffer.Length);
					var nodeType = new Guid(buffer);
					if (NodeType != nodeType)
						throw new InvalidDataException($"Node type (expected) {NodeType} != {nodeType} (actual)");

					// Assume this file was build by MemoryMappedStore
					Length = _accessor.ReadInt32(LengthOffset);
				}
				else
				{ // assume this is a new file
				  // Write tags
					_accessor.Write(NodeSizeOffset, NodeSize);
					_accessor.WriteArray(NodeTypeOffset, NodeType.ToByteArray(), 0, NodeTypeLength);
				}
			}
			catch
			{
				_accessor.Dispose();
				throw;
			}
		}

		long Project(uint inx)
		{
			return inx * NodeSize + DataOffset;
		}

		public int Length { get; set; }

		public TNode this[uint inx]
		{
			get
			{
				if (inx >= Length)
					throw new IndexOutOfRangeException($"inx: {inx} >= {Length} :Length");

				_accessor.Read(Project(inx), out TNode n);
				return n;
			}
			set
			{
				_accessor.Write(Project(inx), ref value);
				Length = (int)Math.Max(Length, inx + 1);
			}
		}

		public void Flush()
		{
			_accessor.Write(LengthOffset, Length);
			_accessor.Flush();
		}

		public IByteIterator CreateIterator()
		{
			var n = new TNode();
			return n.CreateIterator(this);
		}

		#region IDisposable implementation

		public void Dispose()
		{
			if (_accessor != null)
			{
				Flush();
				_accessor.Dispose();
				_accessor = null;
			}
		}

		#endregion
	}
}