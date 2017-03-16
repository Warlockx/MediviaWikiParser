using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MediviaWikiParser.Models;
using Microsoft.Extensions.Logging;

namespace MediviaWikiParser.Services
{
    public class ItemsService
    {
        private readonly string _saveLocation;
        private readonly HttpClient _httpClient;
        private  readonly ILogger _logger = ApplicationLogging.CreateLogger<ItemsService>();
        public ItemsService(string saveLocation)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://medivia.wikispaces.com");
            _saveLocation = saveLocation;
        }

        public async Task<IEnumerable<Item>> GetItems(bool savePictures, bool getDetails)
        {
            HttpResponseMessage response = await _httpClient.GetAsync("/Items");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Unexpected status code while getting items in {nameof(GetItems)}, statuscode: {response.StatusCode}");

            string html = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(html))
                throw new InvalidDataException($"Empty html content while getting items in {nameof(GetItems)}");


            List<Item> items = new List<Item>();
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            IEnumerable<string> itemCategories = GetItemCategories(document);

            foreach (string itemCategory in itemCategories)
            {
                IEnumerable<Item> categoryItems = await GetCategoryItems(itemCategory);
                if (categoryItems != null)
                    items.AddRange(categoryItems); 
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
            if (item.Name == "Life Fluid")
                Console.WriteLine();

            if (string.IsNullOrEmpty(item.ItemLink))
                return;

            HttpResponseMessage response = _httpClient.GetAsync(item.ItemLink).Result;

            if (!response.IsSuccessStatusCode)
              return;

            string html = response.Content.ReadAsStringAsync().Result;

            if (string.IsNullOrEmpty(html))
                throw new InvalidDataException($"Empty html content while getting items in {nameof(GetItemDetailedInfo)}");

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            HtmlNodeCollection infoNodes = document.DocumentNode.SelectNodes("//table[@class='wiki_table']/tr");

            HtmlNodeCollection lootValueNodes = infoNodes?.FirstOrDefault(n => n.InnerText.ToLower().Contains("loot value"))?.SelectNodes("td");
            if (lootValueNodes != null && lootValueNodes.Count > 1)
            {
                GetLootValue(item, lootValueNodes);

                _logger.LogInformation($"Tried to get the lootvalue wiki string: {lootValueNodes[1]?.InnerText} " +
                                       $"current item information: " +
                                       $"\n Name: {item.Name} \n LootValue: {item.LootValue}");
            }
        }

        private void GetLootValue(Item item, HtmlNodeCollection lootValueNode)
        {
            string[] splitRange = lootValueNode[1].InnerText.Split(new[] {"-", "/"},
                StringSplitOptions.RemoveEmptyEntries).Select(s=> s.Replace(".","").Replace(",","")).ToArray();

            int[] lootValueRange = new int[splitRange.Length];
            for (int i = 0; i < splitRange.Length; i++)
            {
                Match cleanNumbers = Regex.Match(splitRange[i], @"\d+");
                if (!cleanNumbers.Success)
                    continue;

                int.TryParse(cleanNumbers.Value, out lootValueRange[i]);

                if (Regex.Match(splitRange[i], @"(\d[k]{2}\s)").Success)
                    lootValueRange[i] = lootValueRange[i] * 1000000;
                else if (Regex.Match(splitRange[i], @"(\d[k]{1}\s)").Success)
                    lootValueRange[i] = lootValueRange[i] * 1000;
            }
            item.LootValue = (int)lootValueRange.Average();
        }

        private async Task<IEnumerable<Item>> GetCategoryItems(string itemCategory)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(itemCategory);

            if (!response.IsSuccessStatusCode) 
                throw new HttpRequestException($"Unexpected status code while getting items in {nameof(GetCategoryItems)}, statuscode: {response.StatusCode}");

