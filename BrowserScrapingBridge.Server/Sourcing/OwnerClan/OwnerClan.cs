#nullable disable warnings
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using BrowserScrapingBridge.Server.Sourcing.OwnerClan.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;


namespace BrowserScrapingBridge.Server.Sourcing.OwnerClan
{
    public class OwnerClan : ICollect
    {
        private string baseAuthUrl = "https://auth.ownerclan.com/auth"; // Production
        //private string baseAuthUrl = "https://auth-sandbox.ownerclan.com/auth"; // Sandbox
        private string baseApiUrl = "https://api.ownerclan.com/v1/graphql"; // Production
        //private string baseApiUrl = "https://api-sandbox.ownerclan.com/v1/graphql"; // Sandbox
        public string Site { get; } = nameof(OwnerClan);
        public string Name => "오너클랜";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public string OwnerClanID { get; private set; }
        public string OwnerClanPW { get; private set; }
        private string _cachedToken;
        private DateTime? _tokenExpiryTime;
        public bool SupportsCaching { get; set; }
        public OwnerClan() { }

        private Task LoadAccountSet()
        {
            OwnerClanID = SourcingExecution.Current.GetSetting("ownerClanAccount");
            OwnerClanPW = SourcingExecution.Current.GetSetting("ownerClanPassword");
            return Task.CompletedTask;
        }

        public string GetProductListParam(string url, int page)
        {
            var keyword = url.GetParamValue("topSearchKeyword");
            if (string.IsNullOrWhiteSpace(keyword)) return null;

            var graphQL = "query { allItems(search: \"" + keyword + "\", status:available, after:\"" + (page - 1) * 100 + "\") { pageInfo { hasNextPage hasPreviousPage startCursor endCursor } edges { cursor node { createdAt updatedAt key name model production origin id price pricePolicy fixedPrice searchKeywords category { key name fullName } content shippingFee shippingType images(size: large) status options { optionAttributes { name value } price quantity } taxFree adultOnly returnable noReturnReason guaranteedShippingPeriod openmarketSellable boxQuantity attributes closingTime returnCriteria metadata } }} }";
            return "query=" + graphQL;
        }

        public string GetProductParam(string url)
        {
            var code = url.GetParamValue("selfcode");
            if (string.IsNullOrWhiteSpace(code)) return null;
            var graphQL = "query testQuery { item(key: \"" + code + "\") {createdAt updatedAt key name model production origin id price pricePolicy fixedPrice searchKeywords category { key name } content shippingFee shippingType images(size: large) status options { optionAttributes { name value } price quantity key } taxFree adultOnly returnable noReturnReason guaranteedShippingPeriod openmarketSellable boxQuantity attributes closingTime metadata } }";
            return "query=" + graphQL;
        }

        #region Common
        /// <summary>
        /// API키 가져오기
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public List<string> GetApiKeys(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;
            List<string> dataApiKeys = html.GetValues("//td[@class='lDataApiKey']", TextEx.XPathOption.InnerText);
            if (dataApiKeys == null || dataApiKeys.Count <= 0) return null;
            return dataApiKeys;
        }

        /// <summary>
        /// 오너클랜 카테고리 정보 조회
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public List<Category> GetCategories(string apiKey)
        {
            var getCategoryListApi = new GetCategoryListApi(apiKey);
            List<Category> categories = getCategoryListApi.GetCategories();
            return categories;
        }
        #endregion

        private string GetTokenRequest(string account, string password, out int statusCode)
        {
            Task.Delay(200); // 0.3초 (300밀리초) 대기

            statusCode = 0;
            using (var httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                using (var request = new HttpRequestMessage(GetMethod("POST"), $"{baseAuthUrl}"))
                {
                    request.Headers.Accept.ParseAdd("*/*");
                    request.Headers.Add("connection", "keep-alive");
                    request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue() { MaxAge = TimeSpan.Zero };

                    //var content = new StringContent("application/json");
                    var req = new
                    {
                        service = "ownerclan",
                        userType = "seller",
                        username = OwnerClanID,
                        password = OwnerClanPW
                    };
                    var content = new StringContent(JsonConvert.SerializeObject(req), null, "application/json");
                    request.Content = content;

                    var response = httpClient.SendAsync(request).GetAwaiter().GetResult();
                    statusCode = (int)response.StatusCode;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string readToEnd = response.Content.ReadAsStringAsync().Result;
                        return readToEnd;
                    }
                    else
                    {
                        Encoding encoding = Encoding.UTF8;
                        byte[] responseBytes = response.Content?.ReadAsByteArrayAsync().Result;
                        string readToEnd = encoding.GetString(responseBytes);
                        return readToEnd;
                    }
                }
            }
        }

