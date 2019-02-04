using PdxFile;

namespace CK2Helper
{
	public class CK2Dynasty
	{
		public string Name { get; set; }
		public CK2Culture Culture { get; private set; }
		public CK2Religion Religion { get; private set; }

		public CK2Dynasty(CK2World world, PdxSublist data)
		{
			if (data.KeyValuePairs.ContainsKey("name"))
			{
				Name = data.KeyValuePairs["name"];
			}
			if (data.KeyValuePairs.ContainsKey("culture"))
			{
				Culture = world.CK2Cultures[data.KeyValuePairs["culture"]];
			}
			if (data.KeyValuePairs.ContainsKey("religion"))
			{
				Religion = world.CK2Religions[data.KeyValuePairs["religion"]];
			}
		}
	}
}