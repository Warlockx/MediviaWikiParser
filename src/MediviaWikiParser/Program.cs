using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediviaWikiParser.Models;
using MediviaWikiParser.Services;

namespace MediviaWikiParser
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Run().Wait(); 
        }

        private static async Task Run()
        {
            string saveLocation = Path.Combine(Directory.GetCurrentDirectory(), "Images");
            MonstersService monsters = new MonstersService(saveLocation);
            try
            {
                IEnumerable<Creature> creatures = await monsters.GetMonsters(false, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadKey();
        }
    }
}
