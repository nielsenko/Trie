using NUnit.Framework;

namespace CompactTrie.Test
{
	public class TrieTest
	{
		[Test]
		public void AltNode_ReadWrite()
		{
			AltNode cn = new AltNode();
			cn.payload = 0;
			Assert.AreEqual(0, cn.payload);
			cn.payload = 1;
			Assert.AreEqual(1, cn.payload);
			cn.payload = 255;
			Assert.AreEqual(255, cn.payload);
			cn.alt = 0;
			Assert.AreEqual(0, cn.alt);
			cn.alt = 196;
			Assert.AreEqual(196, cn.alt);
			cn.alt = 0x8000;
			Assert.AreEqual(0x8000, cn.alt);
			cn.alt = 0x800000;
			Assert.AreEqual(0x800000, cn.alt);
			cn.alt = 0x654321;
			Assert.AreEqual(0x654321, cn.alt);
		}

		[Test]
		public void RecoverSmallStrings()
		{
			Trie t = new Trie();
			t.Add("ab");
			t.Add("ac");

			Assert.IsTrue(t.FindExactMatch("ab"));
			Assert.IsTrue(t.FindExactMatch("ac"));
			Assert.IsFalse(t.FindExactMatch("a")); // substring
			Assert.IsFalse(t.FindExactMatch("ad")); // not found
			Assert.IsFalse(t.FindExactMatch("abc")); // superstring

			t.Add("a");
			Assert.IsTrue(t.FindExactMatch("ab"));
			Assert.IsTrue(t.FindExactMatch("ac"));
			Assert.IsTrue(t.FindExactMatch("a")); // substring
			Assert.IsFalse(t.FindExactMatch("ad")); // not found
			Assert.IsFalse(t.FindExactMatch("abc")); // superstring
		}

		[Test]
		public void RecoverUtf8Strings()
		{
			Trie t = new Trie();
			t.Add("æble");
			t.Add("østers");
			t.Add("ål");

			Assert.IsTrue(t.FindExactMatch("æble"));
			Assert.IsTrue(t.FindExactMatch("østers"));
			Assert.IsTrue(t.FindExactMatch("ål"));
			Assert.IsFalse(t.FindExactMatch("æsel"));
			Assert.IsFalse(t.FindExactMatch("øllebrød"));
			Assert.IsFalse(t.FindExactMatch("å"));
			Assert.IsFalse(t.FindExactMatch("a"));
			Assert.IsFalse(t.FindExactMatch("ad"));

			t.Minimize();

			Assert.IsTrue(t.FindExactMatch("æble"));
			Assert.IsTrue(t.FindExactMatch("østers"));
			Assert.IsTrue(t.FindExactMatch("ål"));
			Assert.IsFalse(t.FindExactMatch("æsel"));
			Assert.IsFalse(t.FindExactMatch("øllebrød"));
			Assert.IsFalse(t.FindExactMatch("å"));
			Assert.IsFalse(t.FindExactMatch("a"));
			Assert.IsFalse(t.FindExactMatch("ad"));
		}

		[Test]
		public void DoubleAdd()
		{
			var t = new Trie();
			var str = "foobar";
			Assert.IsTrue(t.Add(str));
			Assert.IsFalse(t.Add(str), "Nothing added, if already there");
			Assert.That(t.Store.Length, Is.EqualTo(str.Length + 1));
		}

		[Test]
		public void LookupInEmpty()
		{
			var t = new Trie();
			Assert.IsFalse(t.FindExactMatch(""));
			Assert.IsFalse(t.FindExactMatch("42"));
			CharIterator ci;
			int matchLength;
			var found = t.FindLongestMatch("foobar", out ci, out matchLength);
			Assert.IsFalse(found);
			Assert.That(matchLength, Is.EqualTo(0));
		}

		#region Small datasets

		string[] streets_smallDataset = {
			"1000, København K, Købmagergade",
			"1050, København K, Kongens Nytorv",
			"1051, København K, Nyhavn",
			"1052, København K, Herluf Trolles Gade",
			"1053, København K, Cort Adelers Gade",
			"1054, København K, Peder Skrams Gade",
			"1055, København K, August Bournonvilles Passage",
			"1055, København K, Tordenskjoldsgade"
		};

