using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediviaWikiParser.Services;

namespace MediviaWikiParser
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MonstersService monsters = new MonstersService();
            monsters.GetMonsters(false).Wait();

        }
        
    }
}
