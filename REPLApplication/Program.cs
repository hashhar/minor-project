using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using EditCorrector;
using FeatureExtractor;
using FeatureExtractor.Abstract;

namespace REPLApplication
{
	internal class Program
	{
		private static readonly bool UseEditCorrection = ConfigurationManager.AppSettings.Get("editCorrection").ToLower() == "true";
		private static readonly string SolutionPath =
			ContextSensitiveSpellingCorrection.ContextSensitiveSpellingCorrection.SolutionPath;

		private static string ReturnNWord(int n, string str)
		{
			var words = str.Split(' ');
			return words[n];
		}

		private static void Main()
		{
			Console.WriteLine("Starting application. It may take some time to load dictionaries.");
			var editCorrector = new Spelling();
			var confusionSets = GetConfusionSets();
			var trainingCorpora = GetCorpora();
			IPosTagger posTagger = new PosTagger();
			var contextSensitiveSpellingCorrection =
				new ContextSensitiveSpellingCorrection.ContextSensitiveSpellingCorrection(posTagger, trainingCorpora, confusionSets,
					false);
			Console.WriteLine("You can start now.");
			while (true)
			{
				var testSentence = Console.ReadLine();
				if (string.IsNullOrEmpty(testSentence))
					return;
				var defaultColor = Console.BackgroundColor;
				// Edit distance prediction
				Dictionary<int, string> wordsList;
				if (UseEditCorrection)
				{
					Console.WriteLine("EditCorrection: ");
					Console.BackgroundColor = ConsoleColor.DarkGreen;
					var editCorrectedSentence = testSentence;
					foreach (var item in testSentence.Split(' '))
					{
						var correctedItem = editCorrector.Correct(item);
						if (correctedItem != item.ToLower())
							Console.WriteLine(item + " : " + correctedItem);
						editCorrectedSentence = editCorrectedSentence.Replace(item, correctedItem);
					}
					Console.WriteLine(editCorrectedSentence);

					// Context + EditCorrection Prediction
					var contextEditCorrectedSentence = editCorrectedSentence;
					wordsList = contextSensitiveSpellingCorrection.Predict(contextEditCorrectedSentence);
					Console.BackgroundColor = defaultColor;
					Console.WriteLine("EditCorrection + ContextCorrection: ");
					Console.BackgroundColor = ConsoleColor.DarkCyan;
					foreach (var word in wordsList)
					{
						var wrongWord = ReturnNWord(word.Key, editCorrectedSentence);
						Console.WriteLine(wrongWord + " : " + word.Value);
						contextEditCorrectedSentence = contextEditCorrectedSentence.Replace(wrongWord, word.Value);
					}
					Console.WriteLine(contextEditCorrectedSentence);
				}
				// Context Prediction
				var contextCorrectedSentence = testSentence;
				wordsList = contextSensitiveSpellingCorrection.Predict(contextCorrectedSentence);
				Console.BackgroundColor = defaultColor;
				Console.WriteLine("Context Correction: ");
				Console.BackgroundColor = ConsoleColor.DarkCyan;
				foreach (var word in wordsList)
				{
					var wrongWord = ReturnNWord(word.Key, contextCorrectedSentence);
					Console.WriteLine(wrongWord + " : " + word.Value);
					contextCorrectedSentence = contextCorrectedSentence.Replace(wrongWord, word.Value);
				}
				Console.WriteLine(contextCorrectedSentence);
				Console.BackgroundColor = defaultColor;
				Console.WriteLine("-------------------");
			}
		}

		private static IEnumerable<string[]> GetConfusionSets()
		{
			return new List<string[]>
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
		}

		private static IEnumerable<string> GetCorpora()
		{
			var corpus = File.ReadAllText(Path.Combine(SolutionPath, @"Corpus\Release Corpus.txt"));
			return corpus.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}