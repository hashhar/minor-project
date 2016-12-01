using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using EditCorrector;
using FeatureExtractor;
using FeatureExtractor.Abstract;

namespace WebApiServer
{
	public class WebApiServer
	{
		private static readonly string SolutionPath = ContextSensitiveSpellingCorrection.ContextSensitiveSpellingCorrection.SolutionPath;
		private static ContextSensitiveSpellingCorrection.ContextSensitiveSpellingCorrection contextSensitiveSpellingCorrection;

		public static void Main()
		{
			SetupService();
			Console.WriteLine("Starting echo server...");

			int port = 1234;
			TcpListener listener = new TcpListener(IPAddress.Loopback, port);
			listener.Start();

			TcpClient client = listener.AcceptTcpClient();
			NetworkStream stream = client.GetStream();
			StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
			StreamReader reader = new StreamReader(stream, Encoding.ASCII);

			while (true)
			{
				string inputLine = "";
				while (inputLine != null)
				{
					inputLine = reader.ReadLine();
					var testSentence = inputLine;
					Dictionary<int, string> wordsList = contextSensitiveSpellingCorrection.Predict(testSentence);
					var outLine = "";
					var jsonObj = new DataModel();
					jsonObj.Sentence = inputLine;
					jsonObj.Corrections = wordsList;
					string json = JsonConvert.SerializeObject(jsonObj);
					writer.WriteLine(json);
					Console.WriteLine("\nCorrections: " + json);
				}
				Console.WriteLine("Server saw disconnect from client.");
			}
		}

		private static string ReturnNWord(int n, string str)
		{
			var words = str.Split(' ');
			return words[n];
		}

		private static void SetupService()
		{
			Console.WriteLine("Starting application. It may take some time to load dictionaries.");
			var editCorrector = new Spelling();
			var confusionSets = GetConfusionSets();
			var trainingCorpora = GetCorpora();
			IPosTagger posTagger = new PosTagger();
			contextSensitiveSpellingCorrection = new ContextSensitiveSpellingCorrection.ContextSensitiveSpellingCorrection(posTagger, trainingCorpora, confusionSets, false);
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
