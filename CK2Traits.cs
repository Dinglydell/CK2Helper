using System.Collections.Generic;
using PdxFile;

namespace CK2Helper
{
	public class CK2Traits
	{
		public string Name { get; set; }
		public PdxSublist Effects { get; set; }

		public CK2Traits(KeyValuePair<string, PdxSublist> trait)
		{
			Name = trait.Key;
			Effects = trait.Value;

		}
	}
}