		readonly string[] countries0 = {
			"Afrika",
			"Albanien", // shared prefix
			"Belgien", // unique prefix + shared suffix
			"Afrikansk Congo", // prefix superstring
			"Congo", // suffix substring
			"Co", // prefix substring of suffix substring
			"Af" // prefix substring
		};

		string[] countries1 = {
			"Afrika",
			"Albanien", // shared prefix
			"Belgien", // unique prefix + shared suffix
			"Afrikansk Congo", // prefix superstring
			"Congo", // suffix substring
			"Co" // prefix substring of suffix substring
		};

		string[] countries2 = {
			"Afrika",
			"Albanien", // shared prefix
			"Belgien", // unique prefix + shared suffix
			"Afrikansk Congo", // prefix superstring
			"Congo" // suffix substring
			// MISSING "Co" // prefix substring of suffix substring
		};

		string[] countries3 = {
			"Afrika",
			"Albanien", // shared prefix
			"Belgien", // unique prefix + shared suffix
			"Afrikansk Congo", // prefix superstring
			// MISSING "Congo" // suffix substring
			"Co" // prefix substring
		};

		// Same as countries3, but in a differnet order
		string[] countries3mix = {
			"Afrikansk Congo", // prefix superstring
			"Afrika",
			"Belgien", // unique prefix + shared suffix
			"Albanien", // shared prefix
			"Co" // prefix substring
		};

		private void Load(Trie t, string[] list)
		{
			foreach (var s in list)
			{
				Assert.That(t.Add(s));
			}
		}

		private bool DatasetIsEqual(string[] dataSet1, string[] dataSet2)
		{
			Trie t1 = new Trie();
			Trie t2 = new Trie();
			Load(t1, dataSet1);
			Load(t2, dataSet2);
			CharIterator i1 = new CharIterator(t1);
			CharIterator i2 = new CharIterator(t2);
			return i1.IsEqualTo(i2);
		}

		private bool DatasetHasSameContent(string[] dataSet1, string[] dataSet2)
		{
			var t1 = new Trie();
			var t2 = new Trie();
			Load(t1, dataSet1);
			Load(t2, dataSet2);
			CharIterator i1 = new CharIterator(t1);
			CharIterator i2 = new CharIterator(t2);
			return i1.HasSameContent(i2);
		}

		[Test]
		public void CompareTrie()
		{
			Assert.IsTrue(DatasetIsEqual(countries0, countries0));
			Assert.IsTrue(DatasetIsEqual(streets_smallDataset, streets_smallDataset));
			Assert.IsFalse(DatasetIsEqual(countries0, streets_smallDataset));
			Assert.IsFalse(DatasetIsEqual(countries0, countries1));
			Assert.IsFalse(DatasetIsEqual(countries1, countries2));
			Assert.IsFalse(DatasetIsEqual(countries1, countries3));
			Assert.IsFalse(DatasetIsEqual(countries2, countries3));
			Assert.IsFalse(DatasetIsEqual(countries3, countries3mix));

			Assert.IsTrue(DatasetHasSameContent(countries0, countries0));
			Assert.IsTrue(DatasetHasSameContent(streets_smallDataset, streets_smallDataset));
			Assert.IsFalse(DatasetHasSameContent(countries0, streets_smallDataset));
			Assert.IsFalse(DatasetHasSameContent(countries0, countries1));
			Assert.IsFalse(DatasetHasSameContent(countries1, countries2));
			Assert.IsFalse(DatasetHasSameContent(countries1, countries3));
			Assert.IsFalse(DatasetHasSameContent(countries2, countries3));
			Assert.IsTrue(DatasetHasSameContent(countries3, countries3mix));
		}

		private bool CompareAltAndNext(string[] strings)
		{
			var t1 = new Trie();
			Load(t1, strings);
			var oldStore = t1.Store;

			var newStore = new ArrayStore<NextNode>();
			t1.Minimize(newStore);

			var t2 = new Trie(oldStore);

			var ci1 = new CharIterator(t1);
			var ci2 = new CharIterator(t2);

			return ci1.HasSameContent(ci2);
		}

