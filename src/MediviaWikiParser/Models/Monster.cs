﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediviaWikiParser.Models
{
    public class Monster
    {
        public string ImageUrl { get; }
        public string Name { get; }
        public int Experience { get;}
        public int Hitpoints { get; }

        public Monster(string imageUrl, string name, int experience, int hitpoints)
        {
            ImageUrl = imageUrl;
            Name = name;
            Experience = experience;
            Hitpoints = hitpoints;
        }
    }
}