            string html = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(html)) 
                throw new InvalidDataException($"Empty html content while getting items in {nameof(GetCategoryItems)}");
          
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            _logger.LogInformation($"Parsing item category: {itemCategory}");

            HtmlNodeCollection tables = document.DocumentNode.SelectNodes("//table[@class='wiki_table']");

            return tables != null ? ParseRows(tables) : null;
        }

        private static IEnumerable<Item> ParseRows(HtmlNodeCollection tables)
        {
            List<Item> items = new List<Item>();
            foreach (HtmlNode table in tables)
            {
                List<string> tableHeaders = null;
                foreach (HtmlNode itemRow in table.SelectNodes("tr"))
                {
                    if (tableHeaders == null)
                    {
                        tableHeaders = ParseTableHeader(itemRow);
                        continue;
                    }

                    HtmlNodeCollection itemCells = itemRow.SelectNodes("td");

                    Item item = ParseItemInformation(itemCells, tableHeaders);
                    if (item != null && items.All(i => i.Name != item.Name))
                        items.Add(item);
                }
            }
           
            return items;
        }

        private static Item ParseItemInformation(HtmlNodeCollection itemCells, List<string> tableHeaders)
        {
            Item item = new Item();

            foreach (string header in tableHeaders)
            {
                switch (header)
                {
                    case "Picture":
                        GetImageUrl(itemCells, tableHeaders, item);
                        break;
                    case "Name":
                        GetName(itemCells, tableHeaders, item);
                        break;
                    case "Arm":
                    case "Armor":
                    case "Armour":
                        GetArmour(itemCells, tableHeaders, item);
                        break;
                    case "Weight":
                        GetWeight(itemCells, tableHeaders, item);
                        break;
                    case "Attack":
                        GetAttack(itemCells,tableHeaders,item);
                        break;
                    case "Defence":
                        GetDefence(itemCells, tableHeaders, item);
                        break;
                    case "Hands":
                        item.TwoHanded = itemCells[tableHeaders.IndexOf("Hands")].InnerText.StartsWith("one");
                        break;
                    case "Hit% +":
                    case "Hit%+":
                        GetAdditionalHitChance(itemCells, tableHeaders, item);
                        break;
                    case "Attributes":
                        item.Attributes = itemCells[tableHeaders.IndexOf("Attributes")].InnerText.Replace('\n',',').Replace(".,",".");
                        break;
                    case "Number":
                        item.Name = itemCells[tableHeaders.IndexOf("Number")].InnerText;
                        break;
                    case "Price":
                            GetItemvalue(itemCells, tableHeaders, item);
                        break;
                    default:
                        continue;
                }
            }
            return item;
        }

        private static void GetName(HtmlNodeCollection itemCells, List<string> tableHeaders, Item item)
        {
            HtmlNode nameNode = itemCells[tableHeaders.IndexOf("Name")];
            item.Name = nameNode.InnerText.TrimStart(' ').TrimEnd('\n');
            item.ItemLink = nameNode.ChildNodes[0].GetAttributeValue("href", "");
        }

        private static void GetItemvalue(HtmlNodeCollection itemCells, List<string> tableHeaders, Item item)
        {
            int price;
            int.TryParse(itemCells[tableHeaders.IndexOf("Price")].InnerText.Replace(" gp",""), out price);
            item.LootValue = price;
        }

        private static void GetAdditionalHitChance(HtmlNodeCollection itemCells, List<string> tableHeaders, Item item)
        {
            int additionalHitChance;
            int.TryParse(itemCells[tableHeaders.IndexOf("Hit% +")].InnerText, out additionalHitChance);
            item.AdditionalHitChance = additionalHitChance;
        }

        private static void GetAttack(HtmlNodeCollection itemCells, List<string> tableHeaders, Item item)
        {
            int attack;
            int.TryParse(itemCells[tableHeaders.IndexOf("Attack")].InnerText, out attack);
            item.Attack = attack;
        }

        private static void GetDefence(HtmlNodeCollection itemCells, List<string> tableHeaders, Item item)
        {
            int defence;
            int.TryParse(itemCells[tableHeaders.IndexOf("Defence")].InnerText, out defence);
            item.Defence = defence;
        }

        private static void GetWeight(HtmlNodeCollection itemCells, List<string> tableHeaders, Item item)
        {
            string weightString = itemCells[tableHeaders.IndexOf("Weight")].InnerText
                                                                           .Replace("oz.", "")
                                                                           .Replace(".", ",");
            float weight;
            float.TryParse(weightString, out weight);
            item.Weight = weight;
        }

        private static void GetArmour(HtmlNodeCollection itemCells, List<string> tableHeaders, Item item)
        {
            int armour;
            int cellIndex = tableHeaders.IndexOf("Arm");

            if (cellIndex == -1)
                cellIndex = tableHeaders.IndexOf("Armor");

            if (cellIndex == -1)
                cellIndex = tableHeaders.IndexOf("Armour");

            int.TryParse(itemCells[cellIndex].InnerText, out armour);
            item.Armour = armour;
        }

        private static void GetImageUrl(HtmlNodeCollection itemCells, List<string> tableHeaders, Item item)
        {
            int indexOfImageUrl = tableHeaders.IndexOf("Picture");
            item.ImageUrl = itemCells[indexOfImageUrl].ChildNodes.Any(n => n.Name == "div")
                ? itemCells[indexOfImageUrl].SelectSingleNode("div")?.FirstChild?.GetAttributeValue("src", "")
                : itemCells[indexOfImageUrl].SelectSingleNode("img")?.GetAttributeValue("src", "");
        }

        private static List<string> ParseTableHeader(HtmlNode row)
        {
            HtmlNodeCollection headerCells = row.SelectNodes("td");
            return headerCells?.Select(headerCell => headerCell.InnerText.TrimEnd('\n')).ToList();
        }

        private static IEnumerable<string> GetItemCategories(HtmlDocument document)
        {
            HtmlNodeCollection categoryTables = document.DocumentNode.SelectNodes("//table[@class='wiki_table']");
            List<HtmlNode> categoryNodes = new List<HtmlNode>();
            foreach (HtmlNode categoryTable in categoryTables)
            {
                categoryNodes.AddRange(categoryTable.SelectNodes(".//a[@class='wiki_link']"));
            }
            return categoryNodes.Select(c => c.GetAttributeValue("href",""));
        }
    }
}
