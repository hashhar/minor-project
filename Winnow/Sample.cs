using System;
using System.Text;

namespace Winnow
{
	public class Sample
	{
		public bool[] Features { get; set; }
		public bool Class { get; set; }

		public Sample ToggleClass()
		{
			var invertedSample = (Sample) MemberwiseClone();
			invertedSample.Class = !invertedSample.Class;
			return invertedSample;
		}

		public override string ToString()
		{
			var sb = new StringBuilder("(" + Class + ")[");

			for (var i = 0; i < Features.Length; ++i)
			{
				sb.Append(Convert.ToInt16(Features[i]));

				if (i < Features.Length - 1)
					sb.Append(',');
			}

			sb.Append("]");

			return sb.ToString();
		}
	}
}