#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserScrapingBridge.Server.Sourcing.ShopSimon
{
    public class SearchFilterBuilder
    {
        public Uri Uri { get; set; }
        public UriBuilderAdvance UriBuilderAdvance { get; set; }
        public SearchFilterBuilder(string url)
        {
            this.Uri = new Uri(url);
            this.UriBuilderAdvance = new UriBuilderAdvance(Uri);
        }

        public string Build()
        {
            string searchUrl = null;
            var lddelay = 0;
            if (Uri.Segments.Contains("shoes")) lddelay = 150;
            string newHandle = null;
            string rootCollectionHandle = newHandle;
            string search = "";
            var legacy = false;
            var isSearchpage = false;
            var ajaxType = "GET";
            var postData = "";
            var isImageSearch = false;
            var isColorSearch = false;
            var pagesize = 48;
            var showAds = true;
            var colorHex = "";
            object isLoggedIn = null;

            string q = UriBuilderAdvance.GetQuery("q");
            Dictionary<string, string> searchParams = UriBuilderAdvance.queryDictionary;
            if (Uri.Segments.Contains("search-results-page") || string.IsNullOrWhiteSpace(q) == false)
            {
                newHandle = "all";
                rootCollectionHandle = newHandle;
                search = $"&term={q}";
                isSearchpage = true;
                if (Uri.Segments.Contains("collection"))
                {
                    //newHandle = 
                }
            }
            return searchUrl;
        }
    }
}
