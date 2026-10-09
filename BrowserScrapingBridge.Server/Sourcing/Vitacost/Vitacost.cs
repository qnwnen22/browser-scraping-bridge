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
using System.Net.Http;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Vitacost
{
    public class Vitacost : ICollect
    {
        private readonly string host = "https://www.vitacost.com";
        public string Site => nameof(Vitacost);

        public string Name => "비타코스트";

        public Language Language => Language.EN;

        public Currency Currency => Currency.USD;

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
            uriBuilderAdvance.SetQuery("pg", page);
            string requestUrl = uriBuilderAdvance.ToString();
            var html = await GetHtmlAsync(requestUrl);
            //html = await ChatHubService.GetRequest(connectionId, requestUrl);
            List<string> lis = html.GetValues("//li[@class='product-block']", TextEx.XPathOption.OuterHtml);
            var collectPageInfos = new List<CollectPageInfo>();
            if (lis != null && lis.Count > 0)
            {
                foreach (var li in lis)
                {
                    var href = li.GetValue("//p[@class='description']//a", "href");
                    if (string.IsNullOrWhiteSpace(href)) continue;
                    var title = li.GetValue("//p[@class='description']//a", "title");
                    CollectPageInfo collectPageInfo = new CollectPageInfo();
                    collectPageInfo.Code = href;
                    collectPageInfo.Url = string.Join("", this.host, href);
                    collectPageInfo.Title = title;
                    collectPageInfos.Add(collectPageInfo);
                }
            }
            return collectPageInfos;
        }

        public JToken GetData(string html)
        {
            var scripts = html.GetValues("//script[@type='text/javascript' and @language='javascript']", TextEx.XPathOption.InnerHtml);
            var trackADL = scripts.Find(x => x.Contains("TrackADL"));
            JObject data = trackADL.SubstringBetween("(", ")", false)?.ToJObject();
            if (data == null) throw new Exception("상품 정보를 확인할 수 없습니다.");
            return data;
        }

        public List<Name> GetTitle(JToken data)
        {
            var title = data.Value<string>("pageID");
            if (string.IsNullOrWhiteSpace(title)) throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(this.Language, title);
        }

        public List<Name> GetBrand(JToken data)
        {
            return null;
        }

        public List<Price> GetPrice(JToken data)
        {
            JToken productData = data.Value("products").FirstOrDefault();
            double? originalPrice = productData.Value<double?>("price");
            if (originalPrice == null || originalPrice <= 0) throw new Exception("상품 가격을 확인할 수 없습니다.");
            double? salePrice = productData.Value<double?>("salePrice");
            if (salePrice == null || salePrice <= 0)
            {
                salePrice = originalPrice;
            }
            return ProductHelper.CreatePrice(this.Currency, originalPrice, salePrice);
        }

        public List<string> GetMainImages(JToken data, string html)
        {
            List<string> result = new List<string>();
            var mainImage = html.GetValue("//div[@id='productImage']//img", "src");
            if (string.IsNullOrWhiteSpace(mainImage) == false)
            {
                mainImage = string.Join("", this.host, mainImage);
                result.Add(mainImage);
            }

            List<string> scripts = html.GetValues("//script", TextEx.XPathOption.InnerHtml);
            string find = scripts?.Find(x => x.IndexOf("vSKU = String(vSKU);") >= 0);
            if (string.IsNullOrWhiteSpace(find) == false)
            {
                string input = find.SingleSplit("const input = ", ";");
                if (string.IsNullOrWhiteSpace(input) == false)
                {
                    var images = input.ToDeserializeObject<List<string>>();
                    var sku = data.Value("products").FirstOrDefault()?.Value<string>("sku");
                    var findImages = images.FindAll(x => x.IndexOf(sku) == 0);
                    if (findImages != null && findImages.Count > 0)
                    {
                        for (int i = 0; i < findImages.Count; i++)
                        {
                            findImages[i] = string.Join("", this.host, "/images/nsf-multi-images/", findImages[i]);
                        }
                        result.AddRange(findImages);
                    }
                }
            }
            if (result.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            return result;
        }

        public string GetDetailPageHtml(string html)
        {
            var sections = html.GetValues("//div[@class='section']", TextEx.XPathOption.OuterHtml);
            if (sections == null || sections.Count <= 0) return null;
            var section = string.Join("", sections);
            if (string.IsNullOrWhiteSpace(section)) return null;
            var getDafaultHtml = HtmlReader.GetDafaultHtml(section);
            getDafaultHtml = getDafaultHtml.Replace("Vitacost is not responsible for the content provided in customer ratings and reviews. For more information, visit our", "");
            return getDafaultHtml;
        }

        public Option GetOption(JToken data)
        {
            return null; // 각 옵션이 개별 상품이므로 옵션 없음
        }

        public async Task<string> GetHtmlAsync(string url)
        {
            var header = new Dictionary<string, string>();
            header.Add("Connection", "keep-alive");
            header.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
            header.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            header.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            header.Add("Accept-Language", "ko");

            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                Proxy = this.Proxy,
            };
            string result = null;
            using (var httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler))
            {
                httpClient.SetHeader(header);
                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url);
                result = await httpResponseMessage.Content.ReadAsStringAsync();
            }
            return result;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var html = await GetHtmlAsync(url);
            // html = await ChatHubService.GetRequest(connectionId, url);
            var product = new Product();
            try
            {
                var data = GetData(html);
                product.site = this.Site;
                product.code = GetCode(url);
                product.url = url;
                product.currency = this.Currency.ToString();
                product.title = GetTitle(data);
                product.brand = GetBrand(data);
                product.price = GetPrice(data);
                product.mainImages = GetMainImages(data, html);
                product.detailPageHtml = GetDetailPageHtml(html);
                product.option = GetOption(null);
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
            var html = await GetHtmlAsync(url);
            var data = GetData(html);
            return new CollectRawData(url, data, html);
        }

        public Product GetProduct(CollectRawData rawData)
        {
            var product = new Product();
            try
            {
                product.site = this.Site;
                product.code = GetCode(rawData.Url);
                product.url = rawData.Url;
                product.currency = this.Currency.ToString();
                product.title = GetTitle(rawData.Data);
                product.brand = GetBrand(rawData.Data);
                product.price = GetPrice(rawData.Data);
                product.mainImages = GetMainImages(rawData.Data, rawData.Html);
                product.detailPageHtml = GetDetailPageHtml(rawData.Html);
                product.option = GetOption(null);
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
