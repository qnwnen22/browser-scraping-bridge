#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Aventure
{
    public class Aventure : CollectBase
    {
        public override string Site => nameof(Aventure);

        public override string Name => "어반튜";

        public override Language Language { get; set; } = Language.KR;

        public override Currency Currency { get; set; } = Currency.KRW;

        public override bool SupportsCaching { get; set; }

        public override string GetCode(string url)
        {
            throw new System.NotImplementedException();
        }

        public override Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            throw new System.NotImplementedException();
        }

        public override Task<string> GetLoginUrlAsync(string connectionId)
        {
            throw new System.NotImplementedException();
        }

        public override void ThrowIfCaptcha(string url, string html)
        {
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
        }
        
        public override string CreateProductUrl(string url)
        {
            throw new System.NotImplementedException();
        }

        public override Task<JToken> GetData(string connectionId, string url, string html)
        {
            throw new NotImplementedException();
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            throw new NotImplementedException();
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            throw new NotImplementedException();
        }

        public override string GetModel(string html, JToken data)
        {
            throw new NotImplementedException();
        }

        public override List<Desc> GetDescs(string html, JToken data)
        {
            throw new NotImplementedException();
        }

        public override List<Tag> GetTags(string html, JToken data)
        {
            throw new NotImplementedException();
        }

        public override List<Price> GetPrice(string html, JToken data)
        {
            throw new NotImplementedException();
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            throw new NotImplementedException();
        }

        public override string GetVideoUrl(string html, JToken data)
        {
            throw new NotImplementedException();
        }

        public override string GetDetailPageHtml(string html, JToken data)
        {
            throw new NotImplementedException();
        }

        public override Option GetOption(string html, JToken data)
        {
            throw new NotImplementedException();
        }

        public override int? GetTotalQuantity(string html, JToken data)
        {
            throw new NotImplementedException();
        }

        public override Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            throw new NotImplementedException();
        }
    }
}
