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
        private string _saveLocation;
        private HttpClient _httpClient;

        public MonstersService(string saveLocation)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://medivia.wikispaces.com");
            _saveLocation = saveLocation;
        }

     
        public async Task<IEnumerable<Creature>> GetMonsters(bool savePictures,bool getDetails)
        {
            List<Creature> creatures = new List<Creature>();
            HttpResponseMessage response = await _httpClient.GetAsync("/Monsters");

            if (!response.IsSuccessStatusCode) return null;

            string html = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(html)) return null;

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            HtmlNodeCollection creatureRows = document.DocumentNode.SelectNodes("//table[@class='wiki_table']/tr");

            GetSimpleInfo(ref creatures,creatureRows);

            if (getDetails)
               await GetDetailedInfo(creatures);

            if (!savePictures) return creatures;

            if (!Directory.Exists(_saveLocation))
                Directory.CreateDirectory(_saveLocation);

            foreach (Creature creature in creatures)
                await SaveImage(creature);

            return creatures;
        }

        private async Task<IEnumerable<Creature>> GetDetailedInfo(IEnumerable<Creature> oldCreatures)
        {
            List<Creature> creatures = new List<Creature>(oldCreatures);
            for (int i = 0; i < creatures.Count; i++)
            {
                Creature creature = creatures[i];
                HttpResponseMessage response = await _httpClient.GetAsync($"/{creature.Name}");

                if (!response.IsSuccessStatusCode) continue;

                string html = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(html)) continue;

               /* creatures[i] =*/ ParseDetailPage(creature, html);
            }
            return creatures;
        }

        private static void GetSimpleInfo(ref List<Creature> creatures,HtmlNodeCollection creatureRows)
        {
            foreach (HtmlNode creatureNode in creatureRows)
            {
                if (creatureNode.InnerHtml.Contains("Experience")) continue;

                Creature monster = ParseRow(creatureNode);
                if (monster != null)
                    creatures.Add(monster);
            }
        }

        private static Creature ParseRow(HtmlNode node)
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
            
           return new Creature(pictureUrl,creatureName,creatureExperience,creatureHitpoints);
        }

        private static void ParseDetailPage(Creature creature,string html)
        {
           
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            HtmlNode informationContainer = document.DocumentNode.SelectSingleNode("//div[@class='wiki wikiPage']");
            string[] information = informationContainer.InnerText.Split('\n');

            Tuple<int, int> summonInfo = ParseSummonInfo(information.FirstOrDefault(s=>s.Contains("Summon/Convince:")));
            IEnumerable<Ability> abilities = ParseAbilities(information.FirstOrDefault(s => s.Contains("Abilities:")));
            bool? pushable = information.FirstOrDefault(s => s.Contains("Pushable:"))?.Contains("Tick.jpg");
            bool? pushObjects = information.FirstOrDefault(s => s.Contains("Push Objects"))?.Contains("Tick.jpg");
            IEnumerable<Element> walksOn = ParseWalksOn(information.FirstOrDefault(s => s.Contains("Walks around:")));
            int damagePerTurn = ParseDamage(information.FirstOrDefault(s => s.Contains("Est. Max. Damage:")));
            IEnumerable<DamageType> immunities = ParseImmunities(information.FirstOrDefault(s => s.Contains("Immune")));
            Console.WriteLine(creature.Name);
        }

        private static IEnumerable<DamageType> ParseImmunities(string immunitiesString)
        {
            List<DamageType> immunities = new List<DamageType>();
            if (string.IsNullOrEmpty(immunitiesString) || immunitiesString.Contains("None")) return immunities;
            immunitiesString = immunitiesString.Substring(immunitiesString.IndexOf(':')+1); 

            string[] immunityStrings = immunitiesString.Split(',');
            foreach (string immunityString in immunityStrings)
            {
                object damageType = immunityString.GetEnum<DamageType>();
                if (damageType != null)
                    immunities.Add((DamageType)damageType);
            }
            return immunities;
        }

        private static IEnumerable<Ability> ParseAbilities(string abilityNode)
        {
            List<Ability> abilityList = new List<Ability>();
            if (string.IsNullOrEmpty(abilityNode) || abilityNode.Contains("None")) return abilityList;
            abilityNode = abilityNode.Replace("Abilities:","");

            IEnumerable<string> abilityStrings = Regex.Split(abilityNode.Replace(".", "").Replace(":", ""), @"[)],|, (?=[A-Z])|[)] (?=[A-Z])|; (?=[A-Z])").Where(string.IsNullOrWhiteSpace);

            foreach (string abilityString in abilityStrings)
            {
                if (!abilityString.Contains('-'))
                    abilityList.Add(new Ability(abilityString.Trim()));
                else
                {
                    string[] abilityWithRange = abilityString
                        .Replace(")", "").Replace("?","")
                        .Split(new[] {"(","-"}, StringSplitOptions.RemoveEmptyEntries);

                    if (abilityWithRange.Length < 3)
                    {
                        abilityList.Add(new Ability(abilityString));
                        continue;
                    }
                      

                    int minRange;
                    int maxRange;
                    int.TryParse(abilityWithRange[1], out minRange);
                    int.TryParse(abilityWithRange[2], out maxRange);
                    
                    abilityList.Add(new Ability(abilityWithRange[0], minRange, maxRange));
                }
            }
            return abilityList;
        }

        private static Tuple<int, int> ParseSummonInfo(string summonNode)
        {
            if (string.IsNullOrEmpty(summonNode)) return new Tuple<int, int>(0,0);
            //Summon/Convince: 220/220 (Illusionable)
            summonNode = summonNode.Replace("Summon/Convince:","").Replace(" (Illusionable)","");
            string[] splitValue = summonNode?.Split('/');
            if (splitValue.Length < 2) return new Tuple<int, int>(0, 0);
            int summonCost;
            int convinceCost;
            int.TryParse(splitValue?[0], out summonCost);
            int.TryParse(splitValue?[1], out convinceCost);
            return new Tuple<int, int>(summonCost,convinceCost);
        }

        private static IEnumerable<Element> ParseWalksOn(string walksOnNode)
        {
            List<Element> elements = new List<Element>();
            if (string.IsNullOrEmpty(walksOnNode)) return elements;
            walksOnNode = walksOnNode.Replace("Walks around:","");
         
            string[] elementStrings = walksOnNode.Split(',');
            foreach (string element in elementStrings)
            {
                object ele = element.GetEnum<Element>();
                if (ele != null)
                    elements.Add((Element)ele);
            }
            return elements;
        }

        private static int ParseDamage(string damageNode)
        {
            if (string.IsNullOrEmpty(damageNode)) return 0;
            damageNode = damageNode.Replace("Est. Max. Damage:","").Replace("hp per turn","");
            int value;
            int.TryParse(damageNode, out value);
            return value;
        }

        private async Task SaveImage(Creature creature)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(creature.ImageUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to retrieve the image pf the {creature.Name}");
                return;
            }
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
