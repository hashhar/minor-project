using System.Collections.Generic;

namespace Winnow
{
	public interface ISupervisedLearning
	{
		int Mistakes { get; }
		void Train(IEnumerable<Sample> samples);
		void Train(params Sample[] samples);
		bool Predict(Sample sample);
		double GetScore(Sample sample);
	}
}