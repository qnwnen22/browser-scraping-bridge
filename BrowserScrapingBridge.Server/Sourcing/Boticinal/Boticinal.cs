#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Boticinal
{
    public class Boticinal : CollectBase
    {
        public override string Site => nameof(Boticinal);
        public override string Name => "보티시날";
        public override Language Language { get; set; } = Language.FR;
        public override Currency Currency { get; set; } = Currency.EUR;
        public override bool SupportsCaching { get; set; }

        public override async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("p", page);
            var requestUrl = uriBuilderAdvance.ToString();
            var html = await ChatHubService.GetRequest(connectionId, requestUrl);
            var productItemInfos = html.GetValues("//div[@class='product-item-info']", TextEx.XPathOption.OuterHtml);
            if (productItemInfos == null || productItemInfos.Count <= 0) return null;
            List<CollectPageInfo> result = new List<CollectPageInfo>();
            if (productItemInfos != null && productItemInfos.Count > 0)
            {
                foreach (var productItemInfo in productItemInfos)
                {
                    var href = productItemInfo.GetValue("//div[@class='product-item-photo']//a", "href");
                    if (string.IsNullOrWhiteSpace(href)) continue;
                    var code = GetCode(href);
                    if (string.IsNullOrWhiteSpace(code)) continue;
                    var collectPageInfo = new CollectPageInfo();
                    collectPageInfo.Url = href;
                    collectPageInfo.Code = code;
                    List<string> titles = productItemInfo.GetValues("//div[@class='product-item-name']//a", TextEx.XPathOption.InnerText);
                    if (titles != null && titles.Count > 0) collectPageInfo.Title = string.Join(" ", titles)?.Trim();
                    result.Add(collectPageInfo);
                }
            }
            return result;
        }

        public override void ThrowIfCaptcha(string url, string html)
        {

        }

        public override string GetCode(string url)
        {
            var uri = new Uri(url);
            var last = uri.Segments.Last();
            int lastIndexOf = last.LastIndexOf(".html");
            var code = last.Substring(0, lastIndexOf);
            return code;
        }

        public override string CreateProductUrl(string code)
        {
            return null;
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            List<string> scripts = html.GetValues("//script[@type='text/x-magento-init']", TextEx.XPathOption.InnerHtml);
            if (scripts == null || scripts.Count <= 0) throw new Exception("상품 정보를 찾을 수 없습니다.");
            JObject data = new JObject();
            for (int i = 0; i < scripts.Count; i++)
            {
                var innerData = scripts[i].ToJObject();
                if (innerData.Count == 1)
                {
                    JToken all = innerData.Value("*");
                    if (all != null)
                    {
                        data.Merge(all);
                    }
                    else
                    {
                        data.Merge(innerData);
                    }
                }
                else
                {
                    data.Merge(innerData);
                }
            }
            return data;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string title = html.GetValue("//h1[@class='page-title']//span", TextEx.XPathOption.InnerText);
            if (string.IsNullOrWhiteSpace(title)) throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(this.Language, title);
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            var brand = html.GetValue("//div[@class='product-info-brand']//a", "title");
            if (string.IsNullOrWhiteSpace(brand)) return null;
            brand = brand.ToHtmlDecodeComplete();
            return ProductHelper.CreateName(this.Language, brand);
        }

        public override string GetModel(string html, JToken data)
        {
            return null;
        }

        public override List<Desc> GetDescs(string html, JToken data)
        {
            return null;
        }

        public override List<Tag> GetTags(string html, JToken data)
        {
            return null;
        }

        public override List<Price> GetPrice(string html, JToken data)
        {
            List<JToken> children = data.Children().ToList();
            if (children == null || children.Count <= 0) throw new Exception("가격 정보를 찾을 수 없습니다.");
            JToken find = children.Find(x => x.Path.Contains("[data-role=priceBox][data-price-box=product-id-"));
            if (find == null) throw new Exception("가격 정보를 찾을 수 없습니다.");
            string productId = find.FirstOrDefault()?.Value("priceBox", "priceConfig").Value<string>("productId");
            if (string.IsNullOrWhiteSpace(productId)) throw new Exception("가격 정보를 찾을 수 없습니다.");
            double? final_price = data.Value("Magento_Catalog/js/product/view/provider", "data", "items", productId, "price_info")?.Value<double?>("final_price");
            if (final_price == null) throw new Exception("가격 정보를 찾을 수 없습니다.");
            double? regular_price = data.Value("Magento_Catalog/js/product/view/provider", "data", "items", productId, "price_info")?.Value<double?>("regular_price");
            if (regular_price == null) throw new Exception("가격 정보를 찾을 수 없습니다.");
            var price = ProductHelper.CreatePrice(this.Currency, regular_price, final_price, true);
            return price;
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var imageInfos = data.Value("[data-gallery-role=gallery-placeholder]", "mage/gallery/gallery", "data")?.ToList();
            if (imageInfos == null || imageInfos.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            List<string> result = new List<string>();
            foreach (var item in imageInfos)
            {
                string img = item.Value<string>("img");
                var urib = new UriBuilderAdvance(img);
                urib.ClearQuery();
                var imageUrl = urib.ToString();
                result.Add(imageUrl);
            }
            return result;
        }

        public override Option GetOption(string html, JToken data)
        {
            return null; // 옵션 상품이 있을 경우 추가
        }

        public override string GetVideoUrl(string html, JToken data)
        {
            return null;
        }

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var stringBuilder = new StringBuilder();
            AppendDetailHtml(stringBuilder, html, "//div[@id='tab-label-description']");
            AppendDetailHtml(stringBuilder, html, "//div[@id='description']");
            AppendDetailHtml(stringBuilder, html, "//div[@id='tab-label-composition']");
            AppendDetailHtml(stringBuilder, html, "//div[@id='composition']");
            AppendDetailHtml(stringBuilder, html, "//div[@id='tab-label-conseil']");
            AppendDetailHtml(stringBuilder, html, "//div[@id='conseil']");
            AppendDetailHtml(stringBuilder, html, "//div[@id='tab-label-bv_questions']");
            AppendDetailHtml(stringBuilder, html, "//div[@id='bv_questions']");
            var dafaultHtml = HtmlReader.GetDafaultHtml(stringBuilder.ToString());
            return dafaultHtml;
        }
        private void AppendDetailHtml(StringBuilder stringBuilder, string html, string xpath)
        {
            string innerHtml = html.GetValue(xpath, TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(innerHtml)) return;
            stringBuilder.AppendLine(innerHtml);
        }

        public override int? GetTotalQuantity(string html, JToken data)
        {
            return null;
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {

        }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            return;
        }
    }
}
