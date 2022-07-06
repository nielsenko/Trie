using NUnit.Framework;

namespace CompactTrie.Test
{
	public class NextNodeIteratorTest
	{
		[Test]
		public void IsValid()
		{
			var store = new ArrayStore<NextNode>();
			Assert.That(store.Length, Is.EqualTo(0));

			var ni = new NextNodeIterator(store);
			Assert.That(!ni.IsValid());

			store[1] = new NextNode { payload = 42 }; // add a node (store[0] contains termination node) ..
			Assert.That(!ni.IsValid()); // .. but still invalid

			ni = new NextNodeIterator(store); // take a new one ..
			Assert.That(ni.IsValid()); // .. should be valid now
			Assert.That(ni.GetByte(), Is.EqualTo(42));
		}

		[Test]
		public void Alt()
		{
			var store = new ArrayStore<NextNode>();
			const int max = 42;
			for (byte i = 1; i <= max; ++i)
			{ // note store[0] contains termination
				store[i] = new NextNode { payload = i };
			}

			var n = store[1];
			n.noAlt = true;
			store[1] = n;

			var ni = new NextNodeIterator(store);
			Assert.That(ni.IsValid());

			for (byte i = max; i > 0; --i)
			{
				Assert.That(ni.GetByte(), Is.EqualTo(i));
				Assert.That(ni.Alt() || i == 1);
			}

			Assert.That(!ni.Alt());
		}

		[Test]
		public void Next()
		{
			var store = new ArrayStore<NextNode>();
			const int max = 42;
			for (uint i = 1; i <= max; ++i)
			{ // note store[0] contains termination
				store[i] = new NextNode { payload = (byte)i, next = i - 1, noAlt = true };
			}

			var ni = new NextNodeIterator(store);
			Assert.That(ni.IsValid());

			for (byte i = max; i > 0; --i)
			{
				Assert.That(ni.GetByte(), Is.EqualTo(i));
				Assert.That(ni.Next());
			}

			Assert.That(!ni.Next());
		}

		[Test]
		public void NextNode()
		{
			var n = new NextNode();
			Assert.That(n.noAlt, Is.False);
			Assert.That(n.payload, Is.EqualTo(0));
			Assert.That(n.next, Is.EqualTo(0));

			n.noAlt = true;
			Assert.That(n.noAlt);

			n.payload = 42;
			Assert.That(n.payload, Is.EqualTo(42));

			n.next = 1 << 22;
			Assert.That(n.next, Is.EqualTo(1 << 22));
			Assert.That(n.noAlt);

			n.noAlt = false;
			Assert.That(n.noAlt, Is.False);

			n.noAlt = true;
			Assert.That(n.noAlt);
			Assert.That(n.next, Is.EqualTo(1 << 22));

			n.noAlt = false;
			Assert.That(n.noAlt, Is.False);
			Assert.That(n.next, Is.EqualTo(1 << 22));
		}
	}
}
