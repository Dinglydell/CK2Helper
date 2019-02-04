using PdxFile;

namespace CK2Helper
{
	public class CK2Religion
	{

		public CK2Religion(PdxSublist data)
		{
			Name = data.Key;
		}

		public string Name { get; set; }
	}
}