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
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Selfridges
{
    public class Selfridges : ICollect
    {
        private readonly string host = "https://www.selfridges.com";
        public string Site => nameof(Selfridges);

        public string Name => "셀프리지";

        public Language Language => Language.GB;

        public Currency Currency => Currency.GBP;

        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        public string GetCode(string url)
        {
            var uri = new Uri(url);
            int? index = uri.Segments.ToList()?.IndexOf("product/");
            if (index == null || index <= 0) return null;
            string code = uri.Segments[(int)index + 1];
            var lastIndexOf = code.LastIndexOf("/");
            code = code.Substring(0, lastIndexOf);
            return code;
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var html = await ChatHubService.GetRequest(connectionId, url);
            List<string> productCards = html.GetValues("//div[@data-testid='product-card']", TextEx.XPathOption.OuterHtml);
            if (productCards == null || productCards.Count <= 0) return null;
            var collectPageInfos = new List<CollectPageInfo>();
            foreach (string productCard in productCards)
            {
                var href = productCard.GetValue("//a[@data-analytics-link='product_card_link']", "href");
                var itemUrl = $"{this.host}{href}";
                var code = GetCode(itemUrl);
                var title = productCard.GetValue("//a[@data-analytics-link='product_card_link']//div", TextEx.XPathOption.InnerText)?.Trim();
                var collectPageInfo = new CollectPageInfo();
                collectPageInfo.Url = itemUrl;
                collectPageInfo.Code = code;
                collectPageInfo.Title = title;
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }

        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }

        public JToken GetData(string html)
        {
            var scripts = html.GetValues("//script", TextEx.XPathOption.InnerHtml);
            scripts = scripts.FindAll(x => string.IsNullOrWhiteSpace(x) == false);
            var data = new JObject();
            foreach (var script in scripts)
            {
                JObject innerData = script.ToJObject();
                if (innerData != null)
                {
                    data.Merge(innerData);
                }
                else
                {
                    var indexOf = script.IndexOf("self.__next_");
                    if (indexOf >= 0)
                    {
                        string self = script.SubstringBetween(".push(", ")", false);
                        JToken qwe = self.ToJArray()?[1];
                        if (qwe == null) continue;
                        if (qwe.Type == JTokenType.String)
                        {
                            var list = qwe.ToString().Split("\n");
                            foreach (var item in list)
                            {
                                if (string.IsNullOrWhiteSpace(item)) continue;
                                var start = item.IndexOf("[");
                                if (start <= 0)
                                {
                                    start = item.IndexOf("{");
                                    if (start <= 0) continue;
                                }
                                var key = item.Substring(0, start);
                                if (key.Contains(":") == false) continue;
                                string value = item.Substring(start);
                                data[key] = value;
                            }
                        }
                        else
                        {
                            string id = qwe.Value<string>("id");
                            string children = qwe.Value<string>("children");
                            string __next_env__ = children.SubstringBetween("=", ";", false);
                            data[id] = __next_env__.ToJObject();
                        }
                    }
                }
            }
            return data;
        }

        public List<Name> GetTitle(JToken data)
        {
            var name = data.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(this.Language, name);
        }

        public List<Name> GetBrand(JToken data)
        {
            var brand = data.Value<string>("brand");
            if (string.IsNullOrWhiteSpace(brand)) throw new Exception("브랜드명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(this.Language, brand);
        }

        public List<Price> GetPrice(JToken data)
        {
            var price = data.Value("offers").Value<double?>("price");
            if (price == null || price <= 0) throw new Exception("상품 가격을 찾을 수 없습니다.");
            var priceCurrency = data.Value("offers").Value<string>("priceCurrency")?.ToUpper();
            if (string.IsNullOrWhiteSpace(priceCurrency)) priceCurrency = Currency.KRW.ToString();
            Currency currency = priceCurrency.ConvertEnum<Currency>();
            return ProductHelper.CreatePrice(currency, price, price);
        }

        public List<string> GetMainImages(string html)
        {
            var result = new List<string>();
            var pictures = html.GetValues("//div[@data-testid='slider']//picture//source[@type='image/jpg']", "srcset");
            foreach (var item in pictures)
            {
                try
                {
                    var uri = new UriBuilderAdvance(item);
                    uri.ClearQuery();
                    var imageUrl = uri.ToString();
                    if (result.Exists(x => x == imageUrl)) continue;
                    result.Add(imageUrl);
                }
                catch { }
            }
            if (result.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            return result;
        }

        private JToken GetC(JToken data)
        {
            var c = data.Value<string>("c:");
            var datails = c.ToJArray().ToList();
            var find = datails.Find(x => x.Type == JTokenType.Object);
            if (find == null) return null;
            JToken children = find.Value("children")?.Last()?.Last();
            return children;
        }

        public string GetDetailPageHtml(JToken data)
        {
            var c = GetC(data);
            if (c == null) return null;
            var details = c.Value("productResponse").Value<string>("details");
            var dafaultHtml = HtmlReader.GetDafaultHtml(details);
            return dafaultHtml;
        }

        public Option GetOption(JToken data)
        {
            JToken c = GetC(data);
            if (c == null) return null;

            var option = new Option();
            var optionGroups = new List<Option.OptionGroup>();
            List<JToken> colours = c.Value("productResponse", "colours")?.ToList();
            if (colours != null && colours.Count > 0)
            {
                var colorOption = new Option.OptionGroup();
                colorOption.optionId = "color";
                colorOption.groupName = ProductHelper.CreateName(this.Language, "color");
                colorOption.groups = new List<Option.OptionGroup.Group>();
                for (int i = 0; i < colours.Count; i++)
                {
                    var name = colours[i].Value<string>("name");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var group = new Option.OptionGroup.Group();
                    group.groupId = name;
                    group.optionName = ProductHelper.CreateName(this.Language, name);
                    colorOption.groups.Add(group);
                }
                if (colorOption.groups.Count > 0) optionGroups.Add(colorOption);
            }
            List<string> sizes = c.Value("productResponse", "sizes")?.ToString()?.ToDeserializeObject<List<string>>();
            if (sizes != null && sizes.Count > 0)
            {
                var colorOption = new Option.OptionGroup();
                colorOption.optionId = "size";
                colorOption.groupName = ProductHelper.CreateName(this.Language, "size");
                colorOption.groups = new List<Option.OptionGroup.Group>();
                for (int i = 0; i < sizes.Count; i++)
                {
                    var group = new Option.OptionGroup.Group();
                    group.groupId = sizes[i];
                    group.optionName = ProductHelper.CreateName(this.Language, sizes[i]);
                    colorOption.groups.Add(group);
                }
                if (colorOption.groups.Count > 0) optionGroups.Add(colorOption);
            }

            option.optionGroups = optionGroups;
            option.detailOptions = ProductHelper.CreateDetailOptions(optionGroups);
            return option;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string code = GetCode(url);
            string requestUrl = $"https://www.selfridges.com/GB/en/product/{code}";
            var html = await ChatHubService.GetRequest(connectionId, requestUrl);
            var product = new Product();
            product.site = this.Site;
            product.code = code;
            product.url = requestUrl;
            product.currency = this.Currency.ToString();
            try
            {
                JToken data = GetData(html);
                product.title = GetTitle(data);
                product.brand = GetBrand(data);
                product.price = GetPrice(data);
                product.mainImages = GetMainImages(html);
                product.detailPageHtml = GetDetailPageHtml(data);
                product.option = GetOption(data);
                if (product.option != null)
                {
                    var copyPrice = product.price.DeepCopyObject();
                    copyPrice[0].isdefalutPrice = false;
                    for (int i = 0; i < product.option.detailOptions.Count; i++)
                    {
                        product.option.detailOptions[i].price = copyPrice;
                    }
                }
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
            string code = GetCode(url);
            string requestUrl = $"https://www.selfridges.com/GB/en/product/{code}";
            var html = await ChatHubService.GetRequest(connectionId, requestUrl);
            JToken data = GetData(html);
            return new CollectRawData(url, data, html);
        }

        public Product GetProduct(CollectRawData rawData)
        {
            var product = new Product();

            try
            {
                string code = GetCode(rawData.Url);
                string requestUrl = $"https://www.selfridges.com/GB/en/product/{code}";

                product.currency = this.Currency.ToString();
                product.site = this.Site;
                product.code = code;
                product.url = requestUrl;
                product.title = GetTitle(rawData.Data);
                product.brand = GetBrand(rawData.Data);
                product.price = GetPrice(rawData.Data);
                product.mainImages = GetMainImages(rawData.Html);
                product.detailPageHtml = GetDetailPageHtml(rawData.Data);
                product.option = GetOption(rawData.Data);
                if (product.option != null)
                {
                    var copyPrice = product.price.DeepCopyObject();
                    copyPrice[0].isdefalutPrice = false;
                    for (int i = 0; i < product.option.detailOptions.Count; i++)
                    {
                        product.option.detailOptions[i].price = copyPrice;
                    }
                }
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
