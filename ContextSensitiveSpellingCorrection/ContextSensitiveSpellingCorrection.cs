using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using FeatureExtractor.Abstract;
using FeatureExtractor.Concrete;
using FeatureSelector;
using FeatureSelector.Abstract;
using FeatureSelector.Concrete;
using FeatureSelector.Models;
using Winnow;
using System.Configuration;

namespace ContextSensitiveSpellingCorrection
{
	public class ContextSensitiveSpellingCorrection
	{
		private const int K = 10; //context features
		private const int L = 2; //collocation features

		public static readonly string SolutionPath =
			Directory.GetParent(Directory.GetCurrentDirectory()).Parent?.Parent?.FullName;

		private static readonly string SentenceXml = Path.Combine(SolutionPath, @"Models\Sentence.xml");
		private static readonly string TrainedXml = Path.Combine(SolutionPath, @"Models\Trained.xml");
		private readonly CollocationFeaturesExtractor _collocationtFeaturesExtractor;

		private readonly IList<Comparator> _comparators;
		private readonly ContextFeaturesExtractor _contextFeaturesExtractor;
		private readonly object _lock = new object();
		private readonly IPosTagger _posTagger;
		private readonly StatsHelper _statsHelper;

		public ContextSensitiveSpellingCorrection(IPosTagger posTagger, IEnumerable<string> corpora,
			IEnumerable<string[]> confusionSetsEnumerator, bool prune, bool refreshXmlFiles)
		{
			_posTagger = posTagger;
			_contextFeaturesExtractor = new ContextFeaturesExtractor(K);
			_collocationtFeaturesExtractor = new CollocationFeaturesExtractor(L);
			_statsHelper = new StatsHelper();
			var confusionSets = confusionSetsEnumerator as string[][] ?? confusionSetsEnumerator.ToArray();
			_comparators = new List<Comparator>(confusionSets.Count());

			var x = new XmlSerializer(typeof(Sentence[]));
			Sentence[] sentences = {};
			FileStream file;
			if (File.Exists(SentenceXml) && !File.Exists(TrainedXml))
			{
				using (file = new FileStream(SentenceXml, FileMode.Open))
				{
					sentences = (Sentence[]) x.Deserialize(file);
					Console.WriteLine("Deserialize complete");
				}
			}
			else if(!File.Exists(SentenceXml) || refreshXmlFiles)
			{
				sentences = PreProcessCorpora(corpora).ToArray();

				using (file = new FileStream(SentenceXml, FileMode.Create))
				{
					x.Serialize(file, sentences);
					Console.WriteLine("Serialize complete");
				}
			}

			var featureFrequencies = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

			if (prune)
				featureFrequencies = _statsHelper.GetFrequencies(sentences);

			var serializer = new DataContractSerializer(_comparators.GetType());
			if (File.Exists(TrainedXml) && !refreshXmlFiles)
			{
				using (var reader = new XmlTextReader(TrainedXml))
				{
					_comparators = (IList<Comparator>) serializer.ReadObject(reader);
				}
			}
			else if (!File.Exists(TrainedXml) || refreshXmlFiles)
			{
				Parallel.ForEach(confusionSets, confusionSet =>
				{
					var output = GenerateTrainingData(sentences, prune, featureFrequencies, confusionSet);

					Train(confusionSet, output.Features.ToArray(), output.Samples);
				});

				using (var writer = new XmlTextWriter(TrainedXml, Encoding.UTF8))
				{
					writer.Formatting = Formatting.Indented; // indent the Xml so it's human readable
					serializer.WriteObject(writer, _comparators);
					writer.Flush();
				}
			}
		}

		/// <summary>
		///     Checks every word in given sentence to check its contextually correct
		/// </summary>
		/// <param name="phrase">sentence to check</param>
		/// <returns> a Dictionary of the wrong word position in sentence as Key and its correction as Value</returns>
		public Dictionary<int, string> Predict(string phrase)
		{
			var tokens = SplitIntoWords(phrase);
			var correctWords = new Dictionary<int, string>();

			foreach (var comparator in _comparators)
				foreach (var confusedWord in comparator.ConfusionSet)
					for (var i = 0; i < tokens.Length; ++i)
					{
						if (!tokens[i].Equals(confusedWord, StringComparison.OrdinalIgnoreCase))
							continue;

						string[] posTags;

						lock (_lock)
						{
							posTags = _posTagger.Tag(tokens);
						}

						var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

						features.UnionWith(_contextFeaturesExtractor.Extract(tokens, i));
						features.UnionWith(_collocationtFeaturesExtractor.Extract(posTags, i));

						var sample = new Sample
						{
							Features = CreateFeaturesVector(features, comparator.Features)
						};

						var predictions = new Dictionary<string, double>(comparator.Cloud.Count);

						foreach (var classifier in comparator.Cloud.Keys)
							predictions[classifier] = comparator
								.Cloud[classifier]
								.Sum(c => c.GetScore(sample));

						var correctedWord = predictions.Aggregate((a, b) => a.Value > b.Value ? a : b).Key;

						if (!tokens[i].Equals(correctedWord, StringComparison.OrdinalIgnoreCase))
							correctWords.Add(i, correctedWord);
					}

			return correctWords;
		}

