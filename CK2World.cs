using Microsoft.VisualBasic.FileIO;
using PdxFile;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK2Helper
{
	public class CK2World
	{
		private readonly string GAME_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Crusader Kings II\";

		public Dictionary<string, CK2Title> CK2Titles { get; set; }
		public Dictionary<string, CK2Title> CK2IndependentTitles { get; set; }
		public Dictionary<string, CK2Title> CK2TopLevelVassals { get; set; }

		public Dictionary<int, CK2CharacterBase> CK2Characters { get; set; }
		public Dictionary<int, CK2Dynasty> CK2Dynasties { get; set; }

		public List<CK2Traits> CK2Traits { get; set; }

		public Dictionary<string, CK2Religion> CK2Religions { get; set; }

		public Dictionary<string, CK2Culture> CK2Cultures { get; set; }

		public Bitmap ProvinceMap { get; set; }

		public List<CK2Province> CK2Provinces { get; set; }
		/// <summary>
		/// maps ck2 province IDs to the county title ID
		/// </summary>
		public Dictionary<string, string> CK2ProvCounties { get; set; }
		/// <summary>
		/// maps ck2 province colour to the ID
		/// </summary>
		public Dictionary<int, string> CK2ProvColours { get; set; }
		/// <summary>
		/// Maps ck2 province IDs to their positions
		/// </summary>
		public Dictionary<string, List<Point>> CK2ProvPositions { get; set; }

		public Dictionary<string, string> Localisation { get; set; }

		public string ModPath { get; set; }
		public Dictionary<string, CK2Building> CK2Buildings { get; set; }
		public List<Task> TaskPool { get; internal set; }

		public CK2World(string modPath)
		{
			
			Console.WriteLine("Loading CK2 data...");
			ModPath = modPath;
			TaskPool = new List<Task>();
			LoadLocalisation();
			LoadTraits();
			LoadReligion();
			LoadCulture();
			LoadDynasties();
			LoadProvCounties();
			LoadBuildings();
			
			
		}

		private void LoadLocalisation()
		{
			Localisation = new Dictionary<string, string>();
			var localeFiles = GetFilesFor("localisation");
			foreach (var file in localeFiles)
			{
				LoadLocalisationFile(file);
			}
		}

		private void LoadLocalisationFile(string path)
		{
			using (var file = new StreamReader(path, Encoding.Default))
			{


				var key = new StringBuilder();
				var value = new StringBuilder();
				var readKey = true;
				var readValue = false;
				var comment = true;
				//var inQuotes = false;
				while (!file.EndOfStream)
				{
					var ch = Convert.ToChar(file.Read());
					
					if (Environment.NewLine.Contains(ch))
					{
						readValue = false;
						readKey = true;
						if (key.Length > 0 && value.Length > 0 && !Localisation.ContainsKey(key.ToString()))
						{
							Localisation.Add(key.ToString(), value.ToString());
						}
						key = new StringBuilder();
						value = new StringBuilder();
						comment = false;
						continue;
					}
					if (ch == '#' || comment)
					{
						comment = true;
						continue;
					}
					if (ch == ';')
					{
						if (readValue)
						{
							readValue = false;
						}
						if (readKey)
						{
							readKey = false;
							readValue = true;
						}

						continue;
					}
				

					if (readKey)
					{
						key.Append(ch);
					}
					if (readValue)
					{
						value.Append(ch);
					}
				}
				if (key.Length > 0 && value.Length > 0)
				{
					Localisation.Add(key.ToString(), value.ToString());
				}
			}
		}

		private void LoadDynasties()
		{
			Console.WriteLine("Loading static CK2 dynasties...");
			CK2Dynasties = new Dictionary<int, CK2Dynasty>();
			var dynFiles = GetFilesFor(@"common\dynasties");
			foreach (var dynFile in dynFiles)
			{
				var data = PdxSublist.ReadFile(dynFile);
				data.ForEachSublist(sub =>
				{
					CK2Dynasties[int.Parse(sub.Key)] = new CK2Dynasty(this, sub.Value);
				});
			}
		}

		private void LoadBuildings()
		{
			Console.WriteLine("Loading CK2 buildings...");
			CK2Buildings = new Dictionary<string, CK2Building>();

			var buildingFiles = GetFilesFor("common\\buildings");

			foreach(var file in buildingFiles)
			{
				var data = PdxSublist.ReadFile(file);
				data.ForEachSublist(sub =>
				{
					sub.Value.ForEachSublist(building =>
					{
						CK2Buildings.Add(building.Key, new CK2Building(building.Value));
					});
				});
			}
		}

		private void LoadProvCounties()
		{
			Console.WriteLine("Loading CK2 provinces...");
			CK2ProvCounties = new Dictionary<string, string>();
			var provFiles = GetFilesFor("common\\province_setup");
			foreach (var provFile in provFiles)
			{
				var data = PdxSublist.ReadFile(provFile);
				data.ForEachSublist(prov =>
				{
					if (prov.Value.KeyValuePairs.ContainsKey("title"))
					{
						CK2ProvCounties.Add(prov.Key, prov.Value.KeyValuePairs["title"]);
					}
				});
			}

			var mapFiles = GetFilesFor("map");

			var provMapFile = mapFiles.Single(m => Path.GetFileName(m) == "provinces.bmp");
			ProvinceMap = new Bitmap(provMapFile);
			CK2ProvColours = new Dictionary<int, string>();
			var defFile = mapFiles.Single(m => Path.GetFileName(m) == "definition.csv");

			using (TextFieldParser parser = new TextFieldParser(defFile))
			{
				parser.TextFieldType = FieldType.Delimited;
				parser.SetDelimiters(";");
				parser.ReadFields();
				while (!parser.EndOfData)
				{

					//Process row
					string[] fields = parser.ReadFields();
					var col = Color.FromArgb(int.Parse(fields[1]), int.Parse(fields[2]), int.Parse(fields[3]));
					CK2ProvColours[col.ToArgb()] = fields[0];
				}
			}
			Console.WriteLine("Loading province map...");
			CK2ProvPositions = new Dictionary<string, List<Point>>();
			var bmp = ProvinceMap;
			for (int y = 0; y < bmp.Height; y++)
			{
				for (int x = 0; x < bmp.Width; x++)
				{
					var pxColour = bmp.GetPixel(x, y).ToArgb();
					if (CK2ProvColours.ContainsKey(pxColour)) {
						var provID = CK2ProvColours[pxColour];
						if (!CK2ProvPositions.ContainsKey(provID))
						{
							CK2ProvPositions[provID] = new List<Point>();
						}
						CK2ProvPositions[provID].Add(new Point(x, y));
					}
				}
			}
		}

		internal void AddIndependentTitle(CK2Title ck2Title)
		{
			CK2IndependentTitles[ck2Title.Name] = ck2Title;
		}

		private void LoadCulture()
		{
			Console.WriteLine("Loading CK2 cultures...");
			CK2Cultures = new Dictionary<string, CK2Culture>();
			var cultureFiles = GetFilesFor(@"common\cultures");

			foreach (var file in cultureFiles)
			{
				var cultureGroups = PdxSublist.ReadFile(file);
				cultureGroups.ForEachSublist(culGroup =>
				{
					culGroup.Value.ForEachSublist(rel =>
					{
						if (rel.Key != "graphical_cultures")
						{
							CK2Cultures[rel.Key] = new CK2Culture(rel.Value, this);
						}
					});
				});
			}
		}

		private void LoadReligion()
		{
			Console.WriteLine("Loading CK2 religions...");
			CK2Religions = new Dictionary<string, CK2Religion>();
			var religionFiles = GetFilesFor(@"common\religions");
			
			foreach (var file in religionFiles)
			{
				var religionGroups = PdxSublist.ReadFile(file);
				religionGroups.ForEachSublist(relGroup =>
				{
					if (relGroup.Key == "secret_religion_visibility_trigger")
					{
						return;
					}
					relGroup.Value.ForEachSublist(rel =>
					{
						if(!(rel.Key == "color" || rel.Key == "male_names" || rel.Key == "female_names"))
						{
							CK2Religions[rel.Key] = new CK2Religion(rel.Value);
						}
					});
				});
			}
		}

		private void LoadTraits()
		{
			Console.WriteLine("Loading CK2 traits...");
			CK2Traits = new List<CK2Traits>();
			CK2Traits.Add(null);
			var traitFiles = GetFilesFor(@"common\traits");
			traitFiles.Sort();
			foreach (var file in traitFiles)
			{
				var traits = PdxSublist.ReadFile(file);
				traits.ForEachSublist(trait =>
				{
					CK2Traits.Add(new CK2Traits(trait));
				});
			}
		}

		public List<string> GetFilesFor(string path)
		{

			var modPath = Path.Combine(ModPath, path);
			var gameFiles = Directory.GetFiles(Path.Combine(GAME_PATH, path));
			var modFileNames = Directory.Exists(modPath) ? Directory.GetFiles(modPath).Select(Path.GetFileName) : new string[] { };
			var files = new List<string>();
			foreach (var name in gameFiles)
			{
				if (modFileNames.Contains(Path.GetFileName(name)))
				{
					files.Add(Path.Combine(modPath, Path.GetFileName(name)));
				}
				else
				{
					files.Add(name);
				}
			}
			foreach (var name in modFileNames)
			{
				var modFilePath = Path.Combine(modPath, Path.GetFileName(name));
				if (!files.Contains(modFilePath))
				{
					files.Add(modFilePath);
				}
			}
			return files;
		}

		public void AddTitle(CK2Title title)
		{
			CK2Titles.Add(title.Name, title);
		}
	}
}
