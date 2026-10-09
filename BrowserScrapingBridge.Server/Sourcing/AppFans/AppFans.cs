#nullable disable warnings
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.AppFans
{
    /// <summary>
    /// app.fans — K-pop 아티스트 공식 굿즈 쇼핑몰 (JYP Entertainment)
    /// GraphQL API 기반 수집 (https://api.app.fans/graphql)
    /// URL 패턴:
    ///   카테고리: https://app.fans/shop/{shopCode}/category/{category}
    ///   아티스트: https://app.fans/shop/{shopCode} (전체 카테고리)
    ///   상품상세: https://app.fans/shop/{slug} (globalProduct slug)
    /// </summary>
    public class AppFans : CollectBase
    {
        private const string ApiUrl = "https://api.app.fans/graphql";
        private const string BaseUrl = "https://app.fans";
        private const int PageSize = 100;

        public override string Site => nameof(AppFans);
        public override string Name => "팬즈";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; }

        private static readonly HttpClient _httpClient = CreateHttpClient();

        // 페이지네이션 커서 캐시 (cursor-based pagination)
        private static string _lastEndCursor;
        private static string _lastCacheKey;

        // 샵 코드 → globalShop ID 캐시
        private static Dictionary<string, string> _shopIdCache;

        private static HttpClient CreateHttpClient()
        {
            var client = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient();
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("j-client-version", "2.2605.3");
            client.DefaultRequestHeaders.Add("j-context", "web");
            client.DefaultRequestHeaders.Add("j-country-code", "KR");
            client.DefaultRequestHeaders.Add("j-currency", "KRW");
            client.DefaultRequestHeaders.Add("j-guid", Guid.NewGuid().ToString());
            client.DefaultRequestHeaders.Add("j-language", "ko");
            client.DefaultRequestHeaders.Add("j-marketplace", "KR");
            client.DefaultRequestHeaders.Add("j-operation-type", "query");
            client.DefaultRequestHeaders.Add("j-timezone", "Asia/Seoul");
            client.DefaultRequestHeaders.Add("Origin", "https://app.fans");
            client.DefaultRequestHeaders.Add("Referer", "https://app.fans/");
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
            return client;
        }

        // ===== GraphQL 호출 =====

        private async Task<JToken> GraphqlAsync(object requestBody)
        {
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(ApiUrl, content);
            var body = await response.Content.ReadAsStringAsync();
            return JToken.Parse(body);
        }

        /// <summary>
        /// 샵 코드(twice, itzy 등) → globalShop ID 조회. 전체 샵 목록을 캐시.
        /// </summary>
        private async Task<string> GetShopIdAsync(string shopCode)
        {
            if (_shopIdCache != null && _shopIdCache.TryGetValue(shopCode, out var cached))
                return cached;

            var result = await GraphqlAsync(new
            {
                operationName = "GlobalShops",
                variables = new { filter = new { } },
                query = "query GlobalShops($filter: GlobalShopFilterInput) { globalShops(filter: $filter) { objects { id code name } } }"
            });

            _shopIdCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var shops = result?["data"]?["globalShops"]?["objects"];
            if (shops != null)
            {
                foreach (var shop in shops)
                {
                    var code = shop["code"]?.ToString();
                    var id = shop["id"]?.ToString();
                    if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(id))
                        _shopIdCache[code] = id;
                }
            }

            return _shopIdCache.TryGetValue(shopCode, out var shopId) ? shopId : null;
        }

        /// <summary>
        /// 카테고리 URL 파싱
        /// /shop/twice → ("twice", null)
        /// /shop/twice/category/merch → ("twice", "merch")
        /// </summary>
        private static (string shopCode, string categoryCode) ParseCategoryUrl(string url)
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Trim('/').Split('/');

            string shopCode = null;
            string categoryCode = null;

            if (segments.Length >= 2 && segments[0].Equals("shop", StringComparison.OrdinalIgnoreCase))
            {
                shopCode = segments[1];
                if (segments.Length >= 4 && segments[2].Equals("category", StringComparison.OrdinalIgnoreCase))
                    categoryCode = segments[3];
            }

            return (shopCode, categoryCode);
        }

        // ===== ICollect 구현 =====

        public override async Task<string> GetLoginUrlAsync(string connectionId) => null;

        public override string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            // /shop/i/product/{slug} → slug
            // /shop/{slug} → slug (하위호환)
            return segments.Length >= 2 ? segments.Last() : null;
        }

        public override string CreateProductUrl(string code) => $"{BaseUrl}/shop/i/product/{code}?mp=KR";

        // ===== 카테고리 수집 =====

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(
            string connectionId, string url, int page)
        {
            var (shopCode, categoryCode) = ParseCategoryUrl(url);
            if (string.IsNullOrWhiteSpace(shopCode))
                throw new CollectException("URL에서 샵 코드를 추출할 수 없습니다: " + url);

            var shopId = await GetShopIdAsync(shopCode);
            if (string.IsNullOrWhiteSpace(shopId))
                throw new CollectException($"'{shopCode}' 샵을 찾을 수 없습니다.");

            // 커서 관리
            var cacheKey = $"{shopId}_{categoryCode}";
            if (page <= 1)
            {
                _lastEndCursor = null;
                _lastCacheKey = cacheKey;
            }
            else if (_lastCacheKey != cacheKey)
            {
                _lastEndCursor = null;
                _lastCacheKey = cacheKey;
            }

            // 이전 페이지에서 hasNext=false였으면 빈 목록 반환
            if (page > 1 && string.IsNullOrWhiteSpace(_lastEndCursor))
                return new List<CollectPageInfo>();

            // GraphQL filter
            var filter = new JObject
            {
                ["globalShop_Overlap"] = new JArray(shopId),
                ["listingStatus_Exact"] = "LISTED"
            };
            if (!string.IsNullOrWhiteSpace(categoryCode))
                filter["products_MasterTags_NormalizedCode_LookUp"] = categoryCode;

            // 페이지네이션
            var pageObj = new JObject { ["first"] = PageSize };
            if (!string.IsNullOrWhiteSpace(_lastEndCursor))
                pageObj["after"] = _lastEndCursor;

            var sort = new JArray
            {
                new JObject { ["direction"] = "ASC", ["type"] = "RETAIL_STATUS_SCORE" },
                new JObject { ["type"] = "FIRST_LISTED_AT", ["direction"] = "DESC" }
            };

            const string query = @"query GlobalProducts($filter: GlobalProductFilterInput, $sort: [GlobalProductSortInput], $page: PageInput) {
  globalProducts(filter: $filter, sort: $sort, page: $page) {
    objects {
      id
      representativeProduct {
        id
        name
        globalProduct { id slug }
      }
    }
    count { value }
    page { endCursor hasNext }
  }
}";

            CollectExtension.CollectSleep();
            var result = await GraphqlAsync(new
            {
                operationName = "GlobalProducts",
                variables = new { filter, sort, page = pageObj },
                query
            });

            // 에러 체크
            var errors = result?["errors"];
            if (errors != null && errors.HasValues)
                throw new CollectException("팬즈 카테고리 API 오류: " + errors.First?["message"]);

            var products = result?["data"]?["globalProducts"]?["objects"];
            var pageInfo = result?["data"]?["globalProducts"]?["page"];

            // 커서 업데이트
            var hasNext = pageInfo?["hasNext"]?.Value<bool>() ?? false;
            _lastEndCursor = hasNext ? pageInfo?["endCursor"]?.ToString() : null;

            var list = new List<CollectPageInfo>();
            if (products == null) return list;

            foreach (var product in products)
            {
                var rep = product["representativeProduct"];
                var slug = rep?["globalProduct"]?["slug"]?.ToString();
                var name = rep?["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(slug)) continue;

                list.Add(new CollectPageInfo
                {
                    Code = slug,
                    Url = CreateProductUrl(slug),
                    Title = name ?? ""
                });
            }

            return list;
        }

        // ===== 상품 상세 =====

        public override async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            var slug = GetCode(url);
            if (string.IsNullOrWhiteSpace(slug))
                throw new CollectException("URL에서 상품 slug를 추출할 수 없습니다: " + url);

            CollectExtension.CollectSleep();

            const string query = @"query GlobalProduct($filter: GlobalProductFilterInput!) {
  globalProduct(filter: $filter) {
    id
    products {
      id
      name
      price
      discountedPrice
      discountRate
      isOutOfStock
      retailStatus
      category
      marketplace { id category }
      images {
        thumbnailUrl(width: 800, mode: THUMBNAIL)
      }
      variants {
        id
        name
        isOutOfStock
        additionalPrice
        discountedPrice
      }
      description
      descriptionImages {
        url
      }
      shop {
        id
        code
        name
      }
      tags {
        id
        name
      }
      masterTags {
        id
        name
      }
    }
  }
}";

            var result = await GraphqlAsync(new
            {
                operationName = "GlobalProduct",
                variables = new { filter = new { slug_Exact = slug } },
                query
            });

            return result?.ToString();
        }

        public override Task<JToken> GetData(string connectionId, string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new CollectException("팬즈 API 응답이 비었습니다.");

            var json = JToken.Parse(html);

            // KR 마켓플레이스 상품 우선, 없으면 첫 번째
            var products = json?["data"]?["globalProduct"]?["products"];
            JToken product = null;
            if (products != null)
            {
                product = products.FirstOrDefault(p => p["marketplace"]?["category"]?.ToString() == "KR")
                          ?? products.FirstOrDefault();
            }

            if (product == null)
                throw new CollectException("상품 정보를 찾을 수 없습니다.");

            return Task.FromResult(product);
        }

        public override void ThrowIfCaptcha(string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new CollectException("팬즈 API 응답이 비었습니다.");

            if (html.Contains("\"errors\""))
            {
                try
                {
                    var json = JToken.Parse(html);
                    var errors = json?["errors"];
                    if (errors != null && errors.HasValues)
                        throw new CollectException("팬즈 API 오류: " + errors.First?["message"]);
                }
                catch (JsonReaderException) { }
            }
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
            var isOutOfStock = data?["isOutOfStock"]?.Value<bool>() ?? false;
            var retailStatus = data?["retailStatus"]?.ToString();

            // 판매 종료(Discontinued) → 수집 중단
            if (retailStatus == "RETAIL_ENDED")
                throw new SaleException(ProductSaleStatus.Stopped, "판매 종료된 상품입니다.");

            if (isOutOfStock)
                throw new SaleException(ProductSaleStatus.SoldOut, "품절된 상품입니다.");
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            var name = data?["name"]?.ToString() ?? "";
            return ProductHelper.CreateName(Language.KR, name);
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            var shop = data?["shop"]?["name"]?.ToString();
            if (!string.IsNullOrWhiteSpace(shop))
                return ProductHelper.CreateName(Language.KR, shop);
            return null;
        }

        public override string GetModel(string html, JToken data) => null;
        public override List<Desc> GetDescs(string html, JToken data) => new List<Desc>();

        public override List<Tag> GetTags(string html, JToken data)
        {
            var tags = new List<Tag>();

            foreach (var arr in new[] { data?["masterTags"], data?["tags"] })
            {
                if (arr == null) continue;
                foreach (var t in arr)
                {
                    var tagName = t["name"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(tagName))
                    {
                        var tag = new Tag();
                        tag.key = "kw";
                        tag.values = new List<string> { tagName };
                        tags.Add(tag);
                    }
                }
            }

            return tags;
        }

        public override List<Price> GetPrice(string html, JToken data)
        {
            var discountedPrice = data?["discountedPrice"]?.Value<double>() ?? 0;
            var price = data?["price"]?.Value<double>() ?? 0;
            var finalPrice = discountedPrice > 0 ? discountedPrice : price;

            if (finalPrice <= 0)
                throw new CollectException("가격 정보를 찾을 수 없습니다.");

            return ProductHelper.CreatePrice(Currency, finalPrice, finalPrice, true);
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();
            var imageArray = data?["images"];
            if (imageArray == null) return images;

            foreach (var img in imageArray)
            {
                var url = img["thumbnailUrl"]?.ToString();
                if (!string.IsNullOrWhiteSpace(url))
                    images.Add(url);
            }
            return images;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var sb = new StringBuilder();

            var description = data?["description"]?.ToString();
            if (!string.IsNullOrWhiteSpace(description))
                sb.Append(description);

            var descImages = data?["descriptionImages"];
            if (descImages != null)
            {
                foreach (var img in descImages)
                {
                    var url = img["url"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(url))
                        sb.Append($"<img src=\"{url}\" />");
                }
            }

            return sb.ToString();
        }

        public override Option GetOption(string html, JToken data)
        {
            var prices = GetPrice(html, data);
            var category = data?["category"]?.ToString();
            var variants = data?["variants"] as JArray;
            var isOutOfStock = data?["isOutOfStock"]?.Value<bool>() ?? false;
            var retailStatus = data?["retailStatus"]?.ToString();

            // 판매 종료(Discontinued) → 수집 중단
            if (retailStatus == "RETAIL_ENDED")
                throw new SaleException(ProductSaleStatus.Stopped, "판매 종료된 상품입니다.");

            // SINGULAR/BUNDLE 또는 variants가 없는 경우 → 기본 옵션
            // BUNDLE: 세트 상품 — 개별 variant 품절이어도 세트 자체는 판매 중일 수 있음
            if (category == "SINGULAR" || category == "BUNDLE" || variants == null || variants.Count == 0)
                return GetDefaultOption(prices, isOutOfStock);

            if (variants.Count == 1
                && (variants[0]["name"]?.ToString() ?? "").Equals("default", StringComparison.OrdinalIgnoreCase))
                return GetDefaultOption(prices, isOutOfStock);

            // MULTIPLE → 단일 옵션 그룹
            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();

            foreach (var v in variants)
            {
                var variantName = v["name"]?.ToString() ?? "";
                var variantSoldOut = v["isOutOfStock"]?.Value<bool>() ?? false;
                var variantDiscountedPrice = v["discountedPrice"]?.Value<double>() ?? 0;
                var additionalPrice = v["additionalPrice"]?.Value<double>() ?? 0;

                var id = variantName;

                groups.Add(new Option.OptionGroup.Group
                {
                    groupId = id,
                    optionName = ProductHelper.CreateName(Language.KR, variantName)
                });

                // variant별 최종가: discountedPrice가 있으면 사용, 없으면 기본가 + 추가금
                var variantFinalPrice = variantDiscountedPrice > 0
                    ? variantDiscountedPrice
                    : (data?["discountedPrice"]?.Value<double>() ?? 0) + additionalPrice;
                var detailPrices = ProductHelper.CreatePrice(Currency, variantFinalPrice, variantFinalPrice, true);

                details.Add(new Option.DetailOption
                {
                    optionIds = id,
                    isSoldOut = variantSoldOut,
                    quantity = variantSoldOut ? 0 : 9,
                    optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                    optionName = ProductHelper.CreateName(Language.KR, variantName),
                    price = detailPrices
                });
            }

            if (details.Count == 0)
                return GetDefaultOption(prices);

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

        public override Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            return Task.CompletedTask;
        }

        // ===== Helpers =====

        private static Option GetDefaultOption(List<Price> prices, bool isOutOfStock = false)
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
                        isSoldOut = isOutOfStock,
                        quantity = isOutOfStock ? 0 : 9,
                        optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                        optionName = ProductHelper.CreateName(Language.KR, "원사이즈"),
                        price = prices
                    }
                }
            };
        }
    }
}
