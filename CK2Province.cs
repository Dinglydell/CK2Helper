using PdxFile;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Drawing;

namespace CK2Helper
{
	public class CK2Province
	{
		public int ID { get; set; }
		public string Name { get; set; }

		private CK2Culture _culture;
		public CK2Culture Culture
		{
			get
			{
				return _culture ?? CountyTitle.Holder?.Culture;
			}
			set
			{
				_culture = value;
			}
		}
		public CK2Religion Religion { get; set; }
		public string DisplayName { get; set; }

		public List<float> Technology { get; set; }

		public Point MapPosition { get; set; }

		public float TotalTech
		{
			get
			{
				return Technology.Sum();
			}
		}
		//TODO: check this is actually the mil tech part
		public float MilTech
		{
			get
			{
				return Technology.GetRange(0, Technology.Count / 3).Sum();
			}
		}
		public float EconTech
		{
			get
			{
				return Technology.GetRange(Technology.Count / 3, Technology.Count / 3).Sum();
			}
		}
		public float CultureTech
		{
			get
			{
				return Technology.GetRange(2 * Technology.Count / 3, Technology.Count / 3).Sum();
			}
		}

		public int Prosperity { get; set; }

		public CK2Title CountyTitle { get; set; }

		public List<CK2Title> BaronTitles { get; set; }

		public bool Hospital { get; set; }

		public HashSet<string> HospitalBuildings { get; set; }
		public HashSet<string> Modifiers { get; private set; }

		public CK2Province(CK2World world, PdxSublist data, string county)
		{
			ID = int.Parse(data.Key);

			if (data.Sublists.ContainsKey("variables") && data.Sublists["variables"].FloatValues.ContainsKey("prosperity_value"))
			{
				Prosperity = (int)data.Sublists["variables"].FloatValues["prosperity_value"].Single();
			}
			if (data.KeyValuePairs.ContainsKey("culture"))
			{
				Culture = world.CK2Cultures[data.KeyValuePairs["culture"]];
			}

			if (data.KeyValuePairs.ContainsKey("religion"))
			{
				Religion = world.CK2Religions[data.KeyValuePairs["religion"]];
			}
			CountyTitle = world.CK2Titles[county];
			CountyTitle.Province = this;
			if (world.Localisation.ContainsKey($"PROV{ID}"))
			{
				DisplayName = world.Localisation[$"PROV{ID}"];
				if (CountyTitle.DisplayName == null)
				{
					CountyTitle.DisplayName = DisplayName;
				}
			}
			//world.AddTitle(CountyTitle);
			BaronTitles = new List<CK2Title>();

			data.ForEachSublist(sub =>
			{
				if (sub.Key[1] == '_')
				{
					BaronTitles.Add(world.CK2Titles[sub.Key]);
					world.CK2Titles[sub.Key].AddBaronData(sub.Value, CountyTitle);
					world.CK2Titles[sub.Key].Province = this;

				}
			});
			Hospital = data.Sublists.ContainsKey("hospital");
			HospitalBuildings = new HashSet<string>();
			if (Hospital)
			{
				foreach (var building in data.Sublists["hospital"].BoolValues)
				{
					if (building.Value.Single())
					{
						HospitalBuildings.Add(building.Key);
					}
				}
			}

			Technology = data.Sublists["technology"].Sublists["tech_levels"].FloatValues[string.Empty];

			var mapPos = world.CK2ProvPositions[ID.ToString()];
			var mapX = mapPos.Sum(p => p.X) / mapPos.Count;
			var mapY = mapPos.Sum(p => p.Y) / mapPos.Count;
			MapPosition = new Point(mapX, mapY);


			Modifiers = new HashSet<string>();
			data.Sublists.ForEach("modifier", sub =>
			{
				Modifiers.Add(sub.KeyValuePairs["modifier"]);
			});
			//world.TaskPool.Add(FindMapPosition(world));

		}

		//private async Task FindMapPosition(CK2World world)
		//{
		//	var bmp = (Bitmap)world.ProvinceMap.Clone();
		//	await Task.Run(() =>
		//	{

		//		//map position - this could probably be more efficient
		//		//TODO: multithreading?
		//		var colour = world.CK2ProvColours[ID.ToString()];

		//		for (int y = 0; y < bmp.Height; y++)
		//		{
		//			for (int x = 0; x < bmp.Width; x++)
		//			{
		//				var pxColour = bmp.GetPixel(x, y);
		//				if (pxColour.ToArgb() == colour.ToArgb())
		//				{
		//					//TODO: find centre of province instead of the first pixel we come across?
		//					mapX = x;
		//					mapY = y;
		//					return;
		//				}
		//			}
		//		}
		//	});
		//}
	}
}