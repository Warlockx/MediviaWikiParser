using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediviaWikiParser.Models
{
    public class Loot
    {
        public string Name { get;}
        public int MinDropAmount { get; }
        public int MaxDropAmount { get; }
        public string Rarity { get; }

        public Loot(string name, int minDropAmount, int maxDropAmount, string rarity)
        {
            Name = name;
            MinDropAmount = minDropAmount;
            MaxDropAmount = maxDropAmount;
            Rarity = rarity;
        }

        public Loot(string name, int minDropAmount, int maxDropAmount)
        {
            Name = name;
            MinDropAmount = minDropAmount;
            MaxDropAmount = maxDropAmount;
        }

        public Loot(string name, string rarity)
        {
            Name = name;
            Rarity = rarity;
        }

        public Loot(string name)
        {
            Name = name;
        }
    }
}
