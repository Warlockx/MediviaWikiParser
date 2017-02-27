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
            // GetMonsters().Wait(); 
             GetSpells().Wait();
            //GetRunes().Wait();
            Console.ReadKey();
        }

        private static async Task GetSpells()
        {
            string saveLocation = Path.Combine(Directory.GetCurrentDirectory(), "Spells");
            SpellsService spellService = new SpellsService();
            try
            {
                IEnumerable<Spell> spells = await spellService.GetSpells();
                SaveJson(spells, saveLocation, "spells");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        private static async Task GetRunes()
        {
            string saveLocation = Path.Combine(Directory.GetCurrentDirectory(), "Spells");
            RunesService runeService = new RunesService();
            try
            {
                IEnumerable<Rune> spells = await runeService.GetRunes();
                SaveJson(spells, saveLocation, "runes");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        private static async Task GetMonsters()
        {
            string saveLocation = Path.Combine(Directory.GetCurrentDirectory(), "Monsters");
            MonstersService monsters = new MonstersService(saveLocation);
            try
            {
                IEnumerable<Creature> creatures = await monsters.GetMonsters(false, true);

                SaveJson(creatures,saveLocation,"monsters");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void SaveJson(object deserializedObject, string saveLocation, string fileName)
        {

            string json = JsonConvert.SerializeObject(deserializedObject, Formatting.Indented);

            if (!Directory.Exists(saveLocation))
                Directory.CreateDirectory(saveLocation);
           
            File.WriteAllText(Path.Combine(saveLocation, fileName + ".json"), json);
        }
    }
}
