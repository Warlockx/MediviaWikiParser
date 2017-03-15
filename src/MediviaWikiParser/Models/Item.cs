using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MediviaWikiParser.Models
{
    public class Item
    {
        [JsonIgnore]
        public string ImageUrl { get; set; }

        public string  Name { get; set; }
        public int Armour { get; set; }
        public int Attack { get; set; }
        public bool TwoHanded { get; set; }
        public int AdditionalHitChance { get; set; }
        public string Attributes { get; set; }
        public int Defence { get; set; }
        public float Weight { get; set; }
        public int LootValue { get; set; }
        public IEnumerable<string> LootFrom { get; set; }
        public IEnumerable<string> BuyFrom { get; set; }
        public IEnumerable<string> SellTo { get; set; }
       
    }
}
