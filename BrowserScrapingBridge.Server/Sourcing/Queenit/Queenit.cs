#nullable disable warnings
using Newtonsoft.Json;
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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Queenit
{
    /// <summary>
    /// 퀸잇(web.queenit.kr) — 여성 패션 이커머스 플랫폼
    /// REST API 기반 수집 (api.queenit.kr)
    /// JWT 익명 토큰 인증 필요 (POST /signin/anonymous)
    /// URL 패턴:
    ///   카테고리: https://web.queenit.kr/category/{categoryId}
    ///   상품상세: https://web.queenit.kr/product/{32자 hex ID}
    /// </summary>
    public class Queenit : CollectBase
    {
        private const string ApiHost = "https://api.queenit.kr";
        private const string WebHost = "https://web.queenit.kr";
        private const int PageSize = 20;

        public override string Site => nameof(Queenit);
        public override string Name => "퀸잇";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; }

        // 토큰 관리
        private static readonly object _tokenLock = new object();
        private static string _accessToken;
        private static string _refreshToken;
        private static DateTime _tokenExpiry = DateTime.MinValue;

        private static readonly HttpClient _httpClient = CreateHttpClient();

        // 커서 기반 페이지네이션
        private static string _lastCursor;
        private static string _lastCacheKey;

        private static HttpClient CreateHttpClient()
        {
            var client = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient();
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("platform", "WEB");
            client.DefaultRequestHeaders.Add("rpls-client-version", "damoa-mobile-web/1.183.4");
            client.DefaultRequestHeaders.Add("rpls-device-category", "desktop");
            client.DefaultRequestHeaders.Add("rpls-device-id", Guid.NewGuid().ToString());
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Origin", WebHost);
            client.DefaultRequestHeaders.Add("Referer", WebHost + "/");
            return client;
        }

        // ===== 토큰 관리 =====

        private async Task EnsureTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
                return;

            await GetAnonymousTokenAsync();
        }

        private async Task GetAnonymousTokenAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiHost}/signin/anonymous");
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(body);

            lock (_tokenLock)
            {
                _accessToken = json["accessToken"]?.ToString();
                _refreshToken = json["refreshToken"]?.ToString();
                // 50분 유효 (1시간 만료 전 여유)
                _tokenExpiry = DateTime.UtcNow.AddMinutes(50);
            }
        }

        private async Task<string> ApiGetAsync(string path)
        {
            await EnsureTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiHost}{path}");
            request.Headers.Add("authorization", $"Bearer {_accessToken}");

            var response = await _httpClient.SendAsync(request);

            // 401이면 토큰 갱신 후 재시도
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await GetAnonymousTokenAsync();
                request = new HttpRequestMessage(HttpMethod.Get, $"{ApiHost}{path}");
                request.Headers.Add("authorization", $"Bearer {_accessToken}");
                response = await _httpClient.SendAsync(request);
            }

            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> ApiPostAsync(string path, object bodyObj)
        {
            await EnsureTokenAsync();

            var json = JsonConvert.SerializeObject(bodyObj);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiHost}{path}");
            request.Headers.Add("authorization", $"Bearer {_accessToken}");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await GetAnonymousTokenAsync();
                request = new HttpRequestMessage(HttpMethod.Post, $"{ApiHost}{path}");
                request.Headers.Add("authorization", $"Bearer {_accessToken}");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await _httpClient.SendAsync(request);
            }

            return await response.Content.ReadAsStringAsync();
        }

        // ===== URL 파싱 =====

        /// <summary>
        /// URL에서 카테고리 ID 추출
        /// https://web.queenit.kr/category/{catId} → catId
        /// </summary>
        private static string ParseCategoryId(string url)
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            // /category/{catId}
            if (segments.Length >= 2 && segments[0].Equals("category", StringComparison.OrdinalIgnoreCase))
                return segments[1];
            return null;
        }

        // ===== ICollect 구현 =====

        public override async Task<string> GetLoginUrlAsync(string connectionId) => null;

        public override string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            // /product/{productId}
            if (segments.Length >= 2 && segments[0].Equals("product", StringComparison.OrdinalIgnoreCase))
                return segments[1];
            return segments.Last();
        }

        public override string CreateProductUrl(string code) => $"{WebHost}/product/{code}";

        // ===== 카테고리 수집 =====

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(
            string connectionId, string url, int page)
        {
            var categoryId = ParseCategoryId(url);
            if (string.IsNullOrWhiteSpace(categoryId))
                throw new CollectException("URL에서 카테고리 ID를 추출할 수 없습니다: " + url);

            // 커서 관리
            if (page <= 1)
            {
                _lastCursor = null;
                _lastCacheKey = categoryId;
            }
            else if (_lastCacheKey != categoryId)
            {
                _lastCursor = null;
                _lastCacheKey = categoryId;
            }

            // 이전 페이지에서 마지막이었으면 빈 목록
            if (page > 1 && _lastCursor == null)
                return new List<CollectPageInfo>();

            CollectExtension.CollectSleep();

            var body = new Dictionary<string, object>
            {
                ["size"] = PageSize,
                ["orderBy"] = "RECOMMENDATION",
                ["categoryIds"] = new[] { categoryId },
                ["brandIds"] = Array.Empty<string>(),
                ["recentlyClickedProducts"] = Array.Empty<object>(),
                ["clickedOnboardingBrandNames"] = Array.Empty<string>(),
                ["clickedOnboardingProductIds"] = Array.Empty<string>(),
                ["compoundedTagIdsForProductTag"] = Array.Empty<string>(),
                ["listId"] = "category",
                ["personalizedExperimentGroupIds"] = new[] { "default" },
                ["productIdsInLandingUrl"] = Array.Empty<string>(),
                ["context"] = new { },
                ["adTagRealTime"] = new { }
            };
            if (!string.IsNullOrEmpty(_lastCursor))
                body["cursor"] = _lastCursor;

            var responseStr = await ApiPostAsync("/general-products", body);
            if (string.IsNullOrWhiteSpace(responseStr))
                return new List<CollectPageInfo>();

            var json = JObject.Parse(responseStr);
            var list = json["list"] as JArray;
            var nextCursor = json["nextCursor"]?.ToString();
            var isLast = json["last"]?.Value<bool>() ?? false;

            // 커서 업데이트: last=true이거나 nextCursor 없으면 마지막 페이지
            _lastCursor = (!isLast && !string.IsNullOrEmpty(nextCursor))
                ? nextCursor : null;

            var result = new List<CollectPageInfo>();
            if (list == null) return result;

            foreach (var item in list)
            {
                var productId = item["productId"]?.ToString();
                var name = item["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(productId)) continue;

                result.Add(new CollectPageInfo
                {
                    Code = productId,
                    Url = CreateProductUrl(productId),
                    Title = name ?? ""
                });
            }

            return result;
        }

        // ===== 상품 상세 =====

        public override async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            // GetHtmlAsync는 사용하지 않음 — GetData에서 직접 API 호출
            return url;
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            var productId = GetCode(url);
            if (string.IsNullOrWhiteSpace(productId))
                throw new CollectException("URL에서 상품 ID를 추출할 수 없습니다: " + url);

            CollectExtension.CollectSleep();

            // 3개 API 병렬 호출
            var personalTask = ApiGetAsync($"/products/{productId}/personal");
            var itemsTask = ApiGetAsync($"/products/{productId}/items");
            var availTask = ApiGetAsync($"/products/{productId}/item-availabilities");

            await Task.WhenAll(personalTask, itemsTask, availTask);

            var personalStr = await personalTask;
            var itemsStr = await itemsTask;
            var availStr = await availTask;

            if (string.IsNullOrWhiteSpace(personalStr))
                throw new CollectException("퀸잇 상품 API 응답이 비었습니다.");

            var data = new JObject();
            data["personal"] = JToken.Parse(personalStr);

            if (!string.IsNullOrWhiteSpace(itemsStr))
                data["items"] = JToken.Parse(itemsStr);

            if (!string.IsNullOrWhiteSpace(availStr))
                data["availabilities"] = JToken.Parse(availStr);

            // 상세설명 HTML 가져오기
            var descUri = data["personal"]?["contents"]?["descriptionFileUri"]?.ToString();
            if (!string.IsNullOrWhiteSpace(descUri))
            {
                try
                {
                    var descHtml = await _httpClient.GetStringAsync(descUri);
                    data["descriptionHtml"] = descHtml;
                }
                catch { }
            }

            return data;
        }

        public override void ThrowIfCaptcha(string url, string html)
        {
            // API 기반이므로 캡챠 없음
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
            // GetPrice에서 이미 SaleException을 던지므로 여기서는 추가 처리 불필요
        }

        public override Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            return Task.CompletedTask;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            var name = data?["personal"]?["product"]?["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(name))
                throw new CollectException("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(Language.KR, name);
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            var brand = data?["personal"]?["product"]?["brand"]?.ToString();
            if (!string.IsNullOrWhiteSpace(brand))
                return ProductHelper.CreateName(Language.KR, brand);
            return null;
        }

        public override string GetModel(string html, JToken data) => null;
        public override List<Desc> GetDescs(string html, JToken data) => new List<Desc>();

        public override List<Tag> GetTags(string html, JToken data)
        {
            var tags = new List<Tag>();
            var hashtags = data?["personal"]?["product"]?["hashtags"] as JArray;
            if (hashtags != null)
            {
                foreach (var ht in hashtags)
                {
                    var tagName = ht?.ToString();
                    if (!string.IsNullOrWhiteSpace(tagName))
                    {
                        tags.Add(new Tag
                        {
                            key = "kw",
                            values = new List<string> { tagName }
                        });
                    }
                }
            }
            return tags;
        }

        public override List<Price> GetPrice(string html, JToken data)
        {
            var product = data?["personal"]?["product"];

            // salesStatus 체크
            var salesStatus = product?["salesStatus"]?.ToString();
            if (!string.IsNullOrEmpty(salesStatus) && salesStatus != "ACTIVE")
                throw new SaleException(ProductSaleStatus.Stopped, $"판매 중이 아닌 상품입니다. (status: {salesStatus})");

            var finalPrice = product?["finalPrice"]?.Value<double>() ?? 0;
            var originalPrice = product?["originalPrice"]?.Value<double>() ?? 0;

            if (finalPrice <= 0)
                finalPrice = originalPrice;

            if (finalPrice <= 0)
                throw new CollectException("가격 정보를 찾을 수 없습니다.");

            return ProductHelper.CreatePrice(Currency, finalPrice, finalPrice, true);
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();
            var imageUrls = data?["personal"]?["contents"]?["imageUrls"] as JArray;
            if (imageUrls == null) return images;

            foreach (var img in imageUrls)
            {
                var url = img?.ToString();
                if (!string.IsNullOrWhiteSpace(url))
                    images.Add(url);
            }
            return images;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var descHtml = data?["descriptionHtml"]?.ToString();
            if (string.IsNullOrWhiteSpace(descHtml))
                return "";

            // HTML에서 img 태그의 src 추출하여 이미지 목록으로 구성
            var sb = new StringBuilder();
            var matches = System.Text.RegularExpressions.Regex.Matches(descHtml,
                @"<img[^>]+src=[""']([^""'>]+)[""']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                var imgUrl = m.Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(imgUrl))
                    sb.Append($"<img src=\"{imgUrl}\" style=\"max-width:100%;\" />");
            }

            return sb.ToString();
        }

        public override Option GetOption(string html, JToken data)
        {
            var prices = GetPrice(html, data);
            var itemsData = data?["items"];
            var availData = data?["availabilities"];

            var options = itemsData?["options"] as JArray;
            var productItems = itemsData?["productItems"] as JArray;

            // 재고 매핑 (productItemId → availability)
            var availMap = new Dictionary<string, JToken>();
            if (availData is JArray availArr)
            {
                foreach (var av in availArr)
                {
                    var pid = av["productItemId"]?.ToString();
                    if (!string.IsNullOrEmpty(pid))
                        availMap[pid] = av;
                }
            }

            // 옵션 없음
            if (options == null || options.Count == 0 || productItems == null || productItems.Count == 0)
                return GetDefaultOption(prices, data);

            // 옵션 요소 매핑 (id → title) — optionElements는 items 최상위 레벨
            var elementMap = new Dictionary<string, string>();
            var optionElements = itemsData?["optionElements"] as JArray;
            if (optionElements != null)
            {
                foreach (var el in optionElements)
                {
                    var elId = el["id"]?.ToString();
                    var elName = el["title"]?.ToString();
                    if (!string.IsNullOrEmpty(elId) && !string.IsNullOrEmpty(elName))
                        elementMap[elId] = elName;
                }
            }

            if (options.Count == 1)
                return GetSingleOption(options, productItems, availMap, elementMap, prices);

            return GetMultiOption(options, productItems, availMap, elementMap, prices);
        }

        public override int? GetTotalQuantity(string html, JToken data) => null;

        // ===== 옵션 헬퍼 =====

        /// <summary>1단 옵션</summary>
        private Option GetSingleOption(JArray options, JArray productItems,
            Dictionary<string, JToken> availMap, Dictionary<string, string> elementMap, List<Price> defaultPrices)
        {
            var optionName = options[0]["title"]?.ToString() ?? "옵션";
            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();

            foreach (var item in productItems)
            {
                var itemId = item["id"]?.ToString() ?? "";
                var title = item["title"]?.ToString() ?? "";

                // optionElementIds로 이름 매핑
                var elIds = item["optionElementIds"] as JArray;
                if (string.IsNullOrWhiteSpace(title) && elIds != null && elIds.Count > 0)
                {
                    var elId = elIds[0]?.ToString();
                    if (!string.IsNullOrEmpty(elId) && elementMap.TryGetValue(elId, out var mappedName))
                        title = mappedName;
                }

                if (string.IsNullOrWhiteSpace(title)) title = itemId;

                // 재고 확인
                bool isSoldOut = true;
                int quantity = 0;
                if (availMap.TryGetValue(itemId, out var avail))
                {
                    isSoldOut = !(avail["available"]?.Value<bool>() ?? false);
                    quantity = avail["quantity"]?.Value<int>() ?? 0;
                    if (!isSoldOut && quantity <= 0) quantity = 9;
                }

                // 개별 가격
                var itemPrice = item["price"]?.Value<double>() ?? 0;
                var optPrices = itemPrice > 0
                    ? ProductHelper.CreatePrice(Currency, itemPrice, itemPrice, true)
                    : defaultPrices;

                groups.Add(new Option.OptionGroup.Group
                {
                    groupId = title,
                    optionName = ProductHelper.CreateName(Language.KR, title)
                });

                details.Add(new Option.DetailOption
                {
                    optionIds = title,
                    isSoldOut = isSoldOut,
                    quantity = isSoldOut ? 0 : quantity,
                    optionGroupName = ProductHelper.CreateName(Language.KR, optionName),
                    optionName = ProductHelper.CreateName(Language.KR, title),
                    price = optPrices
                });
            }

            if (details.Count == 0)
                return GetDefaultOption(defaultPrices);

            return new Option
            {
                optionGroups = new List<Option.OptionGroup>
                {
                    new Option.OptionGroup
                    {
                        optionId = "1",
                        groupName = ProductHelper.CreateName(Language.KR, optionName),
                        groups = groups
                    }
                },
                detailOptions = details
            };
        }

        /// <summary>2단 옵션 (COLOR × SIZE 등)</summary>
        private Option GetMultiOption(JArray options, JArray productItems,
            Dictionary<string, JToken> availMap, Dictionary<string, string> elementMap, List<Price> defaultPrices)
        {
            // 옵션 그룹 이름
            var opt1Name = options[0]["title"]?.ToString() ?? "옵션1";
            var opt2Name = options[1]["title"]?.ToString() ?? "옵션2";

            // 고유 값 수집
            var group1Values = new List<string>();
            var group2Values = new List<string>();
            var details = new List<Option.DetailOption>();

            foreach (var item in productItems)
            {
                var itemId = item["id"]?.ToString() ?? "";
                var title = item["title"]?.ToString() ?? "";
                var elIds = item["optionElementIds"] as JArray;

                // title을 "/" 기준으로 split하여 각 레벨 값 추출
                string val1 = "", val2 = "";
                if (!string.IsNullOrWhiteSpace(title) && title.Contains("/"))
                {
                    var parts = title.Split(new[] { '/' }, 2);
                    val1 = parts[0].Trim();
                    val2 = parts.Length > 1 ? parts[1].Trim() : "";
                }
                else if (elIds != null && elIds.Count >= 2)
                {
                    // optionElementIds로 매핑
                    var e1 = elIds[0]?.ToString();
                    var e2 = elIds[1]?.ToString();
                    if (!string.IsNullOrEmpty(e1) && elementMap.TryGetValue(e1, out var n1)) val1 = n1;
                    if (!string.IsNullOrEmpty(e2) && elementMap.TryGetValue(e2, out var n2)) val2 = n2;
                }

                if (string.IsNullOrWhiteSpace(val1) && string.IsNullOrWhiteSpace(val2))
                    continue;

                if (!string.IsNullOrWhiteSpace(val1) && !group1Values.Contains(val1))
                    group1Values.Add(val1);
                if (!string.IsNullOrWhiteSpace(val2) && !group2Values.Contains(val2))
                    group2Values.Add(val2);

                // 재고 확인
                bool isSoldOut = true;
                int quantity = 0;
                if (availMap.TryGetValue(itemId, out var avail))
                {
                    isSoldOut = !(avail["available"]?.Value<bool>() ?? false);
                    quantity = avail["quantity"]?.Value<int>() ?? 0;
                    if (!isSoldOut && quantity <= 0) quantity = 9;
                }

                var combinedName = $"{val1}ㅡ{val2}";

                var itemPrice = item["price"]?.Value<double>() ?? 0;
                var optPrices = itemPrice > 0
                    ? ProductHelper.CreatePrice(Currency, itemPrice, itemPrice, true)
                    : defaultPrices;

                details.Add(new Option.DetailOption
                {
                    optionIds = combinedName,
                    isSoldOut = isSoldOut,
                    quantity = isSoldOut ? 0 : quantity,
                    optionGroupName = ProductHelper.CreateName(Language.KR, $"{opt1Name}ㅡ{opt2Name}"),
                    optionName = ProductHelper.CreateName(Language.KR, combinedName),
                    price = optPrices
                });
            }

            if (details.Count == 0)
                return GetDefaultOption(defaultPrices);

            var optionGroups = new List<Option.OptionGroup>
            {
                new Option.OptionGroup
                {
                    optionId = "1",
                    groupName = ProductHelper.CreateName(Language.KR, opt1Name),
                    groups = group1Values.Select(v => new Option.OptionGroup.Group
                    {
                        groupId = v,
                        optionName = ProductHelper.CreateName(Language.KR, v)
                    }).ToList()
                },
                new Option.OptionGroup
                {
                    optionId = "2",
                    groupName = ProductHelper.CreateName(Language.KR, opt2Name),
                    groups = group2Values.Select(v => new Option.OptionGroup.Group
                    {
                        groupId = v,
                        optionName = ProductHelper.CreateName(Language.KR, v)
                    }).ToList()
                }
            };

            // optionGroupName을 ProductHelper로 생성
            string ogName = ProductHelper.GetOptionGroupName(optionGroups);
            foreach (var d in details)
                d.optionGroupName = ProductHelper.CreateName(Language.KR, ogName);

            return new Option
            {
                optionGroups = optionGroups,
                detailOptions = details
            };
        }

        private static Option GetDefaultOption(List<Price> prices, JToken data = null)
        {
            bool isOutOfStock = false;
            if (data != null)
            {
                var salesStatus = data["personal"]?["product"]?["salesStatus"]?.ToString();
                isOutOfStock = !string.IsNullOrEmpty(salesStatus) && salesStatus != "ACTIVE";
            }

            return new Option
            {
                optionGroups = new List<Option.OptionGroup>
                {
                    new Option.OptionGroup
                    {
                        optionId = "0",
                        groupName = ProductHelper.CreateName(Language.KR, "옵션"),
                        groups = new List<Option.OptionGroup.Group>
                        {
                            new Option.OptionGroup.Group
                            {
                                groupId = "0",
                                optionName = ProductHelper.CreateName(Language.KR, "원사이즈")
                            }
                        }
                    }
                },
                detailOptions = new List<Option.DetailOption>
                {
                    new Option.DetailOption
                    {
                        optionIds = "0",
                        isSoldOut = isOutOfStock,
                        quantity = isOutOfStock ? 0 : 9,
                        optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                        optionName = ProductHelper.CreateName(Language.KR, "원사이즈"),
                        price = prices
                    }
                }
            };
        }
    }
}