		[Test]
		public void CompareAltAndNext()
		{
			Assert.That(CompareAltAndNext(countries0));
			Assert.That(CompareAltAndNext(countries1));
			Assert.That(CompareAltAndNext(countries2));
			Assert.That(CompareAltAndNext(countries3));
			Assert.That(CompareAltAndNext(countries3mix));
		}

		[Test]
		public void Minimize()
		{
			var t1 = new Trie();
			Load(t1, countries0);
			var oldStore = t1.Store;

			var newStore = new ArrayStore<NextNode>();
			t1.Minimize(newStore);

			var t2 = new Trie(oldStore);

			var pm = new PrefixMatcher(t1, "");
			foreach (var p in pm)
			{
				Assert.That(t2.FindExactMatch(p), string.Format("Could not find: {0}", p));
			}

			pm = new PrefixMatcher(t2, "");
			foreach (var p in pm)
			{
				Assert.That(t1.FindExactMatch(p), string.Format("Could not find: {0}", p));
			}
		}

		[Test]
		public void Follow() 
		{
			var t = new Trie();
			Load(t, countries0);
			var s = new StringIterator(t);

			Assert.That(s.Follow('A'), Is.True);
			Assert.That(s.NextChar(), Is.EqualTo('f'));
			Assert.That(s.Follow('x'), Is.False);
			Assert.That(s.NextChar(), Is.EqualTo('f'));
			Assert.That(s.FollowUnique(), Is.EqualTo(0));
			Assert.That(s.Follow('f'), Is.True);
			Assert.That(s.FollowUnique(), Is.EqualTo(0));
			Assert.That(s.Follow("ri"), Is.EqualTo(2));
			Assert.That(s.FollowUnique(), Is.EqualTo(2));
			Assert.That(s.FollowUnique(), Is.EqualTo(0));
			Assert.That(s.GetString(), Is.EqualTo("Afrika"));
			Assert.That(s.Follow('n'), Is.True);
			Assert.That(s.Down(), Is.True);
			Assert.That(s.Up(), Is.True);
			Assert.That(s.Up(), Is.True);
			Assert.That(s.GetString(), Is.EqualTo("Afrika"));
			Assert.That(s.Follow("nskxxxxx"), Is.EqualTo(3));
			Assert.That(s.FollowUnique(), Is.EqualTo(6));
			Assert.That(s.FollowUnique(), Is.EqualTo(0));
			Assert.That(s.GetString(), Is.EqualTo("Afrikansk Congo"));
			Assert.That(s.FollowUnique(down: false), Is.EqualTo(9));
			Assert.That(s.GetString(), Is.EqualTo("Afrika"));
			Assert.That(s.FollowUnique(), Is.EqualTo(0));
			Assert.That(s.Up(), Is.True);
			Assert.That(s.GetString(), Is.EqualTo("Afrik"));
			Assert.That(s.FollowUnique(down: false), Is.EqualTo(3));
			Assert.That(s.GetString(), Is.EqualTo("Af"));
			Assert.That(s.Up(3), Is.EqualTo(2));
			Assert.That(s.GetString(), Is.Empty);
			Assert.That(s.Down(), Is.True);
			Assert.That(s.Down(), Is.True);
			Assert.That(s.Up(), Is.True);
			Assert.That(s.Up(), Is.True);
			Assert.That(s.GetString(), Is.Empty);
		}

		[Test]
		public void Unique() 
		{
			var t = new Trie();
			Load(t, countries0);
			var s = new StringIterator(t);

			Assert.That(s.Follow('A'), Is.True);
			Assert.That(s.NextChar(), Is.EqualTo('f'));
			Assert.That(s.Alt(), Is.True);
			Assert.That(s.NextChar(), Is.EqualTo('l'));
			Assert.That(s.HasAlt(), Is.False);
			Assert.That(s.Unique(), Is.False);
		}

		#endregion
	}
}

