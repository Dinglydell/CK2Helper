using System;
using PdxFile;
using System.Linq;
using System.Collections.Generic;

namespace CK2Helper
{
	public class CK2Culture
	{
        public CK2CultureGroup Group { get; set; }
        public CK2Province Centre { get; set; }
		public Dictionary<TitleRank, CK2Title> RankedCentres { get; set; }


		public bool DynastyTitleNames { get; set; }
		public CK2Culture(PdxSublist data, CK2World world, CK2CultureGroup group): this(data.Key, world)
		{
            Group = group;
			if (data.BoolValues.ContainsKey("dynasty_title_names"))
			{
				DynastyTitleNames = data.BoolValues["dynasty_title_names"].Single();
			}
		}
		public CK2Culture(string name, CK2World world)
		{
			Name = name;
			if (world.Localisation.ContainsKey(name))
			{
				DisplayName = world.Localisation[name];
			}
			SubCultures = new List<CK2Culture>();
		}

		public string Name { get; set; }
		public string DisplayName { get; set; }
		public List<CK2Culture> SubCultures { get; set; }
		public bool IsSubCulture { get {
				return Parent != null;
			} }
		public CK2Culture Parent { get; private set; }

		public void FindCentre(CK2World world)
		{
			var culturedProvinces = world.CK2Provinces.Values.Where(c => c.Culture == this);
			if (culturedProvinces.Count() == 0)
			{
				return;
			}
			var x = 0;
			var y = 0;
			foreach (var prov in culturedProvinces)
			{
				x += prov.MapPosition.X;
				y += prov.MapPosition.Y;
			}
			x /= culturedProvinces.Count();
			y /= culturedProvinces.Count();
			CK2Province closest = null;
			var closeDist = int.MaxValue;
			foreach (var prov in culturedProvinces)
			{
				var dx = prov.MapPosition.X - x;
				var dy = prov.MapPosition.Y - y;
				var dist = dx * dx + dy * dy;
				if (closest == null || closeDist > dist)
				{
					closest = prov;
					closeDist = dist;
				}
			}
			RankedCentres = new Dictionary<TitleRank, CK2Title>();
			SetCentreAndLieges(closest.CountyTitle);
			Centre = closest;
			//var culturedTitles = new Dictionary<CK2Title, int>();
			//foreach (var prov in culturedProvinces)
			//{
			//	AddTitleCultures(culturedTitles, prov.CountyTitle);
			//}
			//Centre = culturedTitles.OrderByDescending(c => c.Value).First().Key;
			//
			//var rank = (TitleRank)((int)Centre.Rank - 1);
			//var liege = Centre;
			//if (Centre.Holder.Culture == this && Centre.Capital != 0)
			//{
			//	//something to do with things goes here
			//}
			//RankedCentres[Centre.Rank] = Centre;
			//while ((int)rank > (int)TitleRank.county)
			//{
			//	var centred = culturedTitles.Where(c => (c.Key.LiegeTitle == liege || c.Key.DejureLiegeTitle == liege) && c.Key.Rank == rank).OrderByDescending(c => c.Value);
			//	if (centred.Count() != 0)
			//	{
			//		liege = centred.First().Key;
			//		RankedCentres.Add(rank, liege);
			//	}
			//	rank = (TitleRank)((int)rank - 1);
			//}
			//if (Centre.Rank != TitleRank.county)
			//{
			//	RankedCentres[TitleRank.county] = culturedTitles.Where(c => c.Key.Rank == TitleRank.county && c.Key.LiegeTitle == RankedCentres[TitleRank.duchy]).First().Key;
			//}
			Console.WriteLine($"Centre of {Name} culture is {Centre.Name} ({string.Join(", ", RankedCentres.Values.Select(t => t.Name))})");

		}


		private void AddTitleCultures(Dictionary<CK2Title, int> culturedTitles, CK2Title title)
		{
			if (title == null)
			{
				return;
			}
			if (!culturedTitles.ContainsKey(title))
			{
				culturedTitles[title] = 0;
			}
			culturedTitles[title]++;
			AddTitleCultures(culturedTitles, title.LiegeTitle);

		}

		public int GetDistanceTo(CK2Province province)
		{


			var dx = (Centre.MapPosition.X - province.MapPosition.X);
			var dy = (Centre.MapPosition.Y - province.MapPosition.Y);
			var distance = dx * dx + dy * dy;

			foreach (TitleRank rank in Enum.GetValues(typeof(TitleRank)))
			{
				if (TitleRank.barony == rank || !RankedCentres.ContainsKey(rank))
				{
					continue;
				}
				// check if same defacto
				if (RankedCentres[rank] != province.CountyTitle.GetLiege(rank))
				{
					distance += (int)rank * 2000;
				}
				//check if same dejure
				if (RankedCentres[rank] != province.CountyTitle.GetDejureLiege(rank))
				{
					distance += (int)rank * 2000;
				}
			}



			return distance;
		}

		public CK2Culture CreateSubCulture(CK2Province centre)
		{
			var sub = new CK2Culture(Name + "_" + centre.CountyTitle.Name, centre.CountyTitle.World);
			sub.DisplayName = centre.Culture.DisplayName;
			SubCultures.Add(sub);
			sub.Centre = centre;
			sub.SetCentreAndLieges(centre.CountyTitle);
			
			sub.Parent = this;
			Console.WriteLine($"Created subculture of {Name}: {sub.Name}");
			return sub;
		}

		private void SetCentreAndLieges(CK2Title title)
		{
			if(RankedCentres == null)
			{
				RankedCentres = new Dictionary<TitleRank, CK2Title>();
			}
			RankedCentres[title.Rank] = title;
			if (title.LiegeTitle != null) { 

				SetCentreAndLieges(title.LiegeTitle);
			}
		}
	}

}