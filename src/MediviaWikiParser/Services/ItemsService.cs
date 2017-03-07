using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MediviaWikiParser.Models;

namespace MediviaWikiParser.Services
{
    public class ItemsService
    {
        private readonly string _saveLocation;
        private readonly HttpClient _httpClient;
       
        public ItemsService(string saveLocation)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://medivia.wikispaces.com");
            _saveLocation = saveLocation;
        }

        public async Task<IEnumerable<Item>> GetItems(bool savePictures, bool getDetails)
        {
            HttpResponseMessage response = await _httpClient.GetAsync("/Items");

            if (!response.IsSuccessStatusCode) return null;

            string html = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(html)) return null;


            List<Item> items = new List<Item>();
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            IEnumerable<string> itemCategories = GetItemCategories(document);

            foreach (string itemCategory in itemCategories)
            {
               items.AddRange(await GetCategoryItems(itemCategory)); 
            }

            if (getDetails)
                items.ForEach(i => GetItemDetailedInfo(ref i));

            if (savePictures)
                items.ForEach(SavePictures(_saveLocation));

            return items;
        }

        private Action<Item> SavePictures(string saveLocation)
        {
            throw new NotImplementedException();
        }

        private void GetItemDetailedInfo(ref Item item)
        {
            throw new NotImplementedException();
        }

        private async Task<IEnumerable<Item>> GetCategoryItems(string itemCategory)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"/{itemCategory}");

            if (!response.IsSuccessStatusCode) return null;

            string html = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(html)) return null;

            List<Item> items = new List<Item>();
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            HtmlNodeCollection tableNode = document.DocumentNode.SelectNodes("//table[@class='wiki_table']//td");

            throw new NotImplementedException();
        }

        private IEnumerable<string> GetItemCategories(HtmlDocument document)
        {
            HtmlNodeCollection categoryTables = document.DocumentNode.SelectNodes("//table[@class='wiki_table']");
            List<HtmlNode> categoryNodes = new List<HtmlNode>();
            foreach (HtmlNode categoryTable in categoryTables)
            {
                categoryNodes.AddRange(categoryTable.SelectNodes(".//a[@class='wiki_link']"));
            }
            return categoryNodes.Select(c => c.InnerText);
        }
    }
}
