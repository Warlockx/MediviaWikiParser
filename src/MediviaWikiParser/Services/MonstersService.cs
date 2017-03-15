using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MediviaWikiParser.Models;

namespace MediviaWikiParser.Services
{
    public class MonstersService
    {
        private readonly string _saveLocation;
        private readonly HttpClient _httpClient;

        public MonstersService(string saveLocation)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://medivia.wikispaces.com");
            _saveLocation = saveLocation;
        }

     
        public async Task<IEnumerable<Monster>> GetMonsters(bool savePictures,bool getDetails)
        {
            List<Monster> creatures = new List<Monster>();
            HttpResponseMessage response = await _httpClient.GetAsync("/Monsters");

            if (!response.IsSuccessStatusCode) return null;

            string html = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(html)) return null;

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            HtmlNodeCollection creatureRows = document.DocumentNode.SelectNodes("//table[@class='wiki_table']/tr");
#if DEBUG
            Console.WriteLine("Monsters: Getting simple info.");
#endif
            GetSimpleInfo(ref creatures,creatureRows);

            if (getDetails)
               await GetDetailedInfo(creatures);

            if (!savePictures) return creatures;

            if (!Directory.Exists(_saveLocation))
                Directory.CreateDirectory(_saveLocation);

            foreach (Monster creature in creatures)
                await SaveImage(creature);

