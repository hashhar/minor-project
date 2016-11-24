using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Winnow;

namespace ContextSensitiveSpellingCorrection
{
	[DataContract]
	[KnownType(typeof(IEnumerable<string>))]
	[KnownType(typeof(Dictionary<string, ISupervisedLearning[]>))]
	[KnownType(typeof(ISupervisedLearning[]))]
	[KnownType(typeof(ISupervisedLearning))]
	[KnownType(typeof(Winnow.Winnow))]
	internal class Comparator
	{
		public Comparator(Dictionary<string, ISupervisedLearning[]> cloud, string[] features)
		{
			Cloud = cloud;
			Features = features;
		}

		public Comparator()
		{
		}

		[DataMember]
		public Dictionary<string, ISupervisedLearning[]> Cloud { get; set; }

		[DataMember]
		public string[] Features { get; set; }

		[DataMember]
		public IEnumerable<string> ConfusionSet
		{
			get { return Cloud.Keys; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));
			}
		}
	}
}