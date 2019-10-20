using System;
using PdxFile;
using PdxUtil;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace CK2Helper
{

	public enum TitleRank
	{
		barony, county, duchy, kingdom, empire
	}

	public class CK2Title
	{

		public CK2World World { get; set; }
		public TitleRank Rank { get; set; }


		public string Name { get; set; }
		public string Type { get; set; }
		public bool IsRevolt { get; set; }
		public List<CK2Building> Buildings { get; set; }

		//provinces associated with county or baron level title
		public CK2Province Province { get; set; }

		public CK2Title(string name, CK2World world)
		{
			Name = name;
			switch (name.First())
			{
				case 'b':
					Rank = TitleRank.barony;
					break;
				case 'c':
					Rank = TitleRank.county;
					break;
				case 'd':
					Rank = TitleRank.duchy;
					break;
				case 'k':
					Rank = TitleRank.kingdom;
					break;
				case 'e':
					Rank = TitleRank.empire;
					break;

			}
			World = world;
		}

		public HashSet<string> Laws { get; set; }

		public string Succession { get; set; }

		public string GenderSuccession { get; set; }

		public CK2CharacterBase Holder { get; set; }
		public CK2Title LiegeTitle { get { return LiegeTitleID == null ? (IsRevolt ? BaseTitle : null) : World.CK2Titles[LiegeTitleID]; } }
		public string LiegeTitleID { get; set; }

		public CK2Title DejureLiegeTitle { get { return DejureLiegeTitleID == null ? null : World.CK2Titles[DejureLiegeTitleID]; } }
		public string DejureLiegeTitleID { get; set; }
		public int Capital { get; set; }
		public Colour Colour { get; set; }

		public string DisplayName { get; set; }
		public string DisplayAdj { get; set; }
		private string flag;
		public string Flag
		{
			get {
				var flagFile = flag ?? BaseTitle?.Flag;
				if (flagFile == null) {
					var files = World.GetFilesFor(@"gfx\flags");
					flagFile = files.Where(f => Path.GetFileNameWithoutExtension(f) == Name).SingleOrDefault();
				}

				return flagFile;
			
			}
			set { flag = value; }
		}
		public CK2Title BaseTitle { get; set; }

		public CK2Title(string name, CK2World world, PdxSublist data) : this(name, world)
		{
			Laws = new HashSet<string>();
			data.KeyValuePairs.ForEach("law", (law) =>
			{
				Laws.Add(law);
			});

			if (data.FloatValues.ContainsKey("capital"))
			{
				Capital = (int)(data.FloatValues["capital"].Single());
			}

			if (data.Sublists.ContainsKey("color"))
			{
				var colour = data.GetSublist("color");
				Colour = new Colour(colour.FloatValues[string.Empty]);
			}

			if (data.KeyValuePairs.ContainsKey("name"))
			{
				DisplayName = data.KeyValuePairs["name"];
			}
			else if (world.Localisation.ContainsKey(Name))
			{
				DisplayName = world.Localisation[Name];
			}
			if (data.KeyValuePairs.ContainsKey("adjective"))
			{
				var adj = data.KeyValuePairs["adjective"];
				DisplayAdj = world.Localisation.ContainsKey(adj) ? world.Localisation[adj] : adj;
			}
			else if (world.Localisation.ContainsKey(Name + "_adj"))
			{
				DisplayAdj = world.Localisation[Name + "_adj"];
			}
			Succession = data.KeyValuePairs["succession"];
			GenderSuccession = data.KeyValuePairs["gender"];
			if (data.FloatValues.ContainsKey("holder"))
			{
				Holder = world.CK2Characters[(int)data.FloatValues["holder"].Single()];
				Holder.Titles.Add(this);
			}
			if (data.KeyValuePairs.ContainsKey("liege"))
			{
				LiegeTitleID = data.KeyValuePairs["liege"];
			}
			else if (data.Sublists.ContainsKey("liege"))
			{
				LiegeTitleID = data.Sublists["liege"].KeyValuePairs["title"];
			}
			else
			{
				world.AddIndependentTitle(this);
			}
			if (data.KeyValuePairs.ContainsKey("de_jure_liege"))
			{
				DejureLiegeTitleID = data.KeyValuePairs["de_jure_liege"];
			}
			if(data.BoolValues.ContainsKey("major_revolt"))
			{
				IsRevolt = data.BoolValues["major_revolt"].Single();
			}

			

		}

		public CK2Title GetTopLiegeTitle()
		{
			return LiegeTitle.GetTopLiegeTitle() ?? this;
		}
        /// <summary>
        /// Returns true if the given title is the liege of this title
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public bool IsVassalOf(CK2Title title)
        {
            return title == GetLiege(title.Rank);
        }
        /// <summary>
        /// Returns true if the given title is the dejure liege of this title
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public bool IsDejureVassalOf(CK2Title title)
        {
            return title == GetDejureLiege(title.Rank);
        }
        public CK2Title GetLiege(TitleRank rank)
		{
			var liege = this;
			while (liege != null)
			{
				if (liege.Rank == rank)
				{
					return liege;
				}
				liege = liege.LiegeTitle;
			}
			return null;
		}
		public CK2Title GetDejureLiege(TitleRank rank)
		{
			var liege = this;
			while (liege != null)
			{
				if (liege.Rank == rank)
				{
					return liege;
				}
				liege = liege.DejureLiegeTitle;
			}
			return null;
		}

		/// <summary>
		/// Returns true if either this title is a revolt or any of its liegetitles are revolts
		/// </summary>
		public bool IsInRevolt()
		{
			if (IsRevolt)
			{
				return true;
			}
			return LiegeTitle?.IsInRevolt() ?? false;
		}

		public CK2Title GetRevolt()
		{
			return IsRevolt ? this : LiegeTitle?.GetRevolt();
		}

		public bool IsDirectDejureLiege(CK2Title liege)
		{
			return DejureLiegeTitle != null && (DejureLiegeTitle == liege || DejureLiegeTitle.IsDirectDejureLiege(liege));
		}

		internal void AddBaronData(PdxSublist data, CK2Title parent)
		{
			LiegeTitleID = parent.Name;
			Type = data.KeyValuePairs["type"];
			Buildings = new List<CK2Building>();
			foreach (var building in World.CK2Buildings)
			{
				if (data.BoolValues.ContainsKey(building.Key) && data.BoolValues[building.Key].Single())
				{
					Buildings.Add(building.Value);
				}

			}
		}
	}
}