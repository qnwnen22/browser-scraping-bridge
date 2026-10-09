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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Ktown4u
{
    /// <summary>
    /// KTOWN4U (kr.ktown4u.com)
    /// Next.js SSR — __NEXT_DATA__ JSON 파싱
    /// K-POP 음반/굿즈 전문
    /// </summary>
    public class Ktown4u : CollectBase
    {
        private const string Host = "https://kr.ktown4u.com";

        public override string Site => nameof(Ktown4u);
        public override string Name => "KTOWN4U";
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
            var match = Regex.Match(url, @"goods_no=(\d+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        public override string CreateProductUrl(string code)
            => $"{Host}/iteminfo?goods_no={code}";

        // =========================
        // GetCollectPageInfosAsync
        // =========================
        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            // SSR은 항상 20개 고정, page 2+ 지원 안함
            if (page > 1) return null;

            string html = await ChatHubService.GetRequest(connectionId, url);
            if (string.IsNullOrWhiteSpace(html)) return null;

            var nextData = ExtractNextData(html);
            var items = nextData?["props"]?["pageProps"]?["items"] as JArray;
            if (items == null || items.Count == 0) return null;

            var result = new List<CollectPageInfo>();
            var seen = new HashSet<string>();

            foreach (var item in items)
            {
                var goodsNo = item["goodsNo"]?.ToString();
                if (string.IsNullOrWhiteSpace(goodsNo) || !seen.Add(goodsNo)) continue;

                if (item["saleYn"]?.ToString() == "N") continue;

                result.Add(new CollectPageInfo
                {
                    Code = goodsNo,
                    Url = CreateProductUrl(goodsNo),
                    Title = item["goodsNm"]?.ToString()?.Trim()
                });
            }

            if (result.Count == 0) return null;

            CollectExtension.CollectSleep();
            return result;
        }

        // =========================
        // GetData — __NEXT_DATA__ → productDetails
        // =========================
        public override Task<JToken> GetData(string connectionId, string url, string html)
        {
            var nextData = ExtractNextData(html);
            var productDetails = nextData?["props"]?["pageProps"]?["productDetails"];
            return Task.FromResult(productDetails);
        }

        // =========================
        // ThrowIfCaptcha
        // =========================
        public override void ThrowIfCaptcha(string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new CollectException("페이지를 불러올 수 없습니다.");
        }

        // =========================
        // Build Product parts
        // =========================
        public override List<Name> GetTitle(string html, JToken data)
        {
            if (data == null)
                throw new CollectException("상품 데이터를 찾을 수 없습니다.");

            // 판매 상태 확인
            if (data["saleStatus"]?.Type == JTokenType.Boolean && !data["saleStatus"].Value<bool>())
                throw new SaleException(ProductSaleStatus.Stopped, "판매 종료된 상품입니다.");

            var title = data["productName"]?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(title))
                return ProductHelper.CreateName(Language.KR, title);

            throw new CollectException("상품명을 찾을 수 없습니다.");
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            var artist = data?["artistName"]?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(artist))
                return ProductHelper.CreateName(Language.KR, artist);

            return null;
        }

        public override string GetModel(string html, JToken data) => null;
        public override List<Desc> GetDescs(string html, JToken data) => null;
        public override List<Tag> GetTags(string html, JToken data) => null;

        public override List<Price> GetPrice(string html, JToken data)
        {
            double salePrice = data?["displayDiscountPrice"]?.Value<double>() ?? 0;
            double originalPrice = data?["displayPrice"]?.Value<double>() ?? 0;

            if (salePrice <= 0)
                salePrice = data?["nonRateDiscountPrice"]?.Value<double>() ?? 0;
            if (originalPrice <= 0)
                originalPrice = data?["nonRatePrice"]?.Value<double>() ?? 0;

            if (salePrice <= 0)
                throw new CollectException("가격 정보를 찾을 수 없습니다.");

            if (originalPrice <= 0) originalPrice = salePrice;

            return ProductHelper.CreatePrice(Currency, salePrice, originalPrice, true);
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // productImages 배열
            var productImages = data?["productImages"] as JArray;
            if (productImages != null)
            {
                foreach (var img in productImages.OrderBy(i => i["displayOrder"]?.Value<int>() ?? 999))
                {
                    var imgPath = img["imagePath"]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(imgPath)) continue;
                    imgPath = NormalizeImageUrl(imgPath);
                    if (seen.Add(imgPath)) images.Add(imgPath);
                }
            }

            // 메인 이미지 폴백
            if (images.Count == 0)
            {
                var mainImg = data?["imagePath"]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(mainImg))
                {
                    mainImg = NormalizeImageUrl(mainImg);
                    images.Add(mainImg);
                }
            }

            if (images.Count == 0)
                throw new CollectException("메인 이미지를 찾을 수 없습니다.");

            return images;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            return data?["productExtra"]?["productContent"]?.ToString();
        }

        public override Option GetOption(string html, JToken data)
        {
            var optionValues = data?["productOptionsValues"] as JArray;

            if (optionValues == null || optionValues.Count == 0)
                return GetDefaultOption();

            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();

            foreach (var ov in optionValues)
            {
                var optName = ov["optionValueName"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(optName)) continue;

                // 옵션별 개별 가격
                double optSalePrice = ov["displayDiscountPrice"]?.Value<double>() ?? 0;
                double optOriginalPrice = ov["displayPrice"]?.Value<double>() ?? 0;
                if (optSalePrice <= 0) optSalePrice = ov["nonRateDiscountPrice"]?.Value<double>() ?? 0;
                if (optOriginalPrice <= 0) optOriginalPrice = ov["nonRatePrice"]?.Value<double>() ?? 0;

                List<Price> optPrices = null;
                if (optSalePrice > 0)
                    optPrices = ProductHelper.CreatePrice(Currency, optSalePrice, optOriginalPrice > 0 ? optOriginalPrice : optSalePrice, true);

                bool isSoldOut = ov["saleStatus"]?.ToString() == "N";
                string id = ov["optionValueNo"]?.ToString() ?? groups.Count.ToString();

                groups.Add(new Option.OptionGroup.Group
                {
                    groupId = id,
                    optionName = ProductHelper.CreateName(Language.KR, optName),
                    quantity = isSoldOut ? 0 : 9,
                    price = optPrices
                });

                details.Add(new Option.DetailOption
                {
                    optionIds = id,
                    isSoldOut = isSoldOut,
                    optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                    optionName = ProductHelper.CreateName(Language.KR, optName),
                    quantity = isSoldOut ? 0 : 9,
                    price = optPrices
                });
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
            await Task.CompletedTask;
        }

        // =========================
        // Helpers
        // =========================
        private static JObject ExtractNextData(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;
            var match = Regex.Match(html, @"<script\s+id=""__NEXT_DATA__""[^>]*>(.*?)</script>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success) return null;
            try { return JObject.Parse(match.Groups[1].Value); }
            catch { return null; }
        }

        private static string NormalizeImageUrl(string imgPath)
        {
            if (string.IsNullOrWhiteSpace(imgPath)) return imgPath;
            if (imgPath.StartsWith("http")) return imgPath;
            return $"https://www.ktown4u.com{imgPath}";
        }

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
