using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediviaWikiParser.Models
{
    public class MonsterAbility
    {
        public string Name { get; }
        public int MinRange { get; }
        public int MaxRange { get; }

        public MonsterAbility(string name, int minRange, int maxRange)
        {
            Name = name;
            MinRange = minRange;
            MaxRange = maxRange;
        }

        public MonsterAbility(string name)
        {
            Name = name;
        }
    }
}
