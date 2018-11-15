using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace CompactTrie.Test
{
	public class MemoryMappedStoreTest
	{
		[Test]
		public void IndexerWorkByRefDefault()
		{
			var cts = new ArrayStore<AltNode>();
			IndexerWorkByRef(cts);
		}

		[Test]
		public void IndexerWorkByRefFileBased()
		{
			var path = Path.GetTempFileName();
			using (var cts = new MemoryMappedStore<AltNode>(path, 1000))
			{
				IndexerWorkByRef(cts);
			}
		}

		static void IndexerWorkByRef(IStore<AltNode> cts)
		{
			cts[0] = new AltNode();
			// cts[0].payload = 42; // <- Compile error!
			var n = cts[0];
			n.payload = 42;
			Assert.That(cts[0].payload, Is.EqualTo(0));
			cts[0] = n;
			Assert.That(cts[0].payload, Is.EqualTo(42));
		}

		[Test]
		public void CapacityExceededFileBased()
		{
			var path = Path.GetTempFileName();
			using (var db = MemoryMappedFile.CreateFromFile(path, FileMode.Create, null, 100))
			{
				CapacityExceeded(db);
			}
		}

		static void CapacityExceeded(MemoryMappedFile db)
		{
			using (var cts = new MemoryMappedStore<AltNode>(db))
			{
				var n = new AltNode
				{
					payload = 42
				};
				Assert.That(() => cts[101] = n, Throws.InstanceOf<ArgumentOutOfRangeException>());
				Assert.That(() => cts[101].payload == 42, Throws.InstanceOf<IndexOutOfRangeException>()); // Length is still zero!
			}
		}

		[Test]
		public void IndexOutOfBoundsDefault()
		{
			var cts = new ArrayStore<AltNode>();
			IndexOutOfBounds(cts);
		}

		[Test]
		public void IndexOutOfBoundsFileBased()
		{
			var path = Path.GetTempFileName();
			using (var cts = new MemoryMappedStore<AltNode>(path, 100))
			{
				IndexOutOfBounds(cts);
			}
		}

		internal void IndexOutOfBounds(IStore<AltNode> cts)
		{
			// var n = cts [-1]; // <- Compile error!
			Assert.That(() => cts[0], Throws.InstanceOf<IndexOutOfRangeException>());
			Assert.That(() => cts[1], Throws.InstanceOf<IndexOutOfRangeException>());

			var n = new AltNode { payload = 42 };
			cts[2] = n;

			Assert.That(cts[0], Is.EqualTo(default(AltNode)));
			Assert.That(cts[1], Is.EqualTo(default(AltNode)));
			Assert.That(cts[2], Is.EqualTo(n));
		}

		[Test]
		public void CannotCreateAccessorOnEmptyFile()
		{
			var path = Path.GetTempFileName();
			File.Create(path).Close(); // create empty file
			try
			{
				using (var db = MemoryMappedFile.CreateFromFile(path))
				{
					Assert.That(db.CreateViewAccessor, Throws.InstanceOf<IOException>());
					Assert.That(() => new MemoryMappedStore<AltNode>(db), Throws.InstanceOf<IOException>());
				}
			}
			catch (Exception ex)
			{
				Assert.That(ex, Is.InstanceOf<ArgumentException>()); // on windows CreateFromFile will fail on empty file, which is just as fine
			}
		}

		[Test]
		public void LessThanFourBytesHandledGracefully()
		{
			Assert.That(() => NBytes(1), Throws.InstanceOf<InvalidDataException>());
			Assert.That(() => NBytes(2), Throws.InstanceOf<InvalidDataException>());
			Assert.That(() => NBytes(3), Throws.InstanceOf<InvalidDataException>());
			NBytes(4); // Finally there is space
		}

		public void NBytes(int n)
		{
			var path = Path.GetTempFileName();
			int length = 0;
			using (var w = new BinaryWriter(File.Create(path)))
			{
				w.Write(Marshal.SizeOf(typeof(AltNode))); // valid node size
				w.Write(new Guid("830aa0e6-824f-410b-149c-c2502994fd0d").ToByteArray(), 0, 16); // valid node type id (AltNode)
				for (byte i = 1; i <= n; ++i)
				{
					w.Write(i); // just one byte - non-zero
					length |= i << ((i - 1) * 8);
				}
			}

			// cts.Length will need 4 bytes. If the file is shorter, then this
			// will throw ..
			using (var db = MemoryMappedFile.CreateFromFile(path))
			{
				db.CreateViewAccessor().Dispose(); // no problem here.. 
				using (var cts = new MemoryMappedStore<AltNode>(db))
					Assert.That(cts.Length, Is.EqualTo(length)); // .. otherwise should hold
			}
		}

		[Test]
		public void IllegalNodeSize()
		{
			var path = Path.GetTempFileName();
			using (var fs = File.Create(path))
			{
				fs.WriteByte(1); // something non-zero
			}

			using (var db = MemoryMappedFile.CreateFromFile(path))
			{
				Assert.That(() => new MemoryMappedStore<AltNode>(db), Throws.InstanceOf<InvalidDataException>());
			}
		}

		[Test]
		public void IllegalNodeType()
		{
			var path = Path.GetTempFileName();
			using (var w = new BinaryWriter(File.Create(path)))
			{
				w.Write(Marshal.SizeOf(typeof(AltNode)));
			}

			using (var db = MemoryMappedFile.CreateFromFile(path))
			{
				Assert.That(() => new MemoryMappedStore<AltNode>(db), Throws.InstanceOf<InvalidDataException>());
			}
		}
	}
}