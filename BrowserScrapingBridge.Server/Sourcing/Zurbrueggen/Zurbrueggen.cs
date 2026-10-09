#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Zurbrueggen
{
    public class Zurbrueggen : ICollect
    {
        public string Site => nameof(Zurbrueggen);

        public string Name => "주르브루겐";

        public Language Language => Language.DE;

        public Currency Currency => Currency.EUR;

        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        public string GetCode(string url)
        {
            var uri = new Uri(url);
            return uri.Segments.Last();
        }

        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("p", page);
            string requestUrl = uriBuilderAdvance.ToString();
            string html = await ChatHubService.GetRequest(connectionId, requestUrl);

            var divs = html.GetValues("//div[@class='product-info']//a", TextEx.XPathOption.OuterHtml);
            var result = new List<CollectPageInfo>();
            foreach (string div in divs)
            {
                string href = div.GetValue("//a", "href");
                if (string.IsNullOrWhiteSpace(href)) continue;
                string code = GetCode(href);
                var innerText = div.GetValue("//a", TextEx.XPathOption.InnerText)?.Trim();
                var collectPageInfo = new CollectPageInfo();
                collectPageInfo.Url = href;
                collectPageInfo.Code = code;
                collectPageInfo.Title = $"{innerText} {code}";
                result.Add(collectPageInfo);
            }
            return result;
        }

        public JToken GetData(string html)
        {
            var dataLayer = html.SingleSplit("let onEventDataLayer = JSON.parse('", "');");
            JToken data = dataLayer.ToJArray().First();
            JToken item = data.Value("ecommerce", "items").First();
            return item;
        }

        public List<Name> GetTitle(JToken data)
        {
            string item_name = data.Value<string>("item_name");
            if (string.IsNullOrWhiteSpace(item_name)) throw new Exception("상품명을 찾을 수 없습니다.");
            string item_brand = data.Value<string>("item_brand");
            if (string.IsNullOrWhiteSpace(item_brand) == false)
            {
                int indexOf = item_name.IndexOf(item_brand);
                if (indexOf == 0)
                {
                    item_name = item_name.Substring(item_brand.Length).Trim();
                }
            }
            var title = ProductHelper.CreateName(Language.DE, item_name);
            return title;
        }

        public List<Name> GetBrand(JToken data)
        {
            string item_brand = data.Value<string>("item_brand");
            if (string.IsNullOrWhiteSpace(item_brand)) return null;
            return ProductHelper.CreateName(Language.DE, item_brand);
        }

        public string GetModel(JToken data)
        {
            return data.Value<string>("item_id");
        }


        public List<Desc> GetDescs(string html)
        {
            List<string> dls = html.GetValues("//dl", TextEx.XPathOption.OuterHtml);
            if (dls == null || dls.Count <= 0) return null;
            var result = new List<Desc>();
            int count = 1;
            foreach (var dl in dls)
            {
                List<string> dts = dl.GetValues("//dl//dt", TextEx.XPathOption.InnerText);
                if (dts == null || dts.Count <= 0) continue;
                List<string> dds = dl.GetValues("//dl//dd", TextEx.XPathOption.InnerText);
                if (dds == null || dds.Count <= 0) continue;
                if (dts.Count != dds.Count) continue;
                for (int i = 0; i < dts.Count; i++)
                {
                    string title = dts[i].Trim();
                    if (string.IsNullOrWhiteSpace(title)) continue;
                    string value = dds[i].Trim();
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    var desc = new Desc(count, Language.DE, title, value);
                    result.Add(desc);
                    count++;
                }
            }
            return result;
        }

        public List<Price> GetPrice(JToken data, string html)
        {
            double? salePrice = data.Value<double?>("price");
            if (salePrice == null || salePrice <= 0) throw new Exception("판매가를 찾을 수 없습니다.");
            double? originalPrice = salePrice;
            string suggestedPriceValue = html.GetSelectNodeInnerText("//span[@class='suggested-price-value']");
            if (string.IsNullOrWhiteSpace(suggestedPriceValue) == false)
            {
                originalPrice = suggestedPriceValue.ParseEurToDouble();
            }
            List<Price> price = ProductHelper.CreatePrice(this.Currency, originalPrice, salePrice, true);
            return price;
        }

        public List<string> GetMainImages(string html)
        {
            var pictures = html.GetValues("//picture//source", "srcset");
            if (pictures == null || pictures.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            var mainImages = new List<string>();
            foreach (var item in pictures)
            {
                try
                {
                    var uri = new UriBuilderAdvance(item);
                    uri.ClearQuery();
                    var imageUrl = uri.ToString();
                    if (mainImages.Exists(x => x == imageUrl)) continue;
                    mainImages.Add(imageUrl);
                }
                catch { } // 잘못된 URL이기 때문에 무시
            }
            return mainImages;
        }

        public string GetDetailPageHtml(string html)
        {
            var tabContent = html.GetValue("//div[@class='tab-content']", TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(tabContent)) return null;
            var stringBuilder = new StringBuilder();
            string description = tabContent.GetValue("//div[@aria-label=\"description-tab\"]", TextEx.XPathOption.OuterHtml);
            stringBuilder.AppendLine(description);
            stringBuilder.AppendLine("<br>");
            string properties = tabContent.GetValue("//div[@aria-label=\"properties-tab\"]", TextEx.XPathOption.OuterHtml);
            stringBuilder.AppendLine(properties);
            var dafaultHtml = HtmlReader.GetDafaultHtml(stringBuilder.ToString());
            return dafaultHtml;
        }

        public Option GetOption(JToken data) => null; // 주르브루겐은 각 옵션이 개별 상품이기 때문에 옵션 수집X

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var product = new Product();
            try
            {
                var html = await ChatHubService.GetRequest(connectionId, url);
                JToken data = GetData(html);
                product.url = url;
                product.currency = this.Currency.ToString();
                product.site = this.Site;
                product.code = GetCode(url);
                product.title = GetTitle(data);
                product.brand = GetBrand(data);
                product.model = GetModel(data);
                product.descs = GetDescs(html);
                product.price = GetPrice(data, html);
                product.mainImages = GetMainImages(html);
                product.detailPageHtml = GetDetailPageHtml(html);
                product.option = GetOption(data);
            }
            catch (CaptchaException ce)
            {
                throw ce;
            }
            catch (Exception e)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = e.Message;
            }
            return product;
        }

        public async Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            JToken data = GetData(html);
            var collectRawData = new CollectRawData(url, data, html);
            return collectRawData;
        }

        public Product GetProduct(CollectRawData rawData)
        {
            var product = new Product();
            try
            {
                product.url = rawData.Url;
                product.currency = this.Currency.ToString();
                product.site = this.Site;
                product.code = GetCode(rawData.Url);
                product.title = GetTitle(rawData.Data);
                product.brand = GetBrand(rawData.Data);
                product.model = GetModel(rawData.Data);
                product.descs = GetDescs(rawData.Html);
                product.price = GetPrice(rawData.Data, rawData.Html);
                product.mainImages = GetMainImages(rawData.Html);
                product.detailPageHtml = GetDetailPageHtml(rawData.Html);
                product.option = GetOption(rawData.Data);
            }
            catch (CaptchaException ce)
            {
                throw ce;
            }
            catch (Exception e)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = e.Message;
            }
            return product;
        }

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            return;
        }
    }
}