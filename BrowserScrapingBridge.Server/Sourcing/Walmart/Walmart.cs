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

namespace BrowserScrapingBridge.Server.Sourcing.Walmart
{
    public class Walmart : ICollect
    {
        public string Site => nameof(Walmart);
        public string Name => "월마트";
        public Language Language => Language.EN;
        public Currency Currency => Currency.USD;
        public string CheckLoginUrl => "https://www.walmart.com/";
        public string Host => "www.walmart.com";
        public bool UseDetailHtmlRequest => false;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }


        public string GetCode(string url)
        {
            var uri = new Uri(url);
            string code = uri.Segments.Last();
            if (code.Contains("/"))
            {
                code = code.Substring(0, code.IndexOf("/"));
            }
            return code;
        }
        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }
        public List<CollectPageInfo> GetCollectPageInfos(string url, string html, int page)
        {
            string nextData = html.GetValue("//script[@id='__NEXT_DATA__']", TextEx.XPathOption.InnerText);
            JObject data = nextData.ToJObject();
            if (data == null) return null;

            List<JToken>? items = data["props"]?["pageProps"]?["initialData"]?["searchResult"]?["itemStacks"]?.First()?["items"]?.ToList();
            if (items == null || items.Count <= 0) return null;

            var result = new List<CollectPageInfo>();
            foreach (JToken item in items)
            {
                string? usItemId = item.Value<string>("usItemId");
                if (string.IsNullOrWhiteSpace(usItemId)) continue;
                string? canonicalUrl = item.Value<string>("canonicalUrl");
                if (string.IsNullOrWhiteSpace(canonicalUrl))
                {
                    canonicalUrl = $"https://www.walmart.com/ip/-/{usItemId}";
                }
                string? name = item.Value<string>("name");

                var collectPageInfo = new CollectPageInfo
                {
                    Code = usItemId,
                    Url = $"https://{Host}{canonicalUrl}",
                    Title = name
                };
                result.Add(collectPageInfo);
            }
            return result;
        }
        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            UriBuilderAdvance uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("page", page.ToString());
            var pageUrl = uriBuilderAdvance.ToString();
            var html = await ChatHubService.GetRequest(connectionId, pageUrl);
            return GetCollectPageInfos(url, html, page);
        }
        private JObject GetData(string html)
        {
            var nextData = html.GetValue("//script[@id='__NEXT_DATA__']", TextEx.XPathOption.InnerHtml);
            return nextData.ToJObject();
        }
        public List<Name> GetTitle(JToken data)
        {
            JToken? product = data["props"]?["pageProps"]?["initialData"]?["data"]?["product"];
            if (product == null) throw new Exception("상품명을 찾을 수 없습니다.");
            string? name = product?.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("상품명을 찾을 수 없습니다.");
            var names = new List<Name>() { new Name(Language.EN, name) };
            return names;
        }
        public List<Name> GetBrand(JToken data)
        {
            JToken? product = data["props"]?["pageProps"]?["initialData"]?["data"]?["product"];
            if (product == null) throw new Exception("브랜드를 찾을 수 없습니다.");
            string? name = product?.Value<string>("brand");
            if (string.IsNullOrWhiteSpace(name)) return null;
            var names = new List<Name>() { new Name(Language.EN, name) };
            return names;
        }
        public List<Desc> GetDescs(JToken data)
        {
            JToken? idml = data["props"]?["pageProps"]?["initialData"]?["data"]?["idml"];
            if (idml == null) return null;

            string? longDescription = idml.Value<string>("longDescription");
            if (string.IsNullOrWhiteSpace(longDescription)) return null;

            List<Desc> descs = new List<Desc>();

            List<string> lis = longDescription.GetValues("//li", TextEx.XPathOption.InnerText);
            if (lis == null || lis.Count <= 0) return null;
            for (int i = 0; i < lis.Count; i++)
            {
                string[] split = lis[i].Split(':');
                if (split.Length != 2) continue;
                if (string.IsNullOrWhiteSpace(split[0])) continue;
                if (string.IsNullOrWhiteSpace(split[1])) continue;
                var desc = new Desc((i + 1), Language.EN, split[0].Trim(), split[1].Trim());
                descs.Add(desc);
            }
            return descs;
        }
        public List<Tag> GetTags(JToken data)
        {
            try
            {
                List<JToken>? modules = data["props"]?["pageProps"]?["initialData"]?["data"]?["contentLayout"]?["modules"]?.ToList();
                if (modules == null || modules.Count <= 0) return null;
                JToken? find = modules.Find(x => x["configs"]?["__typename"]?.ToString() == "TempoWM_GLASSWWWItemRelatedShelvesConfigs");
                if (find == null) return null;
                List<JToken>? relms = find["configs"]?["seoItemRelmData"]?["relm"]?.ToList();
                if (relms == null || relms.Count <= 0) return null;

                var tags = new List<Tag>();
                for (int i = 0; i < relms.Count; i++)
                {
                    string? id = relms[i].Value<string>("id");
                    string? name = relms[i].Value<string>("name");

                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    Tag tag = new Tag()
                    {
                        key = id,
                        values = new List<string> { name }
                    };
                    tags.Add(tag);
                }
                return tags;
            }
            catch
            {
                return null;
            }
        }
        public List<Price> GetPrice(JToken data)
        {
            try
            {
                JToken? currentPrice = data["props"]?["pageProps"]?["initialData"]?["data"]?["product"]?["priceInfo"]?["currentPrice"];
                if (currentPrice == null) return null;
                double? salePrice = currentPrice?.Value<double>("price");
                if (salePrice == null) return null;

                JToken? wasPrice = data["props"]?["pageProps"]?["initialData"]?["data"]?["product"]?["priceInfo"]?["wasPrice"];
                double? originalPrice = null;
                if (wasPrice != null && wasPrice.HasValues)
                {
                    originalPrice = wasPrice?.Value<double>("price");
                }

                if (originalPrice == null || originalPrice <= 0)
                {
                    originalPrice = salePrice;
                }

                string? currency = currentPrice?.Value<string>("currencyUnit");
                if (string.IsNullOrWhiteSpace(currency)) return null;

                var result = new List<Price>()
            {
                new Price(currency, (double)originalPrice, (double)salePrice) { isdefalutPrice = true }
            };
                return result;
            }
            catch
            {
                return null;
            }
        }
        public List<string> GetMainImages(JToken data)
        {
            JToken? product = data["props"]?["pageProps"]?["initialData"]?["data"]?["product"];
            if (product == null) throw new Exception("메인 이미지를 찾을 수 없습니다.");
            List<JToken>? allImages = product["imageInfo"]?["allImages"]?.ToList();
            if (allImages == null || allImages.Count <= 0) throw new Exception("메인 이미지를 찾을 수 없습니다.");

            List<string> result = new List<string>();
            foreach (JToken allImage in allImages)
            {
                string? url = allImage.Value<string>("url");
                if (string.IsNullOrWhiteSpace(url)) continue;
                result.Add(url);
            }
            if (result.Count <= 0) throw new Exception("메인 이미지를 찾을 수 없습니다.");
            return result;
        }

        public string GetDetailPageHtml(JToken data)
        {
            JToken? idml = data["props"]?["pageProps"]?["initialData"]?["data"]?["idml"];
            if (idml == null) throw new Exception("상세 페이지를 찾을 수 없습니다.");

            string? shortDescription = idml.Value<string>("shortDescription");
            string? longDescription = idml.Value<string>("longDescription");
            List<JToken>? specifications = idml["specifications"]?.ToList();
            List<JToken>? directions = idml["directions"]?.ToList();

            var htmlBuilder = new StringBuilder();
            htmlBuilder.AppendLine("<h2>About this item</h2>");

            if (string.IsNullOrWhiteSpace(shortDescription) == false && string.IsNullOrWhiteSpace(longDescription) == false)
            {
                htmlBuilder.AppendLine("<hr>");
                htmlBuilder.AppendLine("<div>");
                htmlBuilder.AppendLine("<h2>Product details</h2>");

                htmlBuilder.AppendLine($"<p>{shortDescription}</p>");
                htmlBuilder.AppendLine(longDescription);

                htmlBuilder.AppendLine("</div>");
            }

            if (specifications != null && specifications.Count > 0)
            {
                htmlBuilder.AppendLine("<hr>");
                htmlBuilder.AppendLine("<div>");
                htmlBuilder.AppendLine("<h2>Specifications</h2>");

                foreach (JToken specification in specifications)
                {
                    string? name = specification.Value<string>("name");
                    string? value = specification.Value<string>("value");
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value)) continue;
                    htmlBuilder.AppendLine($"<h3>{name}</h3>");
                    htmlBuilder.AppendLine($"<div>{value}</div>");
                }

                htmlBuilder.AppendLine("</div>");
            }

            if (directions != null && directions.Count > 0)
            {
                htmlBuilder.AppendLine("<hr>");
                htmlBuilder.AppendLine("<div>");
                htmlBuilder.AppendLine("<h2>Directions</h2>");

                foreach (JToken direction in directions)
                {
                    string? name = direction.Value<string>("name");
                    string? value = direction.Value<string>("value");
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value)) continue;
                    htmlBuilder.AppendLine($"<h3>{name}</h3>");
                    htmlBuilder.AppendLine($"<div>{value}</div>");
                }

                htmlBuilder.AppendLine("</div>");
            }

            string result = htmlBuilder.ToString();
            return result;
        }
        public Option GetOption(JToken data)
        {
            try
            {
                JToken? product = data["props"]?["pageProps"]?["initialData"]?["data"]?["product"];

                List<JToken>? variantCriterias = product["variantCriteria"]?.ToList();
                if (variantCriterias == null || variantCriterias.Count <= 0) return null;

                var optionGroups = new List<Option.OptionGroup>();
                foreach (JToken variantCriteria in variantCriterias)
                {
                    string? id = variantCriteria.Value<string>("id");
                    string? name = variantCriteria.Value<string>("name");

                    Option.OptionGroup optionGroup = new Option.OptionGroup();
                    optionGroup.optionId = id;
                    optionGroup.groupName = new List<Name>() { new Name(Language.EN, name) };
                    optionGroup.groups = new List<Option.OptionGroup.Group>();

                    List<JToken>? variantList = variantCriteria["variantList"]?.ToList();
                    foreach (JToken variant in variantList)
                    {
                        string? variantId = variant.Value<string>("id");
                        string? variantName = variant.Value<string>("name");
                        string imageUrl = null;
                        List<JToken>? images = variant["images"]?.ToList();
                        foreach (var image in images)
                        {
                            imageUrl = product["imageMap"]?[image.ToString()]?.Value<string>("url");
                            if (string.IsNullOrWhiteSpace(imageUrl) == false) break;
                        }
                        var group = new Option.OptionGroup.Group(variantId, Language.EN, variantName, imageUrl);
                        optionGroup.groups.Add(group);
                    }
                    optionGroups.Add(optionGroup);
                }

                List<JToken>? variantProductIdMaps = product["variantProductIdMap"]?.ToList();
                if (variantProductIdMaps == null || variantProductIdMaps.Count <= 0) return null;

                List<string> variantProductIds = variantProductIdMaps.Select(x => x.First().ToString()).ToList();

                JToken? variantsMap = product["variantsMap"];
                if (variantsMap == null) return null;

                var detailOptions = new List<Option.DetailOption>();
                foreach (var variantProductId in variantProductIds)
                {
                    JToken? variantsMapValue = variantsMap[variantProductId];

                    string? availabilityStatus = variantsMapValue.Value<string>("availabilityStatus");
                    string? id = variantsMapValue.Value<string>("id");

                    var ids = new List<string>();
                    var optionGroupNames = new List<string>();
                    var optionNames = new List<string>();
                    List<JToken> variants = variantsMapValue["variants"].ToList();
                    foreach (var variant in variants)
                    {
                        string variantStr = variant.ToString();
                        ids.Add(variantStr);

                        string optionId = variantStr.Split("-").First();
                        Option.OptionGroup? findOptionGroups = optionGroups.Find(x => x.optionId == optionId);
                        optionGroupNames.Add(findOptionGroups.groupName.First().text);

                        Option.OptionGroup.Group? findGroup = findOptionGroups.groups.Find(x => x.groupId == variantStr);
                        optionNames.Add(findGroup.optionName.First().text);
                    }
                    string detailOptionId = string.Join("ㅡ", ids);
                    string optionGroupName = string.Join("ㅡ", optionGroupNames);
                    string optionName = string.Join("ㅡ", optionNames);

                    JToken? currentPrice = variantsMapValue["priceInfo"]?["currentPrice"];
                    double? salePrice = currentPrice.Value<double?>("price");
                    if (salePrice == null || salePrice <= 0) throw new Exception("옵션 가격에 문제가 발생했습니다.");

                    double? originalPrice = null;
                    JToken? wasPrice = variantsMapValue["priceInfo"]?["wasPrice"];
                    if (wasPrice != null && wasPrice.HasValues)
                    {
                        originalPrice = wasPrice?.Value<double?>("price");
                    }

                    if (originalPrice == null || originalPrice <= 0)
                    {
                        originalPrice = salePrice;
                    }
                    var detailOption = new Option.DetailOption();
                    detailOption.optionIds = detailOptionId;
                    detailOption.optionGroupName = new List<Name>() { new Name(Language.EN, optionGroupName) };
                    detailOption.optionName = new List<Name>() { new Name(Language.EN, optionName) };
                    detailOption.price = new List<Price> { new Price(Currency.USD, (double)originalPrice, (double)salePrice) };

                    if (availabilityStatus == "IN_STOCK")
                    {
                        detailOption.isSoldOut = false;
                        detailOption.quantity = 9;
                    }
                    else
                    {
                        detailOption.isSoldOut = true;
                    }
                    detailOptions.Add(detailOption);
                }

                Option option = new Option();
                option.optionGroups = optionGroups;
                option.detailOptions = detailOptions;
                return option;
            }
            catch (Exception exception)
            {
                throw new Exception("옵션 정보를 찾을 수 없습니다.", exception);
            }
        }
        public Product GetProduct(string url, string html)
        {
            Product product = new Product();
            try
            {
                JObject data = GetData(html);
                if (data == null) throw new Exception("상품 정보를 조회할 수 없습니다.");
                product.site = this.Site;
                product.code = GetCode(url);
                product.url = url;
                product.currency = Currency.USD.ToString();
                product.title = GetTitle(data);
                product.brand = GetBrand(data);
                product.descs = GetDescs(data);
                product.tags = GetTags(data);
                product.mainImages = GetMainImages(data);
                product.price = GetPrice(data);
                product.detailPageHtml = GetDetailPageHtml(data);
                product.option = GetOption(data);
                if (product.option != null)
                {
                    product.price = ProductHelper.GetMinPriceInDetailOption(product.option.detailOptions, Currency.USD);
                }
                product.regDate = DateTime.Now;
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
            var html = await ChatHubService.GetRequest(connectionId, url);
            return GetProduct(url, html);
        }

        public async Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            var html = await ChatHubService.GetRequest(connectionId, url);
            JObject data = GetData(html);
            return new CollectRawData(url, data, html);
        }

        public Product GetProduct(CollectRawData rawData)
        {
            Product product = new Product();
            try
            {
                if (rawData.Data == null) throw new Exception("상품 정보를 조회할 수 없습니다.");
                product.site = this.Site;
                product.code = GetCode(rawData.Url);
                product.url = rawData.Url;
                product.currency = Currency.USD.ToString();
                product.title = GetTitle(rawData.Data);
                product.brand = GetBrand(rawData.Data);
                product.descs = GetDescs(rawData.Data);
                product.tags = GetTags(rawData.Data);
                product.mainImages = GetMainImages(rawData.Data);
                product.price = GetPrice(rawData.Data);
                product.detailPageHtml = GetDetailPageHtml(rawData.Data);
                product.option = GetOption(rawData.Data);
                if (product.option != null)
                {
                    if (product.price == null)
                    {
                        product.price = ProductHelper.GetMinPriceInDetailOption(product.option.detailOptions, Currency.USD);
                    }
                }
                product.regDate = DateTime.Now;
            }
            catch (Exception exception)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = exception.Message;
            }
            return product;
        }

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            return;
        }
    }
}