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
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.WeverseShop
{
    public class WeverseShop : ICollect
    {
        public string Site => nameof(WeverseShop);
        public string Name => "위버스샵";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        private const string HOST = "https://shop.weverse.io";

        public Task<string> GetLoginUrlAsync(string connectionId)
        {
            return Task.FromResult<string>(null);
        }

        // =========================
        // Code 추출
        // =========================

        public string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            // https://shop.weverse.io/ko/shop/KRW/artists/2/sales/53526
            var m = Regex.Match(url, @"/sales/(\d+)");
            return m.Success ? m.Groups[1].Value : null;
        }

        // =========================
        // __NEXT_DATA__ → $dehydratedState 파싱
        // =========================

        private static JObject ExtractNextData(string html)
        {
            try
            {
                var m = Regex.Match(html,
                    @"<script\s+id=""__NEXT_DATA__""\s+type=""application/json"">(.*?)</script>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (!m.Success) return null;
                return JObject.Parse(m.Groups[1].Value);
            }
            catch { return null; }
        }

        private static JArray GetDehydratedQueries(JObject nextData)
        {
            return nextData?.SelectToken("props.pageProps.$dehydratedState.queries") as JArray;
        }

        private static JObject FindQueryData(JArray queries, string keyContains)
        {
            if (queries == null) return null;
            foreach (var q in queries)
            {
                var qk = q["queryKey"] as JArray;
                if (qk != null && qk.Count > 0 && qk[0]?.ToString().Contains(keyContains) == true)
                {
                    return q.SelectToken("state.data") as JObject;
                }
            }
            return null;
        }

        private static JArray FindQueryDataArray(JArray queries, string keyContains)
        {
            if (queries == null) return null;
            foreach (var q in queries)
            {
                var qk = q["queryKey"] as JArray;
                if (qk != null && qk.Count > 0 && qk[0]?.ToString().Contains(keyContains) == true)
                {
                    var data = q.SelectToken("state.data");
                    if (data is JArray arr) return arr;
                    if (data is JObject obj) return obj["productCards"] as JArray;
                }
            }
            return null;
        }

        // =========================
        // 카테고리 수집
        // =========================

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(
            string connectionId, string url, int page)
        {
            // SSR에는 첫 12개 상품만 포함 (커서 기반 페이지네이션, API 직접 호출 불가)
            if (page > 1) return null;

            string html = await ChatHubService.GetRequest(connectionId, url);
            if (string.IsNullOrWhiteSpace(html)) return null;

            var nextData = ExtractNextData(html);
            if (nextData == null) return null;

            var queries = GetDehydratedQueries(nextData);
            if (queries == null) return null;

            // $dehydratedState.queries에서 카테고리 상품 목록 찾기
            JObject salesData = FindQueryData(queries, "categories/")
                ?? FindQueryData(queries, "/sales");
            JArray productCards = salesData?["productCards"] as JArray;

            if (productCards == null || !productCards.Any()) return null;

            // URL에서 artistId 추출
            var artistMatch = Regex.Match(url, @"/artists/(\d+)");
            string artistId = artistMatch.Success ? artistMatch.Groups[1].Value : "0";

            var result = new List<CollectPageInfo>();
            foreach (var card in productCards)
            {
                string saleId = card["saleId"]?.ToString();
                if (string.IsNullOrWhiteSpace(saleId)) continue;

                string status = card["status"]?.ToString();
                if (status != "SALE") continue;

                string title = card["name"]?.ToString() ?? "";
                string productUrl = $"{HOST}/ko/shop/KRW/artists/{artistId}/sales/{saleId}";

                result.Add(new CollectPageInfo
                {
                    Code = saleId,
                    Url = productUrl,
                    Title = title
                });
            }

            return result.Count > 0 ? result : null;
        }

        // =========================
        // 상품 수집
        // =========================

        public async Task<Product> GetProductAsync(
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

                string html = await ChatHubService.GetRequest(connectionId, url);
                if (string.IsNullOrWhiteSpace(html))
                    throw new Exception("위버스샵 상품 페이지 응답이 없습니다.");

                var nextData = ExtractNextData(html);
                if (nextData == null)
                    throw new Exception("__NEXT_DATA__를 찾을 수 없습니다.");

                var queries = GetDehydratedQueries(nextData);
                if (queries == null)
                    throw new Exception("dehydratedState를 찾을 수 없습니다.");

                // sales/:saleId 쿼리에서 상품 데이터 추출
                var data = FindQueryData(queries, "sales/:saleId")
                    ?? FindQueryData(queries, "/sales/");
                if (data == null)
                    throw new Exception("상품 데이터를 찾을 수 없습니다.");

                // 판매 상태 체크
                string status = data["status"]?.ToString();
                if (status == "SOLD_OUT")
                    throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품입니다.");
                if (status != "SALE" && status != "PRE_ORDER")
                    throw new SaleException(ProductSaleStatus.Stopped, $"판매 중이 아닌 상품입니다. (status: {status})");

                // 상품명
                product.title = GetTitle(data);

                // 가격
                product.price = GetPrice(data);

                // 이미지
                product.mainImages = GetMainImages(data);

                // 옵션
                product.option = GetOption(data, product.price);

                // 상세 페이지 HTML
                product.detailPageHtml = GetDetailPageHtml(data);

                product.regDate = DateTime.Now;
                product.collectStatus = ProductCollectStatus.Success;
            }
            catch (SaleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = ex.Message;
            }

            return product;
        }

        // =========================
        // 상품 파싱 헬퍼
        // =========================

        private List<Name> GetTitle(JToken data)
        {
            string name = data["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(Language.KR, name);
        }

        private List<Price> GetPrice(JToken data)
        {
            var priceObj = data["price"];
            if (priceObj == null)
                throw new Exception("가격 정보를 찾을 수 없습니다.");

            double originalPrice = priceObj["originalPrice"]?.Value<double>() ?? 0;
            double salePrice = priceObj["salePrice"]?.Value<double>() ?? 0;

            if (originalPrice <= 0 && salePrice <= 0)
                throw new Exception("상품 가격을 찾을 수 없습니다.");

            if (salePrice <= 0) salePrice = originalPrice;
            if (originalPrice <= 0) originalPrice = salePrice;
            if (originalPrice < salePrice) originalPrice = salePrice;

            return ProductHelper.CreatePrice(Currency, salePrice, originalPrice, true);
        }

        private List<string> GetMainImages(JToken data)
        {
            var images = new List<string>();

            // thumbnailImageUrls
            var thumbs = data["thumbnailImageUrls"] as JArray;
            if (thumbs != null)
            {
                foreach (var t in thumbs)
                {
                    string imgUrl = t?.ToString();
                    if (!string.IsNullOrWhiteSpace(imgUrl))
                        images.Add(imgUrl);
                }
            }

            return images.Count > 0 ? images : null;
        }

        private Option GetOption(JToken data, List<Price> defaultPrice)
        {
            var optionObj = data["option"];
            if (optionObj == null)
                return CreateDefaultOption(defaultPrice);

            var options = optionObj["options"] as JArray;
            if (options == null || options.Count == 0)
                return CreateDefaultOption(defaultPrice);

            // 옵션이 1개이고 추가가격이 0이면 원사이즈 처리
            if (options.Count == 1)
            {
                var singleOpt = options[0];
                double addPrice = singleOpt["optionAddPrice"]?.Value<double>() ?? 0;
                if (addPrice == 0)
                    return CreateDefaultOption(defaultPrice);
            }

            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();
            int idx = 1;

            foreach (var opt in options)
            {
                bool isSoldOut = opt["isSoldOut"]?.Value<bool>() ?? false;
                string optionName = opt["saleOptionName"]?.ToString() ?? $"옵션{idx}";

                double optSalePrice = opt["optionSalePrice"]?.Value<double>() ?? 0;
                List<Price> optPrice = optSalePrice > 0
                    ? ProductHelper.CreatePrice(Currency, optSalePrice, optSalePrice, true)
                    : defaultPrice;

                string id = idx.ToString();
                groups.Add(new Option.OptionGroup.Group
                {
                    groupId = id,
                    optionName = ProductHelper.CreateName(Language.KR, optionName),
                    quantity = isSoldOut ? 0 : 9,
                    price = optPrice
                });

                details.Add(new Option.DetailOption
                {
                    optionIds = id,
                    isSoldOut = isSoldOut,
                    optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                    optionName = ProductHelper.CreateName(Language.KR, optionName),
                    quantity = isSoldOut ? 0 : 9,
                    price = optPrice
                });

                idx++;
            }

            if (details.Count == 0)
                return CreateDefaultOption(defaultPrice);

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
                        price = prices
                    }
                }
            };
        }

        private string GetDetailPageHtml(JToken data)
        {
            // detailImages를 img 태그로 변환
            var detailImages = data["detailImages"] as JArray;
            if (detailImages == null || detailImages.Count == 0)
                return "";

            var html = new System.Text.StringBuilder();
            foreach (var img in detailImages)
            {
                string imgUrl = img["imageUrl"]?.ToString();
                if (string.IsNullOrWhiteSpace(imgUrl)) continue;

                int width = img["width"]?.Value<int>() ?? 0;
                int height = img["height"]?.Value<int>() ?? 0;

                if (width > 0 && height > 0)
                    html.Append($"<img src=\"{imgUrl}\" width=\"{width}\" height=\"{height}\" />");
                else
                    html.Append($"<img src=\"{imgUrl}\" />");
            }

            return html.ToString();
        }

        // =========================
        // 미구현 (신규 인터페이스)
        // =========================

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
