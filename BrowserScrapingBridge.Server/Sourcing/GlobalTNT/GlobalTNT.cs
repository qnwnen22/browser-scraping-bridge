#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.GlobalTNT
{
    public class GlobalTNT : CollectBase
    {
        public override string Site => nameof(GlobalTNT);
        public override string Name => "글로벌티엔티";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = false;

        private const string BASE_URL = "https://globaltnt.co.kr";

        public override string GetCode(string url)
        {
            var match = Regex.Match(url, @"goodsNo=(\d+)", RegexOptions.IgnoreCase);
            if (match.Success) return match.Groups[1].Value;

            Uri uri = new Uri(url);
            var queries = System.Web.HttpUtility.ParseQueryString(uri.Query);
            string goodsNo = queries["goodsNo"];
            if (!string.IsNullOrWhiteSpace(goodsNo)) return goodsNo;

            return url;
        }

        public override string CreateProductUrl(string code)
        {
            return $"{BASE_URL}/goods/goods_view.php?goodsNo={code}";
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

            // 잘못된 접근 alert 감지
            if (html.Contains("잘못된 접근입니다"))
                return null;

            var result = new List<CollectPageInfo>();
            var matches = Regex.Matches(html, @"goods_view\.php\?goodsNo=(\d+)", RegexOptions.IgnoreCase);
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
                    Url = $"{BASE_URL}/goods/goods_view.php?goodsNo={code}",
                    Title = title?.Trim()
                });
            }

            if (result.Count == 0) return null;

            CollectExtension.CollectSleep();
            return result;
        }

        private string ExtractTitleForCode(string html, string code)
        {
            // <a> → <strong>제목</strong> 패턴
            var titleMatch = Regex.Match(html,
                $@"goodsNo={Regex.Escape(code)}[^""]*""[^>]*>\s*<strong>([^<]+)</strong>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (titleMatch.Success)
            {
                string title = titleMatch.Groups[1].Value.Trim();
                if (title.Length > 2) return title;
            }

            // <img alt="제목"> 패턴
            var altMatch = Regex.Match(html,
                $@"goodsNo={Regex.Escape(code)}[^""]*""[^>]*>\s*<img[^>]+alt=""([^""]+)""",
                RegexOptions.IgnoreCase);
            if (altMatch.Success) return altMatch.Groups[1].Value.Trim();

            return null;
        }

        #endregion

        #region CollectBase 추상 메서드 구현

        public override void ThrowIfCaptcha(string url, string html)
        {
            if (html.Contains("잘못된 접근입니다"))
                throw new CollectException("잘못된 접근입니다.");

            if (html.Contains("login.php") && html.Contains("location.href"))
                throw new CollectException("로그인이 필요한 상품입니다.");
        }

        public override Task<JToken> GetData(string connectionId, string url, string html)
        {
            return Task.FromResult<JToken>(null);
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string title = null;

            // h3 태그
            var h3Match = Regex.Match(html, @"<h3[^>]*>([^<]+)</h3>", RegexOptions.IgnoreCase);
            if (h3Match.Success)
                title = h3Match.Groups[1].Value.Trim();

            // h1 태그
            if (string.IsNullOrWhiteSpace(title))
            {
                var h1Match = Regex.Match(html, @"<h1[^>]*>([^<]+)</h1>", RegexOptions.IgnoreCase);
                if (h1Match.Success)
                    title = h1Match.Groups[1].Value.Trim();
            }

            // og:title
            if (string.IsNullOrWhiteSpace(title))
            {
                var ogMatch = Regex.Match(html, @"<meta\s+property=""og:title""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
                if (ogMatch.Success)
                    title = ogMatch.Groups[1].Value.Trim();
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
            double salePrice = 0;

            // GodoMall 5 JS 변수: var setGoodsPrice = 'NUMBER';
            var priceMatch = Regex.Match(html, @"var\s+setGoodsPrice\s*=\s*'([\d.]+)'");
            if (priceMatch.Success)
                double.TryParse(priceMatch.Groups[1].Value, out salePrice);

            // 폴백: hidden input
            if (salePrice <= 0)
            {
                var hiddenMatch = Regex.Match(html, @"name=""set_goods_price""\s+value=""([\d.]+)""", RegexOptions.IgnoreCase);
                if (hiddenMatch.Success)
                    double.TryParse(hiddenMatch.Groups[1].Value, out salePrice);
            }

            if (salePrice <= 0)
                throw new CollectException("가격 정보를 찾을 수 없습니다.");

            var price = new Price(Currency.KRW, salePrice, salePrice, true);
            price.isoCode = "KRW";
            return new List<Price> { price };
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();

            // og:image
            var ogMatch = Regex.Match(html, @"<meta\s+property=""og:image""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
            if (ogMatch.Success)
            {
                string imgUrl = ogMatch.Groups[1].Value;
                if (!imgUrl.StartsWith("http")) imgUrl = "https:" + imgUrl;
                images.Add(imgUrl);
            }

            // mainImage id
            if (images.Count == 0)
            {
                var mainImgMatch = Regex.Match(html, @"<img[^>]+id=""mainImage""[^>]+src=""([^""]+)""", RegexOptions.IgnoreCase);
                if (!mainImgMatch.Success)
                    mainImgMatch = Regex.Match(html, @"<img[^>]+src=""([^""]+)""[^>]+id=""mainImage""", RegexOptions.IgnoreCase);
                if (mainImgMatch.Success)
                {
                    string imgUrl = mainImgMatch.Groups[1].Value;
                    if (!imgUrl.StartsWith("http")) imgUrl = "https:" + imgUrl;
                    images.Add(imgUrl);
                }
            }

            // esmplus.com 이미지 (메인 영역)
            if (images.Count == 0)
            {
                var esmMatches = Regex.Matches(html, @"(https?://[a-z]+\.esmplus\.com/[^""'\s]+\.(?:jpg|png|gif|jpeg))", RegexOptions.IgnoreCase);
                foreach (Match match in esmMatches)
                {
                    string imgUrl = match.Groups[1].Value;
                    if (!images.Contains(imgUrl))
                    {
                        images.Add(imgUrl);
                        break; // 첫 번째만 메인으로
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
            // #detail 탭 영역에서 이미지 추출
            var detailImgs = new List<string>();

            // esmplus CDN 이미지 (상세 영역)
            var imgMatches = Regex.Matches(html, @"(https?://[a-z]+\.esmplus\.com/[^""'\s]+\.(?:jpg|png|gif|jpeg))", RegexOptions.IgnoreCase);
            var mainImage = GetMainImageUrl(html);

            foreach (Match match in imgMatches)
            {
                string imgUrl = match.Groups[1].Value;
                // 메인 이미지와 동일한 것은 제외
                if (imgUrl == mainImage) continue;
                if (!detailImgs.Contains(imgUrl))
                    detailImgs.Add(imgUrl);
            }

            // 일반 이미지 패턴 (상품 상세)
            if (detailImgs.Count == 0)
            {
                var generalImgs = Regex.Matches(html, @"<img[^>]+src=""(https?://[^""]+\.(?:jpg|png|gif|jpeg))""[^>]*/?>", RegexOptions.IgnoreCase);
                foreach (Match match in generalImgs)
                {
                    string imgUrl = match.Groups[1].Value;
                    if (imgUrl == mainImage) continue;
                    if (imgUrl.Contains("logo") || imgUrl.Contains("icon") || imgUrl.Contains("banner")) continue;
                    if (!detailImgs.Contains(imgUrl))
                        detailImgs.Add(imgUrl);
                }
            }

            if (detailImgs.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                foreach (string img in detailImgs)
                    sb.AppendLine($"<img src=\"{img}\" />");
                return sb.ToString();
            }

            return null;
        }

        public override Option GetOption(string html, JToken data)
        {
            // GodoMall 5 옵션 플래그 확인
            var optionFlMatch = Regex.Match(html, @"setOptionFl\s*[=:]\s*'(\w)'");
            string optionFl = optionFlMatch.Success ? optionFlMatch.Groups[1].Value : "n";

            if (optionFl != "y")
                return GetDefaultOption();

            // 옵션 표시 방식
            var displayFlMatch = Regex.Match(html, @"setOptionDisplayFl\s*[=:]\s*'(\w)'");
            string displayFl = displayFlMatch.Success ? displayFlMatch.Groups[1].Value : "s";

            // 기본 가격
            double basePrice = 0;
            var basePriceMatch = Regex.Match(html, @"var\s+setGoodsPrice\s*=\s*'([\d.]+)'");
            if (basePriceMatch.Success)
                double.TryParse(basePriceMatch.Groups[1].Value, out basePrice);

            // select 요소에서 옵션 파싱
            var selectMatches = Regex.Matches(html,
                @"<select[^>]*(?:name=""opt[^""]*""|id=""optionSelect[^""]*"")[^>]*>(.*?)</select>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (selectMatches.Count == 0)
                return GetDefaultOption();

            var optionGroups = new List<Option.OptionGroup>();
            var detailOptions = new List<Option.DetailOption>();

            if (displayFl == "s")
            {
                // select 모드: 옵션값이 select에 직접 포함
                return ParseSelectOptions(selectMatches, basePrice);
            }
            else
            {
                // design 모드: 첫 번째 select에서 옵션명 추출
                return ParseDesignOptions(selectMatches, basePrice);
            }
        }

        public override int? GetTotalQuantity(string html, JToken data) => null;

        public override void ThrowIfOutOfStock(string html, JToken data, Product product) { }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            await Task.CompletedTask;
        }

        #endregion

        #region 옵션 파싱 헬퍼

        private Option ParseSelectOptions(MatchCollection selectMatches, double basePrice)
        {
            var optionGroups = new List<Option.OptionGroup>();
            var detailOptions = new List<Option.DetailOption>();
            int groupIdx = 0;

            foreach (Match selectMatch in selectMatches)
            {
                string selectHtml = selectMatch.Groups[1].Value;
                var optionMatches = Regex.Matches(selectHtml,
                    @"<option\s+value=""([^""]+)""[^>]*>([^<]+)</option>",
                    RegexOptions.IgnoreCase);

                if (optionMatches.Count == 0) continue;

                string groupName = ExtractGroupName(selectHtml);

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

                    // 추가금액 파싱: (+N원) 또는 (-N원)
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

                    // 품절 체크
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

        private Option ParseDesignOptions(MatchCollection selectMatches, double basePrice)
        {
            // design 모드: 첫 번째 select에서 옵션 이름 추출 (value가 비어있으므로 텍스트만 사용)
            string firstSelectHtml = selectMatches[0].Groups[1].Value;

            // 헤더 옵션(=선택=) 제외, 실제 옵션만 추출
            var optionMatches = Regex.Matches(firstSelectHtml,
                @"<option[^>]*>([^<]+)</option>",
                RegexOptions.IgnoreCase);

            string groupName = ExtractGroupName(firstSelectHtml);

            var optionGroup = new Option.OptionGroup();
            optionGroup.optionId = "0";
            optionGroup.groupName = new List<Name> { new Name(Language.KR, groupName) };
            optionGroup.groups = new List<Option.OptionGroup.Group>();

            var detailOptions = new List<Option.DetailOption>();
            int optIdx = 0;

            foreach (Match optMatch in optionMatches)
            {
                string optText = optMatch.Groups[1].Value.Trim();

                // 헤더 옵션 스킵 (= ... 선택 = 패턴)
                if (optText.StartsWith("=") || optText.Contains("선택해") || optText.Contains("선택="))
                    continue;

                if (string.IsNullOrWhiteSpace(optText)) continue;

                // 품절 체크
                bool isSoldOut = optMatch.Groups[0].Value.Contains("disabled");
                if (isSoldOut) continue;

                string id = $"0_{optIdx}";

                var group = new Option.OptionGroup.Group();
                group.groupId = id;
                group.optionName = new List<Name> { new Name(Language.KR, optText) };
                optionGroup.groups.Add(group);

                var detailOption = new Option.DetailOption();
                detailOption.optionIds = id;
                detailOption.optionGroupName = new List<Name> { new Name(Language.KR, groupName) };
                detailOption.optionName = new List<Name> { new Name(Language.KR, optText) };
                detailOption.price = new List<Price>
                {
                    new Price(Currency.KRW, basePrice, basePrice)
                };
                detailOptions.Add(detailOption);

                optIdx++;
            }

            if (optionGroup.groups.Count == 0 || detailOptions.Count == 0)
                return GetDefaultOption();

            return new Option
            {
                optionGroups = new List<Option.OptionGroup> { optionGroup },
                detailOptions = detailOptions
            };
        }

        private string ExtractGroupName(string selectHtml)
        {
            // =그룹명 선택= 패턴에서 그룹명 추출
            var headerMatch = Regex.Match(selectHtml,
                @"<option[^>]*value=""""[^>]*>=\s*([^=<]+?)\s*선택\s*=</option>",
                RegexOptions.IgnoreCase);
            if (headerMatch.Success)
            {
                string name = headerMatch.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }

            // 일반 헤더 옵션
            var generalMatch = Regex.Match(selectHtml,
                @"<option[^>]*value=""""[^>]*>(?:[-= ]*)?([^-=<]+?)(?:[-= ]*)?</option>",
                RegexOptions.IgnoreCase);
            if (generalMatch.Success)
            {
                string text = generalMatch.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(text) && text != "옵션선택" && text != "옵션을 선택해 주세요")
                    return text;
            }

            return "옵션";
        }

        #endregion

        #region 유틸

        private string GetMainImageUrl(string html)
        {
            var ogMatch = Regex.Match(html, @"<meta\s+property=""og:image""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
            if (ogMatch.Success) return ogMatch.Groups[1].Value;
            return null;
        }

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
