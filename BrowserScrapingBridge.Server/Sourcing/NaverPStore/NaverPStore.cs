#nullable disable warnings
// NaverPStore.cs - 네이버 쇼핑 윈도우(+스토어) 수집기
// 지원 URL 패턴:
//   - https://shopping.naver.com/window/{group}/category/{categoryId}?sort=...
//   - https://shopping.naver.com/gift/category?menu={menuId}
//   - https://shopping.naver.com/window/search/{group}?q={query}
// 상품 상세:
//   - https://shopping.naver.com/window-products/{vertical}/{productId}
//
// 수집 방식: SignalR 브라우저 확장 + __NEXT_DATA__/RSC 파싱
// 페이징: URL에 page 파라미터 추가 (무한 스크롤 → 서버사이드 page 대응)
// ※ __NEXT_DATA__ 경로는 네이버 배포 버전에 따라 변경될 수 있으므로 실패 시 확인 필요

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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace BrowserScrapingBridge.Server.Sourcing.NaverPStore
{
    public class NaverPStore : ICollect
    {
        public string Site => "NaverPStore";
        public string Name => "네이버+스토어";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        private const string Host = "https://shopping.naver.com";
        private const string SearchHost = "https://search.shopping.naver.com";

        private static bool IsBlocked(string html) => IsCaptcha(html);

        private static List<CollectPageInfo> ExtractBrowserProducts(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "null") return new();
            return JArray.Parse(json).Select(item =>
            {
                var id = item["id"]?.ToString();
                var url = item["url"]?.ToString();
                var productUrl = !string.IsNullOrWhiteSpace(url) && url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    && !url.Contains("cr.shopping.naver.com", StringComparison.OrdinalIgnoreCase)
                    ? CleanProductUrl(url) : $"{SearchHost}/catalog/{id}";
                return new CollectPageInfo { Code = id, Title = item["name"]?.ToString(), Url = productUrl };
            }).Where(item => !string.IsNullOrWhiteSpace(item.Code)).DistinctBy(item => item.Code).ToList();
        }

        public Task<string> GetLoginUrlAsync(string connectionId)
        {
            return Task.FromResult<string>(null);
        }

        // =========================
        // Public: Code
        // =========================

        /// <summary>
        /// URL에서 상품 코드(productId) 추출
        /// https://shopping.naver.com/window-products/{vertical}/{productId}
        /// </summary>
        public string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            // window-products/{vertical}/{productId}
            var m = Regex.Match(url, @"window-products/[^/?#]+/(\d+)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;

            // catalog/{productId}
            m = Regex.Match(url, @"/catalog/(\d+)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;

            // /products/{productId}
            m = Regex.Match(url, @"/products?/(\d+)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;

            return null;
        }

        // =========================
        // Public: List
        // =========================

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var pageUrl = AppendPage(StripTrackingParams(url), page);
            var result = await SourcingExecution.Current.Browser.GetDomAsync(pageUrl, "naver");
            if (IsBlocked(result.Html)) throw BuildCaptcha(pageUrl, "브라우저에서 네이버 접속 상태를 확인해주세요.");
            var nextData = ExtractNextData(result.Html);
            var products = nextData == null ? new List<CollectPageInfo>() : ExtractListProducts(nextData);
            if (products.Count == 0) products = ExtractBrowserProducts(result.ExtractedData?["products"]);
            if (products.Count == 0) throw new CollectException("네이버 페이지에서 상품 목록을 찾을 수 없습니다.");
            return products;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var product = CreateSkeleton(url);
            var code = GetCode(url);
            if (string.IsNullOrWhiteSpace(code)) throw new CollectException("상품 코드를 URL에서 추출할 수 없습니다.");
            product.code = code;
            product.url = CleanProductUrl(url);
            if (product.url.Contains("smartstore.naver.com", StringComparison.OrdinalIgnoreCase))
            {
                try { return FetchSmartStoreProduct(product, code); }
                catch (OperationCanceledException) { throw; }
                catch { }
            }
            var result = await SourcingExecution.Current.Browser.GetDomAsync(product.url, "naver");
            if (IsBlocked(result.Html)) throw BuildCaptcha(url, "브라우저에서 네이버 접속 상태를 확인해주세요.");
            var nextData = ExtractNextData(result.Html);
            if (nextData != null) ParseProduct(product, nextData, code);
            else ParseProductFromRsc(product, result.ExtractedData?["product"]);
            product.collectStatus = ProductCollectStatus.Success;
            return product;
        }

        private void ParseProductFromRsc(Product product, string jsResult)
        {
            if (string.IsNullOrWhiteSpace(jsResult) || jsResult == "null")
            {
                throw new CollectException("상품 데이터를 찾을 수 없습니다. (RSC 페이지)");
            }

            var data = JObject.Parse(jsResult);

            var title = data["title"]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(title))
            {
                Console.WriteLine($"[NaverPStore 상품 진단] title 비어있음. data: {jsResult?.Substring(0, Math.Min(500, jsResult?.Length ?? 0))}");
                throw new CollectException("상품명을 찾을 수 없습니다. (RSC 페이지)");
            }

            product.title = ProductHelper.CreateName(Language.KR, title);

            var salePrice = data["salePrice"]?.Value<double>() ?? 0;
            var originalPrice = data["originalPrice"]?.Value<double>() ?? salePrice;
            if (salePrice <= 0) salePrice = originalPrice;
            if (originalPrice <= 0 || originalPrice < salePrice) originalPrice = salePrice;
            product.price = ProductHelper.CreatePrice(Currency, salePrice, originalPrice, true);
            foreach (var p in product.price) p.isoCode = Currency.ToString();

            var images = data["images"]?.ToObject<List<string>>() ?? new List<string>();
            if (images.Count > 0) product.mainImages = images;

            product.option = CreateDefaultOption(product.price);
            product.collectStatus = ProductCollectStatus.Success;
        }

        /// <summary>
        /// SmartStore 상품을 HttpClient로 직접 요청하여 파싱
        /// </summary>
        private Product FetchSmartStoreProduct(Product product, string code)
        {
            using var handler = new System.Net.Http.HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using var client = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept-Language", "ko-KR,ko;q=0.9");
            client.Timeout = TimeSpan.FromSeconds(15);

            var html = client.GetStringAsync(product.url).GetAwaiter().GetResult();

            if (string.IsNullOrWhiteSpace(html))
                throw new CollectException($"SmartStore 빈 응답: {product.url}");

            // 에러 페이지 또는 판매 종료 감지
            if (html.Contains("에러페이지") || html.Contains("시스템오류"))
            {
                product.memo = "SmartStore 상품이 존재하지 않거나 판매 종료됨";
                product.collectStatus = ProductCollectStatus.Failed;
                return product;
            }

            var nextData = ExtractNextData(html);
            if (nextData == null)
                throw new CollectException($"SmartStore __NEXT_DATA__ 없음: {product.url}");

            // 1차: 기존 ParseProduct 경로 시도
            try
            {
                ParseProduct(product, nextData, code);
                product.collectStatus = ProductCollectStatus.Success;
                return product;
            }
            catch { /* SmartStore 구조가 다를 수 있음 — 범용 파싱으로 */ }

            // 2차: SmartStore 고유 경로 탐색
            JToken data = null;

            // initialState.product.{key}.product
            var initialProduct = nextData.SelectToken("props.pageProps.initialState.product");
            if (initialProduct != null)
            {
                foreach (var child in initialProduct.Children<JProperty>())
                {
                    data = child.Value.SelectToken("product");
                    if (data?["name"] != null) break;
                    data = null;
                }
            }

            // 범용: name + salePrice 있는 product 객체
            if (data == null)
            {
                foreach (var t in nextData.SelectTokens("$..product"))
                {
                    if (t.Type == JTokenType.Object && t["name"] != null
                        && (t["salePrice"] != null || t["price"] != null))
                    {
                        data = t;
                        break;
                    }
                }
            }

            if (data == null)
            {
                var pageProps = nextData.SelectToken("props.pageProps");
                var keys = pageProps?.Children<JProperty>().Select(p => p.Name).Take(15)
                    ?? Enumerable.Empty<string>();
                Console.WriteLine($"[NaverPStore] SmartStore 구조: keys=[{string.Join(",", keys)}]");
                throw new CollectException(
                    $"SmartStore 상품 데이터를 찾을 수 없습니다. keys: [{string.Join(",", keys)}]");
            }

            // 파싱
            var name = data["name"]?.ToString() ?? data["title"]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(name))
                throw new CollectException("SmartStore 상품명 없음");

            product.title = ProductHelper.CreateName(Language.KR, name);

            var salePrice = data["salePrice"]?.Value<double?>()
                ?? data["discountedSalePrice"]?.Value<double?>()
                ?? data["price"]?.Value<double?>() ?? 0;
            var originalPrice = data["originalPrice"]?.Value<double?>()
                ?? data["listPrice"]?.Value<double?>() ?? salePrice;
            if (salePrice <= 0) salePrice = originalPrice;
            if (originalPrice <= 0 || originalPrice < salePrice) originalPrice = salePrice;
            product.price = ProductHelper.CreatePrice(Currency, salePrice, originalPrice, true);
            foreach (var p in product.price) p.isoCode = Currency.ToString();

            var images = new List<string>();
            AddImageIfValid(images, data["representativeImageUrl"]?.ToString());
            AddImageIfValid(images, data["imageUrl"]?.ToString());
            foreach (var t in data.SelectTokens("..productImages[*].url")
                         .Concat(data.SelectTokens("..images[*].url")))
                AddImageIfValid(images, t?.ToString());
            product.mainImages = images;

            product.option = BuildOption(data, product.price);
            product.detailPageHtml = data["detailContents"]?.ToString() ?? "";
            product.collectStatus = ProductCollectStatus.Success;
            return product;
        }

        // =========================
        // List 파싱
        // =========================

        private List<CollectPageInfo> ExtractListProducts(JObject nextData)
        {
            var result = new List<CollectPageInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // dehydratedState.queries[*].state.data 에서 상품 배열 탐색
            var dataBags = nextData
                .SelectTokens("props.pageProps.dehydratedState.queries[*].state.data")
                .ToList();

            // pageProps 직하위 배열도 확인 (구조 변경 대비)
            var directBag = nextData.SelectToken("props.pageProps");
            if (directBag != null) dataBags.Add(directBag);

            foreach (var bag in dataBags)
            {
                // 상품 목록이 담길 수 있는 다양한 키 경로
                var candidates = new[]
                {
                    "products", "items", "productList", "windowProducts",
                    "categoryProducts", "searchProducts", "giftProducts"
                };

                IEnumerable<JToken> items = Enumerable.Empty<JToken>();
                foreach (var key in candidates)
                {
                    var found = bag.SelectTokens($"..{key}[*]");
                    items = items.Concat(found);
                }

                foreach (var item in items)
                {
                    var productId = item["productId"]?.ToString();
                    if (string.IsNullOrWhiteSpace(productId)) continue;
                    if (!seen.Add(productId)) continue;

                    var name = item["name"]?.ToString() ?? item["title"]?.ToString() ?? "";

                    // vertical(카테고리) 추출 — URL 생성에 사용
                    var vertical = (item["vertical"]?.ToString()
                        ?? item["subVerticalType"]?.ToString()
                        ?? "products").ToLower();

                    // productUrl이 있으면 우선 사용, 없으면 window-products 형태로 조합
                    var rawUrl = item["productUrl"]?.ToString()
                        ?? item["mobileProductUrl"]?.ToString();

                    string productUrl;
                    if (!string.IsNullOrWhiteSpace(rawUrl)
                        && rawUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        && rawUrl.Contains("window-products", StringComparison.OrdinalIgnoreCase))
                    {
                        productUrl = CleanProductUrl(rawUrl);
                    }
                    else
                    {
                        productUrl = $"{Host}/window-products/{vertical}/{productId}";
                    }

                    result.Add(new CollectPageInfo
                    {
                        Code = productId,
                        Url = productUrl,
                        Title = name
                    });
                }
            }

            return result;
        }

        // =========================
        // 상품 상세 파싱
        // =========================

        private void ParseProduct(Product product, JObject nextData, string code)
        {
            // dehydratedState.queries[*].state.data 에서 상품 데이터 탐색
            JToken data = null;

            var queryDataList = nextData
                .SelectTokens("props.pageProps.dehydratedState.queries[*].state.data")
                .ToList();

            foreach (var d in queryDataList)
            {
                // 가능한 최상위 키들
                data = d["windowProductDetail"]
                    ?? d["product"]
                    ?? d["productDetail"]
                    ?? d["channelProduct"];

                if (data != null) break;

                // productId로 직접 탐색
                var byId = d.SelectToken($"..products[?(@.productId=='{code}')]")
                    ?? d.SelectToken($"..items[?(@.productId=='{code}')]");
                if (byId != null) { data = byId; break; }
            }

            if (data == null)
                throw new Exception($"상품 데이터를 찾을 수 없습니다. (code: {code})");

            // 상품명
            var name = data["name"]?.ToString() ?? data["title"]?.ToString() ?? "";
            product.title = ProductHelper.CreateName(Language.KR, name);

            // 가격
            var salePrice = data["salePrice"]?.Value<double?>()
                ?? data["dispSalePrice"]?.Value<double?>()
                ?? 0d;
            var discountedPrice = data["discountedSalePrice"]?.Value<double?>()
                ?? data["dispDiscountedSalePrice"]?.Value<double?>()
                ?? salePrice;
            product.price = ProductHelper.CreatePrice(Currency, discountedPrice, salePrice, true);
            foreach (var p in product.price) p.isoCode = Currency.ToString();

            // 대표 이미지 + 추가 이미지
            var images = new List<string>();
            AddImageIfValid(images, data["representativeImageUrl"]?.ToString());
            foreach (var t in data.SelectTokens("..productImages[*].url")
                         .Concat(data.SelectTokens("..images[*].url"))
                         .Concat(data.SelectTokens("..xmedia[*].url")))
            {
                AddImageIfValid(images, t?.ToString());
            }
            product.mainImages = images;

            // 옵션
            product.option = BuildOption(data, product.price);

            // 상세 페이지 HTML
            product.detailPageHtml = data["detailContents"]?.ToString()
                ?? data["contents"]?.ToString()
                ?? "";
        }

        // =========================
        // 옵션 구성
        // =========================

        private Option BuildOption(JToken data, List<Price> prices)
        {
            // purchaseOptions 또는 options 배열 탐색
            var optTokens = data["purchaseOptions"]?.ToList()
                ?? data["options"]?.ToList()
                ?? new List<JToken>();

            if (optTokens.Count == 0)
                return CreateDefaultOption(prices);

            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();
            int idx = 1;

            foreach (var opt in optTokens)
            {
                var stockQty = opt["stockQuantity"]?.Value<int?>() ?? 9;
                var soldout = opt["soldout"]?.Value<bool?>() ?? (stockQty <= 0);
                if (soldout) { idx++; continue; }

                var id = idx.ToString();
                var optName = opt["purchaseOptionValueName"]?.ToString()
                    ?? opt["standardPurchaseOptionName"]?.ToString()
                    ?? opt["name"]?.ToString()
                    ?? $"옵션{idx}";
                var qty = stockQty > 0 ? stockQty : 9;
                var optImage = opt["image"]?.ToString();

                groups.Add(new Option.OptionGroup.Group
                {
                    groupId = id,
                    optionName = ProductHelper.CreateName(Language.KR, optName),
                    quantity = qty,
                    optionImage = optImage,
                    price = prices
                });

                details.Add(new Option.DetailOption
                {
                    optionIds = id,
                    isSoldOut = false,
                    optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                    optionName = ProductHelper.CreateName(Language.KR, optName),
                    quantity = qty,
                    optionImage = optImage,
                    price = prices
                });

                idx++;
            }

            if (details.Count == 0)
                return CreateDefaultOption(prices);

            return new Option
            {
                optionGroups = new List<Option.OptionGroup>
                {
                    new Option.OptionGroup
                    {
                        optionId = "1",
                        groupName = ProductHelper.CreateName(Language.KR, "옵션"),
                        groups = groups
                    }
                },
                detailOptions = details
            };
        }

        private Option CreateDefaultOption(List<Price> prices)
        {
            var groupName = new Name(Language.KR, "옵션");
            var optionName = new Name(Language.KR, "단일상품");

            var optionGroup = new Option.OptionGroup();
            optionGroup.optionId = "0";
            optionGroup.groupName = new List<Name> { groupName };
            optionGroup.groups = new List<Option.OptionGroup.Group>
            {
                new Option.OptionGroup.Group
                {
                    groupId = "0",
                    optionName = new List<Name> { optionName }
                }
            };

            var detailOption = new Option.DetailOption();
            detailOption.optionIds = "0";
            detailOption.optionGroupName = new List<Name> { groupName };
            detailOption.optionName = new List<Name> { optionName };
            detailOption.price = prices;

            return new Option
            {
                optionGroups = new List<Option.OptionGroup> { optionGroup },
                detailOptions = new List<Option.DetailOption> { detailOption }
            };
        }

        // =========================
        // 유틸
        // =========================

        private static Product CreateSkeleton(string url)
        {
            return new Product
            {
                site = "NaverPStore",
                url = url,
                currency = Currency.KRW.ToString(),
                collectStatus = ProductCollectStatus.Failed,
                code = "",
                title = new List<Name> { new Name(Language.KR, "") },
                price = ProductHelper.CreatePrice(Currency.KRW, 0, 0, true),
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

        /// <summary>
        /// HTML에서 &lt;script id="__NEXT_DATA__"&gt; 추출
        /// </summary>
        private static JObject ExtractNextData(string html)
        {
            try
            {
                var m = Regex.Match(html,
                    @"<script\s+id=""__NEXT_DATA__""\s+type=""application/json"">(.+?)</script>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (!m.Success) return null;
                return JObject.Parse(m.Groups[1].Value);
            }
            catch { return null; }
        }

        /// <summary>
        /// 캡챠 또는 차단 페이지 감지
        /// </summary>
        private static bool IsCaptcha(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return false;
            return html.Contains("captcha", StringComparison.OrdinalIgnoreCase)
                || html.Contains("naver.com/errors/", StringComparison.OrdinalIgnoreCase)
                || html.Contains("쇼핑 서비스 접속이 일시적으로 제한", StringComparison.OrdinalIgnoreCase)
                || html.Contains("shopping_stop", StringComparison.OrdinalIgnoreCase)
                || (html.Contains("잠시 후 다시 시도", StringComparison.OrdinalIgnoreCase) && html.Length < 5000);
        }

        /// <summary>
        /// 트래킹 파라미터(NaPm 등) 제거
        /// </summary>
        private static string StripTrackingParams(string url)
        {
            try
            {
                var uri = new Uri(url);
                var qs = HttpUtility.ParseQueryString(uri.Query);
                qs.Remove("NaPm");
                string query = qs.Count > 0 ? "?" + qs : "";
                return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}{query}";
            }
            catch { return url; }
        }

        /// <summary>
        /// 상품 URL에서 트래킹 파라미터 전체 제거 (경로만 유지)
        /// </summary>
        private static string CleanProductUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
            }
            catch { return url; }
        }

        /// <summary>
        /// URL에 page 파라미터 추가 (page=1은 생략)
        /// </summary>
        private static string AppendPage(string url, int page)
        {
            if (page <= 1) return url;
            try
            {
                var uri = new Uri(url);
                var qs = HttpUtility.ParseQueryString(uri.Query);
                qs["page"] = page.ToString();
                return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}?{qs}";
            }
            catch { return url; }
        }

        private static void AddImageIfValid(List<string> images, string url)
        {
            if (!string.IsNullOrWhiteSpace(url) && !images.Contains(url))
                images.Add(url);
        }

        private static CaptchaException BuildCaptcha(string url, string message)
        {
            return new CaptchaException
            {
                CaptchaInfo = new CaptchaInfo
                {
                    Message = message,
                    Url = string.IsNullOrWhiteSpace(url) ? Host : url
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
