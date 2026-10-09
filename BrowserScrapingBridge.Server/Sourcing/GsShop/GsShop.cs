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
using System.Web;

namespace BrowserScrapingBridge.Server.Sourcing.GsShop
{
    /// <summary>
    /// GS샵(gsshop.com) — 홈쇼핑/종합 이커머스
    /// 카테고리: AJAX HTML API (/shop/sect/sectL/prdList.gs)
    /// 상품상세: var renderJson JS 전역변수 파싱
    /// URL 패턴:
    ///   카테고리: https://www.gsshop.com/shop/sect/sectL.gs?sectid={sectid}&amp;cate_link={cate_link}
    ///   상품: https://www.gsshop.com/prd/prd.gs?prdid={prdid}
    ///   딜: https://www.gsshop.com/deal/deal.gs?dealNo={dealNo}
    /// </summary>
    public class GsShop : CollectBase
    {
        private const string Host = "https://www.gsshop.com";

        private static readonly HttpClient _httpClient;
        static GsShop()
        {
            var handler = new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate };
            _httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            _httpClient.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
        }

        public override string Site => nameof(GsShop);
        public override string Name => "GS샵";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; }

        // ===== ICollect 구현 =====

        public override async Task<string> GetLoginUrlAsync(string connectionId) => null;

        public override async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Referer", Host);
            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        public override string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var uri = new Uri(url);
            var qs = HttpUtility.ParseQueryString(uri.Query);
            return qs["prdid"] ?? qs["dealNo"];
        }

        public override string CreateProductUrl(string code)
            => $"{Host}/prd/prd.gs?prdid={code}";

        // ===== 카테고리 수집 =====

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(
            string connectionId, string url, int page)
        {
            var uri = new Uri(url);
            var qs = HttpUtility.ParseQueryString(uri.Query);
            var sectid = qs["sectid"];
            var cateLink = qs["cate_link"] ?? "";

            if (string.IsNullOrWhiteSpace(sectid))
                throw new CollectException("URL에서 sectid를 추출할 수 없습니다: " + url);

            CollectExtension.CollectSleep();

            var apiUrl = $"{Host}/shop/sect/sectL/prdList.gs?sectid={sectid}&today=&pageIndex={page}&cate_link={cateLink}";
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("Referer", url);
            var response = await _httpClient.SendAsync(request);
            var html = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(html))
                return new List<CollectPageInfo>();

            var result = new List<CollectPageInfo>();

            // <a href="...prdid=..." class="prd-item" data-prdid="...">
            var matches = Regex.Matches(html,
                @"<a\s+href=""([^""]*(?:prdid|dealNo)=(\d+)[^""]*)""[^>]*class=""prd-item""[^>]*data-prdid=""(\d+)""",
                RegexOptions.IgnoreCase);

            foreach (Match m in matches)
            {
                var prdUrl = m.Groups[1].Value;
                var prdId = m.Groups[3].Value;

                if (string.IsNullOrWhiteSpace(prdId)) continue;

                // 제목 추출: 같은 상품 블록 내 <dt class="prd-name">
                var titleMatch = Regex.Match(html.Substring(m.Index),
                    @"<dt\s+class=""prd-name"">\s*(?:<[^>]+>)*\s*(.+?)\s*</dt>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                var title = titleMatch.Success
                    ? Regex.Replace(titleMatch.Groups[1].Value, @"<[^>]+>", "").Trim()
                    : "";

                // URL 정규화
                if (!prdUrl.StartsWith("http"))
                    prdUrl = Host + prdUrl;

                result.Add(new CollectPageInfo
                {
                    Code = prdId,
                    Url = prdUrl,
                    Title = title
                });
            }

            return result;
        }

        // ===== 상품 상세 =====

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new CollectException("GS샵 상품 페이지 응답이 비었습니다.");

            var m = Regex.Match(html,
                @"var\s+renderJson\s*=\s*(\{.*?\});\s*$",
                RegexOptions.Multiline | RegexOptions.Singleline);

            if (!m.Success)
                throw new CollectException("renderJson을 찾을 수 없습니다.");

            var data = JObject.Parse(m.Groups[1].Value);

            // 상세설명 iframe URL 추출 및 이미지 수집
            var prd = data["prd"];
            var prdId = prd?["prdCd"]?.ToString();
            var prdGbnCd = prd?["prdGbnCd"]?.ToString() ?? "60";

            if (!string.IsNullOrWhiteSpace(prdId))
            {
                try
                {
                    var descUrl = $"{Host}/mi15/prd/prdImgDesc.gs?prdid={prdId}&prdGbnCd={prdGbnCd}&subSupCd=&m2UseYn=Y";
                    var req = new HttpRequestMessage(HttpMethod.Get, descUrl);
                    req.Headers.Add("Referer", url);
                    var resp = await _httpClient.SendAsync(req);
                    var descHtml = await resp.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(descHtml))
                        data["descHtml"] = descHtml;
                }
                catch { }
            }

            return data;
        }

        public override void ThrowIfCaptcha(string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new CollectException("GS샵 상품 페이지 응답이 비었습니다.");
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
            var name = data?["prd"]?["exposPrdNm"]?.ToString();
            if (string.IsNullOrWhiteSpace(name))
                throw new CollectException("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(Language.KR, name);
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            var brand = data?["prd"]?["brandNm"]?.ToString();
            if (!string.IsNullOrWhiteSpace(brand))
                return ProductHelper.CreateName(Language.KR, brand);
            return null;
        }

        public override string GetModel(string html, JToken data) => null;
        public override List<Desc> GetDescs(string html, JToken data) => new List<Desc>();
        public override List<Tag> GetTags(string html, JToken data) => new List<Tag>();

        public override List<Price> GetPrice(string html, JToken data)
        {
            var prd = data?["prd"];
            var pmo = data?["pmo"];

            // 판매 상태 확인
            var saleEnd = prd?["saleNoPsblGbn"]?["saleEnd"]?.ToString();
            if (saleEnd == "Y")
                throw new SaleException(ProductSaleStatus.Stopped, "판매 종료된 상품입니다.");

            var prdSaleSt = prd?["prdSaleSt"]?.ToString();
            if (prdSaleSt == "N")
                throw new SaleException(ProductSaleStatus.Stopped, "판매 불가 상품입니다.");

            var tempoutYn = prd?["tempoutYn"]?.ToString();
            if (tempoutYn == "Y")
                throw new SaleException(ProductSaleStatus.SoldOut, "일시 품절 상품입니다.");

            var prc = pmo?["prc"];
            var salePrc = prc?["salePrc"]?.Value<double>() ?? 0;
            var minPrc = prc?["minPrc"]?.Value<double>() ?? 0;

            var finalPrice = minPrc > 0 ? minPrc : salePrc;

            if (finalPrice <= 0)
                throw new CollectException("가격 정보를 찾을 수 없습니다.");

            return ProductHelper.CreatePrice(Currency, finalPrice, finalPrice, true);
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();
            var imgInfo = data?["prd"]?["imgInfo"] as JArray;
            if (imgInfo == null) return images;

            foreach (var img in imgInfo)
            {
                var imgUrl = img["imgLUrl"]?.ToString();
                if (!string.IsNullOrWhiteSpace(imgUrl))
                    images.Add(imgUrl);
            }
            return images;
        }

        public override string GetVideoUrl(string html, JToken data)
        {
            return data?["prd"]?["videoUrl"]?.ToString();
        }

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var descHtml = data?["descHtml"]?.ToString();
            if (string.IsNullOrWhiteSpace(descHtml))
                return "";

            var sb = new StringBuilder();
            var matches = Regex.Matches(descHtml,
                @"<img[^>]+src=[""']([^""'>]+)[""']",
                RegexOptions.IgnoreCase);

            foreach (Match m in matches)
            {
                var imgUrl = m.Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(imgUrl) && !imgUrl.Contains("no_img"))
                    sb.Append($"<img src=\"{imgUrl}\" style=\"max-width:100%;\" />");
            }

            return sb.ToString();
        }

        public override Option GetOption(string html, JToken data)
        {
            var prices = GetPrice(html, data);
            var prd = data?["prd"];
            var attrTypList = prd?["attrTypList"] as JArray;
            var attrTypGrgCnt = prd?["attrTypGrgCnt"]?.Value<int>() ?? 0;
            var optOnlyOne = prd?["optOnlyOne"]?.ToString();

            // 옵션 없음
            if (optOnlyOne == "Y" || attrTypList == null || attrTypList.Count == 0)
                return GetDefaultOption(prices);

            // 2단 옵션 (color × size)
            if (attrTypGrgCnt >= 2)
                return GetMultiOption(attrTypList, prices);

            // 1단 옵션
            return GetSingleOption(attrTypList, prices);
        }

        public override int? GetTotalQuantity(string html, JToken data) => null;

        // ===== 옵션 헬퍼 =====

        private Option GetSingleOption(JArray attrTypList, List<Price> defaultPrices)
        {
            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();

            foreach (var attr in attrTypList)
            {
                var val = attr["attrTypVal"]?.ToString() ?? "";
                var stockFlg = attr["stockFlg"]?.ToString();
                bool isSoldOut = stockFlg == "Y";

                if (string.IsNullOrWhiteSpace(val)) continue;

                groups.Add(new Option.OptionGroup.Group
                {
                    groupId = val,
                    optionName = ProductHelper.CreateName(Language.KR, val)
                });

                details.Add(new Option.DetailOption
                {
                    optionIds = val,
                    isSoldOut = isSoldOut,
                    quantity = isSoldOut ? 0 : 9,
                    optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                    optionName = ProductHelper.CreateName(Language.KR, val),
                    price = defaultPrices
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
                        groupName = ProductHelper.CreateName(Language.KR, "옵션"),
                        groups = groups
                    }
                },
                detailOptions = details
            };
        }

        private Option GetMultiOption(JArray attrTypList, List<Price> defaultPrices)
        {
            var group1Values = new List<string>();
            var group2Values = new List<string>();
            var details = new List<Option.DetailOption>();

            // 옵션 그룹명 결정 (colorVal이 있으면 "컬러", sizeVal이 있으면 "사이즈")
            bool hasColor = attrTypList.Any(a => !string.IsNullOrWhiteSpace(a["colorVal"]?.ToString()));
            bool hasSize = attrTypList.Any(a => !string.IsNullOrWhiteSpace(a["sizeVal"]?.ToString()));
            string grp1Name = hasColor ? "컬러" : "옵션1";
            string grp2Name = hasSize ? "사이즈" : "옵션2";

            foreach (var attr in attrTypList)
            {
                var colorVal = attr["colorVal"]?.ToString() ?? "";
                var sizeVal = attr["sizeVal"]?.ToString() ?? "";
                var stockFlg = attr["stockFlg"]?.ToString();
                bool isSoldOut = stockFlg == "Y";

                var val1 = hasColor ? colorVal : "";
                var val2 = hasSize ? sizeVal : "";

                // attrTypVal에서 fallback (예: "BLK/S")
                if (string.IsNullOrWhiteSpace(val1) || string.IsNullOrWhiteSpace(val2))
                {
                    var attrVal = attr["attrTypVal"]?.ToString() ?? "";
                    if (attrVal.Contains("/"))
                    {
                        var parts = attrVal.Split(new[] { '/' }, 2);
                        if (string.IsNullOrWhiteSpace(val1)) val1 = parts[0].Trim();
                        if (string.IsNullOrWhiteSpace(val2) && parts.Length > 1) val2 = parts[1].Trim();
                    }
                }

                if (string.IsNullOrWhiteSpace(val1) && string.IsNullOrWhiteSpace(val2))
                    continue;

                if (!string.IsNullOrWhiteSpace(val1) && !group1Values.Contains(val1))
                    group1Values.Add(val1);
                if (!string.IsNullOrWhiteSpace(val2) && !group2Values.Contains(val2))
                    group2Values.Add(val2);

                var combinedName = $"{val1}ㅡ{val2}";

                details.Add(new Option.DetailOption
                {
                    optionIds = combinedName,
                    isSoldOut = isSoldOut,
                    quantity = isSoldOut ? 0 : 9,
                    optionGroupName = ProductHelper.CreateName(Language.KR, $"{grp1Name}ㅡ{grp2Name}"),
                    optionName = ProductHelper.CreateName(Language.KR, combinedName),
                    price = defaultPrices
                });
            }

            if (details.Count == 0)
                return GetDefaultOption(defaultPrices);

            var optionGroups = new List<Option.OptionGroup>
            {
                new Option.OptionGroup
                {
                    optionId = "1",
                    groupName = ProductHelper.CreateName(Language.KR, grp1Name),
                    groups = group1Values.Select(v => new Option.OptionGroup.Group
                    {
                        groupId = v,
                        optionName = ProductHelper.CreateName(Language.KR, v)
                    }).ToList()
                },
                new Option.OptionGroup
                {
                    optionId = "2",
                    groupName = ProductHelper.CreateName(Language.KR, grp2Name),
                    groups = group2Values.Select(v => new Option.OptionGroup.Group
                    {
                        groupId = v,
                        optionName = ProductHelper.CreateName(Language.KR, v)
                    }).ToList()
                }
            };

            string ogName = ProductHelper.GetOptionGroupName(optionGroups);
            foreach (var d in details)
                d.optionGroupName = ProductHelper.CreateName(Language.KR, ogName);

            return new Option
            {
                optionGroups = optionGroups,
                detailOptions = details
            };
        }

        private static Option GetDefaultOption(List<Price> prices)
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
                        isSoldOut = false,
                        quantity = 9,
                        optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                        optionName = ProductHelper.CreateName(Language.KR, "원사이즈"),
                        price = prices
                    }
                }
            };
        }
    }
}
