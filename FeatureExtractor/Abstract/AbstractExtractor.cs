using System.Collections.Generic;

namespace FeatureExtractor.Abstract
{
	public abstract class AbstractExtractor
	{
		protected readonly int N;

		protected AbstractExtractor(int n)
		{
			N = n;
		}

		public abstract HashSet<string> Extract(string[] tokens, int targetPosition);

		protected void AddFeature(HashSet<string> features, string feature)
		{
			if (!features.Contains(feature))
				features.Add(feature);
		}
	}
}