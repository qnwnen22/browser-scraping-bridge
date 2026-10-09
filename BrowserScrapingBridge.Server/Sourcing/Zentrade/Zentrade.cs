#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Zentrade
{
    public class Zentrade : CollectBase
    {
        public override string Site => nameof(Zentrade);
        public override string Name => "젠트레이드";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = false;

        private const string BASE_URL = "http://www.zentrade.co.kr";

        public override string GetCode(string url)
        {
            var match = Regex.Match(url, @"goodsno=(\d+)", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[1].Value;

            Uri uri = new Uri(url);
            var queries = System.Web.HttpUtility.ParseQueryString(uri.Query);
            string goodsno = queries["goodsno"];
            if (!string.IsNullOrWhiteSpace(goodsno)) return goodsno;

            return url;
        }

        public override string CreateProductUrl(string code)
        {
            return $"{BASE_URL}/shop/goods/goods_view.php?goodsno={code}";
        }

        public override Task<string> GetLoginUrlAsync(string connectionId)
        {
            return Task.FromResult<string>(null);
        }

        #region 카테고리 수집

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            UriBuilderAdvance uriBuilder = new(url);
            uriBuilder.SetQuery("page", page.ToString());

            string pageNum = uriBuilder.GetQuery("pageNum");
            if (string.IsNullOrWhiteSpace(pageNum))
                uriBuilder.SetQuery("pageNum", "40");

            string pageUrl = uriBuilder.ToString();
            string html = await ChatHubService.GetRequest(connectionId, pageUrl);
            if (string.IsNullOrWhiteSpace(html)) return null;

            var result = new List<CollectPageInfo>();

            var matches = Regex.Matches(html, @"goods_view\.php\?goodsno=(\d+)(?:&category=\w+)?[^""]*""[^>]*>(?:\s*<[^>]+>)*\s*([^<]+)", RegexOptions.IgnoreCase);
            var seenCodes = new HashSet<string>();

            foreach (Match match in matches)
            {
                string code = match.Groups[1].Value;
                if (seenCodes.Contains(code)) continue;
                seenCodes.Add(code);

                string title = ExtractTitleForCode(html, code);

                result.Add(new CollectPageInfo
                {
                    Code = code,
                    Url = $"{BASE_URL}/shop/goods/goods_view.php?goodsno={code}",
                    Title = title?.Trim()
                });
            }

            if (result.Count == 0) return null;

            CollectExtension.CollectSleep();
            return result;
        }

        private string ExtractTitleForCode(string html, string code)
        {
            var titleMatch = Regex.Match(html,
                $@"goods_view\.php\?goodsno={Regex.Escape(code)}[^""]*""\s*[^>]*>\s*(?:<[^>]+>\s*)*([^<]+?)\s*<",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (titleMatch.Success)
            {
                string title = titleMatch.Groups[1].Value.Trim();
                if (!title.StartsWith("No.") && title.Length > 2)
                    return title;
            }

            var altMatch = Regex.Match(html,
                $@"goodsno={Regex.Escape(code)}[^""]*""[^>]*>\s*<img[^>]+alt=""([^""]+)""",
                RegexOptions.IgnoreCase);
            if (altMatch.Success) return altMatch.Groups[1].Value.Trim();

            return null;
        }

        #endregion

        #region CollectBase 추상 메서드 구현

        public override void ThrowIfCaptcha(string url, string html)
        {
            if (html.Contains("login.php") && html.Contains("alert("))
                throw new CollectException("로그인이 필요한 상품입니다.");
        }

        public override Task<JToken> GetData(string connectionId, string url, string html)
        {
            return Task.FromResult<JToken>(null);
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string title = null;

            var titleMatch = Regex.Match(html, @"<title>\s*(.+?)\s*</title>", RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                title = titleMatch.Groups[1].Value.Trim();
                int dashIdx = title.LastIndexOf(" - ");
                if (dashIdx > 0) title = title.Substring(0, dashIdx).Trim();
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                var ogMatch = Regex.Match(html, @"<meta\s+property=""og:title""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
                if (ogMatch.Success) title = ogMatch.Groups[1].Value.Trim();
            }

            if (string.IsNullOrWhiteSpace(title))
                throw new CollectException("상품명을 찾을 수 없습니다.");

            return new List<Name> { new Name(Language.KR, title) };
        }

        public override List<Name> GetBrand(string html, JToken data) => null;
        public override string GetModel(string html, JToken data) => null;
        public override List<Desc> GetDescs(string html, JToken data) => null;
        public override List<Tag> GetTags(string html, JToken data) => null;

        public override List<Price> GetPrice(string html, JToken data)
        {
            var priceMatches = Regex.Matches(html, @"price\['[^']+'\]\s*=\s*(\d+)\s*;");
            double salePrice = 0;

            if (priceMatches.Count > 0)
                double.TryParse(priceMatches[0].Groups[1].Value, out salePrice);

            if (salePrice <= 0)
            {
                var htmlPriceMatch = Regex.Match(html, @"(\d[\d,]+)\s*원");
                if (htmlPriceMatch.Success)
                {
                    string priceStr = htmlPriceMatch.Groups[1].Value.Replace(",", "");
                    double.TryParse(priceStr, out salePrice);
                }
            }

            if (salePrice <= 0)
                throw new CollectException("가격 정보를 찾을 수 없습니다.");

            double originalPrice = salePrice;
            var consumerMatches = Regex.Matches(html, @"consumer\['[^']+'\]\s*=\s*(\d+)\s*;");
            if (consumerMatches.Count > 0)
            {
                double consumerPrice;
                if (double.TryParse(consumerMatches[0].Groups[1].Value, out consumerPrice) && consumerPrice > salePrice)
                    originalPrice = consumerPrice;
            }

            var price = new Price(Currency.KRW, originalPrice, salePrice, true);
            price.isoCode = "KRW";
            return new List<Price> { price };
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();

            var ogMatch = Regex.Match(html, @"<meta\s+property=""og:image""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
            if (ogMatch.Success)
            {
                string imgUrl = ogMatch.Groups[1].Value;
                if (!imgUrl.StartsWith("http")) imgUrl = "http:" + imgUrl;
                images.Add(imgUrl);
            }

            // productimgs 리스트 이미지
            var imgMatches = Regex.Matches(html, @"(https?://[^""'\s]+productimgs/\d+/[^""'\s]+_listimg_\d+\.(?:jpg|png|gif))", RegexOptions.IgnoreCase);
            foreach (Match match in imgMatches)
            {
                string imgUrl = match.Groups[1].Value;
                if (!images.Contains(imgUrl))
                    images.Add(imgUrl);
            }

            // 일반 상품 이미지
            var generalImgMatches = Regex.Matches(html, @"(https?://[^""'\s]+productimgs/\d+/[^""'\s]+_\d+\.(?:jpg|png|gif))", RegexOptions.IgnoreCase);
            foreach (Match match in generalImgMatches)
            {
                string imgUrl = match.Groups[1].Value;
                if (imgUrl.Contains("_d_") || imgUrl.Contains("_opt") || imgUrl.Contains("_size")) continue;
                if (!images.Contains(imgUrl))
                    images.Add(imgUrl);
            }

            if (images.Count == 0)
                throw new CollectException("메인 이미지를 찾을 수 없습니다.");

            return images;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var detailImgs = new List<string>();
            var detailMatches = Regex.Matches(html, @"(https?://[^""'\s]+productimgs/\d+/[^""'\s]+_d_\d+\.(?:jpg|png|gif))", RegexOptions.IgnoreCase);
            foreach (Match match in detailMatches)
            {
                string imgUrl = match.Groups[1].Value;
                if (!detailImgs.Contains(imgUrl))
                    detailImgs.Add(imgUrl);
            }

            if (detailImgs.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                foreach (string img in detailImgs)
                    sb.AppendLine($"<img src=\"{img}\" />");
                return sb.ToString();
            }

            // 폴백: 상세 영역의 모든 이미지
            var allDetailImgs = Regex.Matches(html, @"(https?://[^""'\s]+productimgs/\d+/[^""'\s]+\.(?:jpg|png|gif))", RegexOptions.IgnoreCase);
            if (allDetailImgs.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                var seen = new HashSet<string>();
                foreach (Match match in allDetailImgs)
                {
                    string imgUrl = match.Groups[1].Value;
                    if (imgUrl.Contains("_listimg_")) continue;
                    if (seen.Contains(imgUrl)) continue;
                    seen.Add(imgUrl);
                    sb.AppendLine($"<img src=\"{imgUrl}\" />");
                }
                if (sb.Length > 0) return sb.ToString();
            }

            return null;
        }

        public override Option GetOption(string html, JToken data)
        {
            var selectMatches = Regex.Matches(html, @"<select[^>]*name=""opt\[\]""[^>]*>(.*?)</select>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (selectMatches.Count == 0)
                return GetDefaultOption();

            // 기본 가격 추출 (옵션 추가가격 계산용)
            double basePrice = 0;
            var basePriceMatch = Regex.Match(html, @"price\['[^']+'\]\s*=\s*(\d+)\s*;");
            if (basePriceMatch.Success)
                double.TryParse(basePriceMatch.Groups[1].Value, out basePrice);

            var optionGroups = new List<Option.OptionGroup>();
            var detailOptions = new List<Option.DetailOption>();
            int groupIdx = 0;

            foreach (Match selectMatch in selectMatches)
            {
                string selectHtml = selectMatch.Groups[1].Value;
                var optionMatches = Regex.Matches(selectHtml, @"<option\s+value=""([^""]+)""[^>]*>([^<]+)</option>", RegexOptions.IgnoreCase);

                if (optionMatches.Count == 0) continue;

                string groupName = "옵션";
                var headerMatch = Regex.Match(selectHtml, @"<option[^>]*value=""""[^>]*>(?:[-= ]*)?([^-=<]+?)(?:[-= ]*)?</option>", RegexOptions.IgnoreCase);
                if (headerMatch.Success)
                {
                    string headerText = headerMatch.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(headerText) && headerText != "옵션선택" && headerText != "옵션을 선택해 주세요")
                        groupName = headerText;
                }

                var optionGroup = new Option.OptionGroup();
                optionGroup.optionId = groupIdx.ToString();
                optionGroup.groupName = new List<Name> { new Name(Language.KR, groupName) };
                optionGroup.groups = new List<Option.OptionGroup.Group>();

                int optIdx = 0;
                foreach (Match optMatch in optionMatches)
                {
                    string optValue = optMatch.Groups[1].Value.Trim();
                    string optText = optMatch.Groups[2].Value.Trim();

                    if (string.IsNullOrWhiteSpace(optValue)) continue;

                    double addPrice = 0;
                    string optName = optText;
                    var addPriceMatch = Regex.Match(optText, @"\(\+\s*([\d,]+)\s*원\)");
                    if (addPriceMatch.Success)
                    {
                        string addPriceStr = addPriceMatch.Groups[1].Value.Replace(",", "");
                        double.TryParse(addPriceStr, out addPrice);
                        optName = optText.Substring(0, addPriceMatch.Index).Trim();
                    }

                    var subPriceMatch = Regex.Match(optText, @"\(-\s*([\d,]+)\s*원\)");
                    if (subPriceMatch.Success)
                    {
                        string subPriceStr = subPriceMatch.Groups[1].Value.Replace(",", "");
                        double subPrice;
                        if (double.TryParse(subPriceStr, out subPrice))
                            addPrice = -subPrice;
                        optName = optText.Substring(0, subPriceMatch.Index).Trim();
                    }

                    bool isSoldOut = optMatch.Groups[0].Value.Contains("disabled");
                    if (isSoldOut) continue;

                    string id = $"{groupIdx}_{optIdx}";

                    var group = new Option.OptionGroup.Group();
                    group.groupId = id;
                    group.optionName = new List<Name> { new Name(Language.KR, optName) };
                    optionGroup.groups.Add(group);

                    double optionPrice = basePrice + addPrice;

                    var detailOption = new Option.DetailOption();
                    detailOption.optionIds = id;
                    detailOption.optionGroupName = new List<Name> { new Name(Language.KR, groupName) };
                    detailOption.optionName = new List<Name> { new Name(Language.KR, optName) };
                    detailOption.price = new List<Price>
                    {
                        new Price(Currency.KRW, optionPrice, optionPrice)
                    };
                    detailOptions.Add(detailOption);

                    optIdx++;
                }

                if (optionGroup.groups.Count > 0)
                    optionGroups.Add(optionGroup);

                groupIdx++;
            }

            if (optionGroups.Count == 0 || detailOptions.Count == 0)
                return GetDefaultOption();

            return new Option
            {
                optionGroups = optionGroups,
                detailOptions = detailOptions
            };
        }

        public override int? GetTotalQuantity(string html, JToken data) => null;

        public override void ThrowIfOutOfStock(string html, JToken data, Product product) { }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            await Task.CompletedTask;
        }

        #endregion

        #region 헬퍼

        private Option GetDefaultOption()
        {
            var groupName = new Name(Language.KR, "사이즈");
            var optionName = new Name(Language.KR, "원사이즈");

            var optionGroup = new Option.OptionGroup();
            optionGroup.optionId = "0";
            optionGroup.groupName = new List<Name> { groupName };
            optionGroup.groups = new List<Option.OptionGroup.Group>
            {
                new Option.OptionGroup.Group
                {
                    groupId = "0",
                    optionName = new List<Name> { optionName }
                }
            };

            var detailOption = new Option.DetailOption();
            detailOption.optionIds = "0";
            detailOption.optionGroupName = new List<Name> { groupName };
            detailOption.optionName = new List<Name> { optionName };

            return new Option
            {
                optionGroups = new List<Option.OptionGroup> { optionGroup },
                detailOptions = new List<Option.DetailOption> { detailOption }
            };
        }

        #endregion
    }
}
