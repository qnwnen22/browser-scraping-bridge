#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Common;
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

namespace BrowserScrapingBridge.Server.Sourcing.AbcMart
{
    public class AbcMart : ICollect
    {
        private const string Host = "https://abcmart.a-rt.com";

        public string Site => nameof(AbcMart);
        public string Name => "ABC마트";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;

        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        public Task<string> GetLoginUrlAsync(string connectionId) => Task.FromResult<string>(null);

        // =========================
        // GetCode
        // =========================
        public string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                var uri = new Uri(url);
                var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var prdtNo = q?.Get("prdtNo");
                if (!string.IsNullOrWhiteSpace(prdtNo)) return prdtNo.Trim();
                return null;
            }
            catch { return null; }
        }

        // =========================
        // GetCollectPageInfosAsync
        // =========================
        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "ABC마트 카테고리 URL이 비었습니다.", Url = Host } };

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
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "ABC마트 카테고리 응답이 비었습니다.", Url = url } };

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
        // GetProductAsync
        // =========================
        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var product = CreateSafeSkeleton(url);
            var code = GetCode(url);

            if (string.IsNullOrWhiteSpace(code))
            {
                product.memo = "ABC마트 상품코드(prdtNo)를 URL에서 추출할 수 없습니다.";
                product.collectStatus = ProductCollectStatus.Failed;
                return product;
            }

            product.code = code;
            product.url = $"{Host}/product/new?prdtNo={code}";

            try
            {
                // 1. 상품 정보 JSON (X-Requested-With 필수)
                var ajaxHeaders = GetAjaxHeaders();
                var infoUrl = $"{Host}/product/info?prdtNo={code}&xramdom={new Random().NextDouble()}";
                var infoBody = await RequestAsync(connectionId, infoUrl, referer: product.url, headers: ajaxHeaders);

                if (string.IsNullOrWhiteSpace(infoBody))
                    throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "ABC마트 상품정보 응답이 비었습니다.", Url = product.url } };

                JObject info;
                try { info = JObject.Parse(infoBody); }
                catch
                {
                    throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "ABC마트 상품정보 응답이 JSON이 아닙니다.", Url = product.url } };
                }

                var data = info["data"] ?? info;
                var priceObj = info["price"]
                    ?? info.SelectToken("product.price")
                    ?? data["price"]
                    ?? data["productPrice"];

                // Title
                var prdtName = data["prdtName"]?.ToString()
                    ?? info.SelectToken("product.data.prdtName")?.ToString()
                    ?? "";
                product.title = ProductHelper.CreateName(Language.KR, prdtName);

                // Price: JSON 다중 경로 탐색
                var normalAmt =
                    priceObj?["normalAmt"]?.Value<double?>()
                    ?? info.SelectToken("product.price.normalAmt")?.Value<double?>()
                    ?? data.SelectToken("productPrice.normalAmt")?.Value<double?>()
                    ?? 0d;
                var sellAmt =
                    priceObj?["sellAmt"]?.Value<double?>()
                    ?? priceObj?["displaySellAmt"]?.Value<double?>()
                    ?? info.SelectToken("product.price.sellAmt")?.Value<double?>()
                    ?? data.SelectToken("productPrice.sellAmt")?.Value<double?>()
                    ?? normalAmt;

                // Price fallback: HTML meta 태그
                if (sellAmt <= 0)
                {
                    try
                    {
                        var htmlBody = await RequestAsync(connectionId, product.url, referer: product.url);
                        if (!string.IsNullOrWhiteSpace(htmlBody))
                        {
                            var priceMatch = Regex.Match(htmlBody, @"<meta\s+property=""product:price:amount""\s+content=""(\d+)""", RegexOptions.IgnoreCase);
                            if (priceMatch.Success && double.TryParse(priceMatch.Groups[1].Value, out var metaPrice))
                            {
                                sellAmt = metaPrice;
                                normalAmt = metaPrice;
                            }
                        }
                    }
                    catch { }
                }

                if (normalAmt <= 0) normalAmt = sellAmt;
                product.price = ProductHelper.CreatePrice(Currency, sellAmt, normalAmt, true);

                // Images (productImageExtra 우선, fileType "I"만)
                var images = new List<string>();
                var seenImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var imgExtra in data.SelectTokens("productImageExtra[*]"))
                {
                    var fileType = imgExtra["fileType"]?.ToString();
                    if (fileType != null && !fileType.Equals("I", StringComparison.OrdinalIgnoreCase)) continue;

                    var imgUrl = imgExtra["imageUrl"]?.ToString();
                    if (string.IsNullOrWhiteSpace(imgUrl)) continue;

                    var cleanUrl = Regex.Replace(imgUrl, @"\?shrink=\d+:\d+$", "");
                    if (seenImages.Add(cleanUrl))
                        images.Add(cleanUrl);
                }

                if (images.Count == 0)
                {
                    foreach (var img in data.SelectTokens("productImage[*]"))
                    {
                        var imgUrl = img["imageUrl"]?.ToString();
                        if (string.IsNullOrWhiteSpace(imgUrl)) continue;

                        var cleanUrl = Regex.Replace(imgUrl, @"\?shrink=\d+:\d+$", "");
                        if (seenImages.Add(cleanUrl))
                            images.Add(cleanUrl);
                    }
                }

                product.mainImages = images;

                // Options (사이즈)
                var options = data.SelectTokens("productOption[*]").ToList();
                product.option = BuildOptions(options, product.price);

                // 2. 상품 상세 설명
                try
                {
                    var detailUrl = $"{Host}/product/info/detail?prdtNo={code}";
                    var detailBody = await RequestAsync(connectionId, detailUrl, referer: product.url, headers: ajaxHeaders);
                    if (!string.IsNullOrWhiteSpace(detailBody))
                    {
                        JObject detailJson;
                        try { detailJson = JObject.Parse(detailBody); }
                        catch { detailJson = null; }

                        if (detailJson != null)
                        {
                            var descRaw = detailJson.SelectToken("data.detail.prdtDtlInfo")?.ToString()
                                ?? detailJson.SelectToken("detail.prdtDtlInfo")?.ToString()
                                ?? detailJson["prdtDtlInfo"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(descRaw))
                                product.detailPageHtml = WebUtility.HtmlDecode(descRaw);
                        }
                    }
                }
                catch { /* description 실패 시 무시 */ }

                product.collectStatus = ProductCollectStatus.Success;
                return product;
            }
            catch (CaptchaException) { throw; }
            catch (Exception ex)
            {
                product.memo = ex.Message;
                product.collectStatus = ProductCollectStatus.Failed;
                return product;
            }
        }

        // =========================
        // Option Builder
        // =========================
        private Option BuildOptions(List<JToken> options, List<Price> productPrices)
        {
            if (options == null || options.Count == 0)
                return CreateDefaultOption(productPrices);

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
                    price = productPrices
                });

                details.Add(new Option.DetailOption
                {
                    optionIds = id,
                    isSoldOut = isSoldOut,
                    optionGroupName = ProductHelper.CreateName(Language.KR, "사이즈"),
                    optionName = ProductHelper.CreateName(Language.KR, name),
                    quantity = isSoldOut ? 0 : Math.Min(stock, 9),
                    optionImage = null,
                    price = productPrices
                });

                idx++;
            }

            if (details.Count == 0)
                return CreateDefaultOption(productPrices);

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
        // HTTP Request
        // =========================

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

        // =========================
        // Helpers
        // =========================
        private static Product CreateSafeSkeleton(string url)
        {
            return new Product
            {
                site = nameof(AbcMart),
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

        private Option CreateDefaultOption(List<Price> productPrices)
        {
            var prices = (productPrices != null && productPrices.Count > 0)
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
                                price = prices
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
                        price = prices
                    }
                }
            };
        }

        public async Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            var code = GetCode(url);
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new CaptchaException { CaptchaInfo = new CaptchaInfo { Message = "ABC마트 상품코드(prdtNo)를 URL에서 추출할 수 없습니다.", Url = url } };
            }

            var productUrl = $"{Host}/product/new?prdtNo={code}";
            var ajaxHeaders = GetAjaxHeaders();
            var infoUrl = $"{Host}/product/info?prdtNo={code}&xramdom={new Random().NextDouble()}";

            var html = await RequestAsync(connectionId, productUrl, referer: productUrl);
            var infoBody = await RequestAsync(connectionId, infoUrl, referer: productUrl, headers: ajaxHeaders);

            JToken data = null;
            if (!string.IsNullOrWhiteSpace(infoBody))
            {
                try
                {
                    var info = JObject.Parse(infoBody);
                    data = info["data"] ?? info;
                }
                catch { }
            }

            // 상세 설명도 가져오기
            try
            {
                var detailUrl = $"{Host}/product/info/detail?prdtNo={code}";
                var detailBody = await RequestAsync(connectionId, detailUrl, referer: productUrl, headers: ajaxHeaders);
                if (!string.IsNullOrWhiteSpace(detailBody))
                {
                    try
                    {
                        var detailJson = JObject.Parse(detailBody);
                        var descRaw = detailJson.SelectToken("data.detail.prdtDtlInfo")?.ToString()
                            ?? detailJson.SelectToken("detail.prdtDtlInfo")?.ToString()
                            ?? detailJson["prdtDtlInfo"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(descRaw))
                        {
                            html += "\n<!-- DETAIL -->\n" + WebUtility.HtmlDecode(descRaw);
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return new CollectRawData(url, data, html);
        }

        public Product GetProduct(CollectRawData rawData)
        {
            var product = CreateSafeSkeleton(rawData.Url);
            var code = GetCode(rawData.Url);

            if (string.IsNullOrWhiteSpace(code))
            {
                product.memo = "ABC마트 상품코드(prdtNo)를 URL에서 추출할 수 없습니다.";
                product.collectStatus = ProductCollectStatus.Failed;
                return product;
            }

            product.code = code;
            product.url = $"{Host}/product/new?prdtNo={code}";

            try
            {
                if (rawData.Data == null)
                {
                    product.memo = "ABC마트 상품정보 데이터가 없습니다.";
                    product.collectStatus = ProductCollectStatus.Failed;
                    return product;
                }

                var data = rawData.Data;
                var priceObj = data["price"] ?? data["productPrice"];

                // Title
                var prdtName = data["prdtName"]?.ToString() ?? "";
                product.title = ProductHelper.CreateName(Language.KR, prdtName);

                // Price
                var normalAmt = priceObj?["normalAmt"]?.Value<double?>() ?? 0d;
                var sellAmt = priceObj?["sellAmt"]?.Value<double?>()
                    ?? priceObj?["displaySellAmt"]?.Value<double?>()
                    ?? normalAmt;

                if (normalAmt <= 0) normalAmt = sellAmt;
                product.price = ProductHelper.CreatePrice(Currency, sellAmt, normalAmt, true);

                // Images
                var images = new List<string>();
                var seenImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var imgExtra in data.SelectTokens("productImageExtra[*]"))
                {
                    var fileType = imgExtra["fileType"]?.ToString();
                    if (fileType != null && !fileType.Equals("I", StringComparison.OrdinalIgnoreCase)) continue;

                    var imgUrl = imgExtra["imageUrl"]?.ToString();
                    if (string.IsNullOrWhiteSpace(imgUrl)) continue;

                    var cleanUrl = Regex.Replace(imgUrl, @"\?shrink=\d+:\d+$", "");
                    if (seenImages.Add(cleanUrl))
                        images.Add(cleanUrl);
                }

                if (images.Count == 0)
                {
                    foreach (var img in data.SelectTokens("productImage[*]"))
                    {
                        var imgUrl = img["imageUrl"]?.ToString();
                        if (string.IsNullOrWhiteSpace(imgUrl)) continue;

                        var cleanUrl = Regex.Replace(imgUrl, @"\?shrink=\d+:\d+$", "");
                        if (seenImages.Add(cleanUrl))
                            images.Add(cleanUrl);
                    }
                }

                product.mainImages = images;

                // Options
                var options = data.SelectTokens("productOption[*]").ToList();
                product.option = BuildOptions(options, product.price);

                // Detail HTML
                if (!string.IsNullOrWhiteSpace(rawData.Html))
                {
                    var detailMatch = Regex.Match(rawData.Html, @"<!-- DETAIL -->\n(.+)", RegexOptions.Singleline);
                    if (detailMatch.Success)
                    {
                        product.detailPageHtml = detailMatch.Groups[1].Value;
                    }
                }

                product.collectStatus = ProductCollectStatus.Success;
                return product;
            }
            catch (Exception ex)
            {
                product.memo = ex.Message;
                product.collectStatus = ProductCollectStatus.Failed;
                return product;
            }
        }

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            // ABC마트는 현재 특별한 수집 옵션이 없으므로 기본 구현만 제공
            if (product == null) return;
            if (setting == null) return;
            // 필요시 추가 옵션 설정 로직 구현
            await Task.CompletedTask;
        }
    }
}
