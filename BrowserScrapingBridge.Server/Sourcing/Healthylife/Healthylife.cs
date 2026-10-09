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

namespace BrowserScrapingBridge.Server.Sourcing.Healthylife
{
    public class Healthylife : ICollect
    {
        public string Site => nameof(Healthylife);

        public string Name => "헬시라이프";

        public Language Language => Language.EN;

        public Currency Currency => Currency.AUD;

        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public string GetCode(string url)
        {
            var uri = new Uri(url);
            var code = uri.Segments.Last();
            return code;
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
                        JToken firstItem = self.ToJArray()?[1];
                        if (firstItem == null) continue;
                        if (firstItem.Type == JTokenType.String)
                        {
                            var list = firstItem.ToString().Split("\n");
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
                            string id = firstItem.Value<string>("id");
                            string children = firstItem.Value<string>("children");
                            string __next_env__ = children.SubstringBetween("=", ";", false);
                            data[id] = __next_env__.ToJObject();
                        }
                    }
                }
            }
            return data;
        }



        private async Task<JToken> GetCategoryPageDatas(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("page", page);
            var requestUrl = uriBuilderAdvance.ToString();
            var html = await ChatHubService.GetRequest(connectionId, requestUrl);
            List<string> scripts = html.GetValues("//script", TextEx.XPathOption.InnerHtml);
            if (scripts == null || scripts.Count <= 0) return null;
            string instantSearchInitialResults = scripts.Find(x => x.Contains("InstantSearchInitialResults"));
            if (string.IsNullOrWhiteSpace(instantSearchInitialResults)) return null;
            string instantSearchInitial = instantSearchInitialResults.SubstringBetween("{", "}");
            if (string.IsNullOrWhiteSpace(instantSearchInitial)) return null;
            var data = instantSearchInitial.ToJObject();
            if (data == null) return null;
            var results = data.Value("prod_products", "results");
            return results;
        }

        public string CreatePostBody(string url, int page)
        {
            // 원본 post 문자열
            //var body = "{\"requests\":[{\"indexName\":\"prod_products\",\"params\":\"attributesToRetrieve=%5B%22objectID%22%2C%22name%22%2C%22code%22%2C%22brand%22%2C%22available%22%2C%22flags%22%2C%22url%22%2C%22rrp%22%2C%22tags%22%2C%22image_sm%22%2C%22schedule%22%2C%22price%22%2C%22price_type%22%2C%22list_price%22%2C%22isPrescription%22%2C%22is_clearance%22%2C%22is_preorder%22%2C%22booking_type%22%2C%22promotions%22%2C%22recommending_journey_ids%22%5D&clickAnalytics=true&facets=%5B%22Product%20Badges%22%2C%22available%22%2C%22brand%22%2C%22categories%22%2C%22hierarchicalCategories.level1%22%2C%22hierarchicalCategories.level2%22%2C%22hierarchicalCategories.level3%22%2C%22is_promo%22%2C%22price_range%22%5D&filters=NOT%20schedule%3A3%20AND%20NOT%20schedule%3A4&highlightPostTag=__%2Fais-highlight__&highlightPreTag=__ais-highlight__&hitsPerPage=20&maxValuesPerFacet=50&page=0&query=vita\"}]}";

            var uriBuilderAdvance = new UriBuilderAdvance(url);
            string query = uriBuilderAdvance.GetQuery("query");

            var data = new JObject();
            var requests = new JArray();
            var request = new JObject();
            request["indexName"] = "prod_products";
            var attributesToRetrieves = new List<string>();
            var projections = new List<string>
            {
                "objectID",
                "name",
                "code",
                "brand",
                "available",
                "flags",
                "url",
                "rrp",
                "tags",
                "image_sm",
                "schedule",
                "price",
                "price_type",
                "list_price",
                "isPrescription",
                "is_clearance",
                "is_preorder",
                "booking_type",
                "promotions",
                "recommending_journey_ids"
            }.ToSerializeObject().ToWebUrlEncoding();
            attributesToRetrieves.Add(projections);
            attributesToRetrieves.Add("clickAnalytics=true");
            var facets = new List<string>
            {
                "Product Badges",
                "available",
                "brand",
                "categories",
                "hierarchicalCategories.level1",
                "hierarchicalCategories.level2",
                "hierarchicalCategories.level3",
                "is_promo",
                "price_range"
            }.ToSerializeObject().ToWebUrlEncoding();
            attributesToRetrieves.Add($"facets={facets}");
            var filters = "NOT schedule:3 AND NOT schedule:4".ToWebUrlEncoding();
            attributesToRetrieves.Add($"filters={filters}");
            attributesToRetrieves.Add("highlightPostTag=__%2Fais-highlight__");
            attributesToRetrieves.Add("highlightPreTag=__ais-highlight__");
            attributesToRetrieves.Add("hitsPerPage=20");
            attributesToRetrieves.Add("maxValuesPerFacet=50");
            if (page <= 1) page = 1;
            attributesToRetrieves.Add($"page={page - 1}");
            attributesToRetrieves.Add($"query={query}");
            var attributesToRetrieve = attributesToRetrieves.Join("&").Replace("+", "%20"); ;
            request["params"] = $"attributesToRetrieve={attributesToRetrieve}";
            requests.Add(request);
            data["requests"] = requests;
            var postString = data.ToString();
            return postString;
        }

        private async Task<JToken> GetSearchPageDatas(string connectionId, string url, int page)
        {
            var apiRequest = "https://qt1r3akgvw-dsn.algolia.net/1/indexes/*/queries?x-algolia-agent=Algolia%20for%20JavaScript%20(4.24.0)%3B%20Browser%3B%20instantsearch.js%20(4.74.0)%3B%20react%20(18.3.0-canary-14898b6a9-20240318)%3B%20react-instantsearch%20(7.13.0)%3B%20react-instantsearch-core%20(7.13.0)%3B%20next.js%20(14.2.4)%3B%20JS%20Helper%20(3.22.4)";
            var body = CreatePostBody(url, page);
            var dic = new Dictionary<string, string>();
            dic.Add("x-algolia-api-key", "cd7968c3a69ab4667fdddb2cedecd5d5");
            dic.Add("x-algolia-application-id", "QT1R3AKGVW");
            var html = await ChatHubService.PostRequest(connectionId, apiRequest, dic, body);
            JObject pageResponse = html.ToJObject();
            if (pageResponse == null) return null;
            JToken results = pageResponse.Value("results");
            return results;
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            var query = uriBuilderAdvance.GetQuery("query");

            JToken results = null;
            if (string.IsNullOrWhiteSpace(query)) // 쿼리를 기준으로 카테고리, 검색 구분
            {
                results = await GetCategoryPageDatas(connectionId, url, page);
            }
            else
            {
                results = await GetSearchPageDatas(connectionId, url, page);
            }
            if (results == null) return null;
            var collectPageInfos = new List<CollectPageInfo>();
            foreach (var result in results)
            {
                var hits = result.Value("hits");
                foreach (JToken hit in hits)
                {
                    var path = hit.Value<string>("url");
                    if (string.IsNullOrWhiteSpace(path)) continue;
                    string code = path.Split("/").Last();
                    string itemUrl = $"https://www.healthylife.com.au{path}";
                    string name = hit.Value<string>("name");
                    var collectPageInfo = new CollectPageInfo();
                    collectPageInfo.Code = code;
                    collectPageInfo.Url = itemUrl;
                    collectPageInfo.Title = name;
                    collectPageInfos.Add(collectPageInfo);
                }
            }
            return collectPageInfos;
        }

        public async Task<string> GetLoginUrlAsync(string connectionId) { return null; }

        public List<Name> GetTitle(JToken data)
        {
            string name = data.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(this.Language, name);
        }

        public List<Name> GetBrand(JToken data)
        {
            var name = data.Value("brand")?.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) return null;
            return ProductHelper.CreateName(this.Language, name);
        }

        public string GetModel(JToken data)
        {
            return data.Value<string>("code");
        }

        public List<Price> GetPrice(JToken data)
        {
            var rrp = data.Value<string>("rrp");
            double? originalPrice = rrp.ParseToDouble();
            List<JToken> prices = data.Value("prices")?.ToList();
            double? salePrice = originalPrice;
            if (prices != null || prices.Count > 0)
            {
                var min = prices.Min(x => x.Value<string>("value"));
                if (string.IsNullOrWhiteSpace(min) == false)
                {
                    salePrice = min.ParseToDouble();
                    if (originalPrice < salePrice)
                    {
                        originalPrice = salePrice;
                    }
                }
            }
            if (salePrice == null || salePrice <= 0) throw new Exception("상품 가격을 확인할 수 없습니다.");
            return ProductHelper.CreatePrice(this.Currency, originalPrice, salePrice);
        }

        public List<string> GetMainImages(JToken data)
        {
            var images = data.Value("images")?.ToList();
            if (images == null || images.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            var result = images.Select(x => x.Value<string>("url"))?.ToList();
            if (result == null || result.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            return result;
        }

        public string GetDetailPageHtml(JToken data)
        {
            var description = data.Value<string>("description");
            var txtIngredients = data.Value<string>("txtIngredients");
            var txtDirections = data.Value<string>("txtDirections");
            var txtStorage = data.Value<string>("txtStorage");
            var warnings = data.Value<string>("txtWarnings");

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(description);
            stringBuilder.AppendLine(txtIngredients);
            stringBuilder.AppendLine(txtDirections);
            stringBuilder.AppendLine(txtStorage);
            stringBuilder.AppendLine(warnings);

            var detailPageHtml = stringBuilder.ToString().ToHtmlDecodeComplete();
            return detailPageHtml;
        }

        public Option GetOption(JToken data)
        {
            return null; // 각 옵션이 개별 상품임
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            Product product = new Product();
            try
            {
                JToken data = GetData(html);
                string _8 = data.Value<string>("8:");
                JArray array = _8.ToJArray();
                JToken productData = null;
                foreach (var item in array)
                {
                    if (item.Type == JTokenType.Null) continue;
                    JToken last = item.Last();
                    JToken innerProduct = last.Value("product");
                    if (innerProduct != null)
                    {
                        productData = innerProduct;
                        break;
                    }
                }
                if (productData == null) throw new Exception("상품 정보를 찾을 수 없습니다.");
                product.site = this.Site;
                product.code = GetCode(url);
                product.url = url;
                product.currency = this.Currency.ToString();
                product.title = GetTitle(productData);
                product.brand = GetBrand(productData);
                product.model = GetModel(productData);
                product.price = GetPrice(productData);
                product.mainImages = GetMainImages(productData);
                product.detailPageHtml = GetDetailPageHtml(productData);
                product.option = GetOption(data);
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
            return new CollectRawData(url, data, html);
        }

        public Product GetProduct(CollectRawData rawData)
        {
            Product product = new Product();
            try
            {
                string _8 = rawData.Data.Value<string>("8:");
                JArray array = _8.ToJArray();
                JToken productData = null;
                foreach (var item in array)
                {
                    if (item.Type == JTokenType.Null) continue;
                    JToken last = item.Last();
                    JToken innerProduct = last.Value("product");
                    if (innerProduct != null)
                    {
                        productData = innerProduct;
                        break;
                    }
                }
                if (productData == null) throw new Exception("상품 정보를 찾을 수 없습니다.");
                product.site = this.Site;
                product.code = GetCode(rawData.Url);
                product.url = rawData.Url;
                product.currency = this.Currency.ToString();
                product.title = GetTitle(productData);
                product.brand = GetBrand(productData);
                product.model = GetModel(productData);
                product.price = GetPrice(productData);
                product.mainImages = GetMainImages(productData);
                product.detailPageHtml = GetDetailPageHtml(productData);
                product.option = GetOption(rawData.Data);
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
