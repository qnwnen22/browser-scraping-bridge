#nullable disable warnings
using HtmlAgilityPack;
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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.ShopSimon
{
    public class ShopSimon : ICollect
    {
        public string Site => nameof(ShopSimon);
        public string Name => "샵사이먼";
        public Language Language => Language.EN;
        public Currency Currency => Currency.USD;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }
        public string GetCode(string url)
        {
            var ub = new UriBuilderAdvance(url);
            string code = ub.Segments.Last();
            if (code.Contains("/"))
            {
                code = code.Substring(0, code.Length - 1);
            }
            return code;
        }
        private string GetSubscriptionKey(string html)
        {
            string searchUrl = html.SingleSplit("const searchUrl = '", "';");
            var ub1 = new UriBuilderAdvance(searchUrl);
            string subscriptionKey = ub1.GetQuery("Subscription-Key");
            return subscriptionKey;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionId">HTML 수집을 수행할 브라우저의 SignalR 연결 ID</param>
        /// <param name="url"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        /// <example>https://shop.simon.com/collections/curated-deals</example>
        /// <example>https://shop.simon.com/collections/men</example>
        /// <example>https://shop.simon.com/collections/store-adidas?cid=SDLP:BC1:Adidas</example>
        /// <example>https://shop.simon.com/collections/luxe-store-salvatore-ferragamo?cid=LSDLP:BC1:Ferragamo</example>^
        /// <example>https://shop.simon.com/pages/search-results-page?q=dress</example>

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            var baseUri = new UriBuilderAdvance(url);
            var queryList = new List<string>();
            string segment = baseUri.Segments.Last();

            bool isActivePromo = false;
            switch (segment)
            {
                case "curated-just-in":
                case "curated-best-sellers":
                case "curated-trending-today":
                case "curated-deals":
                case "curated-clearance":
                    isActivePromo = true;
                    break;

                case "search-results-page":
                    segment = "all";
                    break;
                default:
                    break;
            }

            string subscriptionKey = GetSubscriptionKey(html);
            string q = baseUri.GetQuery("q");
            queryList.Add($"Subscription-Key={subscriptionKey}");
            queryList.Add($"pagesize=48");
            queryList.Add($"page={page}");
            foreach (KeyValuePair<string, string> queryItem in baseUri.queryDictionary)
            {
                string filterValue = null;
                switch (queryItem.Key)
                {
                    case "q":
                    case "page":
                    case "cid":
                        break;
                    case "orderby":
                        filterValue = queryItem.Value.ToWebUrlDecodeComplete().ToUriEscapeDataString();
                        queryList.Add($"sort={filterValue}");
                        break;
                    case "ProductType":
                        filterValue = queryItem.Value.ToWebUrlDecodeComplete().ToUriEscapeDataString();
                        queryList.Add($"filter=Category/Handle|{filterValue}");
                        break;
                    default:
                        filterValue = queryItem.Value.ToWebUrlDecodeComplete();
                        if (filterValue.Contains(",")) filterValue = filterValue.Replace(",", "~");
                        if (filterValue.Contains("'")) filterValue = filterValue.Replace("'", "''");
                        filterValue = filterValue.ToUriEscapeDataString();
                        queryList.Add($"filter={queryItem.Key}|{filterValue}");
                        break;
                }
            }
            if (isActivePromo)
            {
                queryList.Add($"filter=ActivePromo|1");
            }
            else
            {
                queryList.Add($"filter=Category/Handle|{segment}");
            }
            if (string.IsNullOrWhiteSpace(q) == false)
            {
                queryList.Add($"term={q}");
            }
            string query = string.Join("&", queryList);

            string requestUrl = $"https://spo-partner-api.azure-api.net/search?{query}";

            string json = await ChatHubService.GetRequest(connectionId, requestUrl);
            JObject data = json.ToJObject();
            if (data == null) return null;
            List<JToken> values = data["value"]?.ToList();
            if (values == null || values.Count <= 0) return null;

            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            foreach (JToken value in values)
            {
                string code = value.Value<string>("Handle");
                if (string.IsNullOrWhiteSpace(code)) continue;
                string itemUrl = $"https://shop.simon.com/products/{code}";
                string title = value.Value<string>("Title");
                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = code;
                collectPageInfo.Url = itemUrl;
                collectPageInfo.Title = title;
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }
        private static JObject GetLayerData(string html)
        {
            List<string> scripts = html.GetValues("//script", TextEx.XPathOption.OuterHtml);
            var dataLayerData = scripts.Find(x => x.Contains("var dataLayerData"));
            string productJson = dataLayerData.SubstringBetween("dataLayerData.product = {", "};", false);
            productJson = "{" + productJson + "}";
            JObject data = productJson.ToJObject();
            return data;
        }
        public JObject GetProductObjectData(string html)
        {
            string start = "window.productObject = {";
            int startIdx = html.IndexOf(start);
            string productObject = html.Substring(startIdx + start.Length);
            var endIdx = productObject.IndexOf("};");
            productObject = productObject.Substring(0, endIdx);
            string productJson = "{" + productObject + "}";

            JObject data = productJson.ToJObject();
            return data;
        }
        public List<Name> GetTitle(JObject data)
        {
            string title = data.Value<string>("title");
            if (string.IsNullOrWhiteSpace(title)) throw new Exception("상품명을 찾을 수 없습니다.");
            List<Name> result = new List<Name>();
            result.Add(new Name(Language.EN, title));
            return result;
        }
        public List<Name> GetBrand(JObject data)
        {
            string brand = data.Value<string>("designer");
            if (string.IsNullOrWhiteSpace(brand)) throw new Exception("브랜드를 찾을 수 없습니다.");
            List<Name> result = new List<Name>();
            result.Add(new Name(Language.EN, brand));
            return result;
        }
        public List<Price> GetPrice(JObject data)
        {
            double? salePrice = data.Value<string>("price").ParseToDouble();
            if (salePrice == null || salePrice <= 0) throw new Exception("가격을 확인할 수 없습니다.");
            double? originalPrice = data.Value<string>("msrp").ParseToDouble();
            if (originalPrice == null || originalPrice < salePrice)
            {
                originalPrice = salePrice;
            }

            List<Price> result = new List<Price>();
            result.Add(new Price(Currency.USD, (double)originalPrice, (double)salePrice, true));
            return result;
        }
        public List<Desc> GetDescs()
        {
            return null;
        }
        public List<Tag> GetTags()
        {
            return null;
        }
        public string GetVideoUrl()
        {
            return null;
        }
        public List<string> GetMainImages(JObject productObjectData)
        {
            List<JToken>? images = productObjectData["images"]?.ToList();
            if (images == null || images.Count <= 0) throw new Exception("메인 이미지를 찾을 수 없습니다.");
            List<string> result = new List<string>();
            foreach (JToken image in images)
            {
                string imageUrl = image.ToString();
                if (string.IsNullOrWhiteSpace(imageUrl)) continue;
                string uriString = imageUrl.GetUriString();
                if (string.IsNullOrWhiteSpace(uriString)) continue;
                result.Add(uriString);
            }
            if (result.Count <= 0) throw new Exception("메인 이미지를 찾을 수 없습니다.");
            return result;
        }
        public string GetDetailPageHtml(JObject productObjectData, string sizeHtml)
        {
            var htmlBuilder = new StringBuilder();

            string description = productObjectData.Value<string>("description");
            if (string.IsNullOrWhiteSpace(description) == false)
            {
                htmlBuilder.AppendLine($"<div>");
                htmlBuilder.AppendLine(description);
                htmlBuilder.AppendLine($"</div>");
            }

            if (string.IsNullOrWhiteSpace(sizeHtml) == false)
            {
                htmlBuilder.AppendLine(sizeHtml);
            }

            string result = htmlBuilder.ToString();
            result = HtmlReader.GetDafaultHtml(result);

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(result);

            HtmlNodeCollection tables = htmlDocument.DocumentNode.SelectNodes("//table");
            if (tables != null)
            {
                foreach (HtmlNode table in tables)
                {
                    table.Attributes.Add("border", "1px");
                }
                result = htmlDocument.DocumentNode.OuterHtml;
            }
            if (string.IsNullOrWhiteSpace(result)) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            return result;
        }
        public Option GetOption(string html, JObject data, JObject layerData)
        {
            try
            {
                string optionHtml = html.GetValue("//form[@action='/cart/add']", TextEx.XPathOption.OuterHtml);
                if (string.IsNullOrWhiteSpace(optionHtml)) return null;
                List<string> optionWraps = optionHtml.GetValues("//div[contains(@id,'optionWrap')]", TextEx.XPathOption.OuterHtml);
                if (optionWraps == null || optionWraps.Count <= 0) return null;
                var optionGroups = new List<Option.OptionGroup>();

                List<string> colorOrders = new List<string>
                {
                    "xxxsmall",
                    "xxsmall",
                    "xsmall",
                    "small",
                    "medium",
                    "large",
                    "xlarge",
                    "xxlarge",
                    "xxxlarge",
                };

                for (int i = 0; i < optionWraps.Count; i++)
                {
                    string optionWrap = optionWraps[i];
                    string variant__label = optionWrap.GetSelectNodeInnerText("//label[@class='variant__label']").Trim();
                    List<string> variant__labels = variant__label.Split(" ", StringSplitOptions.TrimEntries).ToList();
                    variant__labels = variant__labels.FindAll(x => string.IsNullOrWhiteSpace(x) == false);
                    string optionName = string.Join(" ", variant__labels);

                    var optionGroup = new Option.OptionGroup();
                    optionGroup.optionId = i.ToString();
                    optionGroup.groupName = new List<Name> { new Name(Language.EN, optionName) };
                    optionGroup.groups = new List<Option.OptionGroup.Group>();

                    List<string> data_values = optionWrap.GetValues("//div[@class='variant-input']", TextEx.XPathOption.OuterHtml);

                    int defaultLength = data_values.Count >= colorOrders.Count ? data_values.Count : colorOrders.Count;
                    for (int j = 0; j < defaultLength; j++)
                    {
                        optionGroup.groups.Add(null);
                    }

                    for (int j = 0; j < data_values.Count; j++)
                    {
                        var group = new Option.OptionGroup.Group();
                        //group.id = j.ToString();
                        string label = data_values[j].GetValue("//label", TextEx.XPathOption.InnerText);
                        if (string.IsNullOrWhiteSpace(label)) continue;
                        label = label.Trim();
                        group.optionName = new List<Name> { new Name(Language.EN, label) };
                        var style = data_values[j].GetValue("//label", "style");
                        if (string.IsNullOrWhiteSpace(style) == false)
                        {
                            string imageUrl = style.SingleSplit("background-image: url(", ");");
                            if (imageUrl.Contains("_"))
                            {
                                var getExtension = Path.GetExtension(imageUrl);
                                var indexOf = imageUrl.IndexOf("_");
                                imageUrl = imageUrl.Substring(0, indexOf) + getExtension;
                            }
                            imageUrl = imageUrl.GetUriString();
                            group.optionImage = imageUrl;
                        }

                        int orderIdx = colorOrders.IndexOf(label.ToLower());
                        if (orderIdx >= 0)
                        {
                            if (optionGroup.groups[orderIdx] == null)
                            {
                                optionGroup.groups[orderIdx] = group;
                            }
                            else
                            {
                                optionGroup.groups.Add(group);
                            }
                        }
                        else
                        {
                            optionGroup.groups.Add(group);
                        }
                    }

                    optionGroup.groups = optionGroup.groups.FindAll(x => x != null);
                    for (int j = 0; j < optionGroup.groups.Count; j++)
                    {
                        optionGroup.groups[j].groupId = j.ToString();
                    }
                    optionGroups.Add(optionGroup);
                }

                string optionGroupName = ProductHelper.GetOptionGroupName(optionGroups);

                List<JToken> variants = data["variants"]?.ToList();
                List<JToken> layerDataVariants = layerData["variants"].ToList();
                if (variants == null || variants.Count <= 0) return null;
                var detailOptions = new List<Option.DetailOption>();
                for (int i = 0; i < variants.Count; i++)
                {
                    JToken variant = variants[i];
                    string id = variant.Value<string>("id");

                    var ids = new List<string>();
                    List<JToken> options = variant["options"].ToList();
                    foreach (Option.OptionGroup optionGroup in optionGroups)
                    {
                        foreach (Option.OptionGroup.Group group in optionGroup.groups)
                        {
                            var text = group.optionName.First().text;
                            foreach (var option in options)
                            {
                                if (text == option.ToString())
                                {
                                    ids.Add(group.groupId);
                                    break;
                                }
                            }
                        }
                    }
                    if (ids.Count != options.Count)
                    {
                        continue;
                    }
                    ids.Add(id);
                    var detailOption = new Option.DetailOption();
                    detailOption.optionIds = string.Join("ㅡ", ids);
                    detailOption.optionGroupName = new List<Name> { new Name(Language.EN, optionGroupName) };
                    detailOption.optionName = new List<Name>();
                    var optionNames = new List<string>();
                    for (int j = 0; j < options.Count; j++)
                    {
                        string key = $"option{(j + 1).ToString()}";
                        string optionName = variant.Value<string>(key);
                        optionNames.Add(optionName);
                    }
                    detailOption.optionName.Add(new Name(Language.EN, string.Join("ㅡ", optionNames)));

                    double? salePrice = variant.Value<double?>("price");
                    if (salePrice == null || salePrice <= 0) throw new Exception("옵션 가격 정보를 확인할 수 없습니다.");
                    salePrice /= 100d;
                    double? originalPrice = variant.Value<double?>("compare_at_price");
                    if (originalPrice == null || originalPrice < salePrice)
                    {
                        originalPrice = salePrice;
                    }
                    else
                    {
                        originalPrice /= 100d;
                    }
                    detailOption.price = new List<Price> { new Price(Currency.USD, (double)originalPrice, (double)salePrice) };

                    JToken find = layerDataVariants.Find(x => x.Value<string>("id") == id);
                    bool inStock = find.Value<bool>("inStock");
                    if (inStock)
                    {
                        detailOption.isSoldOut = false;
                        detailOption.quantity = 9;
                    }
                    else
                    {
                        detailOption.isSoldOut = true;
                        detailOption.quantity = 0;
                    }
                    detailOptions.Add(detailOption);
                }

                Option result = new Option();
                result.optionGroups = optionGroups;
                result.detailOptions = detailOptions;

                if (result.detailOptions.Count <= 0)
                {
                    return null;
                }
                return result;
            }
            catch (Exception exception)
            {
                throw new Exception("옵션 수집 과정에서 문제가 발생했습니다.", exception);
            }
        }
        public Product GetProduct(string url, string html, string sizeHtml)
        {
            var product = new Product();
            try
            {
                JObject layerData = GetLayerData(html);
                JObject productObjectData = GetProductObjectData(html);
                product.site = this.Site;
                product.code = GetCode(url);
                product.url = $"https://shop.simon.com/products/{product.code}";
                product.currency = Currency.USD.ToString();
                product.title = GetTitle(layerData);
                product.brand = GetBrand(layerData);
                product.price = GetPrice(layerData);
                product.descs = GetDescs();
                product.tags = GetTags();
                product.mainImages = GetMainImages(productObjectData);
                product.detailPageHtml = GetDetailPageHtml(productObjectData, sizeHtml);
                product.option = GetOption(html, productObjectData, layerData);
                product.regDate = DateTime.Now;

                var inStock = layerData.Value<bool?>("inStock");
                if (inStock == null || inStock == false) throw new Exception("재고가 없는 상품입니다.");
            }
            catch (Exception exception)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = exception.Message;
            }
            return product;
        }
        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            string sizeGuidePath = html.SingleSplit("$.get(\"/pages/", "\", function (data) {");
            string sizeHtml = null;
            if (string.IsNullOrWhiteSpace(sizeGuidePath) == false)
            {
                string sizeUrl = $"https://shop.simon.com/pages/{sizeGuidePath}";
                sizeHtml = await ChatHubService.GetRequest(connectionId, sizeUrl);
            }
            return GetProduct(url, html, sizeHtml);
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
