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

namespace BrowserScrapingBridge.Server.Sourcing.GrandStage
{
    /// <summary>
    /// 그랜드스테이지 (grandstage.a-rt.com) — a-rt.com 플랫폼, ABC마트와 동일 구조
    /// JSON API 기반 (/product/info, /product/info/detail)
    /// </summary>
    public class GrandStage : CollectBase
    {
        private const string Host = "https://grandstage.a-rt.com";

        public override string Site => nameof(GrandStage);
        public override string Name => "그랜드스테이지";
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
            try
            {
                var uri = new Uri(url);
                var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var prdtNo = q?.Get("prdtNo");
                return string.IsNullOrWhiteSpace(prdtNo) ? null : prdtNo.Trim();
            }
            catch { return null; }
        }

        public override string CreateProductUrl(string code)
            => $"{Host}/product/new?prdtNo={code}";

        // =========================
        // GetCollectPageInfosAsync
        // =========================
        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "그랜드스테이지 카테고리 URL이 비었습니다.", Url = Host } };

            if (page < 1) page = 1;

            var uri = new Uri(url);
            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var ctgrNo = queryParams?.Get("ctgrNo");

            if (string.IsNullOrWhiteSpace(ctgrNo))
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "카테고리 번호(ctgrNo)를 URL에서 찾을 수 없습니다.", Url = url } };

            var apiUrl = $"{Host}/display/category/product/list?ctgrNo={ctgrNo}&pagingSortType=point&rowsPerPage=40&pageNum={page}";

            var genderCode = queryParams?.Get("genderGbnCode");
            if (!string.IsNullOrWhiteSpace(genderCode))
                apiUrl += $"&genderGbnCode={genderCode}";

            var body = await RequestAsync(connectionId, apiUrl, referer: url);

            if (string.IsNullOrWhiteSpace(body))
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "그랜드스테이지 카테고리 응답이 비었습니다.", Url = url } };

            var result = new List<CollectPageInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var itemMatches = Regex.Matches(body, @"<li\s[^>]*data-product-no=""(\d+)""[^>]*>", RegexOptions.IgnoreCase);

            foreach (Match m in itemMatches)
            {
                var prdtNo = m.Groups[1].Value;
                if (!seen.Add(prdtNo)) continue;

                var startIdx = m.Index;
                var chunk = body.Substring(startIdx, Math.Min(2000, body.Length - startIdx));

                var nameMatch = Regex.Match(chunk, @"<span\s+class=""prod-name"">([^<]+)</span>", RegexOptions.IgnoreCase);
                var title = nameMatch.Success ? WebUtility.HtmlDecode(nameMatch.Groups[1].Value.Trim()) : "";

                result.Add(new CollectPageInfo
                {
                    Code = prdtNo,
                    Url = $"{Host}/product/new?prdtNo={prdtNo}",
                    Title = title
                });
            }

            return result;
        }

        // =========================
        // GetHtmlAsync — 상품 페이지 HTML (가격 meta 폴백용)
        // =========================
        public override async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            return await RequestAsync(connectionId, url, referer: url);
        }

        // =========================
        // GetData — JSON API 호출하여 상품 데이터 + 상세설명 조합
        // =========================
        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            var code = GetCode(url);
            if (string.IsNullOrWhiteSpace(code)) return null;

            var ajaxHeaders = GetAjaxHeaders();

            // 1. 상품 정보 API
            var infoUrl = $"{Host}/product/info?prdtNo={code}&xramdom={new Random().NextDouble()}";
            var infoBody = await RequestAsync(connectionId, infoUrl, referer: url, headers: ajaxHeaders);

            if (string.IsNullOrWhiteSpace(infoBody))
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "그랜드스테이지 상품정보 응답이 비었습니다.", Url = url } };

            JObject info;
            try { info = JObject.Parse(infoBody); }
            catch
            {
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "그랜드스테이지 상품정보 응답이 JSON이 아닙니다.", Url = url } };
            }

            // 2. 상세설명 API
            try
            {
                var detailUrl = $"{Host}/product/info/detail?prdtNo={code}";
                var detailBody = await RequestAsync(connectionId, detailUrl, referer: url, headers: ajaxHeaders);
                if (!string.IsNullOrWhiteSpace(detailBody))
                {
                    var detailJson = JObject.Parse(detailBody);
                    info["_detail"] = detailJson;
                }
            }
            catch { }

            // html도 포함 (가격 meta 폴백용)
            info["_html"] = html;

            return info;
        }

        // =========================
        // ThrowIfCaptcha
        // =========================
        public override void ThrowIfCaptcha(string url, string html)
        {
            // a-rt.com은 봇 차단 없음
        }

        // =========================
        // Build Product parts
        // =========================
        public override List<Name> GetTitle(string html, JToken data)
        {
            var d = data?["data"] ?? data;
            var prdtName = d?["prdtName"]?.ToString()
                ?? data?.SelectToken("product.data.prdtName")?.ToString()
                ?? "";
            return ProductHelper.CreateName(Language.KR, prdtName);
        }

        public override List<Name> GetBrand(string html, JToken data) => null;
        public override string GetModel(string html, JToken data) => null;
        public override List<Desc> GetDescs(string html, JToken data) => new List<Desc>();
        public override List<Tag> GetTags(string html, JToken data) => new List<Tag>();

        public override List<Price> GetPrice(string html, JToken data)
        {
            var d = data?["data"] ?? data;
            var priceObj = data?["price"]
                ?? data?.SelectToken("product.price")
                ?? d?["price"]
                ?? d?["productPrice"];

            var normalAmt =
                priceObj?["normalAmt"]?.Value<double?>()
                ?? data?.SelectToken("product.price.normalAmt")?.Value<double?>()
                ?? d?.SelectToken("productPrice.normalAmt")?.Value<double?>()
                ?? 0d;
            var sellAmt =
                priceObj?["sellAmt"]?.Value<double?>()
                ?? priceObj?["displaySellAmt"]?.Value<double?>()
                ?? data?.SelectToken("product.price.sellAmt")?.Value<double?>()
                ?? d?.SelectToken("productPrice.sellAmt")?.Value<double?>()
                ?? normalAmt;

            // HTML meta 폴백
            if (sellAmt <= 0)
            {
                var rawHtml = data?["_html"]?.ToString() ?? html;
                if (!string.IsNullOrWhiteSpace(rawHtml))
                {
                    var priceMatch = Regex.Match(rawHtml, @"product:price:amount""\s+content=""(\d+)""", RegexOptions.IgnoreCase);
                    if (priceMatch.Success && double.TryParse(priceMatch.Groups[1].Value, out var metaPrice))
                    {
                        sellAmt = metaPrice;
                        normalAmt = metaPrice;
                    }
                }
            }

            if (normalAmt <= 0) normalAmt = sellAmt;
            return ProductHelper.CreatePrice(Currency, sellAmt, normalAmt, true);
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var d = data?["data"] ?? data;
            var images = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // productImageExtra (fileType "I"만)
            foreach (var imgExtra in d?.SelectTokens("productImageExtra[*]") ?? Enumerable.Empty<JToken>())
            {
                var fileType = imgExtra["fileType"]?.ToString();
                if (fileType != null && !fileType.Equals("I", StringComparison.OrdinalIgnoreCase)) continue;

                var imgUrl = imgExtra["imageUrl"]?.ToString();
                if (string.IsNullOrWhiteSpace(imgUrl)) continue;

                var cleanUrl = Regex.Replace(imgUrl, @"\?shrink=\d+:\d+$", "");
                if (seen.Add(cleanUrl)) images.Add(cleanUrl);
            }

            // productImage 폴백
            if (images.Count == 0)
            {
                foreach (var img in d?.SelectTokens("productImage[*]") ?? Enumerable.Empty<JToken>())
                {
                    var imgUrl = img["imageUrl"]?.ToString();
                    if (string.IsNullOrWhiteSpace(imgUrl)) continue;

                    var cleanUrl = Regex.Replace(imgUrl, @"\?shrink=\d+:\d+$", "");
                    if (seen.Add(cleanUrl)) images.Add(cleanUrl);
                }
            }

            return images;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var detail = data?["_detail"];
            if (detail == null) return "";

            var descRaw = detail.SelectToken("data.detail.prdtDtlInfo")?.ToString()
                ?? detail.SelectToken("detail.prdtDtlInfo")?.ToString()
                ?? detail["prdtDtlInfo"]?.ToString();

            return string.IsNullOrWhiteSpace(descRaw) ? "" : WebUtility.HtmlDecode(descRaw);
        }

        public override Option GetOption(string html, JToken data)
        {
            var d = data?["data"] ?? data;
            var options = d?.SelectTokens("productOption[*]")?.ToList();

            if (options == null || options.Count == 0)
                return GetDefaultOption();

            var prices = GetPrice(html, data);
            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();
            int idx = 1;

            foreach (var opt in options)
            {
                var name = opt["optnName"]?.ToString() ?? $"옵션{idx}";
                var stock = opt["orderPsbltQty"]?.Value<int?>() ?? 0;
                var isSoldOut = stock <= 0;
                var id = idx.ToString();

                groups.Add(new Option.OptionGroup.Group
                {
                    groupId = id,
                    optionName = ProductHelper.CreateName(Language.KR, name),
                    quantity = isSoldOut ? 0 : Math.Min(stock, 9),
                    optionImage = null,
                    price = prices
                });

                details.Add(new Option.DetailOption
                {
                    optionIds = id,
                    isSoldOut = isSoldOut,
                    optionGroupName = ProductHelper.CreateName(Language.KR, "사이즈"),
                    optionName = ProductHelper.CreateName(Language.KR, name),
                    quantity = isSoldOut ? 0 : Math.Min(stock, 9),
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
                        groupName = ProductHelper.CreateName(Language.KR, "사이즈"),
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
            await Task.CompletedTask;
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

        private static Dictionary<string, string> GetAjaxHeaders()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Accept"] = "application/json, text/javascript, */*; q=0.01",
                ["Accept-Language"] = "ko-KR,ko;q=0.9",
                ["X-Requested-With"] = "XMLHttpRequest",
                ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36",
            };
        }

        private async Task<string> RequestAsync(string connectionId, string requestUrl, string referer = null, Dictionary<string, string> headers = null)
        {
            try
            {
                var body = headers != null
                    ? await ChatHubService.GetRequest(connectionId, requestUrl, headers)
                    : await ChatHubService.GetRequest(connectionId, requestUrl);
                if (!string.IsNullOrWhiteSpace(body)) return body;
            }
            catch { }

            return await GetByHttpClientAsync(requestUrl, referer, headers);
        }

        private async Task<string> GetByHttpClientAsync(string url, string referer = null, Dictionary<string, string> headers = null)
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

            request.Headers.TryAddWithoutValidation("Accept", "*/*");
            request.Headers.TryAddWithoutValidation("Accept-Language", "ko-KR,ko;q=0.9");
            request.Headers.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");

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
    }
}
