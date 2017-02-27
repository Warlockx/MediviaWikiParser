using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MediviaWikiParser.Models
{
    public class Creature
    {
        [JsonIgnore]
        public string ImageUrl { get; }
        public string Name { get; }
        public int Experience { get; }
        public int Hitpoints { get; }
        public int SummonCost { get; set; }
        public int ConvinceCost { get; set; }
        public IEnumerable<Ability> Abilities { get; set; }
        public bool Pushable { get; set; }
        public bool CanPushObjects { get; set; }
        public IEnumerable<Element> CanWalkOn { get; set; }
        public IEnumerable<DamageType> Immunities { get; set; }
        public IEnumerable<DamageType> NeutralTo { get; set; }
        public int DamagePerTurn { get; set; }
        public IEnumerable<string> Sounds { get; set; }
        public string Notes { get; set; }
        public IEnumerable<string> WhereToFind { get; set; }
        public string Strategy { get; set; }
        public IEnumerable<Loot> Loot { get; set; }

        public Creature(string imageUrl, string name, int experience, int hitpoints)
        {
            ImageUrl = imageUrl;
            Name = name;
            Experience = experience;
            Hitpoints = hitpoints;
        }
    }
}
