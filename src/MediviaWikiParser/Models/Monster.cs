using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediviaWikiParser.Models
{
    public class Creature
    {
        public string ImageUrl { get; }
        public string Name { get; }
        public int Experience { get; }
        public int Hitpoints { get; }
        public int SummonCost { get; }
        public int ConvinceCost { get; }
        public bool Pushable { get; }
        public bool CanPushObjects { get; }
        public IEnumerable<Element> CanWalkOn { get; }
        public int DamagePerTurn { get; }
        public IEnumerable<DamageType> Immunities { get; }
        public IEnumerable<DamageType> NeutralTo { get; }
        public IEnumerable<string> Sounds { get; }
        public TaskInfo Task { get; }
        public string Notes { get; }
        public IEnumerable<string> WhereToFind { get; }
        public string Strategy { get; }
        public IEnumerable<Loot> Loot { get; }
        
        public Creature(string imageUrl, string name, int experience, int hitpoints, int summonCost, int convinceCost, bool pushable, bool canPushObjects, IEnumerable<Element> canWalkOn, int maxDamagePerTurn, IEnumerable<DamageType> immunities, IEnumerable<DamageType> neutralTo, IEnumerable<string> sounds, TaskInfo task, string notes, IEnumerable<string> whereToFind, string strategy, IEnumerable<Loot> loot)
        {
            ImageUrl = imageUrl;
            Name = name;
            Experience = experience;
            Hitpoints = hitpoints;
            SummonCost = summonCost;
            ConvinceCost = convinceCost;
            Pushable = pushable;
            CanPushObjects = canPushObjects;
            CanWalkOn = canWalkOn;
            DamagePerTurn = maxDamagePerTurn;
            Immunities = immunities;
            NeutralTo = neutralTo;
            Sounds = sounds;
            Task = task;
            Notes = notes;
            WhereToFind = whereToFind;
            Strategy = strategy;
            Loot = loot;
        }

        public Creature(string imageUrl, string name, int experience, int hitpoints)
        {
            ImageUrl = imageUrl;
            Name = name;
            Experience = experience;
            Hitpoints = hitpoints;
        }
    }
}
