using PdxFile;
using System.Collections.Generic;
using System.Linq;

namespace CK2Helper
{
	public class CK2Religion
	{
        //TODO: consier whether religions even need to be loaded from the CK2 game files instead of the save
		public CK2Religion(string name)
		{
			Name = name;

		}

        public CK2Religion initFromSave(PdxSublist data, CK2World world)
        {
            HolySites = data.Sublists["holy_sites"].Values.Select(v => world.CK2Titles[v]).ToList();
            if (data.Sublists.ContainsKey("features"))
            {
                Features = data.Sublists["features"].Values;
            }
            return this;
        }

        public List<CK2Title> HolySites { get; set; }
        public string Name { get; set; }
        public bool HardToConvert { get; set; }
        public bool Feminist { get; set; }
        public List<string> Features { get; set; }
    }
}