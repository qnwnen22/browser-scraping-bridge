#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Itovic
{
    public class Itovic : CollectBase
    {
        public override string Site => nameof(Itovic);
        public override string Name => "아이토빅";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = false;

        private const string BASE_URL = "https://itovic.com";
        private static readonly HttpClient _httpClient;

        static Itovic()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            _httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ko-KR,ko;q=0.9");
        }

        public override string GetCode(string url)
        {
            // /product/{slug}/{id}/category/...
            var match = Regex.Match(url, @"/product/[^/]+/(\d+)/", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[1].Value;

            // product_no={id}
            match = Regex.Match(url, @"product_no=(\d+)", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[1].Value;

            return url;
        }

        public override string CreateProductUrl(string code)
        {
            return $"{BASE_URL}/product/detail.html?product_no={code}";
        }

        public override Task<string> GetLoginUrlAsync(string connectionId)
        {
            return Task.FromResult<string>(null);
        }

        public override async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        #region 카테고리 수집

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            // page 파라미터 교체/추가
            string requestUrl = Regex.Replace(url, @"[&?]page=\d+", "");
            string separator = requestUrl.Contains("?") ? "&" : "?";
            requestUrl = $"{requestUrl}{separator}page={page}";

            string html = await GetHtmlAsync(connectionId, requestUrl);
            if (string.IsNullOrWhiteSpace(html)) return null;

            var result = new List<CollectPageInfo>();
            var seenCodes = new HashSet<string>();

            // /product/{slug}/{id}/category/{cat}/display/{n}/
            var matches = Regex.Matches(html, @"href=""(/product/[^""]+/(\d+)/category/[^""]+)""", RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                string code = match.Groups[2].Value;
                if (seenCodes.Contains(code)) continue;
                seenCodes.Add(code);

                string productPath = match.Groups[1].Value;
                string title = ExtractTitle(html, match.Index);

                result.Add(new CollectPageInfo
                {
                    Code = code,
                    Url = $"{BASE_URL}{productPath}",
                    Title = title?.Trim()
                });
            }

            if (result.Count == 0) return null;

            CollectExtension.CollectSleep();
            return result;
        }

        private string ExtractTitle(string html, int linkIndex)
        {
            int searchEnd = Math.Min(html.Length, linkIndex + 600);
            string context = html.Substring(linkIndex, searchEnd - linkIndex);

            var altMatch = Regex.Match(context, @"alt=""([^""]{3,})""", RegexOptions.IgnoreCase);
            if (altMatch.Success) return altMatch.Groups[1].Value.Trim();

            var textMatch = Regex.Match(context, @">([^<]{3,})<", RegexOptions.IgnoreCase);
            if (textMatch.Success) return textMatch.Value.Trim('>', '<').Trim();

            return null;
        }

        #endregion

        #region CollectBase 구현

        public override void ThrowIfCaptcha(string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new CollectException("페이지를 불러올 수 없습니다.");

            if (html.Contains("품절된 상품") || html.Contains("삭제된 상품"))
                throw new SaleException(ProductSaleStatus.Stopped, "판매 중지 상품입니다.");
        }

        public override Task<JToken> GetData(string connectionId, string url, string html)
        {
            var data = new JObject();

            // JSON-LD
            var ldMatch = Regex.Match(html,
                @"<script\s+type=""application/ld\+json""[^>]*>\s*(\{.*?\})\s*</script>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (ldMatch.Success)
            {
                try { data["jsonLd"] = JObject.Parse(ldMatch.Groups[1].Value); }
                catch { }
            }

            // Cafe24 JS 전역변수
            ExtractJsVar(html, data, "iProductNo");
            ExtractJsVar(html, data, "product_name");
            ExtractJsVar(html, data, "product_price");
            ExtractJsVar(html, data, "has_option");
            ExtractJsVar(html, data, "stock_number");

            // option_stock_data (nested JSON → balanced brace 파싱)
            var optStartMatch = Regex.Match(html, @"option_stock_data\s*=\s*\{");
            if (optStartMatch.Success)
            {
                int braceStart = optStartMatch.Index + optStartMatch.Length - 1;
                string jsonStr = ExtractBalancedBraces(html, braceStart);
                if (!string.IsNullOrEmpty(jsonStr))
                {
                    try { data["option_stock_data"] = JObject.Parse(jsonStr); }
                    catch { }
                }
            }

            return Task.FromResult<JToken>(data);
        }

        private void ExtractJsVar(string html, JObject data, string varName)
        {
            // var iProductNo = '14738';
            var match = Regex.Match(html, $@"var\s+{Regex.Escape(varName)}\s*=\s*'([^']*)';");
            if (match.Success) { data[varName] = match.Groups[1].Value; return; }

            // var iProductNo = "14738";
            match = Regex.Match(html, $@"var\s+{Regex.Escape(varName)}\s*=\s*""([^""]*)"";");
            if (match.Success) { data[varName] = match.Groups[1].Value; return; }

            // var iProductNo = 14738;
            match = Regex.Match(html, $@"var\s+{Regex.Escape(varName)}\s*=\s*(\d+);");
            if (match.Success) { data[varName] = match.Groups[1].Value; }
        }

        private string ExtractBalancedBraces(string text, int startIdx)
        {
            if (startIdx >= text.Length || text[startIdx] != '{') return null;
            int depth = 0;
            bool inString = false;
            char strChar = '"';
            for (int i = startIdx; i < text.Length; i++)
            {
                char c = text[i];
                if (inString)
                {
                    if (c == '\\') { i++; continue; }
                    if (c == strChar) inString = false;
                    continue;
                }
                if (c == '"' || c == '\'') { inString = true; strChar = c; continue; }
                if (c == '{') depth++;
                else if (c == '}') { depth--; if (depth == 0) return text.Substring(startIdx, i - startIdx + 1); }
            }
            return null;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string title = data?["jsonLd"]?["name"]?.ToString();

            if (string.IsNullOrWhiteSpace(title))
                title = data?["product_name"]?.ToString();

            if (string.IsNullOrWhiteSpace(title))
            {
                var ogMatch = Regex.Match(html, @"<meta\s+property=""og:title""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
                if (ogMatch.Success) title = ogMatch.Groups[1].Value.Trim();
            }

            if (string.IsNullOrWhiteSpace(title))
                throw new CollectException("상품명을 찾을 수 없습니다.");

            return ProductHelper.CreateName(Language.KR, title);
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            string brand = data?["jsonLd"]?["brand"]?["name"]?.ToString();
            if (!string.IsNullOrWhiteSpace(brand))
                return ProductHelper.CreateName(Language.KR, brand);
            return null;
        }

        public override string GetModel(string html, JToken data) => null;
        public override List<Desc> GetDescs(string html, JToken data) => null;
        public override List<Tag> GetTags(string html, JToken data) => null;

        public override List<Price> GetPrice(string html, JToken data)
        {
            double salePrice = 0;

            // product_price = 판매가
            string priceStr = data?["product_price"]?.ToString();
            if (!string.IsNullOrWhiteSpace(priceStr))
                double.TryParse(priceStr.Replace(",", ""), out salePrice);

            // JSON-LD 폴백
            if (salePrice <= 0)
            {
                var offersPrice = data?["jsonLd"]?["offers"]?["price"];
                if (offersPrice != null)
                    double.TryParse(offersPrice.ToString(), out salePrice);
            }

            if (salePrice <= 0)
                throw new CollectException("가격 정보를 찾을 수 없습니다.");

            var price = ProductHelper.CreatePrice(Currency.KRW, salePrice, salePrice, true);
            price[0].isoCode = "KRW";
            return price;
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();

            // JSON-LD images
            var ldImages = data?["jsonLd"]?["image"];
            if (ldImages is JArray arr)
            {
                foreach (var img in arr)
                {
                    string imgUrl = img.ToString();
                    if (!string.IsNullOrWhiteSpace(imgUrl))
                        images.Add(EnsureHttps(imgUrl));
                }
            }
            else if (ldImages != null)
            {
                string imgUrl = ldImages.ToString();
                if (!string.IsNullOrWhiteSpace(imgUrl))
                    images.Add(EnsureHttps(imgUrl));
            }

            // og:image 폴백
            if (images.Count == 0)
            {
                var ogMatch = Regex.Match(html, @"<meta\s+property=""og:image""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
                if (ogMatch.Success)
                    images.Add(EnsureHttps(ogMatch.Groups[1].Value));
            }

            if (images.Count == 0)
                throw new CollectException("메인 이미지를 찾을 수 없습니다.");

            return images;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            // Cafe24 #prdDetail ~ 다음 섹션 사이 추출
            var match = Regex.Match(html,
                @"<div[^>]*id=""prdDetail""[^>]*>([\s\S]+?)<div[^>]*id=""(?:prdReview|prdQnA|prdRelated|anchorBoxId)",
                RegexOptions.IgnoreCase);
            if (match.Success && match.Groups[1].Length > 50)
                return match.Groups[1].Value;

            // #prdDetailContent
            match = Regex.Match(html,
                @"<div[^>]*id=""prdDetailContent(?:Lazy)?""[^>]*>([\s\S]+?)</div>\s*</div>",
                RegexOptions.IgnoreCase);
            if (match.Success && match.Groups[1].Length > 50)
                return match.Groups[1].Value;

            return null;
        }

        public override Option GetOption(string html, JToken data)
        {
            string hasOption = data?["has_option"]?.ToString();

            if (hasOption != "T")
                return GetDefaultOption();

            var optionData = data?["option_stock_data"] as JObject;
            if (optionData == null || !optionData.HasValues)
                return GetDefaultOption();

            // 옵션 구조 판별 (단일/다단계)
            var firstItem = optionData.Properties().FirstOrDefault()?.Value as JObject;
            if (firstItem == null)
                return GetDefaultOption();

            string optionNameField = firstItem["option_name"]?.ToString() ?? "옵션";
            bool isMultiLevel = optionNameField.Contains("|") || optionNameField.Contains("/");

            if (isMultiLevel)
                return GetMultiLevelOption(optionData);
            else
                return GetSingleLevelOption(optionData);
        }

        private Option GetSingleLevelOption(JObject optionData)
        {
            string groupName = "옵션";
            var optionGroup = new Option.OptionGroup
            {
                optionId = "0",
                groupName = new List<Name> { new Name(Language.KR, groupName) },
                groups = new List<Option.OptionGroup.Group>()
            };
            var detailOptions = new List<Option.DetailOption>();

            foreach (var prop in optionData.Properties())
            {
                var item = prop.Value as JObject;
                if (item == null) continue;

                string optName = item["option_value"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(optName)) continue;

                groupName = item["option_name"]?.ToString()?.Trim() ?? "옵션";
                int stockNum = item["stock_number"]?.Value<int>() ?? 0;
                bool isSoldOut = stockNum <= 0;
                double optPrice = item["option_price"]?.Value<double>() ?? 0;

                optionGroup.groups.Add(new Option.OptionGroup.Group
                {
                    groupId = optName,
                    optionName = new List<Name> { new Name(Language.KR, optName) }
                });

                var detailOption = new Option.DetailOption
                {
                    optionIds = optName,
                    optionGroupName = new List<Name> { new Name(Language.KR, groupName) },
                    optionName = new List<Name> { new Name(Language.KR, optName) },
                    isSoldOut = isSoldOut,
                    quantity = isSoldOut ? 0 : Math.Min(stockNum, 999)
                };

                if (optPrice > 0)
                {
                    detailOption.price = ProductHelper.CreatePrice(Currency.KRW, optPrice, optPrice, true);
                    detailOption.price[0].isoCode = "KRW";
                }

                detailOptions.Add(detailOption);
            }

            if (optionGroup.groups.Count == 0)
                return GetDefaultOption();

            optionGroup.groupName = new List<Name> { new Name(Language.KR, groupName) };

            return new Option
            {
                optionGroups = new List<Option.OptionGroup> { optionGroup },
                detailOptions = detailOptions
            };
        }

        private Option GetMultiLevelOption(JObject optionData)
        {
            var firstItem = optionData.Properties().FirstOrDefault()?.Value as JObject;
            string[] groupNames = (firstItem?["option_name"]?.ToString() ?? "옵션1|옵션2")
                .Split(new[] { '|', '/' }, StringSplitOptions.RemoveEmptyEntries);

            var groupValues = new Dictionary<int, List<string>>();

            // 레벨별 고유 값 수집
            foreach (var prop in optionData.Properties())
            {
                var item = prop.Value as JObject;
                if (item == null) continue;
                string[] values = (item["option_value"]?.ToString() ?? "")
                    .Split(new[] { '|', '/' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < values.Length && i < groupNames.Length; i++)
                {
                    if (!groupValues.ContainsKey(i))
                        groupValues[i] = new List<string>();
                    string val = values[i].Trim();
                    if (!groupValues[i].Contains(val))
                        groupValues[i].Add(val);
                }
            }

            // OptionGroup 생성
            var optionGroups = new List<Option.OptionGroup>();
            for (int i = 0; i < groupNames.Length; i++)
            {
                var group = new Option.OptionGroup
                {
                    optionId = (i + 1).ToString(),
                    groupName = new List<Name> { new Name(Language.KR, groupNames[i].Trim()) },
                    groups = new List<Option.OptionGroup.Group>()
                };
                if (groupValues.ContainsKey(i))
                {
                    foreach (string val in groupValues[i])
                    {
                        group.groups.Add(new Option.OptionGroup.Group
                        {
                            groupId = val,
                            optionName = new List<Name> { new Name(Language.KR, val) }
                        });
                    }
                }
                optionGroups.Add(group);
            }

            // DetailOption 생성
            var detailOptions = new List<Option.DetailOption>();
            string optionGroupName = ProductHelper.GetOptionGroupName(optionGroups);

            foreach (var prop in optionData.Properties())
            {
                var item = prop.Value as JObject;
                if (item == null) continue;

                string[] values = (item["option_value"]?.ToString() ?? "")
                    .Split(new[] { '|', '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim()).ToArray();

                string combinedName = string.Join("ㅡ", values);
                int stockNum = item["stock_number"]?.Value<int>() ?? 0;
                bool isSoldOut = stockNum <= 0;
                double optPrice = item["option_price"]?.Value<double>() ?? 0;

                var detailOption = new Option.DetailOption
                {
                    optionIds = combinedName,
                    optionGroupName = new List<Name> { new Name(Language.KR, optionGroupName) },
                    optionName = new List<Name> { new Name(Language.KR, combinedName) },
                    isSoldOut = isSoldOut,
                    quantity = isSoldOut ? 0 : Math.Min(stockNum, 999)
                };

                if (optPrice > 0)
                {
                    detailOption.price = ProductHelper.CreatePrice(Currency.KRW, optPrice, optPrice, true);
                    detailOption.price[0].isoCode = "KRW";
                }

                detailOptions.Add(detailOption);
            }

            return new Option
            {
                optionGroups = optionGroups,
                detailOptions = detailOptions
            };
        }

        public override int? GetTotalQuantity(string html, JToken data)
        {
            string stockStr = data?["stock_number"]?.ToString();
            if (!string.IsNullOrWhiteSpace(stockStr) && int.TryParse(stockStr, out int stock))
                return Math.Min(stock, 999);
            return null;
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product) { }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            // detailOption.price null 방지 (CheckCollectLimit에서 .price.First() 호출)
            if (product.option?.detailOptions != null && product.price != null)
            {
                foreach (var detailOption in product.option.detailOptions)
                {
                    if (detailOption.price == null)
                        detailOption.price = product.price.Select(p => new Price(p.currency, p.originalPrice, p.salePrice)).ToList();
                }
            }
            await Task.CompletedTask;
        }

        #endregion

        #region 헬퍼

        private string EnsureHttps(string url)
        {
            if (url.StartsWith("//"))
                return "https:" + url;
            return url;
        }

        private Option GetDefaultOption()
        {
            var groupName = new Name(Language.KR, "사이즈");
            var optionName = new Name(Language.KR, "원사이즈");

            var optionGroup = new Option.OptionGroup
            {
                optionId = "0",
                groupName = new List<Name> { groupName },
                groups = new List<Option.OptionGroup.Group>
                {
                    new Option.OptionGroup.Group
                    {
                        groupId = "0",
                        optionName = new List<Name> { optionName }
                    }
                }
            };

            var detailOption = new Option.DetailOption
            {
                optionIds = "0",
                optionGroupName = new List<Name> { groupName },
                optionName = new List<Name> { optionName }
            };

            return new Option
            {
                optionGroups = new List<Option.OptionGroup> { optionGroup },
                detailOptions = new List<Option.DetailOption> { detailOption }
            };
        }

        #endregion
    }
}
