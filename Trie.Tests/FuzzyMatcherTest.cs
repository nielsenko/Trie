using NUnit.Framework;

using System;
using System.IO;

namespace CompactTrie.Test
{
	public class FuzzyMatcherTest
	{
		[Test]
		public void CaseInsensitiveMatch()
		{
			var trie = new Trie();
			const string text = "AaBbCcÆæØøÅå";

			trie.Add(text);

			var fuzzy = new FuzzyMatcher(trie)
			{
				Comparer = new CaseInsensitiveCharComparer(),
				MaxDistance = text.Length
			};

			Assert.That(fuzzy.FindBestMatch("aabb", out var dist), Is.EqualTo(text));
			Assert.That(dist, Is.EqualTo(text.Length - 4));

			Assert.That(fuzzy.FindBestMatch("AABB", out dist), Is.EqualTo(text));
			Assert.That(dist, Is.EqualTo(text.Length - 4));

			Assert.That(fuzzy.FindBestMatch("aabbccææøøåå", out dist), Is.EqualTo(text));
			Assert.That(dist, Is.Zero);

			Assert.That(fuzzy.FindBestMatch("AAåå", out dist), Is.EqualTo(text));
			Assert.That(dist, Is.EqualTo(text.Length - 4));
		}

		[Test]
		public void SimpleExactStringMatch()
		{
			var trie = new Trie();
			trie.Add("A");
			trie.Add("B");
			var sm = new FuzzyMatcher(trie);

			// No changes
			string found;
			found = sm.FindBestMatch("B", out int dist);
			Assert.AreEqual("B", found, "Could not find exact match");
			Assert.AreEqual(0, dist, "Unexpected distance");

			found = sm.FindBestMatch("A", out dist);
			Assert.AreEqual("A", found, "Could not find exact match");
			Assert.AreEqual(0, dist, "Unexpected distance");

			trie.Add("AA");
			trie.Add("BB");

			found = sm.FindBestMatch("AA", out dist);
			Assert.AreEqual("AA", found, "Could not find exact match");
			Assert.AreEqual(0, dist, "Unexpected distance");

			found = sm.FindBestMatch("BB", out dist);
			Assert.AreEqual("BB", found, "Could not find exact match");
			Assert.AreEqual(0, dist, "Unexpected distance");
		}

		[Test]
		public void ExactStringMatch()
		{
			var trie = new Trie();
			trie.Add("Herluf Trolles Gade");
			trie.Add("August Bournonvilles Passage");
			trie.Add("Tordenskjoldsgade");
			trie.Add("Heibergsgade");
			var sm = new FuzzyMatcher(trie);

			string input;
			string found;
			string expected;
			int exp_dist;

			// No changes
			input = "Tordenskjoldsgade";
			expected = "Tordenskjoldsgade";
			exp_dist = 0;
			found = sm.FindBestMatch(input, out int act_dist);
			Assert.AreEqual(expected, found, "Could not find exact match");
			Assert.AreEqual(exp_dist, act_dist, "Unexpected distance");

			// Not found (= match shortest string in set)
			input = "Cort Adelers Gade";
			expected = input; exp_dist = 0;
			found = sm.FindBestMatch(input, out act_dist);
			Assert.AreNotEqual(expected, found, "Matched unknown string");
			Assert.AreNotEqual(exp_dist, act_dist, "Matched unknown string");
		}

		[Test]
		public void AproximateStringMatch()
		{
			var trie = new Trie();
			trie.Add("Herluf Trolles Gade");
			trie.Add("Cort Adelers Gade");
			trie.Add("Peder Skrams Gade");
			trie.Add("August Bournonvilles Passage");
			trie.Add("Tordenskjoldsgade");
			trie.Add("Heibergsgade");
			trie.Add("Holbergsgade");
			var sm = new FuzzyMatcher(trie);

			string input;
			string found;
			string expected;
			int exp_dist;

			// Change case = 1 exchange
			input = "Herluf Trolles gade";
			expected = "Herluf Trolles Gade"; exp_dist = 1;
			found = sm.FindBestMatch(input, out int act_dist);
			Assert.AreEqual(exp_dist, act_dist, "Unexpected distance");
			Assert.AreEqual(expected, found, "Could not match change of case");

			// Insert 2 chars
			input = "Peder Skramels Gade";
			expected = "Peder Skrams Gade"; exp_dist = 2;
			found = sm.FindBestMatch(input, out act_dist);
			Assert.AreEqual(exp_dist, act_dist, "Unexpected distance");
			Assert.AreEqual(expected, found, "Could not match 2 insertions");

			// 1 del, 1 xchg, 2 del
			input = "August Bornorvils Passage";
			expected = "August Bournonvilles Passage"; exp_dist = 4;
			found = sm.FindBestMatch(input, out act_dist);
			Assert.AreEqual(exp_dist, act_dist, "Unexpected distance");
			Assert.AreEqual(expected, found, "Could not match 1del, 1xchg, 2del");
		}

