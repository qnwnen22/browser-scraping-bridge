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
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Nike
{
    /// <summary>
    /// Nike Korea (nike.com/kr) - __NEXT_DATA__ + Nike API 기반 수집
    /// URL 패턴:
    ///   카테고리: https://www.nike.com/kr/w/{slug}
    ///   상품상세: https://www.nike.com/kr/t/{slug}/{styleColor}
    /// </summary>
    public class Nike : CollectBase
    {
        private const string BaseUrl = "https://www.nike.com";
        private const string ApiBase = "https://api.nike.com";
        private const int PageSize = 24;

        public override string Site => nameof(Nike);
        public override string Name => "나이키";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; }

        private static readonly HttpClient _htmlClient = CreateHtmlClient();
        private static readonly HttpClient _apiClient = CreateApiClient();

        // 카테고리 페이징: page 1에서 추출한 API next URL 템플릿
        private static string _nextApiPath;
        private static string _lastCategoryUrl;
        private static int _totalPages;

        private static HttpClient CreateHtmlClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            var client = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7");
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("sec-ch-ua", "\"Not:A-Brand\";v=\"99\", \"Google Chrome\";v=\"145\", \"Chromium\";v=\"145\"");
            client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            client.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
            client.DefaultRequestHeaders.Add("sec-fetch-dest", "document");
            client.DefaultRequestHeaders.Add("sec-fetch-mode", "navigate");
            client.DefaultRequestHeaders.Add("sec-fetch-site", "none");
            client.DefaultRequestHeaders.Add("sec-fetch-user", "?1");
            client.DefaultRequestHeaders.Add("upgrade-insecure-requests", "1");
            return client;
        }

        private static HttpClient CreateApiClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            var client = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Accept-Language", "ko-KR,ko;q=0.9");
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("nike-api-caller-id", "com.nike:commerce.nikedotcom.web");
            client.DefaultRequestHeaders.Add("Origin", "https://www.nike.com");
            client.DefaultRequestHeaders.Add("Referer", "https://www.nike.com/");
            return client;
        }

        // ===== __NEXT_DATA__ 파싱 =====

        private static JToken ExtractNextData(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;

            var match = Regex.Match(html, @"<script\s+id=""__NEXT_DATA__""\s+type=""application/json"">\s*(\{.*?})\s*</script>", RegexOptions.Singleline);
            if (!match.Success) return null;

            return JToken.Parse(match.Groups[1].Value);
        }

        // ===== HTML 요청 (HttpClient 직접 사용 — ChatHubService 우회) =====

        public override async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            CollectExtension.CollectSleep();
            var response = await _htmlClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // ===== ICollect 구현 =====

        public override async Task<string> GetLoginUrlAsync(string connectionId) => null;

        public override string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var uri = new Uri(url);
            // /kr/t/{slug}/{styleColor} → styleColor
            var segments = uri.AbsolutePath.TrimEnd('/').Split('/');
            return segments.Length >= 2 ? segments.Last() : null;
        }

        public override string CreateProductUrl(string code)
        {
            // styleColor만으로는 Nike URL을 구성할 수 없으므로 null 반환
            // BuildProduct에서 원본 URL이 사용됨
            return null;
        }

        // ===== 카테고리 수집 =====

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(
            string connectionId, string url, int page)
        {
            if (page <= 1)
            {
                _lastCategoryUrl = url;
                _nextApiPath = null;

                // Page 1: HTML에서 __NEXT_DATA__ 파싱 (HttpClient 직접 사용)
                CollectExtension.CollectSleep();
                var html = await GetHtmlAsync(connectionId, url);
                var nextData = ExtractNextData(html);
                if (nextData == null)
                    throw new CollectException("Nike 페이지에서 __NEXT_DATA__를 찾을 수 없습니다.");

                var wall = nextData?["props"]?["pageProps"]?["initialState"]?["Wall"];
                if (wall == null)
                    throw new CollectException("Nike 카테고리 데이터(Wall)를 찾을 수 없습니다.");

                // 페이지 정보 저장
                _nextApiPath = wall["pageData"]?["next"]?.ToString();
                _totalPages = wall["pageData"]?["totalPages"]?.Value<int>() ?? 1;

                return ParseProductGroupings(wall["productGroupings"]);
            }
            else
            {
                // Page 2+: Nike API 호출
                if (string.IsNullOrWhiteSpace(_nextApiPath) || _lastCategoryUrl != url)
                    return new List<CollectPageInfo>();

                if (page > _totalPages)
                    return new List<CollectPageInfo>();

                int anchor = (page - 1) * PageSize;
                var apiUrl = BuildApiUrl(anchor);

                CollectExtension.CollectSleep();
                var response = await _apiClient.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                    return new List<CollectPageInfo>();

                var body = await response.Content.ReadAsStringAsync();
                var json = JToken.Parse(body);

                // API 응답 형식: { objects: [...], pages: {...} }
                // objects 내부에 productGroupings와 동일한 구조의 상품 배열
                var objects = json?["objects"] as JArray;
                if (objects != null && objects.Count > 0)
                    return ParseProductGroupings(objects);

                // 또는 productGroupings 형태
                var groupings = json?["productGroupings"];
                if (groupings != null && groupings.HasValues)
                    return ParseProductGroupings(groupings);

                return new List<CollectPageInfo>();
            }
        }

        private string BuildApiUrl(int anchor)
        {
            // _nextApiPath에서 anchor 값만 교체
            var path = Regex.Replace(_nextApiPath, @"anchor=\d+", $"anchor={anchor}");
            return ApiBase + path;
        }

        private static List<CollectPageInfo> ParseProductGroupings(JToken groupings)
        {
            var list = new List<CollectPageInfo>();
            if (groupings == null) return list;

            foreach (var item in groupings)
            {
                // __NEXT_DATA__ 형식: { products: [...], cardType: "..." }
                var products = item["products"];
                if (products != null)
                {
                    foreach (var p in products)
                        AddProduct(list, p);
                }
                else
                {
                    // API 형식: objects 배열의 각 항목이 직접 상품일 수 있음
                    AddProduct(list, item);
                }
            }
            return list;
        }

        private static void AddProduct(List<CollectPageInfo> list, JToken p)
        {
            var code = p["productCode"]?.ToString();
            var pdpUrl = p["pdpUrl"]?["url"]?.ToString();
            var title = p["copy"]?["title"]?.ToString();

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(pdpUrl)) return;

            list.Add(new CollectPageInfo
            {
                Code = code,
                Url = pdpUrl,
                Title = title ?? ""
            });
        }

        // ===== 상품 상세 =====

        public override Task<JToken> GetData(string connectionId, string url, string html)
        {
            var nextData = ExtractNextData(html);
            if (nextData == null)
                throw new CollectException("Nike 상품 페이지에서 __NEXT_DATA__를 찾을 수 없습니다.");

            var pageProps = nextData["props"]?["pageProps"];

            // URL에서 styleColor 추출
            var styleColor = GetCode(url);

            // query에서도 가져올 수 있음
            if (string.IsNullOrWhiteSpace(styleColor))
                styleColor = nextData["query"]?["styleColor"]?.ToString();

            // productGroups[0].products 에서 해당 styleColor 찾기
            var productGroups = pageProps?["productGroups"];
            if (productGroups == null || !productGroups.HasValues)
                throw new CollectException("Nike 상품 그룹 데이터를 찾을 수 없습니다.");

            JToken product = null;
            foreach (var group in productGroups)
            {
                var products = group["products"];
                if (products == null) continue;

                product = products[styleColor];
                if (product != null) break;
            }

            if (product == null)
                throw new CollectException($"Nike 상품 데이터를 찾을 수 없습니다: {styleColor}");

            return Task.FromResult(product);
        }

        public override void ThrowIfCaptcha(string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new CollectException("Nike 페이지 응답이 비었습니다.");

            if (html.Contains("Access Denied") || html.Contains("Request unsuccessful"))
                throw new CaptchaException("Nike 봇 차단 감지 (Akamai)");
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
            // GetOption에서 이미 SaleException을 던지므로 여기서는 추가 처리 불필요
        }

        public override Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            return Task.CompletedTask;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            var title = data?["productInfo"]?["fullTitle"]?.ToString();
            if (string.IsNullOrWhiteSpace(title))
                title = data?["productInfo"]?["title"]?.ToString();
            return ProductHelper.CreateName(Language.KR, title ?? "");
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            return ProductHelper.CreateName(Language.KR, "나이키");
        }

        public override string GetModel(string html, JToken data)
        {
            return data?["styleColor"]?.ToString();
        }

        public override List<Desc> GetDescs(string html, JToken data) => new List<Desc>();

        public override List<Tag> GetTags(string html, JToken data)
        {
            var tags = new List<Tag>();

            var taxonomyLabels = data?["taxonomyLabels"];
            if (taxonomyLabels == null) return tags;

            foreach (var prop in taxonomyLabels.Children<JProperty>())
            {
                var values = prop.Value?.Select(v => v.ToString()).Where(v => !string.IsNullOrEmpty(v)).ToList();
                if (values != null && values.Count > 0)
                {
                    tags.Add(new Tag { key = "kw", values = values });
                }
            }

            return tags;
        }

        public override List<Price> GetPrice(string html, JToken data)
        {
            var currentPrice = data?["prices"]?["currentPrice"]?.Value<double>() ?? 0;
            var initialPrice = data?["prices"]?["initialPrice"]?.Value<double>() ?? 0;

            var finalPrice = currentPrice > 0 ? currentPrice : initialPrice;
            if (finalPrice <= 0)
                throw new CollectException("가격 정보를 찾을 수 없습니다.");

            var prices = ProductHelper.CreatePrice(Currency, finalPrice, finalPrice, true);
            foreach (var p in prices) p.isoCode = "KRW";
            return prices;
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();
            var contentImages = data?["contentImages"];
            if (contentImages == null) return images;

            foreach (var img in contentImages)
            {
                if (img["cardType"]?.ToString() != "image") continue;
                var url = img["properties"]?["squarish"]?["url"]?.ToString();
                if (!string.IsNullOrWhiteSpace(url))
                    images.Add(url);
            }
            return images;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            // HTML에서 data-testid="epdp" 영역 추출 (리치 콘텐츠: 이미지+텍스트)
            var epdpHtml = ExtractEpdpSection(html);
            if (!string.IsNullOrWhiteSpace(epdpHtml))
                return epdpHtml;

            // epdp 못 찾으면 __NEXT_DATA__ JSON에서 조합
            var sb = new StringBuilder();
            var productInfo = data?["productInfo"];
            if (productInfo == null) return "";

            var desc = productInfo["productDescription"]?.ToString();
            if (!string.IsNullOrWhiteSpace(desc))
                sb.Append($"<p>{desc}</p>");

            var features = productInfo["featuresAndBenefits"];
            if (features != null)
            {
                foreach (var section in features)
                {
                    var header = section["header"]?.ToString();
                    var body = section["body"] as JArray;
                    if (body != null && body.Count > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(header))
                            sb.Append($"<h3>{header}</h3>");
                        sb.Append("<ul>");
                        foreach (var item in body)
                            sb.Append($"<li>{item}</li>");
                        sb.Append("</ul>");
                    }
                }
            }

            var details = productInfo["productDetails"];
            if (details != null)
            {
                foreach (var section in details)
                {
                    var header = section["header"]?.ToString();
                    var body = section["body"] as JArray;
                    if (body != null && body.Count > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(header))
                            sb.Append($"<h3>{header}</h3>");
                        sb.Append("<ul>");
                        foreach (var item in body)
                            sb.Append($"<li>{item}</li>");
                        sb.Append("</ul>");
                    }
                }
            }

            return sb.ToString();
        }

        public override Option GetOption(string html, JToken data)
        {
            var prices = GetPrice(html, data);
            var sizes = data?["sizes"] as JArray;
            var statusModifier = data?["statusModifier"]?.ToString();

            // 구매 불가 상품
            if (statusModifier != null && !statusModifier.StartsWith("BUYABLE"))
                throw new SaleException(ProductSaleStatus.Stopped, $"판매 중지 상품 (status: {statusModifier})");

            // 사이즈 없음 또는 ONE SIZE → 기본 옵션
            if (sizes == null || sizes.Count == 0)
                return GetDefaultOption(prices, false);

            if (sizes.Count == 1 && sizes[0]["label"]?.ToString() == "ONE SIZE")
            {
                var sizeStatus = sizes[0]["status"]?.ToString();
                var isSoldOut = sizeStatus != "ACTIVE";
                return GetDefaultOption(prices, isSoldOut);
            }

            // 멀티 사이즈 → 단일 옵션 그룹
            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();

            foreach (var size in sizes)
            {
                var label = size["localizedLabel"]?.ToString() ?? size["label"]?.ToString() ?? "";
                var status = size["status"]?.ToString();
                var isSoldOut = status != "ACTIVE";

                groups.Add(new Option.OptionGroup.Group
                {
                    groupId = label,
                    optionName = ProductHelper.CreateName(Language.KR, label)
                });

                details.Add(new Option.DetailOption
                {
                    optionIds = label,
                    isSoldOut = isSoldOut,
                    quantity = isSoldOut ? 0 : 9,
                    optionGroupName = ProductHelper.CreateName(Language.KR, "사이즈"),
                    optionName = ProductHelper.CreateName(Language.KR, label),
                    price = prices
                });
            }

            if (details.Count == 0)
                return GetDefaultOption(prices, false);

            return new Option
            {
                optionGroups = new List<Option.OptionGroup>
                {
                    new Option.OptionGroup
                    {
                        optionId = "1",
                        groupName = ProductHelper.CreateName(Language.KR, "사이즈"),
                        groups = groups
                    }
                },
                detailOptions = details
            };
        }

        public override int? GetTotalQuantity(string html, JToken data) => null;

        // ===== Helpers =====

        /// <summary>
        /// HTML에서 data-testid="epdp" 영역(리치 설명: 이미지+텍스트)을 추출.
        /// 중첩 div를 정확히 처리하기 위해 태그 카운팅 사용.
        /// </summary>
        private static string ExtractEpdpSection(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;

            var startIdx = html.IndexOf("data-testid=\"epdp\"", StringComparison.Ordinal);
            if (startIdx < 0) return null;

            // 해당 div의 시작 < 위치 찾기
            var divStart = html.LastIndexOf('<', startIdx);
            if (divStart < 0) return null;

            // div 중첩 카운팅으로 매칭되는 닫는 태그 찾기
            int depth = 0;
            int i = divStart;
            while (i < html.Length)
            {
                if (html[i] == '<')
                {
                    if (i + 1 < html.Length && html[i + 1] == '/')
                    {
                        // 닫는 태그
                        var closeTag = html.Substring(i, Math.Min(10, html.Length - i)).ToLower();
                        if (closeTag.StartsWith("</div"))
                        {
                            depth--;
                            if (depth == 0)
                            {
                                var endIdx = html.IndexOf('>', i) + 1;
                                return html.Substring(divStart, endIdx - divStart);
                            }
                        }
                    }
                    else
                    {
                        // 여는 태그 (self-closing 제외)
                        var tagEnd = html.IndexOf('>', i);
                        if (tagEnd > 0)
                        {
                            var tag = html.Substring(i, tagEnd - i + 1).ToLower();
                            if (tag.StartsWith("<div") && !tag.EndsWith("/>"))
                                depth++;
                        }
                    }
                }
                i++;
            }

            return null;
        }

        private static Option GetDefaultOption(List<Price> prices, bool isOutOfStock)
        {
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
