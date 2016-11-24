using System.Collections.Generic;
using FeatureSelector.Models;

namespace FeatureSelector.Abstract
{
	public interface IFeatureSelector
	{
		IDictionary<string, Stats> Select(IDictionary<string, Stats> terms);
	}
}