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

namespace BrowserScrapingBridge.Server.Sourcing.Makestar
{
    public class Makestar : CollectBase
    {
        public override string Site => nameof(Makestar);
        public override string Name => "메이크스타";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = false;

        private const string API_BASE = "https://new-commerce-api.makestar.com";
        private const string WEB_BASE = "https://www.makestar.com";

        private static readonly HttpClient _httpClient;

        static Makestar()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            _httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public override string GetCode(string url)
        {
            var match = Regex.Match(url, @"/product/(\d+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : url;
        }

        public override string CreateProductUrl(string code)
        {
            return $"{WEB_BASE}/product/{code}";
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
            var apiUrl = $"{API_BASE}/v1/commerce/product_event/list_shop_product/?page={page}&size=20&market_type=2&base_currency=krw";

            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(json);

            var list = data["product_event_list"] as JArray;
            if (list == null || list.Count == 0) return null;

            var result = new List<CollectPageInfo>();
            foreach (var item in list)
            {
                string id = item["id"]?.ToString();
                if (string.IsNullOrEmpty(id)) continue;

                string title = item["title"]?["ko"]?.ToString()
                    ?? item["title"]?["en"]?.ToString();

                result.Add(new CollectPageInfo
                {
                    Code = id,
                    Url = $"{WEB_BASE}/product/{id}",
                    Title = title?.Trim()
                });
            }

            CollectExtension.CollectSleep();
            return result.Count > 0 ? result : null;
        }

        #endregion

        #region CollectBase 구현

        public override void ThrowIfCaptcha(string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new CollectException("API 응답을 받을 수 없습니다.");
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
            // GetData에서 이미 SaleException을 던지므로 여기서는 추가 처리 불필요
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            string code = GetCode(url);
            var apiUrl = $"{API_BASE}/v1/commerce/product_event/{code}/";

            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
                throw new CollectException($"상품 API 호출 실패: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(json);
            var pe = data["product_event"];

            if (pe == null)
                throw new CollectException("상품 데이터를 찾을 수 없습니다.");

            // event_status: 1=ONGOING, 5=END, 6=SOLD_OUT
            int status = pe["event_status"]?.Value<int>() ?? 0;
            if (status == 6)
                throw new SaleException(ProductSaleStatus.SoldOut, "품절된 상품입니다.");
            if (status == 5)
                throw new SaleException(ProductSaleStatus.Stopped, "판매 종료된 상품입니다.");
            if (status != 1 && status != 8)
                throw new SaleException(ProductSaleStatus.Stopped, $"판매 중이 아닌 상품입니다. (status={status})");

            return pe;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string title = data?["title"]?["ko"]?.ToString()
                ?? data?["title"]?["en"]?.ToString();

            if (string.IsNullOrWhiteSpace(title))
                throw new CollectException("상품명을 찾을 수 없습니다.");

            return ProductHelper.CreateName(Language.KR, title.Trim());
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            string artist = data?["product"]?["artist"]?["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(artist))
                artist = data?["product"]?["artist"]?["i18n_name"]?["ko"]?.ToString();

            if (!string.IsNullOrWhiteSpace(artist))
                return ProductHelper.CreateName(Language.KR, artist.Trim());

            return null;
        }

        public override string GetModel(string html, JToken data) => null;
        public override List<Desc> GetDescs(string html, JToken data) => null;
        public override List<Tag> GetTags(string html, JToken data) => null;

        public override List<Price> GetPrice(string html, JToken data)
        {
            // 첫 번째 옵션의 가격을 대표 가격으로 사용
            var options = data?["sales_data"]?["product_options"] as JArray;
            double salePrice = 0;

            if (options != null && options.Count > 0)
            {
                salePrice = options[0]?["sales_data"]?["price"]?["krw"]?.Value<double>() ?? 0;
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

            // images[].image.origin_url
            var imageList = data?["images"] as JArray;
            if (imageList != null)
            {
                foreach (var item in imageList)
                {
                    string url = item?["image"]?["origin_url"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(url))
                        images.Add(url);
                }
            }

            // base_info 내 이미지 추출 (폴백)
            if (images.Count == 0)
            {
                var baseInfo = data?["display_data"]?["base_info"] as JArray;
                if (baseInfo != null)
                {
                    foreach (var info in baseInfo)
                    {
                        string content = info?["content"]?["ko"]?.ToString() ?? "";
                        var imgMatches = Regex.Matches(content, @"<img[^>]+src=""([^""]+)""", RegexOptions.IgnoreCase);
                        foreach (Match m in imgMatches)
                        {
                            string imgUrl = m.Groups[1].Value;
                            if (!string.IsNullOrWhiteSpace(imgUrl) && !images.Contains(imgUrl))
                                images.Add(imgUrl);
                        }
                    }
                }
            }

            if (images.Count == 0)
                throw new CollectException("메인 이미지를 찾을 수 없습니다.");

            return images;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var parts = new List<string>();

            // album_info — 앨범 구성품 정보
            var albumInfo = data?["display_data"]?["album_info"] as JArray;
            if (albumInfo != null)
            {
                foreach (var info in albumInfo)
                {
                    if (info?["is_displayed"]?.Value<bool>() == false) continue;
                    string content = info?["content"]?["ko"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(content))
                        parts.Add(content);
                }
            }

            // base_info — 상세 설명
            var baseInfo = data?["display_data"]?["base_info"] as JArray;
            if (baseInfo != null)
            {
                foreach (var info in baseInfo)
                {
                    if (info?["is_displayed"]?.Value<bool>() == false) continue;
                    string content = info?["content"]?["ko"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(content))
                        parts.Add(content);
                }
            }

            return parts.Count > 0 ? string.Join("", parts) : null;
        }

        public override Option GetOption(string html, JToken data)
        {
            var options = data?["sales_data"]?["product_options"] as JArray;

            if (options == null || options.Count == 0)
                return GetDefaultOption();

            // 옵션이 1개이고 이름이 상품명과 동일하면 → 기본 옵션
            if (options.Count == 1)
            {
                string optName = options[0]?["name"]?["ko"]?.ToString()?.Trim();
                string title = data?["title"]?["ko"]?.ToString()?.Trim();
                if (optName == title || string.IsNullOrWhiteSpace(optName))
                    return GetDefaultOption();
            }

            // 단일 옵션 그룹 (버전 선택)
            string groupName = "옵션";
            var optionGroup = new Option.OptionGroup
            {
                optionId = "0",
                groupName = new List<Name> { new Name(Language.KR, groupName) },
                groups = new List<Option.OptionGroup.Group>()
            };
            var detailOptions = new List<Option.DetailOption>();

            foreach (var opt in options)
            {
                string optName = opt?["name"]?["ko"]?.ToString()?.Trim()
                    ?? opt?["name"]?["en"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(optName)) continue;

                double optPrice = opt?["sales_data"]?["price"]?["krw"]?.Value<double>() ?? 0;

                optionGroup.groups.Add(new Option.OptionGroup.Group
                {
                    groupId = optName,
                    optionName = new List<Name> { new Name(Language.KR, optName) }
                });

                var prices = ProductHelper.CreatePrice(Currency.KRW, optPrice, optPrice, true);
                prices[0].isoCode = "KRW";

                detailOptions.Add(new Option.DetailOption
                {
                    optionIds = optName,
                    optionGroupName = new List<Name> { new Name(Language.KR, groupName) },
                    optionName = new List<Name> { new Name(Language.KR, optName) },
                    isSoldOut = false,
                    quantity = 9,
                    price = prices
                });
            }

            if (optionGroup.groups.Count == 0)
                return GetDefaultOption();

            return new Option
            {
                optionGroups = new List<Option.OptionGroup> { optionGroup },
                detailOptions = detailOptions
            };
        }

        public override int? GetTotalQuantity(string html, JToken data) => null;

        public override Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region 헬퍼

        private Option GetDefaultOption()
        {
            var groupName = new Name(Language.KR, "옵션");
            var optionName = new Name(Language.KR, "원사이즈");

            return new Option
            {
                optionGroups = new List<Option.OptionGroup>
                {
                    new Option.OptionGroup
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
                    }
                },
                detailOptions = new List<Option.DetailOption>
                {
                    new Option.DetailOption
                    {
                        optionIds = "0",
                        optionGroupName = new List<Name> { groupName },
                        optionName = new List<Name> { optionName },
                        isSoldOut = false,
                        quantity = 9
                    }
                }
            };
        }

        #endregion
    }
}
