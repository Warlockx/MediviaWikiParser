using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediviaWikiParser.Services;

namespace MediviaWikiParser
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string saveLocation = Path.Combine(Directory.GetCurrentDirectory(),"Images");
            MonstersService monsters = new MonstersService(saveLocation);
            monsters.GetMonsters(false,true).Wait();
            Console.ReadKey();
        }
        
    }
}