        /// <summary>
        /// 요청의 ownerClanToken을 사용하거나, 요청으로 전달한 계정으로 토큰을 발급받습니다.
        /// </summary>
        private Task<string> GetTokenAsync()
        {
            var supplied = SourcingExecution.Current.GetSetting("ownerClanToken");
            if (!string.IsNullOrWhiteSpace(supplied)) return Task.FromResult(supplied);
            if (string.IsNullOrWhiteSpace(OwnerClanID) || string.IsNullOrWhiteSpace(OwnerClanPW))
                throw new ArgumentException("오너클랜 토큰 또는 계정·비밀번호가 필요합니다.");
            var response = GetTokenRequest(OwnerClanID, OwnerClanPW, out int status);
            if (status != 200 || string.IsNullOrWhiteSpace(response))
                throw new CollectException($"오너클랜 토큰 발급 실패: HTTP {status}");
            var token = response;
            try { token = JObject.Parse(response)["access_token"]?.ToString() ?? response; } catch { }
            return Task.FromResult(token);
        }

        public string GetReturnRequest(string method, string parameter, string data, string token, out int statusCode, string contentType = "application/json")
        {
            statusCode = 0;
            Task.Delay(200); // 0.3초 (300밀리초) 대기

            using (var httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                using (var request = new HttpRequestMessage(GetMethod(method), $"{baseApiUrl}?{parameter}"))
                {
                    request.Headers.Accept.ParseAdd("*/*");
                    if (string.IsNullOrWhiteSpace(token) == false) request.Headers.Add("Authorization", $"Bearer {token}");
                    request.Headers.Add("connection", "keep-alive");
                    request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue() { MaxAge = TimeSpan.Zero };
                    if (method.ToUpper() != "GET")
                    {
                        var content = new StringContent(data, Encoding.UTF8, contentType);
                        request.Content = content;
                    }
                    else
                    {
                        var content = new StringContent(contentType);
                        request.Content = content;
                    }
                    var response = httpClient.SendAsync(request).GetAwaiter().GetResult();
                    statusCode = (int)response.StatusCode;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string readToEnd = response.Content.ReadAsStringAsync().Result;
                        return readToEnd;
                    }
                    else
                    {
                        Encoding encoding = Encoding.UTF8;
                        byte[] responseBytes = response.Content?.ReadAsByteArrayAsync().Result;
                        string readToEnd = encoding.GetString(responseBytes);
                        return readToEnd;
                    }
                }
            }
        }

        private HttpMethod GetMethod(string method)
        {
            switch (method.ToUpper())
            {
                case "POST":
                    return HttpMethod.Post;
                case "GET":
                    return HttpMethod.Get;
                case "PUT":
                    return HttpMethod.Put;
                case "DELETE":
                    return HttpMethod.Delete;
                case "PATCH":
                    return HttpMethod.Patch;
                case "OPTIONS":
                    return HttpMethod.Options;
                default:
                    return HttpMethod.Get;
            }
        }

        public string GetCode(string url)
        {         
                Uri uri = new Uri(url);
                var query = HttpUtility.ParseQueryString(uri.Query);
                return query["selfcode"];            
        }

        public async Task<string> GetLoginUrlAsync(string connectionId) { return null; }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            Task.Delay(200); // 0.3초 (300밀리초) 대기

            await LoadAccountSet();

            string searchUrl = HttpUtility.HtmlDecode(url);
            var ownerClanToken = await GetTokenAsync();

            var ownerClanParams = GetProductListParam(searchUrl, page);

            string getOwnerClanPageHtml = GetReturnRequest("GET", WebUtility.UrlDecode(ownerClanParams), "", ownerClanToken, out int statusCode);

            if (string.IsNullOrWhiteSpace(getOwnerClanPageHtml))
            {
                throw new CollectException($"페이지 조회에 실패했습니다. URL을 확인해주세요.");
            }
            var collectPageInfos = new List<CollectPageInfo>();
            var jObject = JObject.Parse(getOwnerClanPageHtml);
            var edges = jObject["data"]?["allItems"]?["edges"];
            if (edges != null && edges.Type == JTokenType.Array)
            {
                foreach (var edge in edges)
                {
                    var key = edge["node"]?["key"]?.ToString();
                    var title = edge["node"]?["name"]?.ToString();
                    var gurl = "https://ownerclan.com/V2/product/view.php?selfcode=" + key;

                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        collectPageInfos.Add(new CollectPageInfo { Code = key, Url = gurl });
                    }
                }
            }
            return collectPageInfos;

        }

        private List<Name> GetTitle(JToken data)
        {
            string? name = data["data"]?["item"]?.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) throw new CollectException("상품명을 조회할 수 없습니다.");
            List<Name> names = new List<Name>();
            names.Add(new Name(this.Language, name));
            return names;
        }

        private List<Name> GetBrand(JToken data)
        {
            string? designer = data["data"]?["item"]?.Value<string>("production");
            if (string.IsNullOrWhiteSpace(designer)) throw new CollectException("브랜드명을 조회할 수 없습니다.");
            List<Name> names = new List<Name>();
            names.Add(new Name(this.Language, designer));
            return names;
        }

        private string GetModel(JToken data)
        {
            string sku = data["data"]?["item"]?.Value<string>("model");            
            return sku;
        }

        private List<Desc> GetDescs(JToken data) { return null; }

        private List<Tag> GetTags(JToken data)
        {

            List<string>? tags = data["data"]?["item"]?["searchKeywords"]?.ToObject<List<string>>();

            List<Tag> result = new List<Tag>();

            if (tags != null)
            {
                var tag = new Tag();
                tag.key = "kw";
                tag.values = new List<string>();

                for (int i = 0; i < tags.Count; i++)
                {
                    var value = tags[i].ToString();
                    tag.values.Add(value);
                    result.Add(tag);
                }
            }
            return result;
        }

        private List<Price> GetPrice(JToken data)
        {
            double? price = data["data"]?["item"]?.Value<double>("price");
            if (price == null) throw new CollectException("가격 정보를 찾을 수 없습니다.");
            List<Price> priceList = ProductHelper.CreatePrice(this.Currency, price, price, true);
            return priceList;
        }

        private List<string> GetMainImages(JToken data)
        {
            List<string> result = new List<string>();
            List<string>? images = data["data"]?["item"]?["images"]?.ToObject<List<string>>();
            if (images == null || images.Count <= 0) throw new CollectException("상품 이미지를 찾을 수 없습니다.");
            foreach (string Imagestr in images)
            {
                result.Add(Imagestr);
            }
            return result;
        }

        private string GetVideoUrl(JToken data) { return null; }

        private List<string> GetOriginalDetailPageUrls(JToken data) { return null; }

        private List<string> GetTranslateDetailPageUrls(JToken data) { return null; }

        private string GetDetailPageHtml(JToken data)
        {
            string? getDafaultHtml = data["data"]?["item"]?.Value<string>("content");
            if (getDafaultHtml == null) throw new CollectException("상세 페이지 정보를 찾을 수 없습니다.");
            return getDafaultHtml;
        }

        private Option GetOption(JToken data)
        {
            try
            {
                JToken? options = data["data"]?["item"]["options"];
                if (options == null || !options.Any()) return null;

                var option = new Option();

                // === OptionGroup 구성 ===
                var optionGroups = new List<Option.OptionGroup>();
                var group = new Option.OptionGroup
                {
                    optionId = "0",
                    groupName = new List<Name> { new Name(this.Language, "색상") },
                    groups = new List<Option.OptionGroup.Group>()
                };

                int groupIndex = 0;
                foreach (var opt in options)
                {
                    var attr = opt["optionAttributes"]?.FirstOrDefault();
                    if (attr == null) continue;

                    string? value = attr.Value<string>("value");
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    var groupItem = new Option.OptionGroup.Group
                    {
                        groupId = groupIndex.ToString(),
                        optionName = new List<Name> { new Name(this.Language, value) },
                        quantity = opt.Value<int?>("quantity") ?? 0,
                        price = new List<Price>
                        {
                            new Price(this.Currency, opt.Value<double?>("price") ?? 0, opt.Value<double?>("price") ?? 0)
                        }
                    };

                    group.groups.Add(groupItem);
                    groupIndex++;
                }

                optionGroups.Add(group);
                option.optionGroups = optionGroups;

                // === DetailOption 구성 ===
                var detailOptions = new List<Option.DetailOption>();
                int detailIndex = 0;
                foreach (var opt in options)
                {
                    var attr = opt["optionAttributes"]?.FirstOrDefault();
                    if (attr == null) continue;

                    string? value = attr.Value<string>("value");
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    var detail = new Option.DetailOption
                    {
                        optionIds = detailIndex.ToString(),
                        isSoldOut = (opt.Value<int?>("quantity") ?? 0) <= 0,
                        quantity = opt.Value<int?>("quantity") ?? 0,
                        optionGroupName = new List<Name> { new Name(this.Language, "색상") },
                        optionName = new List<Name> { new Name(this.Language, value) },
                        price = new List<Price>
                        {
                            new Price(this.Currency, opt.Value<double?>("price") ?? 0, opt.Value<double?>("price") ?? 0)
                        }
                    };

                    detailOptions.Add(detail);
                    detailIndex++;
                }

                option.detailOptions = detailOptions;

                return option;
            }
            catch (Exception ex)
            {
                throw new CollectException("옵션 데이터 파싱 중 오류 발생", ex);
            }
        }

        public Product GetProduct(string url, string html)
        {
            var product = new Product();
            try
            {
                product.site = this.Site;
                product.currency = this.Currency.ToString();
                JObject originalData = html.ToJObject();
                product.code = originalData["data"]["item"]["key"].ToString();
                product.url = url;
                product.title = GetTitle(originalData);
                product.brand = GetBrand(originalData);
                product.model = GetModel(originalData);
                product.descs = GetDescs(originalData);
                product.tags = GetTags(originalData);
                product.price = GetPrice(originalData);
                product.mainImages = GetMainImages(originalData);
                product.videoUrl = GetVideoUrl(originalData);
                product.originalDetailPageUrls = GetOriginalDetailPageUrls(originalData);
                product.translateDetailPageUrls = GetTranslateDetailPageUrls(originalData);
                product.detailPageHtml = GetDetailPageHtml(originalData);
                product.option = GetOption(originalData);
                product.regDate = DateTime.Now;

                if (product?.option?.detailOptions != null && product.option.detailOptions.Count > 0)
                {
                    if (product.price == null)
                    {
                        product.price = ProductHelper.GetMinPriceInDetailOption(product.option.detailOptions, product.currency.ConvertEnum<Currency>());
                    }
                }
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
            if(OwnerClanID.IsNullOrWhiteSpace())
                await LoadAccountSet();

            string itemUrl = HttpUtility.HtmlDecode(url);
            var ownerClanToken = await GetTokenAsync();
            
            await Task.Delay(300);
            
            var ownerClanParams = GetProductParam(itemUrl);

            string getOwnerClanItemHtml = GetReturnRequest("GET", WebUtility.UrlDecode(ownerClanParams), "", ownerClanToken, out int statusCode);

            if (string.IsNullOrWhiteSpace(getOwnerClanItemHtml))
            {
                throw new Exception("상품 데이터가 없습니다. ");
            }


            await Task.Delay(300);
            return GetProduct(url, getOwnerClanItemHtml);
             

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
