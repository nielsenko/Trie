using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace CompactTrie.Test
{
	public class PrefixMatcherTest
	{
		[Test]
		public void EmptyPrefix()
		{
			var trie = new Trie();
			trie.Add("abe");
			trie.Add("abekat");
			trie.Add("abemad");
			trie.Add("aben");
			trie.Add("abens");
			var pm = new PrefixMatcher(trie, "");
			foreach (var match in pm)
			{
				Assert.That(trie.FindExactMatch(match));
			}
			Assert.That(pm.Count(), Is.EqualTo(5));

			trie.Minimize();

			foreach (var match in pm)
			{
				Assert.That(trie.FindExactMatch(match));
			}
			Assert.That(pm.Count(), Is.EqualTo(5));
		}

		[Test]
		public void NonEmptyPrefix()
		{
			var trie = new Trie();

			Assert.That(trie.Add("abe"));
			Assert.That(trie.Add("abekat"));
			Assert.That(trie.Add("abemad"));
			Assert.That(trie.Add("aben"));
			Assert.That(trie.Add("abens"));

			var pm = new PrefixMatcher(trie, "aben");
			foreach (var match in pm)
			{
				Assert.That(trie.FindExactMatch(match));
			}
			Assert.That(pm, Contains.Item("aben"));
			Assert.That(pm, Contains.Item("abens"));
			Assert.That(pm.Count(), Is.EqualTo(2));

			trie.Minimize();

			foreach (var match in pm)
			{
				Assert.That(trie.FindExactMatch(match));
			}
			Assert.That(pm, Contains.Item("aben"));
			Assert.That(pm, Contains.Item("abens"));
			Assert.That(pm.Count(), Is.EqualTo(2));
		}

		[Test]
		public void OrderIsPreserved()
		{
			var trie = new Trie();
			var entries = new[] { "abe", "abekat", "abemad", "aben", "abens" };
			foreach (var e in entries)
				Assert.That(trie.Add(e));

			var æøå = new[] { "æ", "ø", "å" };
			foreach (var e in æøå)
				Assert.That(trie.Add(e));

			var pm = new PrefixMatcher(trie, "");
			Assert.That(pm, Is.EqualTo(entries.Concat(æøå)));

			pm = new PrefixMatcher(trie, "abe");
			Assert.That(pm, Is.EqualTo(entries));

			pm = new PrefixMatcher(trie, "aben");
			Assert.That(pm, Is.EqualTo(new[] { "aben", "abens" }));

			trie.Minimize();

			Assert.That(pm, Is.EqualTo(new[] { "aben", "abens" }));

			// Danish case-sensitive string comparer
			var c = StringComparer.Create(new CultureInfo("da"), false);

			Assert.That(c.Compare("z", "æ"), Is.LessThan(0));
			Assert.That(c.Compare("Z", "æ"), Is.LessThan(0));
			Assert.That(c.Compare("æ", "æ"), Is.EqualTo(0));
			Assert.That(c.Compare("æ", "Æ"), Is.LessThan(0));
			Assert.That(c.Compare("æ", "ø"), Is.LessThan(0));
			Assert.That(c.Compare("æ", "å"), Is.LessThan(0));
			Assert.That(c.Compare("Æ", "ø"), Is.LessThan(0));
			Assert.That(c.Compare("Æ", "å"), Is.LessThan(0));
			Assert.That(c.Compare("ø", "ø"), Is.EqualTo(0));
			// Assert.That (c.Compare ("ø", "Ø"), Is.LessThan(0)); // <- bug in culture
			Assert.That(c.Compare("ø", "å"), Is.LessThan(0));
			Assert.That(c.Compare("Ø", "å"), Is.LessThan(0));
			Assert.That(c.Compare("å", "å"), Is.EqualTo(0));
			// Assert.That (c.Compare ("å", "Å"), Is.LessThan(0)); // <- bug in culture

			var dc = new DanishCharComparer();
			Assert.That(dc.Compare('z', 'æ'), Is.LessThan(0));
			Assert.That(dc.Compare('Z', 'æ'), Is.LessThan(0));
			Assert.That(dc.Compare('æ', 'æ'), Is.EqualTo(0));
			Assert.That(dc.Compare('æ', 'Æ'), Is.LessThan(0));
			Assert.That(dc.Compare('æ', 'ø'), Is.LessThan(0));
			Assert.That(dc.Compare('æ', 'å'), Is.LessThan(0));
			Assert.That(dc.Compare('Æ', 'ø'), Is.LessThan(0));
			Assert.That(dc.Compare('Æ', 'å'), Is.LessThan(0));
			Assert.That(dc.Compare('ø', 'ø'), Is.EqualTo(0));
			Assert.That(dc.Compare('ø', 'Ø'), Is.LessThan(0)); // <- bug in culture
			Assert.That(dc.Compare('ø', 'å'), Is.LessThan(0));
			Assert.That(dc.Compare('Ø', 'å'), Is.LessThan(0));
			Assert.That(dc.Compare('å', 'å'), Is.EqualTo(0));
			Assert.That(dc.Compare('å', 'Å'), Is.LessThan(0)); // <- bug in culture

			pm = new PrefixMatcher(trie, "");
			Assert.That(pm, Is.EqualTo(entries.Concat(æøå)));

			pm = new PrefixMatcher(trie, "abe");
			Assert.That(pm, Is.EqualTo(entries));

			pm = new PrefixMatcher(trie, "aben");
			Assert.That(pm, Is.EqualTo(new[] { "aben", "abens" }));
		}

		[Test]
		public void EmptyTrie()
		{
			var trie = new Trie();
			var pm = new PrefixMatcher(trie, "");
			Assert.That(pm, Is.Empty);

			trie.Minimize();

			Assert.That(pm, Is.Empty);
		}

		[Test]
		public void EmptyString()
		{
			var trie = new Trie();
			trie.Add("");

			var pm = new PrefixMatcher(trie, "");
			Assert.That(pm, Is.Not.Empty);
			Assert.That(pm.Count(), Is.EqualTo(1));
			Assert.That(pm.First(), Is.EqualTo(""));

			trie.Minimize();

			Assert.That(pm, Is.Not.Empty);
			Assert.That(pm.Count(), Is.EqualTo(1));
			Assert.That(pm.First(), Is.EqualTo(""));
		}

		[Test]
		public void StopChars()
		{
			var trie = new Trie();
			Assert.That(trie.Add("Peter Vessel"));
			Assert.That(trie.Add("Peter Vessel Christensen"));
			Assert.That(trie.Add("Peter Olsen"));
			Assert.That(trie.Add("Peter-Anton"));
			Assert.That(trie.Add("Peter-Bent"));

			var pm = new PrefixMatcher(trie, "P", new[] { ' ', '-' });
			Assert.That(pm, Is.Not.Empty);
			Assert.That(pm.Count(), Is.EqualTo(2));
			Assert.That(pm, Is.SubsetOf(new[] { "Peter ", "Peter-" }));
			Assert.That(new[] { "Peter ", "Peter-" }, Is.SubsetOf(pm));
			Assert.That(pm, Is.EqualTo(new[] { "Peter ", "Peter-" }));

			trie.Minimize();

			Assert.That(pm, Is.Not.Empty);
			Assert.That(pm.Count(), Is.EqualTo(2));
			Assert.That(pm, Is.SubsetOf(new[] { "Peter ", "Peter-" }));
			Assert.That(new[] { "Peter ", "Peter-" }, Is.SubsetOf(pm));
			Assert.That(pm, Is.EqualTo(new[] { "Peter ", "Peter-" }));
		}

		[Test]
		public void TrickyUtf8Alternatives()
		{
			var trie = new Trie();
			Assert.That(trie.Add("bæ"));
			Assert.That(trie.Add("båd"));
			Assert.That(trie.Add("bøde"));
			Assert.That(trie.Add("bØde"));
			Assert.That(trie.Add("BÆ"));
			Assert.That(trie.Add("BÅD"));
			Assert.That(trie.Add("BØDE"));

			var pm = new PrefixMatcher(trie, "b");
			Assert.That(pm, Is.Not.Empty);
			Assert.That(pm.Count(), Is.EqualTo(4));
			Assert.That(pm, Is.EqualTo(new[] { "bæ", "båd", "bøde", "bØde" }));

			var PM = new PrefixMatcher(trie, "B");
			Assert.That(PM, Is.Not.Empty);
			Assert.That(PM.Count(), Is.EqualTo(3));
			Assert.That(PM, Is.EqualTo(new[] { "BÆ", "BÅD", "BØDE" }));

			trie.Minimize();

			Assert.That(pm, Is.Not.Empty);
			Assert.That(pm.Count(), Is.EqualTo(4));
			Assert.That(pm, Is.SubsetOf(new[] { "bæ", "bøde", "bØde", "båd" }));
			Assert.That(new[] { "bæ", "bøde", "bØde", "båd" }, Is.SubsetOf(pm));

			Assert.That(PM, Is.Not.Empty);
			Assert.That(PM.Count(), Is.EqualTo(3));
			Assert.That(PM, Is.SubsetOf(new[] { "BÆ", "BØDE", "BÅD" }));
			Assert.That(new[] { "BÆ", "BØDE", "BÅD" }, Is.SubsetOf(PM));
		}

		[Test]
		public void SimpleUtf8()
		{
			var trie = new Trie();
			Assert.That(trie.Add("bæ"));
			Assert.That(trie.Add("Ø"));

			var pm = new PrefixMatcher(trie, "b");
			Assert.That(pm, Is.Not.Empty);
			Assert.That(pm.Count(), Is.EqualTo(1));
			Assert.That(pm.First(), Is.EqualTo("bæ"));

			var PM = new PrefixMatcher(trie, "");
			Assert.That(PM, Is.Not.Empty);
			Assert.That(PM.Count(), Is.EqualTo(2));
			Assert.That(PM, Is.SubsetOf(new[] { "bæ", "Ø" }));
			Assert.That(new[] { "bæ", "Ø" }, Is.SubsetOf(PM));
			Assert.That(PM, Is.EqualTo(new[] { "bæ", "Ø" }));

			trie.Minimize();

			Assert.That(pm, Is.Not.Empty);
			Assert.That(pm.Count(), Is.EqualTo(1));
			Assert.That(pm.First(), Is.EqualTo("bæ"));

			Assert.That(PM, Is.Not.Empty);
			Assert.That(PM.Count(), Is.EqualTo(2));
			Assert.That(PM, Is.SubsetOf(new[] { "bæ", "Ø" }));
			Assert.That(new[] { "bæ", "Ø" }, Is.SubsetOf(PM));
			Assert.That(PM, Is.EqualTo(new[] { "bæ", "Ø" }));
		}

		[Test]
		public void NoMatch()
		{
			var trie = new Trie();
			Assert.That(trie.Add("æø"));
			Assert.That(trie.Add("åø"));

			var pm = new PrefixMatcher(trie, "P");
			Assert.That(pm.Count(), Is.EqualTo(0));

			trie.Minimize();

			Assert.That(pm.Count(), Is.EqualTo(0));
		}
	}
}
