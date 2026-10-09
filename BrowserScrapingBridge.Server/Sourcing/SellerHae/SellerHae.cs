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
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.SellerHae
{
    /// <summary>
    /// 셀러해 (1020bag.com) — GodoMall 5 플랫폼, B2B 가방 도매
    /// 카테고리 목록: 로그인 불필요 (가격/옵션 컬러 포함)
    /// 상품 상세: 로그인 필요 (GodoMall JS 변수 파싱)
    /// </summary>
    public class SellerHae : CollectBase
    {
        private const string Host = "https://1020bag.com";

        public override string Site => nameof(SellerHae);
        public override string Name => "셀러해";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = false;

        public override Task<string> GetLoginUrlAsync(string connectionId)
            => Task.FromResult<string>(null);

        // =========================
        // GetCode
        // =========================
        public override string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var m = Regex.Match(url, @"goodsNo=(\d+)", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        public override string CreateProductUrl(string code)
            => $"{Host}/goods/goods_view.php?goodsNo={code}";

        // =========================
        // GetCollectPageInfosAsync
        // =========================
        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "셀러해 카테고리 URL이 비었습니다.", Url = Host } };

            if (page < 1) page = 1;

            // URL에서 cateCd 추출
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var cateCd = query?.Get("cateCd");
            if (string.IsNullOrWhiteSpace(cateCd))
                cateCd = "001"; // 기본 전체상품

            var listUrl = $"{Host}/goods/goods_list.php?cateCd={cateCd}&page={page}";
            var html = await GetByHttpClientAsync(listUrl);

            if (string.IsNullOrWhiteSpace(html))
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "셀러해 카테고리 응답이 비었습니다.", Url = listUrl } };

            var result = new List<CollectPageInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // <a href="../goods/goods_view.php?goodsNo=22687"
            var matches = Regex.Matches(html, @"goods_view\.php\?goodsNo=(\d+)", RegexOptions.IgnoreCase);
            foreach (Match m in matches)
            {
                var goodsNo = m.Groups[1].Value;
                if (!seen.Add(goodsNo)) continue;

                // 제목 추출: goodsNo 근처의 텍스트에서 상품명 찾기
                var title = ExtractTitleFromList(html, goodsNo);

                result.Add(new CollectPageInfo
                {
                    Code = goodsNo,
                    Url = $"{Host}/goods/goods_view.php?goodsNo={goodsNo}",
                    Title = title
                });
            }

            return result;
        }

        private static string ExtractTitleFromList(string html, string goodsNo)
        {
            // goodsNo 링크 이후의 <strong> 태그에서 제목 추출
            var idx = html.IndexOf($"goodsNo={goodsNo}", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return "";

            var chunk = html.Substring(idx, Math.Min(2000, html.Length - idx));
            var titleMatch = Regex.Match(chunk, @"<strong[^>]*>([^<]+)</strong>", RegexOptions.IgnoreCase);
            return titleMatch.Success ? WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim()) : "";
        }

        // =========================
        // GetHtmlAsync — GodoMall은 ChatHubService(브라우저 확장) 우선 + HttpClient 폴백
        // =========================
        public override async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            string html = null;
            try
            {
                html = await ChatHubService.GetRequest(connectionId, url);
            }
            catch { }

            if (!string.IsNullOrWhiteSpace(html) && !html.Contains("로그인 후 확인"))
                return html;

            // ChatHubService 실패 또는 로그인 필요 → HttpClient 시도
            return await GetByHttpClientAsync(url);
        }

        // =========================
        // GetData — GodoMall JS 변수를 JToken으로 변환
        // =========================
        public override Task<JToken> GetData(string connectionId, string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return Task.FromResult<JToken>(null);

            var data = new JObject();

            // GodoMall 표준 JS 변수 추출
            data["goodsNo"] = ExtractJsVar(html, "goodsNo");
            data["goodsNm"] = ExtractJsString(html, "goodsNm");
            data["setGoodsPrice"] = ExtractJsVar(html, "setGoodsPrice");
            data["fixedPrice"] = ExtractJsVar(html, "fixedPrice");
            data["goodsDiscount"] = ExtractJsVar(html, "goodsDiscount");
            data["gdCurrencyCode"] = ExtractJsVar(html, "gdCurrencyCode");
            data["setOptionFl"] = ExtractJsVar(html, "setOptionFl");
            data["setStockCnt"] = ExtractJsVar(html, "setStockCnt");

            return Task.FromResult<JToken>(data);
        }

        // =========================
        // Build Product parts
        // =========================
        public override void ThrowIfCaptcha(string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "셀러해 응답이 비었습니다." } };

            if (html.Contains("로그인 후 확인") || html.Contains("로그인이 필요"))
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "셀러해 로그인이 필요합니다." } };
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            // JS 변수 우선
            var title = data?["goodsNm"]?.ToString();

            // HTML <title> 폴백
            if (string.IsNullOrWhiteSpace(title))
            {
                var m = Regex.Match(html, @"<title>([^<]+)</title>", RegexOptions.IgnoreCase);
                if (m.Success) title = m.Groups[1].Value.Trim();
            }

            // og:title 폴백
            if (string.IsNullOrWhiteSpace(title))
            {
                var m = Regex.Match(html, @"og:title""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
                if (m.Success) title = m.Groups[1].Value.Trim();
            }

            return ProductHelper.CreateName(Language.KR, title ?? "");
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            var m = Regex.Match(html, @"<span\s+class=""brand"">([^<]+)</span>", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var brand = WebUtility.HtmlDecode(m.Groups[1].Value.Trim());
                if (!string.IsNullOrWhiteSpace(brand))
                    return ProductHelper.CreateName(Language.KR, brand);
            }
            return null;
        }

        public override string GetModel(string html, JToken data) => null;
        public override List<Desc> GetDescs(string html, JToken data) => new List<Desc>();
        public override List<Tag> GetTags(string html, JToken data) => new List<Tag>();

        public override List<Price> GetPrice(string html, JToken data)
        {
            double sellPrice = 0;
            double normalPrice = 0;

            // JS 변수: setGoodsPrice
            var priceStr = data?["setGoodsPrice"]?.ToString();
            if (!string.IsNullOrWhiteSpace(priceStr))
                double.TryParse(priceStr.Replace(",", "").Replace(".00", ""), out sellPrice);

            // fixedPrice (정가)
            var fixedStr = data?["fixedPrice"]?.ToString();
            if (!string.IsNullOrWhiteSpace(fixedStr))
                double.TryParse(fixedStr.Replace(",", "").Replace(".00", ""), out normalPrice);

            // HTML meta 폴백
            if (sellPrice <= 0)
            {
                var m = Regex.Match(html, @"product:price:amount""\s+content=""(\d+)""", RegexOptions.IgnoreCase);
                if (m.Success) double.TryParse(m.Groups[1].Value, out sellPrice);
            }

            // HTML .price 클래스 폴백
            if (sellPrice <= 0)
            {
                var m = Regex.Match(html, @"class=""price""[^>]*>([0-9,]+)원", RegexOptions.IgnoreCase);
                if (m.Success) double.TryParse(m.Groups[1].Value.Replace(",", ""), out sellPrice);
            }

            if (normalPrice <= 0) normalPrice = sellPrice;

            return ProductHelper.CreatePrice(Currency, sellPrice, normalPrice, true);
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // magnify 영역 이미지 (상품 상세 메인 이미지)
            var mainMatches = Regex.Matches(html, @"<img[^>]+(?:id=""objImg""|class=""[^""]*thumb[^""]*"")[^>]+src=""([^""]+)""", RegexOptions.IgnoreCase);
            foreach (Match m in mainMatches)
            {
                var imgUrl = NormalizeImageUrl(m.Groups[1].Value);
                if (seen.Add(imgUrl)) images.Add(imgUrl);
            }

            // og:image 폴백
            if (images.Count == 0)
            {
                var ogMatch = Regex.Match(html, @"og:image""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
                if (ogMatch.Success)
                {
                    var imgUrl = NormalizeImageUrl(ogMatch.Groups[1].Value);
                    if (seen.Add(imgUrl)) images.Add(imgUrl);
                }
            }

            // 추가 이미지 슬라이드
            var slideMatches = Regex.Matches(html, @"<li[^>]*class=""[^""]*slide[^""]*""[^>]*>.*?<img[^>]+src=""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match m in slideMatches)
            {
                var imgUrl = NormalizeImageUrl(m.Groups[1].Value);
                if (seen.Add(imgUrl)) images.Add(imgUrl);
            }

            return images;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            // GodoMall 상세설명: #detail 또는 .goods_description 영역
            var m = Regex.Match(html, @"<div[^>]+(?:id=""detail""|class=""[^""]*goods_description[^""]*"")[^>]*>(.*?)</div>\s*(?:</div>|<div[^>]+id=""(?:relation|review|qna)"")",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (m.Success)
                return m.Groups[1].Value.Trim();

            // 상세이미지 추출 폴백
            var imgMatches = Regex.Matches(html, @"<img[^>]+src=""(https?://[^""]*(?:detail|desc|content)[^""]*\.(?:jpg|png|gif|webp))[^""]*""", RegexOptions.IgnoreCase);
            if (imgMatches.Count > 0)
            {
                var descHtml = "";
                foreach (Match im in imgMatches)
                    descHtml += $"<img src=\"{im.Groups[1].Value}\" />";
                return descHtml;
            }

            return "";
        }

        public override Option GetOption(string html, JToken data)
        {
            var optionFl = data?["setOptionFl"]?.ToString();

            // 옵션 없음 → 기본 옵션
            if (optionFl == "n" || optionFl == "N" || string.IsNullOrWhiteSpace(optionFl))
                return GetDefaultOption();

            // GodoMall <select name="optionSnoInput[]"> 파싱
            var selectMatch = Regex.Match(html, @"<select[^>]+name=""optionSnoInput\[\]""[^>]*>(.*?)</select>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!selectMatch.Success)
                return GetDefaultOption();

            var selectHtml = selectMatch.Groups[1].Value;
            var optionMatches = Regex.Matches(selectHtml,
                @"<option\s+value=""(\d+)""[^>]*>([^<]+)</option>",
                RegexOptions.IgnoreCase);

            if (optionMatches.Count == 0)
                return GetDefaultOption();

            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();
            var prices = GetPrice(html, data);

            int idx = 1;
            foreach (Match m in optionMatches)
            {
                var optValue = m.Groups[2].Value.Trim();
                if (optValue.Contains("선택하세요") || optValue.Contains("선택") && optValue.Length < 5)
                    continue;

                // 품절 확인
                var isSoldOut = optValue.Contains("품절") || m.Value.Contains("disabled");

                // 추가금 파싱: "옵션명 (+1,000원)" 또는 "옵션명 (-500원)"
                var cleanName = Regex.Replace(optValue, @"\s*\([+-][0-9,]+원\)\s*", "").Trim();
                if (string.IsNullOrWhiteSpace(cleanName)) cleanName = optValue;

                // 품절 텍스트 제거
                cleanName = Regex.Replace(cleanName, @"\s*\[?품절\]?\s*", "").Trim();

                var id = idx.ToString();

                groups.Add(new Option.OptionGroup.Group
                {
                    groupId = id,
                    optionName = ProductHelper.CreateName(Language.KR, cleanName),
                    quantity = isSoldOut ? 0 : 9,
                    optionImage = null,
                    price = prices
                });

                details.Add(new Option.DetailOption
                {
                    optionIds = id,
                    isSoldOut = isSoldOut,
                    optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                    optionName = ProductHelper.CreateName(Language.KR, cleanName),
                    quantity = isSoldOut ? 0 : 9,
                    optionImage = null,
                    price = prices
                });

                idx++;
            }

            if (details.Count == 0)
                return GetDefaultOption();

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

        public override int? GetTotalQuantity(string html, JToken data) => null;

        public override void ThrowIfOutOfStock(string html, JToken data, Product product) { }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            // 추후 필요 시 구현
            await Task.CompletedTask;
        }

        // =========================
        // Helpers
        // =========================
        private static Option GetDefaultOption()
        {
            var defaultPrices = ProductHelper.CreatePrice(Currency.KRW, 0, 0, true);
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
                                groupId = "0",
                                optionName = ProductHelper.CreateName(Language.KR, "원사이즈"),
                                optionImage = null
                            }
                        }
                    }
                },
                detailOptions = new List<Option.DetailOption>
                {
                    new Option.DetailOption
                    {
                        optionIds = "0",
                        optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                        optionName = ProductHelper.CreateName(Language.KR, "원사이즈"),
                        optionImage = null
                    }
                }
            };
        }

        private static string ExtractJsVar(string html, string varName)
        {
            var m = Regex.Match(html, $@"var\s+{varName}\s*=\s*'([^']*)'", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;

            m = Regex.Match(html, $@"var\s+{varName}\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;

            m = Regex.Match(html, $@"var\s+{varName}\s*=\s*([^;\r\n]+)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value.Trim().Trim('\'', '"');

            return null;
        }

        private static string ExtractJsString(string html, string varName)
        {
            // GodoMall은 goodsNm을 HTML 인코딩된 상태로 할당할 수 있음
            var val = ExtractJsVar(html, varName);
            return val != null ? WebUtility.HtmlDecode(val) : null;
        }

        private static string NormalizeImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            url = url.Trim();
            if (url.StartsWith("//")) url = "https:" + url;
            return url;
        }

        private async Task<string> GetByHttpClientAsync(string url)
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

            using var httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,*/*");
            request.Headers.TryAddWithoutValidation("Accept-Language", "ko-KR,ko;q=0.9");
            request.Headers.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");

            using var response = await httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
