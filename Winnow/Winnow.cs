using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Winnow
{
	/// <summary>
	///     Algorithm for learning monotone disjunction function from labeled examples
	/// </summary>
	[DataContract]
	[KnownType(typeof(Winnow))]
	[KnownType(typeof(double[]))]
	public class Winnow : ISupervisedLearning
	{
		public Winnow()
		{
		}

		public Winnow(int featuresCount, double threshold, double promotion, double demotion, double initialWeight)
		{
			Threshold = threshold;
			Promotion = promotion;
			Demotion = demotion;
			Weights = new double[featuresCount];

			for (var i = 0; i < Weights.Length; ++i)
				Weights[i] = initialWeight;
		}

		[DataMember]
		public double[] Weights { get; set; }

		[DataMember]
		public double Threshold { get; set; }

		[DataMember]
		public double Promotion { get; set; }

		[DataMember]
		public double Demotion { get; set; }

		public void Train(IEnumerable<Sample> samples)
		{
			Train(samples.ToArray());
		}

		public void Train(params Sample[] samples)
		{
			foreach (var s in samples)
			{
				var prediction = Predict(s);

				if (prediction != s.Class) //prediction was wrong
				{
					++Mistakes;

					if (!prediction && s.Class)
						AdjustWeights(s, Promotion);
					else
						AdjustWeights(s, Demotion);
				}
			}
		}

		public bool Predict(Sample sample)
		{
			var sum = GetScore(sample);

			return sum >= Threshold;
		}

		public double GetScore(Sample sample)
		{
			double sum = 0;

			for (var i = 0; i < Weights.Length; ++i)
				if (sample.Features[i])
					sum += Weights[i];

			return sum;
		}

		[DataMember]
		public int Mistakes { get; set; }

		public override string ToString()
		{
			var sb = new StringBuilder();

			foreach (var item in Weights)
			{
				sb.Append(item);
				sb.Append(",");
			}

			return sb.ToString();
		}

		private void AdjustWeights(Sample s, double adjustment)
		{
			for (var i = 0; i < Weights.Length; ++i)
				if (s.Features[i])
					Weights[i] *= adjustment;
		}
	}
}