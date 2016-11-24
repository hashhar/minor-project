using System;
using System.Collections.Generic;
using System.Linq;
using FeatureSelector.Models;

namespace FeatureSelector
{
	public class StatsHelper
	{
		public Dictionary<string, Dictionary<string, int>> GetFrequencies(Sentence[] documents)
		{
			var featureFrequencies = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

			for (var i = 0; i < documents.Length; ++i)
				foreach (var t in documents[i].Words)
					AddTermFrequency(featureFrequencies, t, i);

			return featureFrequencies;
		}

		public Dictionary<string, Stats> GetFeaturesStats(HashSet<string> wordFeatures,
			Dictionary<string, Dictionary<string, int>> termFrequencies, string classification, int numberOfSentences)
		{
			var statsDictionary = new Dictionary<string, Stats>(wordFeatures.Count, StringComparer.OrdinalIgnoreCase);

			foreach (var term in wordFeatures)
			{
				if (!termFrequencies.ContainsKey(classification))
					continue;

				var docsContainTerm = termFrequencies[term].Keys;

				var docsContainClass = termFrequencies[classification].Keys;

				double n11 = docsContainTerm.Intersect(docsContainClass).Count(); //Both occured
				double n10 = docsContainTerm.Except(docsContainClass).Count(); //Term occurs but class doesnt
				double n01 = docsContainClass.Except(docsContainTerm).Count(); //Class occurs but term doesnt
				var n00 = numberOfSentences - (n11 + n10 + n01); //Neither occured

				statsDictionary.Add(term, new Stats
				{
					N = numberOfSentences,
					N11 = n11,
					N10 = n10,
					N01 = n01,
					N00 = n00
				});
			}

			return statsDictionary;
		}

		private static void AddTermFrequency(Dictionary<string, Dictionary<string, int>> termFrequencies, string token,
			int docName)
		{
			if (!termFrequencies.ContainsKey(token))
				termFrequencies[token] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			if (!termFrequencies[token].ContainsKey(docName.ToString()))
				termFrequencies[token][docName.ToString()] = 0;

			++termFrequencies[token][docName.ToString()];
		}
	}
}