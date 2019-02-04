using PdxFile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK2Helper
{
	public enum Gender
	{
		male, female
	}
	public enum Attribute
	{
		diplomacy, martial, stewardship, intrigue, learning
	}
	public class AttributeSet
	{
		public int Diplomacy
		{
			get
			{
				return GetAttribute(Attribute.diplomacy);
			}
		}
		public int Martial
		{
			get
			{
				return GetAttribute(Attribute.martial);
			}
		}
		public int Stewardship
		{
			get
			{
				return GetAttribute(Attribute.stewardship);
			}
		}
		public int Intrigue
		{
			get
			{
				return GetAttribute(Attribute.intrigue);
			}
		}
		public int Learning
		{
			get
			{
				return GetAttribute(Attribute.learning);
			}
		}

		private Dictionary<Attribute, int> baseAttributes = new Dictionary<Attribute, int>();


		private CK2CharacterBase character;
		public AttributeSet(CK2CharacterBase character, List<int> attributes)
		{
			this.character = character;
			baseAttributes[Attribute.diplomacy] = attributes[0];
			baseAttributes[Attribute.martial] = attributes[1];
			baseAttributes[Attribute.stewardship] = attributes[2];
			baseAttributes[Attribute.intrigue] = attributes[3];
			baseAttributes[Attribute.learning] = attributes[4];
		}

		public int GetAttribute(Attribute att)
		{
			var val = baseAttributes[att];
			var attName = Enum.GetName(typeof(Attribute), att);
			val += character.Traits.Sum(t => t.Effects.FloatValues.ContainsKey(attName) ? (int)t.Effects.FloatValues[attName].Sum() : 0);
			return val;
		}
	}

	public enum Job
	{
		job_none, job_chancellor, job_martial, job_steward, job_spymaster, job_court_chaplain
	}

	public abstract class CK2CharacterBase
	{

		public int ID { get; set; }

		public CK2World World { get; set; }

		public bool IsPlayer { get; set; }

		public string Name { get; set; }

		public int DynastyID { get; set; }

		public string GovernmentType { get; set; }

		public Gender Gender { get; set; }

		public DateTime BirthDate { get; set; }

		public string CapitalID { get; private set; }
		public CK2Title Capital { get { return CapitalID == null ? Titles.Where(t => t.Rank == TitleRank.barony).FirstOrDefault() : World.CK2Titles[CapitalID]; } }

		public int FatherID { get; set; }
		public CK2CharacterBase Father
		{
			get
			{
				return FatherID == 0 ? null : World.CK2Characters[FatherID];
			}
			set
			{
				FatherID = value.ID;
			}
		}
		public int MotherID { get; set; }
		public CK2CharacterBase Mother
		{
			get
			{
				return MotherID == 0 ? null : World.CK2Characters[MotherID];
			}
			set
			{
				MotherID = value.ID;
			}
		}

		public int HeirID { get; set; }
		public CK2CharacterBase Heir
		{
			get
			{
				return HeirID == 0 ? 
					((PrimaryTitle.Succession == "primogeniture" || PrimaryTitle.Succession == "gavelkind") 
					? Children.OrderBy(c => c.BirthDate).FirstOrDefault() :  //if gavelkind or primo, order children by birth (oldest -> youngest) and pick first
					//if ultmo, order children by youngest to oldest and pick first
					(PrimaryTitle.Succession == "ultmogeniture" ? Children.OrderByDescending(c => c.BirthDate).FirstOrDefault() : null))
					: World.CK2Characters[HeirID];
			}
			set
			{
				HeirID = value.ID;
			}
		}
		public string PrimaryTitleID { get; set; }
		public CK2Title PrimaryTitle
		{
			get
			{
				return PrimaryTitleID == null ? Titles.OrderByDescending(t => t.Rank).First() : World.CK2Titles[PrimaryTitleID];
			}
		}
		public List<CK2Title> Titles { get; set; }

		/// <summary>
		/// Does this character directly own this title?
		/// </summary>
		/// <param name="title"></param>
		/// <returns></returns>
		public bool DoesDirectlyOwn(CK2Title title)
		{
			return Titles.Contains(title);
		}

		//if is revolt, liege is base title
		// else if liegetitle is revolt, liege is liegetitle's basetitle
		// else liege is liegetitle
		public CK2CharacterBase Liege { get { return PrimaryTitle.IsRevolt ? PrimaryTitle.BaseTitle.Holder : ((PrimaryTitle.LiegeTitle?.IsRevolt ?? false) ? PrimaryTitle.LiegeTitle.BaseTitle.Holder : PrimaryTitle.LiegeTitle?.Holder); } }

		public List<CK2Traits> Traits { get; set; }

		public AttributeSet Attribites { get; set; }

		public float Wealth { get; set; }

		public float Prestige { get; set; }
		public float Piety { get; set; }

		private CK2Religion religion;
		public CK2Religion Religion
		{
			get
			{
				return religion ?? Capital?.Province?.Religion ?? Titles.Where(t => t.Rank == TitleRank.county).GroupBy(t => t.Province.Religion).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
			}
			set
			{
				religion = value;
			}
		}

		public CK2Culture Culture { get; set; }

		//TODO: investigate multiple spouses - presumably this is something to do with concubines or secondary wives?
		public List<int> SpouseIDs { get; set; }

		public Job Job { get; set; }


		public List<string> Flags { get; set; }

		public List<string> Modifiers { get; set; }
		public List<CK2CharacterBase> Children { get; private set; }

		public CK2CharacterBase(CK2World world, PdxSublist data)
		{
			Children = new List<CK2CharacterBase>();
			World = world;
			ID = int.Parse(data.Key);
			Name = data.KeyValuePairs["bn"];
			FatherID = (int)GetFloat(data, "fat");
			MotherID = (int)GetFloat(data, "mot");
			Titles = new List<CK2Title>();
			Gender = (data.BoolValues.ContainsKey("fem") && data.BoolValues["fem"].Single()) ? Gender.female : Gender.male;
			if (data.FloatValues.ContainsKey("spouse"))
			{
				SpouseIDs = data.FloatValues["spouse"].ConvertAll(f => (int)f);
			}

			if (data.Sublists.ContainsKey("dmn"))
			{
				var dmn = data.Sublists["dmn"];
				if (dmn.KeyValuePairs.ContainsKey("primary"))
				{
					PrimaryTitleID = dmn.KeyValuePairs["primary"];
				}
				if (dmn.KeyValuePairs.ContainsKey("capital"))
				{
					CapitalID = dmn.KeyValuePairs["capital"];
				}
			}
			Attribites = new AttributeSet(this, data.Sublists["att"].FloatValues[string.Empty].Select(f => (int)f).ToList());

			Traits = new List<CK2Traits>();
			if (data.Sublists.ContainsKey("traits"))
			{
				data.Sublists["traits"].FloatValues[string.Empty].ForEach(id =>
				{
					Traits.Add(world.CK2Traits[(int)id]);
				});
			}

			Modifiers = new List<string>();


			data.Sublists.ForEach("md", (sub) =>
			{
				Modifiers.Add(sub.KeyValuePairs["modifier"]);
			});


			Prestige = GetFloat(data, "prs");
			Piety = GetFloat(data, "piety");

			if (data.KeyValuePairs.ContainsKey("gov"))
			{
				GovernmentType = data.KeyValuePairs["gov"];
			}

			IsPlayer = false;
			if (data.BoolValues.ContainsKey("player") && data.BoolValues["player"].Single())
			{
				IsPlayer = true;
			}

			if (data.FloatValues.ContainsKey("dnt"))
			{
				DynastyID = (int)data.FloatValues["dnt"].Single();
			}
			if (data.KeyValuePairs.ContainsKey("rel"))
			{
				var rel = data.KeyValuePairs["rel"];
				if (rel != "noreligion")
				{
					Religion = world.CK2Religions[rel];
				}
			}
			//Culture
			if (data.KeyValuePairs.ContainsKey("cul"))
			{
				var cul = data.KeyValuePairs["cul"];
				//if (rel != "noreligion")
				//{
				Culture = world.CK2Cultures[cul];
				//}
			}


			BirthDate = data.GetDate("b_d");
			if (ID == 664379)
			{
				Console.WriteLine("Me!");
			}
		}

		public void PostInitialise()
		{
			if (Mother != null)
			{
				Mother.AddChild(this);
			}
			if (Father != null)
			{
				Father.AddChild(this);
			}
			if (Religion == null)
			{
				Religion = Capital?.Province?.Religion ?? (DynastyID == 0 ? null : World.CK2Dynasties[DynastyID].Religion);
			}
			if (Culture == null)
			{
				Culture = Capital?.Province?.Culture ?? (DynastyID == 0 ? null : World.CK2Dynasties[DynastyID].Culture);
			}
		}

		private void AddChild(CK2CharacterBase child)
		{
			Children.Add(child);
		}

		public float GetFloat(PdxSublist data, string key)
		{
			return data.FloatValues.ContainsKey(key) ? data.FloatValues[key].Single() : 0;
		}


	}
}