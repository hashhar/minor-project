using System;
using System.Collections.Generic;
using FeatureExtractor.Abstract;

namespace FeatureExtractor.Concrete
{
	public class ContextFeaturesExtractor : AbstractExtractor
	{
		public ContextFeaturesExtractor(int k) : base(k)
		{
		}

		public override HashSet<string> Extract(string[] tokens, int targetPosition)
		{
			var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			var backward = targetPosition - 1;
			var forward = targetPosition + 1;

			for (var counter = 0; counter < N; ++counter)
			{
				if ((backward <= -1) && (forward >= tokens.Length))
					break;

				if (backward > -1)
				{
					AddFeature(features, tokens[backward]);
					backward--;
				}

				if (forward < tokens.Length)
				{
					AddFeature(features, tokens[forward]);
					++forward;
				}
			}

			return features;
		}
	}
}