using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediviaWikiParser.Models
{
    public class Ability
    {
        public string Name { get; }
        public int MinRange { get; }
        public int MaxRange { get; }

        public Ability(string name, int minRange, int maxRange)
        {
            Name = name;
            MinRange = minRange;
            MaxRange = maxRange;
        }

        public Ability(string name)
        {
            Name = name;
        }
    }
}
