using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FeatureExtractor;
using FeatureExtractor.Abstract;

namespace ConsoleClient
{
	internal class Program
	{
		private static readonly bool Prune = ConfigurationManager.AppSettings.Get("pruning").ToLower() == "true";
		private static readonly string SolutionPath =
			ContextSensitiveSpellingCorrection.ContextSensitiveSpellingCorrection.SolutionPath;
		private static readonly bool refreshXmlFiles = ConfigurationManager.AppSettings.Get("refreshXmlFiles").ToLower() == "true";

		private static readonly Stopwatch Stopwatch = new Stopwatch();

		private static void Main()
		{
			IEnumerable<string[]> confusionSets = GetConfusionSets();
			IEnumerable<string> trainingCorpora = GetCorpora();

			var testData = GetTestData()
				.GroupBy(s => s.ConfusionSet)
				.ToDictionary(s => s.Key, s => s.ToArray());

			IPosTagger posTagger = new PosTagger();
			Stopwatch.Start();
			Console.WriteLine("Started...." + DateTime.Now);

			var contextSensitiveSpellingCorrection =
				new ContextSensitiveSpellingCorrection.ContextSensitiveSpellingCorrection(posTagger, trainingCorpora, confusionSets,
					Prune, refreshXmlFiles);

			Console.WriteLine("feature extraction + training took {0} Minutes", Stopwatch.Elapsed.TotalMinutes);
			int totalWrongPredictions = 0;

			Console.WriteLine("Pruning:{0}", Prune ? "On" : "Off");

			string csvPath = Path.Combine(SolutionPath, "Results.csv");

			if (File.Exists(csvPath))
				File.Delete(csvPath);

			foreach (string set in testData.Keys)
			{
				int wrongPredictions = 0;

				Parallel.For(0, testData[set].Length, i =>
				{
					TestCase test = testData[set][i];
					Dictionary<int, string> wordsList = contextSensitiveSpellingCorrection.Predict(test.Sentence);
					bool correctAnswer = wordsList.Values.Contains(test.CorrectWord, StringComparer.OrdinalIgnoreCase);

					if (!correctAnswer)
					{
						Interlocked.Increment(ref wrongPredictions);
						Interlocked.Increment(ref totalWrongPredictions);
					}
				});

				WriteToCsv(csvPath, set, wrongPredictions, testData[set].Length);
			}

			Console.WriteLine("----------------------------------------------");
			Console.Write("Test ");
			DisplayStats(totalWrongPredictions, testData.Sum(t => t.Value.Length));
		}

		private static void WriteToCsv(string csvPath, string set, double wrongPredictions, int totalTestsCount)
		{
			double failures = 100 * (wrongPredictions / totalTestsCount);
			double accuracy = 100 - failures;

			using (var sw = new StreamWriter(csvPath, true))
			{
				sw.WriteLine(set.Replace(',', '-') + "," + accuracy);
			}
		}

		private static void DisplayStats(double wrongPredictions, double count)
		{
			double failures = 100 * (wrongPredictions / count);
			Console.WriteLine("Accuracy: {0:00} % of {1} test samples, took: {2:00 Minutes}", 100 - failures, count,
				Stopwatch.Elapsed.TotalMinutes);
		}

		private static IEnumerable<string[]> GetConfusionSets()
		{
			IEnumerable<string[]> confusionSets = new List<string[]>
			{
				new[] {"peace", "piece"},
				new[] {"where", "were"},
				new[] {"hour", "our"},
				new[] {"by", "buy", "bye"},
				new[] {"cite", "site", "sight"},
				new[] {"coarse", "course"},
				new[] {"desert", "dessert"},
				new[] {"knew", "new"},
				new[] {"hear", "here"},
				new[] {"vain", "vane", "vein"},
				new[] {"loose", "lose"},
				new[] {"plaine", "plane"},
				new[] {"principal", "principle"},
				new[] {"sea", "see"},
				new[] {"quiet", "quit", "quite"},
				new[] {"rain", "reign", "rein"},
				new[] {"waist", "waste"},
				new[] {"weak", "week"},
				new[] {"weather", "whether"},
				new[] {"fourth", "forth"},
				new[] {"passed", "past"},
				new[] {"council", "counsel"},
				new[] {"complement", "compliment"},
				new[] {"their", "there"},
				new[] {"later", "latter"},
				new[] {"threw", "through"},
				new[] {"to", "too", "two"},
				new[] {"brake", "break"}
			};

			return confusionSets;
		}

		private static IEnumerable<string> GetCorpora()
		{
			string corpus = File
				.ReadAllText(Path.Combine(SolutionPath, @"Corpus\Release Corpus.txt"));

			return corpus.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		}

		private static IEnumerable<TestCase> GetTestData()
		{
			IEnumerable<string> lines = File.ReadAllText(Path.Combine(SolutionPath, @"Corpus\Test.txt"))
				.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (string line in lines)
			{
				int pipeIdx = line.LastIndexOf('|');
				int commaIdx = pipeIdx;
				for (; commaIdx > -1; commaIdx--)
					if (line[commaIdx] == ',')
						break;

				string sentence = line.Substring(0, commaIdx);
				string correctWord = line.Substring(commaIdx + 1, pipeIdx - (commaIdx + 1));
				string confusionSet = line.Substring(pipeIdx + 1);

				yield return new TestCase(sentence, correctWord, confusionSet);
			}
		}

		private class TestCase
		{
			public TestCase(string sentence, string correctWord, string confusionSet)
			{
				Sentence = sentence;
				CorrectWord = correctWord;
				ConfusionSet = confusionSet;
			}

			public string Sentence { get; }
			public string CorrectWord { get; }
			public string ConfusionSet { get; }
		}
	}
}