#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Flaconi
{
    public class Flaconi : ICollect
    {
        public string Site => nameof(Flaconi);

        public string Name => "플라코니";

        public Language Language => Language.DE;

        public Currency Currency => Currency.EUR;

        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public string GetCode(string url)
        {
            return null;
        }

        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            if (page > 0)
            {
                int offset = (page - 1) * 24;
                uriBuilderAdvance.SetQuery("offset", offset);
            }
            var requestUrl = uriBuilderAdvance.ToString();
            requestUrl = "https://www.chemistwarehouse.com.au/shop-online/1031/aveeno";
            string html = await ChatHubService.GetRequest(connectionId, requestUrl);
            List<CollectPageInfo> collectPageInfo = new List<CollectPageInfo>();
            return collectPageInfo;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var product = new Product();
            try
            {

            }
            catch (Exception e)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = e.Message;
            }
            return product;
        }

        public Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            throw new NotImplementedException();
        }

        public Product GetProduct(CollectRawData data)
        {
            throw new NotImplementedException();
        }

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            throw new NotImplementedException();
        }
    }
}
