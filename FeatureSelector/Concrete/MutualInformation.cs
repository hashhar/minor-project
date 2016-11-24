using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FeatureSelector.Abstract;
using FeatureSelector.Models;

namespace FeatureSelector.Concrete
{
	public class MutualInformation : IFeatureSelector
	{
		private readonly int _k;

		public MutualInformation(int k)
		{
			_k = k;
		}

		public IDictionary<string, Stats> Select(IDictionary<string, Stats> terms)
		{
			var features = new Dictionary<string, double>(terms.Count, StringComparer.OrdinalIgnoreCase);

			foreach (var term in terms)
			{
				var value = Calculate(term.Value.N,
					term.Value.N00,
					term.Value.N01,
					term.Value.N10,
					term.Value.N11);
				features.Add(term.Key, value);
			}

			var orderedFeatures = features
				.OrderByDescending(s => s.Value)
				.Select(s => s.Key);
			var prunedFeatures = orderedFeatures
				.Take(_k)
				.ToDictionary(key => key, key => terms[key]);

			return prunedFeatures;
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private double Calculate(int n, double n00, double n01, double n10, double n11)
		{
			var n1_ = n10 + n11;
			var n_1 = n01 + n11;
			var n0_ = n00 + n01;
			var n_0 = n10 + n00;

			double sum = 0;
			sum += n11/n*Math.Log(n*n11/(n1_*n_1), 2);
			sum += n01/n*Math.Log(n*n01/(n0_*n_1), 2);
			sum += n10/n*Math.Log(n*n10/(n1_*n_0), 2);
			sum += n00/n*Math.Log(n*n00/(n0_*n_0), 2);

			return sum;
		}
	}
}