		private IEnumerable<Sentence> PreProcessCorpora(IEnumerable<string> corpora)
		{
			var sentences = new ConcurrentBag<Sentence>();

			Parallel.ForEach(corpora, phrase =>
			{
				var tokens = SplitIntoWords(phrase);
				string[] posTags;

				lock (_lock)
				{
					posTags = _posTagger.Tag(tokens);
				}

				sentences.Add(new Sentence
				{
					Words = tokens,
					PosTags = posTags
				});
			});

			return sentences;
		}

		private TrainingData GenerateTrainingData(Sentence[] sentences, bool prune,
			Dictionary<string, Dictionary<string, int>> featureFrequencies, IEnumerable<string> confusionSetEnumerable)
		{
			var allFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var samples = new List<RoughSample>();
			var totalDocumentsCount = sentences.Length;

			/* get exhaustive list of all features */
			var confusionSet = confusionSetEnumerable as string[] ?? confusionSetEnumerable.ToArray();
			foreach (var sentence in sentences)
				foreach (var word in confusionSet)
				{
					var smallFeatures = ExtractAllFeatures(sentence.Words, sentence.PosTags, word, prune, totalDocumentsCount,
						featureFrequencies);

					if (smallFeatures.Count == 0) //sentence doesnt contain target word
						continue;

					samples.Add(new RoughSample
					{
						Word = word,
						Features = smallFeatures
					});

					allFeatures.UnionWith(smallFeatures);
				}

			Console.WriteLine("Extracting Features for " + confusionSet.Aggregate((a, b) => a + "," + b) + " " + DateTime.Now);

			return new TrainingData
			{
				Samples = samples,
				Features = allFeatures
			};
		}

		private HashSet<string> ExtractAllFeatures(string[] tokens, string[] posTags, string target, bool prune,
			int totalCount, Dictionary<string, Dictionary<string, int>> featureFrequencies)
		{
			var contextFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var collocationFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			for (var i = 0; i < tokens.Length; ++i)
				if (StringComparer.OrdinalIgnoreCase.Equals(tokens[i], target))
				{
					contextFeatures.UnionWith(_contextFeaturesExtractor.Extract(tokens, i));
					collocationFeatures.UnionWith(_collocationtFeaturesExtractor.Extract(posTags, i));
				}

			if (prune)
			{
				/* get stats */
				var stats = _statsHelper.GetFeaturesStats(contextFeatures, featureFrequencies, target, totalCount);

				/* prune */
				contextFeatures = PruneFeatures(stats);
			}

			contextFeatures.UnionWith(collocationFeatures);
			return contextFeatures;
		}

		private string[] SplitIntoWords(string corpus)
		{
			var tokens = corpus
				.Split(new[] {',', ' ', '\r', '\n', ':', '-', '"', ';'}, StringSplitOptions.RemoveEmptyEntries);
			return tokens.ToArray();
		}

		private HashSet<string> PruneFeatures(Dictionary<string, Stats> featuresStats)
		{
			var prunedFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			IFeatureSelector mutualInformationSelector = new MutualInformation(10);
			IFeatureSelector chiSquaredTestSelector = new ChiSquaredTest(0.05);

			prunedFeatures.UnionWith(mutualInformationSelector.Select(featuresStats).Select(s => s.Key));
			prunedFeatures.UnionWith(chiSquaredTestSelector.Select(featuresStats).Select(s => s.Key));

			return prunedFeatures;
		}

		private void Train(IEnumerable<string> confusionSetEnumerable, string[] features, List<RoughSample> trainingData)
		{
			var confusionSet = confusionSetEnumerable as string[] ?? confusionSetEnumerable.ToArray();
			var cloudClassifiers = new Dictionary<string, ISupervisedLearning[]>(confusionSet.Count());

			foreach (var word in confusionSet)
				cloudClassifiers[word] = new ISupervisedLearning[]
				{
					new Winnow.Winnow(features.Length, 1, 1.5, 0.5, 1)
				};

			Parallel.ForEach(trainingData, sample =>
			{
				var positive = new Sample
				{
					Class = true,
					Features = CreateFeaturesVector(sample.Features, features)
				};

				var negative = positive.ToggleClass();

				foreach (var cloud in cloudClassifiers)
				{
					var example = cloud.Key == sample.Word ? positive : negative;

					foreach (var classifier in cloud.Value)
						lock (_lock)
						{
							classifier.Train(example);
						}
				}
			});

			_comparators.Add(new Comparator(cloudClassifiers, features));

			Console.WriteLine("Training done for " + confusionSet.Aggregate((a, b) => a + "," + b) + " " + DateTime.Now);
		}

		private bool[] CreateFeaturesVector(HashSet<string> subsetFeatures, string[] allFeatures)
		{
			var featuresVector = new bool[allFeatures.Length];

			for (var i = 0; i < allFeatures.Length; ++i)
				if (subsetFeatures.Contains(allFeatures[i], StringComparer.OrdinalIgnoreCase))
					featuresVector[i] = true;

			return featuresVector;
		}

		private class RoughSample
		{
			public HashSet<string> Features { get; set; }
			public string Word { get; set; }
		}

		private class TrainingData
		{
			public HashSet<string> Features { get; set; }
			public List<RoughSample> Samples { get; set; }
		}
	}
}