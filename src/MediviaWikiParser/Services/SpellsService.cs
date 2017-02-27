using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MediviaWikiParser.Models;

namespace MediviaWikiParser.Services
{
    public class SpellsService
    {
        private readonly string _saveLocation;
        private readonly HttpClient _httpClient;

        public SpellsService(string saveLocation)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://medivia.wikispaces.com");
            _saveLocation = saveLocation;
        }

        public async Task<IEnumerable<Spell>> GetSpells()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("/All Spells");

            return  !response.IsSuccessStatusCode ? null : await ParseSpells(response);
        }

        private static async Task<IEnumerable<Spell>> ParseSpells(HttpResponseMessage response)
        {
            string content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content)) return null;

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(content);

            HtmlNodeCollection spellRows = document.DocumentNode.SelectNodes("//table[@class='wiki_table']/tr");

            return spellRows != null ? ParseRows(spellRows) : null;
        }

        private static IEnumerable<Spell> ParseRows(HtmlNodeCollection spellRows)
        {
            List<Spell> spells = new List<Spell>();
            foreach (HtmlNode row in spellRows)
            {
                if (row.InnerHtml.Contains("ML Create")) continue;

                HtmlNodeCollection spellCells = row.SelectNodes("td");
                if (spellCells.Count < 10) continue;

                int castMagicLevel = ParseNumberValue(spellCells[1].InnerText.TrimEnd('\n'));
                int useMagicLevel =  ParseNumberValue(spellCells[2].InnerText.TrimEnd('\n'));
                string formula = spellCells[3].InnerText.TrimEnd('\n');
                string name = spellCells[4].InnerText.TrimEnd('\n');
                int manaCost = ParseNumberValue(spellCells[5].InnerText.TrimEnd('\n'));
                IEnumerable<string> vocationsToCast = spellCells[6].InnerText
                    .Split('\n')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s));

                int charges = ParseNumberValue(spellCells[7].InnerText.TrimEnd('\n'));
                bool needsPremium = spellCells[8].InnerText.Contains("yes");

                spells.Add(new Spell(name,formula,vocationsToCast,castMagicLevel,useMagicLevel,manaCost,charges,needsPremium));
            }
            return spells;
        }

        private static int ParseNumberValue(string valueString)
        { 
            if (valueString.Equals("-")) return 0;
            int result;
            int.TryParse(valueString, out result);
            return result;
        }

    }
}
