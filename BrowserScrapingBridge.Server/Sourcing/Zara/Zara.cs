#nullable disable warnings
// Zara.cs - Robust Zara KR collecto
// - v1(colorProductId)으로 products-details 호출 -> 색상별 정확한 재고
// - sizes[].availability 직접 사용 (itxrest API 불필요)
// - 상품명에 색상명 suffix
// - 리스트: 색상별 별도 CollectPageInfo 생성
// - Extension proxy -> HttpClient fallback

using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Common.Exceptions;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Price = BrowserScrapingBridge.Server.Sourcing.Models.Product.Price;

namespace BrowserScrapingBridge.Server.Sourcing.Zara
{
    public class Zara : ICollect
    {
        private const string Host = "https://www.zara.com";
        private const string DefaultCategoryUrl = "https://www.zara.com/kr/ko/";

        public string Site => nameof(Zara);
        public string Name => "Zara";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;

        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        public Task<string> GetLoginUrlAsync(string connectionId) => Task.FromResult<string>(null);

        // =========================
        // Public: Code
        // =========================
        public string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            var pid = GetQuery(url, "__pid");
            if (!string.IsNullOrWhiteSpace(pid)) return pid;

            try
            {
                var uri = new Uri(url);
                var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var v1 = q?.Get("v1");
                if (!string.IsNullOrWhiteSpace(v1)) return v1.Trim();

                var m = Regex.Match(uri.AbsolutePath, @"-p(\d+)\.html$", RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value;

                return null;
            }
            catch { return null; }
        }

        // =========================
        // Public: List
        // =========================
        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw BuildCaptcha(DefaultCategoryUrl, "Zara 카테고리 URL이 비었습니다.");

            var categoryUrl = StripInternalParams(url);
            if (page < 1) page = 1;

            var listUrl = NormalizeListUrl(categoryUrl, page);
            var body = await RequestSmartAsync(connectionId, listUrl, referer: categoryUrl);

            if (string.IsNullOrWhiteSpace(body))
                throw BuildCaptcha(categoryUrl, "Zara 리스트(JSON) 응답이 없습니다. 브라우저에서 해당 URL을 직접 열고(로딩 완료) 다시 수집해주세요.");

            if (LooksLikeChallenge(body))
                throw BuildCaptcha(categoryUrl, "Zara 챌린지/차단 화면이 감지되었습니다. 브라우저에서 카테고리 페이지를 열어 확인(해제) 후 다시 수집해주세요.");

            JObject root;
            try { root = JObject.Parse(body); }
            catch
            {
                throw BuildCaptcha(categoryUrl, "Zara 리스트 응답이 JSON이 아닙니다. 브라우저에서 카테고리 페이지를 열어 확인(해제) 후 다시 수집해주세요.");
            }

            var pageItems = ExtractListPageItems(root, page);
            var result = new List<CollectPageInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var it in pageItems)
            {
                if (string.IsNullOrWhiteSpace(it.ProductId)) continue;

                if (it.Colors.Count > 0)
                {
                    // ✅ 색상별로 별도 URL 생성
                    foreach (var colorEntry in it.Colors)
                    {
                        var seenKey = $"{it.ProductId}_{colorEntry.ColorProductId}";
                        if (!seen.Add(seenKey)) continue;

                        string detailUrl;
                        if (!string.IsNullOrWhiteSpace(it.SeoKeyword) && !string.IsNullOrWhiteSpace(it.SeoProductId))
                            detailUrl = $"{Host}/kr/ko/{it.SeoKeyword}-p{it.SeoProductId}.html";
                        else
                            detailUrl = $"{Host}/kr/ko/product-p{it.ProductId}.html";

                        if (!string.IsNullOrWhiteSpace(colorEntry.ColorProductId))
                            detailUrl = AppendQuery(detailUrl, "v1", colorEntry.ColorProductId);

                        detailUrl = AppendQuery(detailUrl, "__pid", it.ProductId);
                        detailUrl = AppendQuery(detailUrl, "__cat", Uri.EscapeDataString(categoryUrl));

                        // 상품명: "상품명 - 색상명"
                        var title = string.IsNullOrWhiteSpace(colorEntry.ColorName)
                            ? it.Name
                            : $"{it.Name} - {colorEntry.ColorName}";

                        result.Add(new CollectPageInfo
                        {
                            Code = it.ProductId,
                            Url = detailUrl,
                            Title = title ?? ""
                        });
                    }
                }
                else
                {
                    // colors 없으면 기존 방식 fallback
                    if (!seen.Add(it.ProductId)) continue;

                    string detailUrl;
                    if (!string.IsNullOrWhiteSpace(it.SeoKeyword) && !string.IsNullOrWhiteSpace(it.SeoProductId))
                        detailUrl = $"{Host}/kr/ko/{it.SeoKeyword}-p{it.SeoProductId}.html";
                    else
                        detailUrl = $"{Host}/kr/ko/product-p{it.ProductId}.html";

                    detailUrl = AppendQuery(detailUrl, "__pid", it.ProductId);
                    detailUrl = AppendQuery(detailUrl, "__cat", Uri.EscapeDataString(categoryUrl));

                    result.Add(new CollectPageInfo
                    {
                        Code = it.ProductId,
                        Url = detailUrl,
                        Title = it.Name ?? ""
                    });
                }
            }

