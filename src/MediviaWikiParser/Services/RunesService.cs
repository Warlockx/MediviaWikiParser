using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MediviaWikiParser.Models;

namespace MediviaWikiParser.Services
{
    public class RunesService
    {
        private readonly HttpClient _httpClient;

        public RunesService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://medivia.wikispaces.com");
        }

        public async Task<IEnumerable<Rune>> GetRunes()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("/Runes");

            return !response.IsSuccessStatusCode ? null : await ParseRunes(response);
        }

        private static async Task<IEnumerable<Rune>> ParseRunes(HttpResponseMessage response)
        {
            string content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content)) return null;

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(content);

            HtmlNodeCollection runeRows = document.DocumentNode.SelectNodes("//table[@class='wiki_table']/tr");

            return runeRows != null ? ParseRows(runeRows) : null;
        }

        private static IEnumerable<Rune> ParseRows(HtmlNodeCollection runeRows)
        {
            List<Rune> runes = new List<Rune>();
            foreach (HtmlNode row in runeRows)
            {
                if (row.InnerHtml.Contains("ML Create")) continue;

                HtmlNodeCollection runeCells = row.SelectNodes("td");
                if (runeCells.Count < 10) continue;

                int castMagicLevel = ParseNumberValue(runeCells[1].InnerText.TrimEnd('\n'));
                int useMagicLevel = ParseNumberValue(runeCells[2].InnerText.TrimEnd('\n'));
                string formula = runeCells[3].InnerText.TrimEnd('\n');
                string name = runeCells[4].InnerText.TrimEnd('\n');
                int manaCost = ParseNumberValue(runeCells[5].InnerText.TrimEnd('\n'));
                IEnumerable<string> vocationsToCast = runeCells[6].InnerText
                    .Split('\n')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s));

                int charges = ParseNumberValue(runeCells[7].InnerText.TrimEnd('\n'));
                int price = ParseNumberValue(runeCells[7].InnerText.Replace("gp","").TrimEnd('\n'));
                bool needsPremium = runeCells[8].InnerText.Contains("yes");

                runes.Add(new Rune(name, formula, vocationsToCast, castMagicLevel, useMagicLevel, manaCost, charges,price, needsPremium));
            }
            return runes;
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

