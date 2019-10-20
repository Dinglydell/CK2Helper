using PdxFile;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Drawing;
using System.Collections.Specialized;

namespace CK2Helper
{
    public class CK2Province
    {
        public int ID { get; set; }
        public string Name { get; set; }

        private List<Dated<CK2Culture>> _cultureHistory;
        private CK2Culture _cultureOverride;
        public CK2Culture Culture
        {
            get
            {
                if(_cultureOverride != null)
                {
                    return _cultureOverride;
                }
                var date = PdxSublist.ParseDate(World.Date);
                CK2Culture culture = null;
                foreach(var ch in _cultureHistory)
                {
                    if(date < ch.Date)
                    {
                        return culture;
                    }
                    culture = ch.Value;
                }
                return culture;
            }
            set
            {
                _cultureOverride = value;
            }
        }
        private List<Dated<CK2Religion>> _religionHistory;
        private CK2Religion _religionOverride;
        public CK2Religion Religion
        {
            get
            {
                if (_religionOverride != null)
                {
                    return _religionOverride;
                }
                var date = PdxSublist.ParseDate(World.Date);
                CK2Religion religion = null;
                foreach (var ch in _religionHistory)
                {
                    if (date < ch.Date)
                    {
                        return religion;
                    }
                    religion = ch.Value;
                }
                return religion;
            }
            set
            {
                _religionOverride = value;
            }
        }
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
        private string county;
        public CK2Title CountyTitle { get { return World.CK2Titles.ContainsKey(county) ? World.CK2Titles[county] : null; } }
        public CK2World World { get; set; }
        //  public CK2Title DupeCountyTitle { get; set; }

        public List<CK2Title> BaronTitles { get; set; }

        public bool Hospital { get; set; }

        public HashSet<string> HospitalBuildings { get; set; }
        public HashSet<string> Modifiers { get; private set; }

        public CK2Province(int id, CK2World world, PdxSublist historyData)
        {
            World = world;
            ID = id;
            county = historyData.KeyValuePairs["title"];
            _cultureHistory = new List<Dated<CK2Culture>>();
            _religionHistory = new List<Dated<CK2Religion>>();
            if (historyData.KeyValuePairs.ContainsKey("culture"))
            {
                _cultureHistory.Add(new Dated<CK2Culture>(new DateTime(769, 1, 1), world.CK2Cultures[historyData.KeyValuePairs["culture"]]));
            }
            if (historyData.KeyValuePairs.ContainsKey("religion"))
            {
                _religionHistory.Add(new Dated<CK2Religion>(new DateTime(769, 1, 1), world.CK2Religions[historyData.KeyValuePairs["religion"]]));
            }
            historyData.ForEachSublist((sub) =>
            {
                var date = PdxSublist.ParseDate(sub.Key);
                if (sub.Value.KeyValuePairs.ContainsKey("religion"))
                {
                    _religionHistory.Add(new Dated<CK2Religion>(date, world.CK2Religions[sub.Value.KeyValuePairs["religion"]]));
                }
                if (sub.Value.KeyValuePairs.ContainsKey("culture"))
                {
                    _cultureHistory.Add(new Dated<CK2Culture>(date, world.CK2Cultures[sub.Value.KeyValuePairs["culture"]]));
                }
            });

        }

        public bool InitFromSaveFile(PdxSublist data)
        {
            if(CountyTitle == null)
            {
                return false;
            }
            if (data.Sublists.ContainsKey("variables") && data.Sublists["variables"].FloatValues.ContainsKey("prosperity_value"))
            {
                Prosperity = (int)data.Sublists["variables"].FloatValues["prosperity_value"].Single();
            }
            if (data.KeyValuePairs.ContainsKey("culture"))
            {
                Culture = World.CK2Cultures[data.KeyValuePairs["culture"]];
            }

            if (data.KeyValuePairs.ContainsKey("religion"))
            {
                Religion = World.CK2Religions[data.KeyValuePairs["religion"]];
            }
            if (ID == 28)
            {
                Console.WriteLine();
            }
            // if(ID == 1682)
            //  {
            //     Console.WriteLine();
            //}
            ////a mess. blame pdx for having duplicates where they shouldn't
            //if (!world.CK2Titles.ContainsKey(county) && !world.CK2Titles.ContainsKey(countyDupe))
            // {
            //    return;       
            //}

            CountyTitle.Province = this;
           

            if (World.Localisation.ContainsKey($"PROV{ID}"))
            {
                DisplayName = World.Localisation[$"PROV{ID}"];
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
                    BaronTitles.Add(World.CK2Titles[sub.Key]);
                    World.CK2Titles[sub.Key].AddBaronData(sub.Value, CountyTitle);
                    World.CK2Titles[sub.Key].Province = this;

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

            var mapPos = World.CK2ProvPositions[ID.ToString()];
            var mapX = mapPos.Sum(p => p.X) / mapPos.Count;
            var mapY = mapPos.Sum(p => p.Y) / mapPos.Count;
            MapPosition = new Point(mapX, mapY);


            Modifiers = new HashSet<string>();
            data.Sublists.ForEach("modifier", sub =>
            {
                Modifiers.Add(sub.KeyValuePairs["modifier"]);
            });
            //world.TaskPool.Add(FindMapPosition(world));
            return true;
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