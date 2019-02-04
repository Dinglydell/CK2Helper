using PdxFile;
using System.Text.RegularExpressions;

namespace CK2Helper
{
	public class CK2Building
	{
		public string ID { get; set; }
		public string Type { get {
				return (new Regex(@"_\d+$")).Replace(ID, string.Empty);
			} }
		public int Level {
					get {
						return int.Parse((new Regex(@"\d+$")).Match(ID).Value);
					}
			
			}
		public CK2Building(PdxSublist data)
		{
			ID = data.Key;
		}
	}
}