using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EditCorrector
{
	public class Spelling
	{
		private static readonly Regex WordRegex = new Regex("[a-z]+", RegexOptions.Compiled);

		private static readonly string SolutionPath =
			ContextSensitiveSpellingCorrection.ContextSensitiveSpellingCorrection.SolutionPath;

		private static readonly string BigCorpus = Path.Combine(SolutionPath, @"Corpus\big.txt");
		private readonly Dictionary<string, int> _dictionary = new Dictionary<string, int>();

		public Spelling()
		{
			var fileContent = File.ReadAllText(BigCorpus);
			var wordList = fileContent.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries).ToList();

			foreach (var word in wordList)
			{
				var trimmedWord = word.Trim().ToLower();
				if (!WordRegex.IsMatch(trimmedWord)) continue;
				if (_dictionary.ContainsKey(trimmedWord))
					_dictionary[trimmedWord]++;
				else
					_dictionary.Add(trimmedWord, 1);
			}
		}

		public string Correct(string word)
		{
			if (string.IsNullOrEmpty(word))
				return word;

			word = word.ToLower();

			// known()
			if (_dictionary.ContainsKey(word))
				return word;

			var list = Edits(word);
			var candidates = new Dictionary<string, int>();

			foreach (var wordVariation in list)
				if (_dictionary.ContainsKey(wordVariation) && !candidates.ContainsKey(wordVariation))
					candidates.Add(wordVariation, _dictionary[wordVariation]);

			if (candidates.Count > 0)
				return candidates.OrderByDescending(x => x.Value).First().Key;

			// known_edits2()
			foreach (var item in list)
				foreach (var wordVariation in Edits(item))
					if (_dictionary.ContainsKey(wordVariation) && !candidates.ContainsKey(wordVariation))
						candidates.Add(wordVariation, _dictionary[wordVariation]);

			return candidates.Count > 0 ? candidates.OrderByDescending(x => x.Value).First().Key : word;
		}

		private List<string> Edits(string word)
		{
			var splits = new List<Tuple<string, string>>();
			var transposes = new List<string>();
			var deletes = new List<string>();
			var replaces = new List<string>();
			var inserts = new List<string>();

			// Splits
			for (var i = 0; i < word.Length; i++)
			{
				var tuple = new Tuple<string, string>(word.Substring(0, i), word.Substring(i));
				splits.Add(tuple);
			}

			// Deletes
			foreach (var t in splits)
			{
				var a = t.Item1;
				var b = t.Item2;
				if (!string.IsNullOrEmpty(b))
					deletes.Add(a + b.Substring(1));
			}

			// Transposes
			foreach (var t in splits)
			{
				var a = t.Item1;
				var b = t.Item2;
				if (b.Length > 1)
					transposes.Add(a + b[1] + b[0] + b.Substring(2));
			}

			// Replaces
			foreach (var t in splits)
			{
				var a = t.Item1;
				var b = t.Item2;
				if (!string.IsNullOrEmpty(b))
					for (var c = 'a'; c <= 'z'; c++)
						replaces.Add(a + c + b.Substring(1));
			}

			// Inserts
			foreach (var t in splits)
			{
				var a = t.Item1;
				var b = t.Item2;
				for (var c = 'a'; c <= 'z'; c++)
					inserts.Add(a + c + b);
			}

			return deletes.Union(transposes).Union(replaces).Union(inserts).ToList();
		}
	}
}