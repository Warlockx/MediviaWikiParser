using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediviaWikiParser.Models
{
    public class Loot
    {
        public string Name { get;}
        public int DropAmount { get; }
        public string Rarity { get; }

        public Loot(string name, int dropAmount, string rarity)
        {
            Name = name;
            DropAmount = dropAmount;
            Rarity = rarity;
        }
    }
}
