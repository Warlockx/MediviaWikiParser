using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
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

     
        public async Task<IEnumerable<Creature>> GetMonsters(bool savePictures)
        {
            List<Creature> creatures = new List<Creature>();
            HttpResponseMessage response = await _httpClient.GetAsync("/Monsters");

            if (!response.IsSuccessStatusCode) return null;

            string html = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(html)) return null;

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            HtmlNodeCollection creatureRows = document.DocumentNode.SelectNodes("//table[@class='wiki_table']/tr");

            foreach (HtmlNode creatureNode in creatureRows)
            {
                if(creatureNode.InnerHtml.Contains("Experience")) continue;

                Creature monster = ParseRow(creatureNode);
                if (monster != null)
                    creatures.Add(monster);
            }

            if (!savePictures) return creatures;

            if (!Directory.Exists(_saveLocation))
                Directory.CreateDirectory(_saveLocation);

            foreach (Creature creature in creatures)
                await SaveImage(creature);

            return creatures;
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
