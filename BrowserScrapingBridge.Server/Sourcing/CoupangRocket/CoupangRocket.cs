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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Web;

namespace BrowserScrapingBridge.Server.Sourcing.CoupangRocket
{
    /// <summary>
    /// 쿠팡로켓배송 수집기
    /// - 연결된 브라우저 확장으로 DOM HTML 수집
    /// - 현재 브라우저 프로필의 세션 사용
    /// - exports.sdp JSON 파싱 (1순위) + JSON-LD/HTML 폴백
    /// </summary>
    public class CoupangRocket : ICollect
    {
        public string Site => nameof(CoupangRocket);
        public string Name => "쿠팡로켓배송";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        private const string HOST = "https://www.coupang.com";
        public Task<string> GetLoginUrlAsync(string connectionId) => Task.FromResult<string>(null);

        private static string FetchHtml(string url)
        {
            var page = SourcingExecution.Current.Browser.GetDomAsync(url).GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(page.Html)) throw new CollectException("쿠팡 페이지 응답이 비어있습니다.");
            if (page.Html.Contains("Access Denied") && page.Html.Length < 1000)
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Url = url, Message = "브라우저에서 쿠팡 접속 상태를 확인해주세요." } };
            return page.Html;
        }

        // =========================
        // Code 추출
        // =========================

        public string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var m = Regex.Match(url, @"/vp/products/(\d+)");
            if (m.Success) return m.Groups[1].Value;
            return null;
        }

        // =========================
        // 카테고리 수집
        // =========================

        public Task<List<CollectPageInfo>> GetCollectPageInfosAsync(
            string connectionId, string url, int page)
        {
            string pageUrl = AppendPage(url, page);
            string html = FetchHtml(pageUrl);

            var result = new List<CollectPageInfo>();
            var addedCodes = new HashSet<string>();

            // 패턴1: /vp/products/{id} + 상품명
            var productLinks = Regex.Matches(html,
                @"href=""(/vp/products/(\d+)[^""]*)""\s[^>]*>.*?class=""[^""]*name[^""]*""\s*>([^<]+)<",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match match in productLinks)
            {
                string relativeUrl = HttpUtility.HtmlDecode(match.Groups[1].Value);
                string code = match.Groups[2].Value;
                string title = HttpUtility.HtmlDecode(match.Groups[3].Value).Trim();
                if (string.IsNullOrWhiteSpace(code) || addedCodes.Contains(code)) continue;
                addedCodes.Add(code);
                result.Add(new CollectPageInfo { Code = code, Url = HOST + relativeUrl, Title = title });
            }

            // 패턴2: 링크만 추출
            if (result.Count == 0)
            {
                var simpleLinks = Regex.Matches(html, @"href=""(/vp/products/(\d+)[^""]*)""", RegexOptions.IgnoreCase);
                foreach (Match match in simpleLinks)
                {
                    string relativeUrl = HttpUtility.HtmlDecode(match.Groups[1].Value);
                    string code = match.Groups[2].Value;
                    if (string.IsNullOrWhiteSpace(code) || addedCodes.Contains(code)) continue;
                    addedCodes.Add(code);
                    result.Add(new CollectPageInfo { Code = code, Url = HOST + relativeUrl, Title = $"쿠팡상품_{code}" });
                }
            }

            if (result.Count == 0)
            {
                bool hasProducts = html.Contains("/vp/products/");
                throw new Exception($"쿠팡 카테고리에서 상품을 찾을 수 없습니다. (HTML길이:{html.Length}, products링크:{hasProducts})");
            }

            return Task.FromResult(result);
        }

        // =========================
        // 상품 수집
        // =========================

        public Task<Product> GetProductAsync(
            string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var product = new Product();
            try
            {
                string code = GetCode(url);
                product.site = this.Site;
                product.code = code;
                product.currency = Currency.KRW.ToString();
                product.url = url;

                string html = FetchHtml(url);

                // 1순위: exports.sdp JSON
                var sdp = ExtractExportsSdp(html);
                var jsonLd = sdp == null ? ExtractProductJsonLd(html) : null;

                if (sdp != null)
                    ParseFromSdp(product, sdp, html);
                else
                    ParseFromHtmlAndJsonLd(product, html, jsonLd);

                product.regDate = DateTime.Now;
                product.collectStatus = ProductCollectStatus.Success;
            }
            catch (SaleException) { throw; }
            catch (Exception ex)
            {
                if (product.title == null)
                    throw new CollectException(ex.Message);
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = ex.Message;
            }

            return Task.FromResult(product);
        }

        // =========================
        // exports.sdp 파싱
        // =========================

        private static JObject ExtractExportsSdp(string html)
        {
            try
            {
                var m = Regex.Match(html, @"exports\.sdp\s*=\s*(\{.+?\});\s*</script>", RegexOptions.Singleline);
                if (m.Success) return JObject.Parse(m.Groups[1].Value);

                m = Regex.Match(html, @"window\.__PRELOADED_STATE__\s*=\s*(\{.+?\});\s*</script>", RegexOptions.Singleline);
                if (m.Success) return JObject.Parse(m.Groups[1].Value);
            }
            catch { }
            return null;
        }

        private void ParseFromSdp(Product product, JObject sdp, string html)
        {
            if (sdp["soldOut"]?.Value<bool>() == true)
                throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품입니다.");

            string title = sdp["title"]?.ToString() ?? sdp["itemName"]?.ToString() ?? GetTitleFromHtml(html);
            if (string.IsNullOrWhiteSpace(title)) throw new Exception("상품명을 찾을 수 없습니다.");
            product.title = ProductHelper.CreateName(Language.KR, title);

            double salePrice = 0, originalPrice = 0;
            var priceObj = sdp.SelectToken("quantityBase[0].price");
            if (priceObj != null)
            {
                double.TryParse(priceObj["salePrice"]?.ToString()?.Replace(",", ""), out salePrice);
                double.TryParse(priceObj["originPrice"]?.ToString()?.Replace(",", ""), out originalPrice);
            }
            if (salePrice <= 0) salePrice = ExtractPriceFromHtml(html, "final-price-amount");
            if (originalPrice <= 0) originalPrice = ExtractPriceFromHtml(html, "sales-price-amount");
            if (salePrice <= 0) throw new Exception("상품 가격을 찾을 수 없습니다.");
            if (originalPrice <= 0 || originalPrice < salePrice) originalPrice = salePrice;
            product.price = CreatePrice(salePrice, originalPrice);

            // 이미지: sdp에서 추출 → HTML 폴백
            product.mainImages = ExtractImagesFromSdp(sdp);
            if (product.mainImages == null || product.mainImages.Count <= 1)
            {
                var htmlImages = ExtractImagesFromHtml(html);
                if (htmlImages != null && htmlImages.Count > (product.mainImages?.Count ?? 0))
                    product.mainImages = htmlImages;
            }

            var options = sdp["options"];
            product.option = (options != null && options.Type != JTokenType.Null)
                ? ParseSdpOptions(options, product.price)
                : CreateDefaultOption(product.price);

            product.detailPageHtml = GetDetailPageHtml(html);
        }

        /// <summary>
        /// exports.sdp에서 이미지 배열 추출 (여러 경로 시도)
        /// </summary>
        private static List<string> ExtractImagesFromSdp(JObject sdp)
        {
            var images = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string url)
            {
                url = NormalizeImageUrl(url);
                if (url != null && seen.Add(url)) images.Add(url);
            }

            // sdp.images (배열)
            if (sdp["images"] is JArray imgArr)
                foreach (var img in imgArr) Add(img.ToString());

            // sdp.mainImageUrls (배열)
            if (sdp["mainImageUrls"] is JArray mainArr)
                foreach (var img in mainArr) Add(img.ToString());

            // sdp.mainImage (단일)
            Add(sdp["mainImage"]?.ToString());
            Add(sdp["mainImageUrl"]?.ToString());

            // sdp.media[*].url 또는 media[*].imageUrl
            if (sdp["media"] is JArray mediaArr)
                foreach (var m in mediaArr) { Add(m["url"]?.ToString()); Add(m["imageUrl"]?.ToString()); }

            // vendorItemImages
            if (sdp["vendorItemImages"] is JArray viArr)
                foreach (var vi in viArr) Add(vi.ToString());

            return images.Count > 0 ? images : null;
        }

        /// <summary>
        /// sdp options 파싱 — attributes에서 차원별 이름 추출 (개당용량/수량 등)
        /// </summary>
        private Option ParseSdpOptions(JToken options, List<Price> defaultPrice)
        {
            var attributeMap = options["attributeVendorItemMap"] as JObject;
            if (attributeMap == null || attributeMap.Count == 0) return CreateDefaultOption(defaultPrice);

            // attributes에서 옵션 차원 이름 추출 (예: ["개당용량", "수량"])
            var attrNames = new List<string>();
            if (options["attributes"] is JArray attrArr)
            {
                foreach (var attr in attrArr)
                {
                    string name = attr["attributeTypeName"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(name)) attrNames.Add(name);
                }
            }
            string groupNameStr = attrNames.Count > 0 ? string.Join(" / ", attrNames) : "옵션";

            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();
            int idx = 1;
            foreach (var prop in attributeMap.Properties())
            {
                // 옵션 키: "100ml, 3개" → " / " 구분으로 정규화
                string rawKey = prop.Name;
                string optName = attrNames.Count > 1
                    ? Regex.Replace(rawKey, @"\s*,\s*", " / ")
                    : rawKey;

                var vi = prop.Value;
                bool isSoldOut = vi["soldOut"]?.Value<bool>() ?? false;
                double optPrice = 0;
                var pt = vi.SelectToken("quantityBase[0].price.salePrice");
                if (pt != null) double.TryParse(pt.ToString().Replace(",", ""), out optPrice);
                var price = optPrice > 0 ? CreatePrice(optPrice, optPrice) : defaultPrice;
                string id = idx.ToString();
                groups.Add(new Option.OptionGroup.Group { groupId = id, optionName = ProductHelper.CreateName(Language.KR, optName), quantity = isSoldOut ? 0 : 9, price = price });
                details.Add(new Option.DetailOption { optionIds = id, isSoldOut = isSoldOut, optionGroupName = ProductHelper.CreateName(Language.KR, groupNameStr), optionName = ProductHelper.CreateName(Language.KR, optName), quantity = isSoldOut ? 0 : 9, price = price });
                idx++;
            }
            if (details.Count <= 1) return CreateDefaultOption(defaultPrice);
            return new Option
            {
                optionGroups = new List<Option.OptionGroup> { new Option.OptionGroup { optionId = "1", groupName = ProductHelper.CreateName(Language.KR, groupNameStr), groups = groups } },
                detailOptions = details
            };
        }

        // =========================
        // JSON-LD + HTML 폴백
        // =========================

        private static JObject ExtractProductJsonLd(string html)
        {
            try
            {
                var matches = Regex.Matches(html, @"type=""application/ld\+json""[^>]*>(\{.+?\})</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    try { var obj = JObject.Parse(match.Groups[1].Value); if (obj["@type"]?.ToString() == "Product") return obj; } catch { }
                }
            }
            catch { }
            return null;
        }

        private void ParseFromHtmlAndJsonLd(Product product, string html, JObject jsonLd)
        {
            if (jsonLd != null && (jsonLd.SelectToken("offers.availability")?.ToString() ?? "").Contains("OutOfStock"))
                throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품입니다.");

            string title = GetTitleFromHtml(html) ?? jsonLd?["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(title)) throw new Exception("상품명을 찾을 수 없습니다.");
            product.title = ProductHelper.CreateName(Language.KR, title);

            double salePrice = ExtractPriceFromHtml(html, "final-price-amount");
            double originalPrice = ExtractPriceFromHtml(html, "sales-price-amount");
            if (salePrice <= 0 && jsonLd != null) { var p = jsonLd.SelectToken("offers.price")?.ToString(); if (!string.IsNullOrWhiteSpace(p)) double.TryParse(p, out salePrice); }
            if (salePrice <= 0) throw new Exception("상품 가격을 찾을 수 없습니다.");
            if (originalPrice <= 0 || originalPrice < salePrice) originalPrice = salePrice;
            product.price = CreatePrice(salePrice, originalPrice);

            product.mainImages = ExtractImagesFromHtml(html);
            // JSON-LD 이미지로 보강
            if (jsonLd?["image"] is JArray arr)
            {
                var ldImages = arr.Select(i => NormalizeImageUrl(i.ToString())).Where(u => u != null).ToList();
                if (ldImages.Count > (product.mainImages?.Count ?? 0))
                    product.mainImages = ldImages;
            }

            product.option = ParseHtmlOptions(html, product.price);
            product.detailPageHtml = GetDetailPageHtml(html);
        }

        // =========================
        // HTML 헬퍼
        // =========================

        private static string GetTitleFromHtml(string html)
        {
            var m = Regex.Match(html, @"class=""product-title[^""]*""[^>]*>\s*<span[^>]*>([^<]+)<", RegexOptions.IgnoreCase);
            if (m.Success) return HttpUtility.HtmlDecode(m.Groups[1].Value).Trim();
            m = Regex.Match(html, @"og:title""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
            if (m.Success) return HttpUtility.HtmlDecode(m.Groups[1].Value).Replace(" | 쿠팡", "").Trim();
            return null;
        }

        private static double ExtractPriceFromHtml(string html, string cls)
        {
            var m = Regex.Match(html, cls + @"[^""]*"">([^<]+)<", RegexOptions.IgnoreCase);
            return m.Success ? ParseKrwPrice(m.Groups[1].Value) : 0;
        }

        private static List<string> ExtractImagesFromHtml(string html)
        {
            var images = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string url)
            {
                url = NormalizeImageUrl(url);
                if (url != null && seen.Add(url)) images.Add(url);
            }

            // 1. data-origin (쿠팡 대표 이미지 속성 — 원본 고해상도)
            foreach (Match m in Regex.Matches(html, @"data-origin=""(https?://[^""]+)""", RegexOptions.IgnoreCase))
                Add(m.Groups[1].Value);

            // 2. 상품 갤러리 이미지 (prod-image 영역)
            foreach (Match m in Regex.Matches(html, @"class=""prod-image[^""]*""[^>]*>.*?<img[^>]*src=""([^""]+)""", RegexOptions.Singleline | RegexOptions.IgnoreCase))
                Add(m.Groups[1].Value);

            // 3. data-src (lazy load)
            foreach (Match m in Regex.Matches(html, @"data-src=""(https?://thumbnail[^""]+)""", RegexOptions.IgnoreCase))
                Add(m.Groups[1].Value);

            // 4. og:image (폴백)
            var og = Regex.Match(html, @"og:image""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
            if (og.Success) Add(og.Groups[1].Value);

            return images.Count > 0 ? images : null;
        }

        private Option ParseHtmlOptions(string html, List<Price> defaultPrice)
        {
            var optN = Regex.Matches(html, @"option-table-list__option-name"">([^<]+)<", RegexOptions.IgnoreCase);
            var optP = Regex.Matches(html, @"option-table-list__option-price"">([^<]+)<", RegexOptions.IgnoreCase);
            if (optN.Count <= 1) return CreateDefaultOption(defaultPrice);
            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();
            for (int i = 0; i < optN.Count; i++)
            {
                string nm = HttpUtility.HtmlDecode(optN[i].Groups[1].Value).Trim();
                double p = i < optP.Count ? ParseKrwPrice(optP[i].Groups[1].Value) : 0;
                var price = p > 0 ? CreatePrice(p, p) : defaultPrice;
                string id = (i + 1).ToString();
                groups.Add(new Option.OptionGroup.Group { groupId = id, optionName = ProductHelper.CreateName(Language.KR, nm), quantity = 9, price = price });
                details.Add(new Option.DetailOption { optionIds = id, isSoldOut = false, optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"), optionName = ProductHelper.CreateName(Language.KR, nm), quantity = 9, price = price });
            }
            return new Option
            {
                optionGroups = new List<Option.OptionGroup> { new Option.OptionGroup { optionId = "1", groupName = ProductHelper.CreateName(Language.KR, "옵션"), groups = groups } },
                detailOptions = details
            };
        }

        private string GetDetailPageHtml(string html)
        {
            var m = Regex.Match(html,
                @"<div\s+class=""product-detail-content-inside"">(.*?)</div>\s*</div>\s*(?:<div\s+class=""product-detail-essentials|<div\s+class=""return-policy|$)",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (m.Success) return Regex.Replace(m.Groups[1].Value, @"src=""//", @"src=""https://", RegexOptions.IgnoreCase);

            var imgs = Regex.Matches(html, @"subType-IMAGE[^""]*"">\s*<img[^>]*src=""([^""]+)""", RegexOptions.IgnoreCase);
            if (imgs.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                foreach (Match img in imgs) sb.Append($"<img src=\"{NormalizeImageUrl(img.Groups[1].Value)}\" width=\"100%\" alt=\"\" />");
                return sb.ToString();
            }
            return "";
        }

        private Option CreateDefaultOption(List<Price> prices)
        {
            // isoCode 보장
            foreach (var p in prices) if (string.IsNullOrEmpty(p.isoCode)) p.isoCode = Currency.ToString();
            return new Option
        {
            optionGroups = new List<Option.OptionGroup> { new Option.OptionGroup { optionId = "1", groupName = ProductHelper.CreateName(Language.KR, "옵션"), groups = new List<Option.OptionGroup.Group> { new Option.OptionGroup.Group { groupId = "1", optionName = ProductHelper.CreateName(Language.KR, "단일상품"), quantity = 9, price = prices } } } },
            detailOptions = new List<Option.DetailOption> { new Option.DetailOption { optionIds = "1", isSoldOut = false, optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"), optionName = ProductHelper.CreateName(Language.KR, "단일상품"), quantity = 9, price = prices } }
            };
        }

        // =========================
        // 유틸
        // =========================

        /// <summary>
        /// Price 생성 + isoCode 설정 (ASP.NET Core 8 model validation 필수)
        /// </summary>
        private List<Price> CreatePrice(double originalPrice, double salePrice, bool isDefaultPrice = true)
        {
            var prices = ProductHelper.CreatePrice(Currency, originalPrice, salePrice, isDefaultPrice);
            foreach (var p in prices) p.isoCode = Currency.ToString();
            return prices;
        }

        private static double ParseKrwPrice(string t) { if (string.IsNullOrWhiteSpace(t)) return 0; double.TryParse(Regex.Replace(t, @"[^\d]", ""), out double p); return p; }
        private static string NormalizeImageUrl(string u) { if (string.IsNullOrWhiteSpace(u)) return null; u = u.Trim(); return u.StartsWith("//") ? "https:" + u : u; }
        private static string AppendPage(string url, int page)
        {
            if (page <= 1) return url;
            try { var uri = new Uri(url); var qs = HttpUtility.ParseQueryString(uri.Query); qs["page"] = page.ToString(); return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}?{qs}"; }
            catch { return url; }
        }

        public Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url) => throw new NotImplementedException();
        public Product GetProduct(CollectRawData rawData) => throw new NotImplementedException();
        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting) => throw new NotImplementedException();
    }
}