		[Test]
		public void AproximateStringMatcher_LargeDataset()
		{
			var t = new System.Diagnostics.Stopwatch();
			var trie = new Trie();

			// Load file (Will throw an exception if file not found)
			t.Restart();
			int adrCount = 0;
			using (StreamReader sr = new StreamReader(Path.Combine(TestContext.CurrentContext.TestDirectory, "Streets.txt")))
			{
				while (!sr.EndOfStream)
				{
					adrCount++;
					string[] e = sr.ReadLine().Split(new char[] { ',' }, 3);
					string s = e[2].Trim() + "#" + e[0].Trim() + "#" + e[1].Trim();
					trie.Add(s);
				}
			}
			t.Stop();
			Console.WriteLine("{0} lines read from file and inserted in trie in {1} ms", adrCount, t.ElapsedMilliseconds);
			Assume.That(adrCount > 10000, "Only read {0} lines from file", adrCount);

			var sm = new FuzzyMatcher(trie);

			var probeList = new[] {
				new {title = "No changes",
					input = "Tordenskjoldsgade#1055#København K",
					expected = "Tordenskjoldsgade#1055#København K",
					exp_dist = 0 },
				new {title = "Change case = 1 exchange",
					input = "Herluf Trolles gade#9000#Aalborg",
					expected = "Herluf Trolles Gade#9000#Aalborg",
					exp_dist = 1 },
				new {title = "Insert 2 chars",
					input = "Peder Skramels Gade#1054#København K",
					expected = "Peder Skrams Gade#1054#København K",
					exp_dist = 2 },
				new {title = "Insert 2 chars",
					input = "Peder Skramels Gade#1054#København K",
					expected = "Peder Skrams Gade#1054#København K",
					exp_dist = 2 },
				new {title = "1 del, 1 xchg, 2 del",
					input = "August Bornorvils Passage#1055#København K",
					expected = "August Bournonvilles Passage#1055#København K",
					exp_dist = 4 },
				new {title = "2 late insert",
					input = "August Bournonvilles Passage#1055#København KKK",
					expected = "August Bournonvilles Passage#1055#København K",
					exp_dist = 2 },
				new {title = "1 early insert",
					input = "PAugust Bournonvilles Passage#1055#København K",
					expected = "August Bournonvilles Passage#1055#København K",
					exp_dist = 1 } /*,
				new {title = "Not Found", 
					input = "XXXXXXXXXXXXXXXXX#0000#Odense", 
					expected = null, 
					exp_dist = 20 } */
			};

			foreach (var probe in probeList)
			{
				string found;
				long elapsed_ms;
				const long budget_ms = 250;

				t.Restart();
				found = sm.FindBestMatch(probe.input, out int act_dist);
				t.Stop();
				elapsed_ms = t.ElapsedMilliseconds;

				Console.WriteLine("---- Large dict search - {0} ----", probe.title);
				Console.WriteLine("Search string: " + probe.input);
				Console.WriteLine("Found string: " + found);
				Console.WriteLine("Distance: {0}", act_dist);
				Console.WriteLine("Elapsed: {0} ms", elapsed_ms);
				Console.WriteLine("Nodes searched: {0} -> {1:0} ns/node", sm.NodesSearched, (elapsed_ms * 1000.0) / sm.NodesSearched);

				Assert.AreEqual(Math.Min(probe.exp_dist, sm.MaxDistance + 1), act_dist, "Unexpected distance");
				Assert.AreEqual(probe.expected, found, "Failed on " + probe.title);
				Assert.That(elapsed_ms, Is.LessThan(budget_ms), "Too slow");
			}
		}
	}
}
