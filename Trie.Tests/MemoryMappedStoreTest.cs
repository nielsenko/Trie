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
		public void IndexerWorkByRefMemoryMapped()
		{
			using (var db = MemoryMappedFile.CreateNew("test", 100))
			{
				var cts = new MemoryMappedStore<AltNode>(db);
				IndexerWorkByRef(cts);
			}
		}

		[Test]
		public void IndexerWorkByRefFileBased()
		{
			var path = Path.GetTempFileName();
			using (var fs = File.Create(path))
			{
				fs.WriteByte(0); // add a zero byte, or creating a view accessor will fail!
			}

			using (var db = MemoryMappedFile.CreateFromFile(path))
			using (var cts = new MemoryMappedStore<AltNode>(db))
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
		public void CapacityExceededMemoryMapped()
		{
			using (var db = MemoryMappedFile.CreateNew("test", 100))
			{
				CapacityExceeded(db);
			}
		}

		[Test]
		public void CapacityExceededFileBased()
		{
			var path = Path.GetTempFileName();
			using (var fs = File.Create(path))
			{
				fs.WriteByte(0); // add a zero byte, or creating a view accessor will fail!
			}

			using (var db = MemoryMappedFile.CreateFromFile(path))
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
				cts[101] = n;
				// actually this exceeds capacity by far, since node is 4 bytes..
				Assert.That(cts[101].payload, Is.EqualTo(42));
				// .. but it doesn't matter!
			}
		}

		[Test]
		public void IndexOutOfBoundsDefault()
		{
			var cts = new ArrayStore<AltNode>();
			IndexOutOfBounds(cts);
		}

		[Test]
		public void IndexOutOfBoundsMemoryMapped()
		{
			using (var db = MemoryMappedFile.CreateNew("test", 100))
			using (var cts = new MemoryMappedStore<AltNode>(db))
			{
				IndexOutOfBounds(cts);
			}
		}

		[Test]
		public void IndexOutOfBoundsFileBased()
		{
			var path = Path.GetTempFileName();
			using (var fs = File.Create(path))
			{
				fs.WriteByte(0); // add a zero byte, or creating a view accessor will fail!
			}

			using (var db = MemoryMappedFile.CreateFromFile(path))
			using (var cts = new MemoryMappedStore<AltNode>(db))
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
					Assert.That(() => db.CreateViewAccessor(), Throws.InstanceOf<IOException>());
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
			NBytes(1);
			NBytes(2);
			NBytes(3);
			NBytes(4);
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

			// cts.Length will always read 4 bytes, even if the file is shorter
			// If the bytes that are past the last byte are initialized to zero, then this
			// test will pass
			using (var db = MemoryMappedFile.CreateFromFile(path))
			{
				db.CreateViewAccessor().Dispose(); // no problem here.. 
				using (var cts = new MemoryMappedStore<AltNode>(db))
					Assert.That(cts.Length, Is.EqualTo(length));
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