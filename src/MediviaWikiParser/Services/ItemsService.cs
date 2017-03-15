using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
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
            HttpResponseMessage response = _httpClient.GetAsync($"/{item.Name}").Result;

            if (!response.IsSuccessStatusCode) return;

            string html = response.Content.ReadAsStringAsync().Result;

            if (string.IsNullOrEmpty(html)) return;

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            HtmlNodeCollection infoNodes = document.DocumentNode.SelectNodes("//table[@class='wiki_table']/tr");

            if (infoNodes.Count >= 5)
            {
               
            }
        }

        private async Task<IEnumerable<Item>> GetCategoryItems(string itemCategory)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(itemCategory);

            if (!response.IsSuccessStatusCode) return null;

            string html = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(html)) return null;
          
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

#if DEBUG
            Console.WriteLine("Parsing item category: {");
#endif
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
            if (itemCells[0].InnerText.StartsWith("Picture") || itemCells[0].InnerText.Contains("Bone Key"))
                return null;

            Item item = new Item();

            foreach (string header in tableHeaders)
            {
                switch (header)
                {
                    case "Picture":
                        GetImageUrl(itemCells, tableHeaders, item);
                        break;
                    case "Name":
                        item.Name = itemCells[tableHeaders.IndexOf("Name")].InnerText.TrimEnd('\n');
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
                        GetAdditionalHitChance(itemCells, tableHeaders, item);
                        break;
                    case "Attributes":
                        item.Attributes = itemCells[tableHeaders.IndexOf("Attributes")].InnerText.Replace('\n',',');
                        break;
                    case "Number":
                        item.Name = itemCells[tableHeaders.IndexOf("Number")].InnerText;
                        break;
                    case "Location":
                        item.LootFrom = new[] { itemCells[tableHeaders.IndexOf("Location")].InnerText };
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
