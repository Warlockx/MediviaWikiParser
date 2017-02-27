using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediviaWikiParser.Models;
using MediviaWikiParser.Services;
using Newtonsoft.Json;

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

                Console.WriteLine("Converting into json.");
                //transform into json
                string json = JsonConvert.SerializeObject(creatures, Formatting.Indented);

                //save
                string currentDirectory = Directory.GetCurrentDirectory();
                if (!Directory.Exists(currentDirectory + "/json"))
                    Directory.CreateDirectory(currentDirectory + "/json");

                Console.WriteLine($"Saving to file at {currentDirectory}/json/monsters.json");
                File.WriteAllText($"{currentDirectory}/json/monsters.json", json);

                Console.WriteLine("Finished.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadKey();
        }
    }
}
