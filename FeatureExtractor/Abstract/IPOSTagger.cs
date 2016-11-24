namespace FeatureExtractor.Abstract
{
	public interface IPosTagger
	{
		string[] Tag(string[] tokens);
	}
}