            return result;
        }

        // =========================
        // Public: Detail
        // =========================
        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var product = CreateSafeSkeleton(url);

            var categoryUrl = GetQuery(url, "__cat");
            if (!string.IsNullOrWhiteSpace(categoryUrl)) categoryUrl = Uri.UnescapeDataString(categoryUrl);
            if (string.IsNullOrWhiteSpace(categoryUrl)) categoryUrl = DefaultCategoryUrl;

            var productId = GetQuery(url, "__pid");
            if (string.IsNullOrWhiteSpace(productId))
                productId = GetCode(url);

            // ✅ v1 = color productId -> products-details 호출에 사용
            var v1 = GetQuery(url, "v1");

            product.code = productId ?? "";
            product.site = Site;

            if (string.IsNullOrWhiteSpace(productId) && string.IsNullOrWhiteSpace(v1))
            {
                product.url = StripInternalParams(url);
                product.memo = "Zara 상품 ID를 추출할 수 없습니다.";
                product.collectStatus = ProductCollectStatus.Failed;
                return product;
            }

            try
            {
                // ✅ v1(colorProductId)으로 products-details 호출 -> 해당 색상만 정확히 응답
                var queryId = !string.IsNullOrWhiteSpace(v1) ? v1 : productId;
                var jsonUrl = BuildProductsDetailsUrl(categoryUrl, queryId);
                var body = await RequestSmartAsync(connectionId, jsonUrl, referer: categoryUrl);

                if (string.IsNullOrWhiteSpace(body))
                    throw BuildCaptcha(categoryUrl, "Zara 상품상세(JSON) 응답이 없습니다. 브라우저에서 카테고리/상품 페이지를 열고(로딩 완료) 다시 수집해주세요.");

                if (LooksLikeChallenge(body))
                    throw BuildCaptcha(categoryUrl, "Zara 상품상세 요청 중 챌린지/차단 화면이 감지되었습니다. 브라우저에서 카테고리 페이지를 열어 확인(해제) 후 다시 수집해주세요.");

                // ✅ JToken.Parse: 배열/객체 모두 대응
                JToken rootToken;
                try { rootToken = JToken.Parse(body); }
                catch
                {
                    throw BuildCaptcha(categoryUrl, "Zara 상품상세 응답이 JSON이 아닙니다. 브라우저에서 카테고리 페이지를 열어 확인(해제) 후 다시 수집해주세요.");
                }

                JToken p;
                if (rootToken is JArray arr)
                {
                    p = arr.FirstOrDefault();
                    if (p == null)
                    {
                        product.url = StripInternalParams(url);
                        product.memo = "Zara 상품상세 응답 배열이 비어있습니다.";
                        product.collectStatus = ProductCollectStatus.Failed;
                        return product;
                    }
                }
                else if (rootToken is JObject obj)
                {
                    p = obj.SelectToken("products[0]") ?? obj.SelectToken("product") ?? obj;
                }
                else
                {
                    product.url = StripInternalParams(url);
                    product.memo = "Zara 상품상세 응답 형식을 알 수 없습니다.";
                    product.collectStatus = ProductCollectStatus.Failed;
                    return product;
                }

                // ✅ colors 추출: detail.colors 우선, 없으면 colors 직접
                var allColors = p.SelectTokens("detail.colors[*]").ToList();
                if (allColors.Count == 0)
                    allColors = p.SelectTokens("colors[*]").ToList();

                // ✅ v1(colorProductId)으로 해당 색상 1개만 선택, 없으면 첫 번째
                JToken targetColor = null;
                if (!string.IsNullOrWhiteSpace(v1))
                    targetColor = allColors.FirstOrDefault(c => c["productId"]?.ToString() == v1);
                targetColor ??= allColors.FirstOrDefault();

                var colorName = targetColor?["name"]?.ToString() ?? "";
                var colorProductId = targetColor?["productId"]?.ToString() ?? v1 ?? "";

                // ✅ product.url: v1을 colorProductId로 정확하게 설정
                product.url = !string.IsNullOrWhiteSpace(colorProductId)
                    ? AppendQuery(StripInternalParams(url), "v1", colorProductId)
                    : StripInternalParams(url);

                // ✅ Title: "상품명 - 색상명"
                var baseName = p.SelectToken("name")?.ToString() ?? "";
                product.title = ProductHelper.CreateName(Language.KR,
                    string.IsNullOrWhiteSpace(colorName) ? baseName : $"{baseName} - {colorName}");

                // ✅ Price: 해당 색상 price 우선
                var priceVal =
                    targetColor?.SelectToken("price")?.Value<double?>()
                    ?? targetColor?.SelectToken("pricing.price.value")?.Value<double?>()
                    ?? p.SelectToken("price")?.Value<double?>()
                    ?? 0d;
                product.price = ProductHelper.CreatePrice(Currency, priceVal, priceVal, true);

                // ✅ Images: 해당 색상의 이미지만 수집
                var images = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                void AddImages(IEnumerable<JToken> tokens)
                {
                    foreach (var t in tokens)
                    {
                        var s = t?.ToString();
                        if (!string.IsNullOrWhiteSpace(s)) images.Add(s);
                    }
                }

                if (targetColor != null)
                {
                    AddImages(targetColor.SelectTokens("xmedia[*].extraInfo.deliveryUrl"));
                    AddImages(targetColor.SelectTokens("mainImgs[*].extraInfo.deliveryUrl"));
                    if (images.Count == 0)
                    {
                        AddImages(targetColor.SelectTokens("xmedia[*].url"));
                        AddImages(targetColor.SelectTokens("mainImgs[*].url"));
                    }
                }

                // 이미지 없으면 전체 fallback
                if (images.Count == 0)
                {
                    AddImages(p.SelectTokens("detail.colors[*].xmedia[*].extraInfo.deliveryUrl"));
                    AddImages(p.SelectTokens("detail.colors[*].mainImgs[*].extraInfo.deliveryUrl"));
                    AddImages(p.SelectTokens("colors[*].xmedia[*].extraInfo.deliveryUrl"));
                    AddImages(p.SelectTokens("colors[*].mainImgs[*].extraInfo.deliveryUrl"));
                }

                product.mainImages = images.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                // ✅ 옵션: 해당 색상의 sizes[].availability 직접 사용 (itxrest API 불필요)
                var targetColors = targetColor != null
                    ? new List<JToken> { targetColor }
                    : allColors;

                product.option = BuildSizeOptions(targetColors, product.price);

                // ✅ Description: HTML 페이지에서 상품 설명 추출
                try
                {
                    var pageUrl = product.url;
                    var html = await RequestSmartAsync(connectionId, pageUrl, referer: categoryUrl);
                    if (!string.IsNullOrWhiteSpace(html))
                    {
                        var descHtml = ExtractDescription(html);
                        if (!string.IsNullOrWhiteSpace(descHtml))
                            product.detailPageHtml = descHtml;
                    }
                }
                catch { /* description 실패 시 전체 수집에 영향 없음 */ }

                product.collectStatus = ProductCollectStatus.Success;
                return product;
            }
            catch (CaptchaException)
            {
                throw;
            }
            catch (Exception ex)
            {
                product.url = StripInternalParams(url);
                product.memo = ex.Message;
                product.collectStatus = ProductCollectStatus.Failed;
                return product;
            }
        }

        // =========================
        // Option Builder
        // =========================

        /// <summary>
        /// sizes[].availability 직접 사용하여 옵션 구성
        /// in_stock: 재고있음 / coming_soon, out_of_stock 등: 품절
        /// </summary>
        private Option BuildSizeOptions(List<JToken> colors, List<Price> productPrices)
        {
            if (colors == null || colors.Count == 0)
                return CreateSingleDefaultOption(productPrices);

            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();
            int idx = 1;

            foreach (var color in colors)
            {
                var colorSizes = color.SelectTokens("sizes[*]").ToList();

                foreach (var size in colorSizes)
                {
                    var sizeName = size["name"]?.ToString() ?? $"SIZE_{idx}";
                    var sizePrice = size["price"]?.Value<double?>() ?? 0d;

                    // ✅ sizes[].availability 직접 사용
                    var availability = size["availability"]?.ToString() ?? "out_of_stock";
                    var isSoldOut = !availability.Equals("in_stock", StringComparison.OrdinalIgnoreCase);

                    var prices = sizePrice > 0
                        ? ProductHelper.CreatePrice(Currency, sizePrice, sizePrice, true)
                        : productPrices;

                    var id = idx.ToString();

                    groups.Add(new Option.OptionGroup.Group
                    {
                        groupId = id,
                        optionName = ProductHelper.CreateName(Language.KR, sizeName),
                        optionImage = null,
                        price = prices
                    });

                    details.Add(new Option.DetailOption
                    {
                        id = id,
                        isSoldOut = isSoldOut,
                        optionGroupName = ProductHelper.CreateName(Language.KR, "사이즈"),
                        optionName = ProductHelper.CreateName(Language.KR, sizeName),
                        quantity = isSoldOut ? 0 : 9,
                        optionImage = null,
                        price = prices
                    });

                    idx++;
                }
            }

            if (details.Count == 0)
                return CreateSingleDefaultOption(productPrices);

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

        // =========================
        // Request Strategy
        // =========================

        private async Task<string> RequestSmartAsync(string connectionId, string requestUrl, string referer = null)
        {
            var body = await TryGetByExtensionAsync(connectionId, requestUrl, headers: null);
            if (!string.IsNullOrWhiteSpace(body)) return body;

            body = await TryGetByExtensionAsync(connectionId, requestUrl, headers: GetSafeHeaders());
            if (!string.IsNullOrWhiteSpace(body)) return body;

            body = await GetByHttpClientAsync(requestUrl, headers: null, referer: referer);
            if (!string.IsNullOrWhiteSpace(body)) return body;

            body = await GetByHttpClientAsync(requestUrl, headers: GetSafeHeaders(), referer: referer);
            return body;
        }

        private async Task<string> TryGetByExtensionAsync(string connectionId, string url, Dictionary<string, string> headers)
        {
            try
            {
                return headers == null
                    ? await ChatHubService.GetRequest(connectionId, url)
                    : await ChatHubService.GetRequest(connectionId, url, headers);
            }
            catch (System.Net.Sockets.SocketException) { return null; }
            catch (System.IO.IOException) { return null; }
            catch { return null; }
        }

        private async Task<string> GetByHttpClientAsync(string url, Dictionary<string, string> headers = null, string referer = null)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };

            if (Proxy != null)
            {
                handler.UseProxy = true;
                handler.Proxy = Proxy;
            }

            using var httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.TryAddWithoutValidation("Accept", "*/*");
            request.Headers.TryAddWithoutValidation("Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7");
            request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
            request.Headers.TryAddWithoutValidation("Pragma", "no-cache");
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");

            if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refUri))
                request.Headers.Referrer = refUri;

            if (headers != null)
            {
                foreach (var kv in headers)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key) || kv.Value == null) continue;
                    request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }
            }

            using var response = await httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        private static Dictionary<string, string> GetSafeHeaders()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Accept"] = "*/*",
                ["Accept-Language"] = "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7",
                ["Cache-Control"] = "no-cache",
                ["Pragma"] = "no-cache",
                ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36",
            };
        }

        // =========================
        // List Parsing
        // =========================

        private sealed class ColorEntry
        {
            public string ColorProductId { get; set; }
            public string ColorName { get; set; }
        }

        private sealed class ListItem
        {
            public string ProductId { get; set; }
            public string Name { get; set; }
            public string SeoKeyword { get; set; }
            public string SeoProductId { get; set; }
            public List<ColorEntry> Colors { get; set; } = new List<ColorEntry>();
        }

        private static List<ListItem> ExtractListPageItems(JObject root, int page)
        {
            int pageSize = root.SelectToken("paginationInfo.pageSize")?.Value<int?>() ?? 20;

            var all = new List<ListItem>();

            void ParseComponents(IEnumerable<JToken> components)
            {
                foreach (var c in components)
                {
                    var type = c["type"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(type) && !type.Equals("Product", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var id = c["id"]?.ToString();
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    // ✅ 색상별 productId + name 추출
                    var colorEntries = new List<ColorEntry>();
                    foreach (var color in c.SelectTokens("detail.colors[*]"))
                    {
                        var colorPid = color["productId"]?.ToString();
                        var colorName = color["name"]?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(colorPid))
                            colorEntries.Add(new ColorEntry { ColorProductId = colorPid, ColorName = colorName });
                    }

                    all.Add(new ListItem
                    {
                        ProductId = id,
                        Name = c["name"]?.ToString() ?? "",
                        SeoKeyword = c.SelectToken("seo.keyword")?.ToString(),
                        SeoProductId = c.SelectToken("seo.seoProductId")?.ToString(),
                        Colors = colorEntries
                    });
                }
            }

            ParseComponents(root.SelectTokens("productGroups[*].elements[*].commercialComponents[*]"));

            if (all.Count == 0)
                ParseComponents(root.SelectTokens("$..commercialComponents[*]"));

            int skip = Math.Max(0, (page - 1) * pageSize);
            if (skip >= all.Count) return new List<ListItem>();

            int take = Math.Min(pageSize, all.Count - skip);
            return all.Skip(skip).Take(take).ToList();
        }

        // =========================
        // URL Helpers
        // =========================

        private static string NormalizeListUrl(string categoryUrl, int page)
        {
            try
            {
                var ub = new UriBuilder(categoryUrl);
                var q = System.Web.HttpUtility.ParseQueryString(ub.Query ?? "");
                q["page"] = page.ToString();
                q["ajax"] = "true";
                ub.Query = q.ToString();
                return ub.ToString();
            }
            catch
            {
                var sep = categoryUrl.Contains("?") ? "&" : "?";
                return $"{categoryUrl}{sep}page={page}&ajax=true";
            }
        }

        private static string BuildProductsDetailsUrl(string baseUrlForPrefix, string productId)
        {
            var baseUri = new Uri(baseUrlForPrefix);
            var prefix = $"{baseUri.Scheme}://{baseUri.Host}";

            var pathPrefix = "/kr/ko";
            var parts = baseUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                pathPrefix = $"/{parts[0]}/{parts[1]}";

            return $"{prefix}{pathPrefix}/products-details?productIds={productId}&ajax=true";
        }

        private static string AppendQuery(string url, string key, string value)
        {
            try
            {
                var ub = new UriBuilder(url);
                var q = System.Web.HttpUtility.ParseQueryString(ub.Query ?? "");
                q[key] = value;
                ub.Query = q.ToString();
                return ub.ToString();
            }
            catch { return url; }
        }

        private static string GetQuery(string url, string key)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                var uri = new Uri(url);
                var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
                return q?.Get(key);
            }
            catch { return null; }
        }

        private static string StripInternalParams(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            try
            {
                var ub = new UriBuilder(url);
                var q = System.Web.HttpUtility.ParseQueryString(ub.Query ?? "");
                q.Remove("__pid");
                q.Remove("__cat");
                ub.Query = q.ToString();
                return ub.ToString();
            }
            catch { return url; }
        }

        // =========================
        // Challenge Detection
        // =========================

        private static bool LooksLikeChallenge(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var t = text.TrimStart();
            var lower = text.ToLowerInvariant();
            var isJsonLike = t.StartsWith("{") || t.StartsWith("[");

            if (isJsonLike)
            {
                if (lower.Contains("bm-verify") || lower.Contains("triggerinterstitialchallenge"))
                    return true;

                if ((lower.Contains("\"status\":403") || lower.Contains("\"status\": 403")) &&
                    (lower.Contains("access denied") || lower.Contains("forbidden")))
                    return true;

                return false;
            }

            if (t.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                t.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                return true;

            if (lower.Contains("interstitial") &&
                (lower.Contains("bm-verify") || lower.Contains("triggerinterstitialchallenge")))
                return true;

            if (lower.Contains("access denied") ||
                lower.Contains("pardon our interruption") ||
                lower.Contains("bm-verify"))
                return true;

            return false;
        }

        private static CaptchaException BuildCaptcha(string url, string message)
        {
            return new CaptchaException
            {
                CaptchaInfo = new CaptchaInfo
                {
                    Message = message,
                    Url = string.IsNullOrWhiteSpace(url) ? DefaultCategoryUrl : url
                }
            };
        }

        // =========================
        // Description Extraction
        // =========================

        private static string ExtractDescription(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;

            var desc = ExtractDivByMarker(html, "product-detail-description");
            var composition = ExtractDivByMarker(html, "product-detail-view__secondary-info");

            if (string.IsNullOrWhiteSpace(desc) && string.IsNullOrWhiteSpace(composition))
                return null;

            return string.Join("\n", new[] { desc, composition }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        private static string ExtractDivByMarker(string html, string marker)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;

            var idx = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            var divStart = html.LastIndexOf("<div", idx, StringComparison.OrdinalIgnoreCase);
            if (divStart < 0) return null;

            int depth = 0;
            int pos = divStart;

            while (pos < html.Length)
            {
                var nextOpen = html.IndexOf("<div", pos, StringComparison.OrdinalIgnoreCase);
                var nextClose = html.IndexOf("</div>", pos, StringComparison.OrdinalIgnoreCase);

                if (nextClose < 0) break;

                if (nextOpen >= 0 && nextOpen < nextClose)
                {
                    depth++;
                    pos = nextOpen + 4;
                }
                else
                {
                    depth--;
                    pos = nextClose + 6;
                    if (depth == 0)
                        return html.Substring(divStart, pos - divStart);
                }
            }

            return null;
        }

        // =========================
        // Product Skeleton + Option
        // =========================

        private static Product CreateSafeSkeleton(string url)
        {
            return new Product
            {
                site = nameof(Zara),
                url = url,
                currency = Currency.KRW.ToString(),
                collectStatus = ProductCollectStatus.Failed,
                code = "",
                title = new List<Name> { new Name(Language.KR, "") },
                price = new List<Price> { new Price(Currency.KRW, 0, 0, true), new Price("KRW", 0, 0) },
                mainImages = new List<string>(),
                descs = new List<Desc>(),
                tags = new List<Tag>(),
                detailPageHtml = "",
                option = new Option
                {
                    optionGroups = new List<Option.OptionGroup>(),
                    detailOptions = new List<Option.DetailOption>()
                }
            };
        }

        private Option CreateSingleDefaultOption(List<Price> productPrices)
        {
            var safePrices = (productPrices != null && productPrices.Count > 0)
                ? productPrices
                : ProductHelper.CreatePrice(Currency, 0, 0, true);

            return new Option
            {
                optionGroups = new List<Option.OptionGroup>
                {
                    new Option.OptionGroup
                    {
                        optionId = "1",
                        groupName = ProductHelper.CreateName(Language.KR, "옵션"),
                        groups = new List<Option.OptionGroup.Group>
                        {
                            new Option.OptionGroup.Group
                            {
                                groupId = "1",
                                optionName = ProductHelper.CreateName(Language.KR, "단일상품"),
                                quantity = 9,
                                optionImage = null,
                                price = safePrices
                            }
                        }
                    }
                },
                detailOptions = new List<Option.DetailOption>
                {
                    new Option.DetailOption
                    {
                        optionIds = "1",
                        isSoldOut = false,
                        optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                        optionName = ProductHelper.CreateName(Language.KR, "단일상품"),
                        quantity = 9,
                        optionImage = null,
                        price = safePrices
                    }
                }
            };
        }

        public Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            throw new NotImplementedException();
        }

        public Product GetProduct(CollectRawData rawData)
        {
            throw new NotImplementedException();
        }

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            throw new NotImplementedException();
        }
    }
}