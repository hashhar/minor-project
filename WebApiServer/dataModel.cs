using System.Collections.Generic;

namespace WebApiServer
{
		public class DataModel
		{
			public string Sentence { get; set; }
			public Dictionary<int, string> Corrections { get; set; }
		}
}
