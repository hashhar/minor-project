using System.IO;
using OpenNLP.Tools.PosTagger;
using IPosTagger = FeatureExtractor.Abstract.IPosTagger;

namespace FeatureExtractor
{
	public class PosTagger : IPosTagger
	{
		private readonly EnglishMaximumEntropyPosTagger _posTagger;

		public PosTagger()
		{
			var modelsPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent?.Parent?.FullName;
			if (modelsPath != null)
			{
				modelsPath = Path.Combine(modelsPath, "models");
				_posTagger = new EnglishMaximumEntropyPosTagger(Path.Combine(modelsPath, "EnglishPOS.nbin"),
					Path.Combine(modelsPath, "tagdict"));
			}
		}

		public string[] Tag(string[] tokens)
		{
			return _posTagger.Tag(tokens);
		}
	}
}