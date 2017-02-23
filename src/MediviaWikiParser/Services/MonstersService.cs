using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MediviaWikiParser.Models;

namespace MediviaWikiParser.Services
{
    public class MonstersService
    {
        private HttpClient _httpClient;

        public MonstersService()
        {
          
           
           
        }

     
        public async Task<IEnumerable<Monster>>  GetMonsters(bool savePictures)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("cache-control", "no-cache");
            HttpResponseMessage response = await _httpClient.GetAsync("https://medivia.wikispaces.com/Monsters");

           // response = await _httpClient.GetAsync(response.RequestMessage.RequestUri);
            string html = await response.Content.ReadAsStringAsync();
          
        

            if (string.IsNullOrEmpty(html)) return null;

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            HtmlNodeCollection monsterNodes = document.DocumentNode.SelectNodes("//table[@class='wiki_table']/tr");

            return null;


        }
    }

}