            return creatures;
        }

        private async Task<IEnumerable<Monster>> GetDetailedInfo(IEnumerable<Monster> oldCreatures)
        {
            List<Monster> creatures = new List<Monster>(oldCreatures);
            for (int i = 0; i < creatures.Count; i++)
            {
#if DEBUG
                Console.WriteLine($"Monsters: Getting detailed info from creature {i}/{creatures.Count-1}.");
#endif
                Monster creature = creatures[i];
                HttpResponseMessage response = await _httpClient.GetAsync($"/{creature.Name}");

                if (!response.IsSuccessStatusCode) continue;

                string html = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(html)) continue;

                creatures[i] = ParseDetailPage(creature, html);
            }
            return creatures;
        }

        private static void GetSimpleInfo(ref List<Monster> creatures,HtmlNodeCollection creatureRows)
        {
            foreach (HtmlNode creatureNode in creatureRows)
            {
                if (creatureNode.InnerHtml.Contains("Experience")) continue;

                Monster monster = ParseRow(creatureNode);
                if (monster != null)
                    creatures.Add(monster);
            }
        }

        private static Monster ParseRow(HtmlNode node)
        {
            HtmlNodeCollection creatureCells = node.SelectNodes("td");
            if (creatureCells.Count < 7) return null;
          
            string pictureUrl = creatureCells[0].ChildNodes.Any(n => n.Name == "div") ? 
                creatureCells[0].SelectSingleNode("div")?.FirstChild?.GetAttributeValue("src", "") :
                creatureCells[0].SelectSingleNode("img")?.GetAttributeValue("src", "");

            pictureUrl = WebUtility.HtmlDecode(pictureUrl);
           
            string creatureName = creatureCells[1].InnerText;
            creatureName = creatureName.Remove(creatureName.Length - 1); 

            int creatureExperience;
            int.TryParse(creatureCells[2].InnerText, out creatureExperience);
            int creatureHitpoints;
            int.TryParse(creatureCells[3].InnerText, out creatureHitpoints);
            
           return new Monster(pictureUrl,creatureName,creatureExperience,creatureHitpoints);
        }

        private static Monster ParseDetailPage(Monster creature, string html)
        {

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            HtmlNode informationContainer = document.DocumentNode.SelectSingleNode("//div[@class='wiki wikiPage']");
            string[] information = informationContainer.InnerText.Split('\n');

            Tuple<int, int> summonInfo = ParseSummonInfo(information.FirstOrDefault(s => s.Contains("Summon/Convince")));
            IEnumerable<MonsterAbility> abilities = ParseAbilities(information.FirstOrDefault(s => s.Contains("Abilities")));
            bool? pushable = information.FirstOrDefault(s => s.Contains("Pushable"))?.Contains("Tick.jpg");
            bool? pushObjects = information.FirstOrDefault(s => s.Contains("Push Objects"))?.Contains("Tick.jpg");
            IEnumerable<Element> walksOn = ParseWalksOn(information.FirstOrDefault(s => s.Contains("Walks around")));
            int damagePerTurn = ParseDamage(information.FirstOrDefault(s => s.Contains("Est. Max. Damage")));
            IEnumerable<DamageElement> immunities = ParseDamageTypes(information.FirstOrDefault(s => s.Contains("Immune")));
            IEnumerable<DamageElement> neutralities =
                ParseDamageTypes(information.FirstOrDefault(s => s.Contains("Neutral")));
            IEnumerable<string> sounds = ParseSounds(information.FirstOrDefault(s => s.Contains("Sounds")));
            string strategy = ParseSimpleField(information.FirstOrDefault(s => s.Contains("Strategy")));
            IEnumerable<string> whereToFind = ParseLocation(information.FirstOrDefault(s => s.Contains("Location")));
            IEnumerable<Loot> loots = ParseLoot(information.FirstOrDefault(s => s.Contains("Loot")));
           
            creature.SummonCost = summonInfo.Item1;
            creature.ConvinceCost = summonInfo.Item2;
            creature.Abilities = abilities;
            if (pushable != null) creature.Pushable = pushable.Value;
            if (pushObjects != null) creature.CanPushObjects = pushObjects.Value;
            creature.CanWalkOn = walksOn;
            creature.DamagePerTurn = damagePerTurn;
            creature.Immunities = immunities;
            creature.NeutralTo = neutralities;
            creature.Sounds = sounds;
            creature.Strategy = strategy;
            creature.WhereToFind = whereToFind;
            creature.Loot = loots;
            return creature;
        }

        private static IEnumerable<Loot> ParseLoot(string lootStrings)
        {
            if (string.IsNullOrEmpty(lootStrings) || lootStrings.Contains("Nothing")) return null;
            IEnumerable<string> lootSplit = lootStrings.Replace(".","")
                                                       .Replace("gp","Gold Coin")
                                                       .Substring(lootStrings.IndexOf(':') + 1)
                                                       .Split(',')
                                                       .Select(s => s.TrimStart());
            List<Loot> loots = new List<Loot>();
            foreach (string loot in lootSplit)
            {
                List<string> lootDetails = Regex.Split(loot, @"[-](?=\d{1,3}|[?])|(\d{1,3}|[?])\s(?=\w)|[(|)]").Where(s=> !string.IsNullOrEmpty(s)).ToList();
              
                switch (lootDetails.Count)
                {
                    case 1:
                    {
                        if (loot.Length > 2)
                            loots.Add(new Loot(loot.TrimEnd()));
                        break;
                    }
                    case 2:
                    {
                        if(lootDetails[0].Length > 2)
                        loots.Add(new Loot(lootDetails[0].Trim(), lootDetails[1].Replace("-", "")));
                        break;
                    }
                    case 3:
                    {
                        if (lootDetails[2].Length > 2)
                        {
                            int minDropQuantity, maxDropQuantity;
                            int.TryParse(lootDetails[0], out minDropQuantity);
                            int.TryParse(lootDetails[1], out maxDropQuantity);
                            loots.Add(new Loot(lootDetails[2].Trim(),minDropQuantity, maxDropQuantity));
                        }
                        break;
                    }
                    case 4:
                    {
                        if (lootDetails[2].Length > 2)
                        {
                            int minDropQuantity, maxDropQuantity;
                            int.TryParse(lootDetails[0], out minDropQuantity);
                            int.TryParse(lootDetails[1], out maxDropQuantity);
                            loots.Add(new Loot(lootDetails[2].Trim(),
                               minDropQuantity, maxDropQuantity, lootDetails[3].Replace("-", "")));
                        }
                        break;
                    }
                }
            }
            return loots;
        }

        private static IEnumerable<string> ParseLocation(string locationStrings)
        {
            return string.IsNullOrEmpty(locationStrings) ? null 
                            : locationStrings
                            .Substring(locationStrings.IndexOf(':') + 1)
                            .Split(',')
                            .Select(l => l.TrimStart())
                            .Where(s=> !string.IsNullOrEmpty(s));
        }
       
        private static string ParseSimpleField(string str)
        {
            if (string.IsNullOrEmpty(str)) return "None";
            int index = str.IndexOf(':') +1;
            return str.Length > index ? str.Substring(index).TrimStart() : "None";
        }

        private static IEnumerable<string> ParseSounds(string soundsString)
        {
            if (string.IsNullOrEmpty(soundsString)) return null;
            soundsString = soundsString.Substring(soundsString.IndexOf(':') + 1).TrimStart();
            if (soundsString.StartsWith("None")) return null;

            List<string> sounds = new List<string>();
            soundsString = WebUtility.HtmlDecode(soundsString).Replace("\"", "");
            string[] soundStrings = soundsString.Split(';');
            sounds.AddRange(soundStrings.Select(sound => sound.TrimStart()).Where(s => !string.IsNullOrEmpty(s) && s.Length > 1));
            return sounds;
        }

        private static IEnumerable<DamageElement> ParseDamageTypes(string damageTypesString)
        {
            if (string.IsNullOrEmpty(damageTypesString) || damageTypesString.Contains("None")) return null;
            return damageTypesString.Substring(damageTypesString.IndexOf(':') + 1)
                                    .TrimStart()
                                    .Split(',')
                                    .Select(immunityString => immunityString.GetEnum<DamageElement>())
                                    .Where(damageType => damageType != null)
                                    .Cast<DamageElement>()
                                    .ToList();
        }

        private static IEnumerable<MonsterAbility> ParseAbilities(string abilityNode)
        {
            if (string.IsNullOrEmpty(abilityNode) || abilityNode.Contains("None")) return null;
            List<MonsterAbility> abilityList = new List<MonsterAbility>();
            abilityNode = abilityNode.Substring(abilityNode.IndexOf(':') + 1)
                                     .Replace(".", "")
                                     .Replace(":", "");

            string[] abilityStrings = Regex.Split(abilityNode, @"[)],|[,|)|;] (?=[A-Z])");

            foreach (string abilityString in abilityStrings)
            {
                if (string.IsNullOrEmpty(abilityString)) continue;

                string ability = abilityString.Trim();
                if (!ability.Contains('-'))
                    abilityList.Add(new MonsterAbility(ability));
                else
                {
                    string[] abilityWithRange = ability
                        .Replace(")", "").Replace("?","")
                        .Split(new[] {"(","-"}, StringSplitOptions.RemoveEmptyEntries);

                    if (abilityWithRange.Length < 3)
                    {
                        abilityList.Add(new MonsterAbility(ability));
                        continue;
                    }
                    int minRange;
                    int maxRange;
                    int.TryParse(abilityWithRange[1], out minRange);
                    int.TryParse(abilityWithRange[2], out maxRange);
                    
                    abilityList.Add(new MonsterAbility(abilityWithRange[0], minRange, maxRange));
                }
            }
            return abilityList;
        }

        private static Tuple<int, int> ParseSummonInfo(string summonNode)
        {
            if (string.IsNullOrEmpty(summonNode)) return new Tuple<int, int>(0,0);
       
            string[] splitValue = summonNode.Substring(summonNode.IndexOf(':') + 1)
                                            .Replace(" (Illusionable)", "")
                                            .Split('/'); 

            if (splitValue.Length < 2) return new Tuple<int, int>(0, 0);
            int summonCost;
            int convinceCost;
            int.TryParse(splitValue[0], out summonCost);
            int.TryParse(splitValue[1], out convinceCost);
            return new Tuple<int, int>(summonCost,convinceCost);
        }

        private static IEnumerable<Element> ParseWalksOn(string walksOnNode)
        {
            List<Element> elements = new List<Element>();
            if (string.IsNullOrEmpty(walksOnNode)) return elements;

            string[] elementStrings = walksOnNode.Substring(walksOnNode.IndexOf(':') + 1).Split(',');
            elements
                .AddRange(elementStrings
                .Select(element => element.GetEnum<Element>())
                .Where(ele => ele != null).Cast<Element>());
            return elements;
        }

        private static int ParseDamage(string damageNode)
        {
            if (string.IsNullOrEmpty(damageNode)) return 0;
            damageNode = damageNode.Substring(damageNode.IndexOf(':') + 1).Replace("hp per turn","");
            int value;
            int.TryParse(damageNode, out value);
            return value;
        }

        private async Task SaveImage(Monster creature)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(creature.ImageUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to retrieve the image pf the {creature.Name}");
                return;
            }
#if DEBUG
            Console.WriteLine($"Monsters: Saving creature image from {creature.Name}.");
#endif
            using (Stream responseStream = await response.Content.ReadAsStreamAsync())
            {
                using (FileStream fileStream = new FileStream(Path.Combine(_saveLocation,$"{creature.Name}.gif"), FileMode.Create, FileAccess.Write))
                {
                    responseStream.CopyTo(fileStream);
                }
            }
        }
    }

}
