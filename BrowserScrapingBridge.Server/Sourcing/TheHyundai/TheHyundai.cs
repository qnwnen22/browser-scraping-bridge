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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.TheHyundai
{
    /// <summary>
    /// 더현대닷컴 (thehyundai.com)
    /// SSR HTML + POST API 옵션 (/front/pda/selectBsicUitmMst.thd)
    /// </summary>
    public class TheHyundai : CollectBase
    {
        private const string Host = "https://www.thehyundai.com";
        private const string ImageHost = "https://image.thehyundai.com";

        public override string Site => nameof(TheHyundai);
        public override string Name => "더현대";
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

            // slitmCd=40A2158926
            var match = Regex.Match(url, @"slitmCd=([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[1].Value;

            // /front/pda/itemPtc.thd 뒤 마지막 경로 세그먼트
            var pathMatch = Regex.Match(url, @"/([A-Za-z0-9]{10,})(?:\?|$)");
            return pathMatch.Success ? pathMatch.Groups[1].Value : null;
        }

        public override string CreateProductUrl(string code)
            => $"{Host}/front/pda/itemPtc.thd?slitmCd={code}";

        // =========================
        // GetCollectPageInfosAsync
        // =========================
        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            // page 파라미터 교체/추가
            string requestUrl = BuildCategoryUrl(url, page);
            string html = await ChatHubService.GetRequest(connectionId, requestUrl);
            if (string.IsNullOrWhiteSpace(html)) return null;

            var result = new List<CollectPageInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // data-slitmcd='40A2285915:40838' 패턴
            var matches = Regex.Matches(html, @"data-slitmcd='([^:']+)", RegexOptions.IgnoreCase);
            foreach (Match m in matches)
            {
                var code = m.Groups[1].Value.Trim();
                if (string.IsNullOrWhiteSpace(code) || !seen.Add(code)) continue;

                // 제목: class='title ellipsis'> ... </a> (같은 prod-unit 내)
                string title = ExtractTitleNear(html, m.Index);

                result.Add(new CollectPageInfo
                {
                    Code = code,
                    Url = CreateProductUrl(code),
                    Title = title
                });
            }

            if (result.Count == 0) return null;

            CollectExtension.CollectSleep();
            return result;
        }

        private string BuildCategoryUrl(string url, int page)
        {
            // page 파라미터가 이미 있으면 교체
            if (Regex.IsMatch(url, @"[&?]page=\d+", RegexOptions.IgnoreCase))
                return Regex.Replace(url, @"(page=)\d+", $"$1{page}", RegexOptions.IgnoreCase);

            string sep = url.Contains("?") ? "&" : "?";
            return $"{url}{sep}page={page}";
        }

        private string ExtractTitleNear(string html, int matchIndex)
        {
            // data-slitmcd 이후 ~800자 범위에서 title ellipsis 찾기
            int start = matchIndex;
            int end = Math.Min(html.Length, matchIndex + 800);
            string chunk = html.Substring(start, end - start);

            var titleMatch = Regex.Match(chunk, @"class='title\s+ellipsis'[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase);
            if (titleMatch.Success) return WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim());

            var altMatch = Regex.Match(chunk, @"alt=""([^""]{3,})""", RegexOptions.IgnoreCase);
            if (altMatch.Success) return WebUtility.HtmlDecode(altMatch.Groups[1].Value.Trim());

            return null;
        }

        // =========================
        // GetHtmlAsync
        // =========================
        public override async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            return await ChatHubService.GetRequest(connectionId, url);
        }

        // =========================
        // GetData — 옵션 API 호출 포함
        // =========================
        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            var code = GetCode(url);
            if (string.IsNullOrWhiteSpace(code)) return null;

            var data = new JObject();
            data["slitmCd"] = code;

            // 옵션 API: POST /front/pda/selectBsicUitmMst.thd
            try
            {
                var optionUrl = $"{Host}/front/pda/selectBsicUitmMst.thd";
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("slitmCd", code)
                });

                using var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                using var client = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", url);

                var response = await client.PostAsync(optionUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        var optionJson = JObject.Parse(body);
                        data["optionApi"] = optionJson;
                    }
                }
            }
            catch { }

            return data;
        }

        // =========================
        // ThrowIfCaptcha
        // =========================
        public override void ThrowIfCaptcha(string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new CollectException("페이지를 불러올 수 없습니다.");

            if (html.Contains("상품 준비중입니다") || html.Contains("판매가 종료된 상품"))
                throw new SaleException(ProductSaleStatus.Stopped, "판매 종료된 상품입니다.");
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
            // ThrowIfCaptcha에서 이미 SaleException을 던지므로 여기서는 추가 처리 불필요
        }

        // =========================
        // Build Product parts
        // =========================
        public override List<Name> GetTitle(string html, JToken data)
        {
            // og:title
            var ogMatch = Regex.Match(html, @"<meta\s+property=""og:title""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
            if (ogMatch.Success)
            {
                string title = ogMatch.Groups[1].Value.Trim();
                title = Regex.Replace(title, @"\s*-\s*더현대닷컴\s*$", "").Trim();
                if (!string.IsNullOrWhiteSpace(title))
                    return ProductHelper.CreateName(Language.KR, title);
            }

            // <div class="prd-title"><h2>제목</h2>
            var h2Match = Regex.Match(html, @"class=""prd-title""[^>]*>\s*<h2[^>]*>([^<]+)</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (h2Match.Success)
                return ProductHelper.CreateName(Language.KR, h2Match.Groups[1].Value.Trim());

            // <title> 폴백
            var titleMatch = Regex.Match(html, @"<title>([^<]+)</title>", RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                string title = Regex.Replace(titleMatch.Groups[1].Value.Trim(), @"\s*-\s*더현대닷컴\s*$", "").Trim();
                return ProductHelper.CreateName(Language.KR, title);
            }

            throw new CollectException("상품명을 찾을 수 없습니다.");
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            // <f1tg id="f1_brand">아디다스</f1tg>
            var match = Regex.Match(html, @"<f1tg[^>]*id=""f1_brand""[^>]*>([^<]+)</f1tg>", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string brand = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(brand))
                    return ProductHelper.CreateName(Language.KR, brand);
            }

            // <p class="brand"><a ...><img ... alt="브랜드"/>
            var brandImgMatch = Regex.Match(html, @"class=""brand""[^>]*>.*?alt=""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (brandImgMatch.Success)
                return ProductHelper.CreateName(Language.KR, brandImgMatch.Groups[1].Value.Trim());

            return null;
        }

        public override string GetModel(string html, JToken data) => null;
        public override List<Desc> GetDescs(string html, JToken data) => null;
        public override List<Tag> GetTags(string html, JToken data) => null;

        public override List<Price> GetPrice(string html, JToken data)
        {
            double salePrice = 0;
            double originalPrice = 0;

            // <div class="dis-price">179,000<span>원</span>
            var disPriceMatch = Regex.Match(html, @"class=""dis-price""[^>]*>\s*([\d,]+)", RegexOptions.IgnoreCase);
            if (disPriceMatch.Success)
                double.TryParse(disPriceMatch.Groups[1].Value.Replace(",", ""), out salePrice);

            // 원래가: <span class=o-price> 또는 class="o-price"
            var oPriceMatch = Regex.Match(html, @"class=o-price[^>]*>([\d,]+)", RegexOptions.IgnoreCase);
            if (!oPriceMatch.Success)
                oPriceMatch = Regex.Match(html, @"class=""o-price""[^>]*>([\d,]+)", RegexOptions.IgnoreCase);
            if (oPriceMatch.Success)
                double.TryParse(oPriceMatch.Groups[1].Value.Replace(",", ""), out originalPrice);

            // 시중판매가 영역 폴백
            if (salePrice <= 0)
            {
                var priceMatch = Regex.Match(html, @"<div[^>]*class=""prd-price""[^>]*>.*?([\d,]+)\s*<span>원</span>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (priceMatch.Success)
                    double.TryParse(priceMatch.Groups[1].Value.Replace(",", ""), out salePrice);
            }

            if (salePrice <= 0)
                throw new CollectException("가격 정보를 찾을 수 없습니다.");

            if (originalPrice <= 0) originalPrice = salePrice;

            return ProductHelper.CreatePrice(Currency, salePrice, originalPrice, true);
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // cloudzoom-gallery에서 600px 이미지 추출
            var galleryMatches = Regex.Matches(html,
                @"cloudzoom-gallery[^>]*data-cloudzoom=""[^""]*image:\s*'([^']+_600\.[^']+)'",
                RegexOptions.IgnoreCase);

            foreach (Match m in galleryMatches)
            {
                var imgUrl = m.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(imgUrl) && seen.Add(imgUrl))
                    images.Add(imgUrl);
            }

            // 메인 zoom 이미지 폴백
            if (images.Count == 0)
            {
                var mainMatch = Regex.Match(html,
                    @"<img[^>]*class=""cloudzoom""[^>]*src=""([^""]+)""",
                    RegexOptions.IgnoreCase);
                if (mainMatch.Success)
                    images.Add(mainMatch.Groups[1].Value.Trim());
            }

            // og:image 폴백
            if (images.Count == 0)
            {
                var ogMatch = Regex.Match(html, @"<meta\s+property=""og:image""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
                if (ogMatch.Success)
                {
                    // 300 → 600 사이즈 변환
                    string imgUrl = ogMatch.Groups[1].Value.Replace("_300.", "_600.");
                    images.Add(imgUrl);
                }
            }

            if (images.Count == 0)
                throw new CollectException("메인 이미지를 찾을 수 없습니다.");

            return images;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            // 상품 상세 이미지 영역
            var detailMatch = Regex.Match(html,
                @"<div[^>]*id=""prd-detail""[^>]*>(.*?)</div>\s*(?:</div>|<div[^>]*id="")",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (detailMatch.Success && detailMatch.Groups[1].Length > 50)
                return detailMatch.Groups[1].Value;

            return null;
        }

        public override Option GetOption(string html, JToken data)
        {
            var uitmList = data?["optionApi"]?["uitmList"] as JArray;

            // 옵션 API 응답이 없거나 비어있으면 기본 옵션
            if (uitmList == null || uitmList.Count == 0)
                return GetDefaultOption();

            // 단일 옵션 + 이름 없음 → 기본 옵션
            if (uitmList.Count == 1)
            {
                var name = uitmList[0]["uitmTotNm"]?.ToString();
                if (string.IsNullOrWhiteSpace(name) || name == "없음")
                    return GetDefaultOption();
            }

            var prices = GetPrice(html, data);
            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();

            // 옵션 그룹명 결정 (색상+사이즈 또는 사이즈만)
            bool hasColor = uitmList.Any(u => !string.IsNullOrWhiteSpace(u["uitm1AttrNm"]?.ToString()));
            string groupLabel = hasColor ? "옵션" : "사이즈";

            int idx = 0;
            foreach (var uitm in uitmList)
            {
                var optName = uitm["uitmTotNm"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(optName)) continue;

                int stock = uitm["stockCount"]?.Value<int?>() ?? 0;
                bool isSoldOut = stock <= 0;
                string id = idx.ToString();

                groups.Add(new Option.OptionGroup.Group
                {
                    groupId = id,
                    optionName = ProductHelper.CreateName(Language.KR, optName),
                    quantity = isSoldOut ? 0 : Math.Min(stock, 9),
                    price = prices
                });

                details.Add(new Option.DetailOption
                {
                    optionIds = id,
                    isSoldOut = isSoldOut,
                    optionGroupName = ProductHelper.CreateName(Language.KR, groupLabel),
                    optionName = ProductHelper.CreateName(Language.KR, optName),
                    quantity = isSoldOut ? 0 : Math.Min(stock, 9),
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
                        groupName = ProductHelper.CreateName(Language.KR, groupLabel),
                        groups = groups
                    }
                },
                detailOptions = details
            };
        }

        public override int? GetTotalQuantity(string html, JToken data) => null;

        public override Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            return Task.CompletedTask;
        }

        // =========================
        // Helpers
        // =========================
        private static Option GetDefaultOption()
        {
            return new Option
            {
                optionGroups = new List<Option.OptionGroup>
                {
                    new Option.OptionGroup
                    {
                        optionId = "1",
                        groupName = ProductHelper.CreateName(Language.KR, "사이즈"),
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
                        optionGroupName = ProductHelper.CreateName(Language.KR, "사이즈"),
                        optionName = ProductHelper.CreateName(Language.KR, "원사이즈")
                    }
                }
            };
        }
    }
}
