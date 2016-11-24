using System;
using System.Collections.Generic;
using FeatureExtractor.Abstract;

namespace FeatureExtractor.Concrete
{
	public class CollocationFeaturesExtractor : AbstractExtractor
	{
		public CollocationFeaturesExtractor(int l)
			: base(l)
		{
		}

		public override HashSet<string> Extract(string[] posTags, int targetPosition)
		{
			var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			for (var c = 0; c < N; ++c)
			{
				var preceding = string.Empty;
				var backward = targetPosition - (c + 1);

				while ((backward < targetPosition) && (backward > -1))
				{
					preceding += posTags[backward] + " ";
					++backward;
				}
				if (!string.IsNullOrEmpty(preceding))
				{
					preceding += "_";
					AddFeature(features, preceding);
				}

				var succeeding = "_ ";

				var forward = targetPosition + 1;
				for (var j = 0; (j <= c) && (forward < posTags.Length); ++j, forward = targetPosition + j + 1)
					succeeding += posTags[forward] + " ";
				succeeding = succeeding.TrimEnd();
				if (succeeding != "_")
					AddFeature(features, succeeding.TrimEnd());
			}

			return features;
		}
	}
}