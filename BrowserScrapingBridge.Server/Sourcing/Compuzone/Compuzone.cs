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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Compuzone
{
    public class Compuzone : CollectBase
    {
        public override string Site => nameof(Compuzone);
        public override string Name => "컴퓨존";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = false;

        private const string BASE_URL = "https://www.compuzone.co.kr";
        private const string IMAGE_HOST = "https://image3.compuzone.co.kr";

        private static readonly Encoding EucKr;

        static Compuzone()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            EucKr = Encoding.GetEncoding("euc-kr");
        }

        /// <summary>
        /// EUC-KR 인코딩 처리 — ChatHubService.GetRequest는 UTF-8 디코딩이라 한글 깨짐
        /// </summary>
        public override async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            using var client = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient();
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept-Language", "ko-KR,ko;q=0.9");

            var response = await client.GetAsync(url);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            return EucKr.GetString(bytes);
        }

        public override string GetCode(string url)
        {
            var match = Regex.Match(url, @"ProductNo=(\d+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : url;
        }

        public override string CreateProductUrl(string code)
        {
            return $"{BASE_URL}/product/product_detail.htm?ProductNo={code}";
        }

        public override Task<string> GetLoginUrlAsync(string connectionId)
        {
            return Task.FromResult<string>(null);
        }

        #region 카테고리 수집

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            Uri uri = new Uri(url);
            var queries = System.Web.HttpUtility.ParseQueryString(uri.Query);
            string bigDivNo = queries["BigDivNo"] ?? "";
            string mediumDivNo = queries["MediumDivNo"] ?? "";
            string divNo = queries["DivNo"] ?? "";

            int pageCount = 60;
            int startNum = (page - 1) * pageCount;

            using var client = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient();
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            client.DefaultRequestHeaders.Add("Referer", url);

            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("actype", "getList"),
                new KeyValuePair<string, string>("BigDivNo", bigDivNo),
                new KeyValuePair<string, string>("MediumDivNo", mediumDivNo),
                new KeyValuePair<string, string>("DivNo", divNo),
                new KeyValuePair<string, string>("PageCount", pageCount.ToString()),
                new KeyValuePair<string, string>("StartNum", startNum.ToString()),
                new KeyValuePair<string, string>("ScrollPage", page.ToString()),
                new KeyValuePair<string, string>("ProductType", "biglist"),
                new KeyValuePair<string, string>("PageType", "ProductListB"),
                new KeyValuePair<string, string>("PreOrder", "recommand"),
                new KeyValuePair<string, string>("lvm", "L"),
            });

            var response = await client.PostAsync($"{BASE_URL}/product/product_list.php?Blist=1", formData);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            string html = EucKr.GetString(bytes);

            if (string.IsNullOrWhiteSpace(html)) return null;

            var result = new List<CollectPageInfo>();
            var seenCodes = new HashSet<string>();

            var matches = Regex.Matches(html, @"product_detail\.htm\?ProductNo=(\d+)", RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                string code = match.Groups[1].Value;
                if (seenCodes.Contains(code)) continue;
                seenCodes.Add(code);

                string title = ExtractTitleNear(html, match.Index);

                result.Add(new CollectPageInfo
                {
                    Code = code,
                    Url = CreateProductUrl(code),
                    Title = title?.Trim()
                });
            }

            if (result.Count == 0) return null;

            CollectExtension.CollectSleep();
            return result;
        }

        private string ExtractTitleNear(string html, int linkIndex)
        {
            int searchStart = Math.Max(0, linkIndex - 100);
            int searchEnd = Math.Min(html.Length, linkIndex + 500);
            string context = html.Substring(searchStart, searchEnd - searchStart);

            var altMatch = Regex.Match(context, @"alt=""([^""]{5,})""", RegexOptions.IgnoreCase);
            if (altMatch.Success) return altMatch.Groups[1].Value.Trim();

            var titleMatch = Regex.Match(context, @"title=""([^""]{5,})""", RegexOptions.IgnoreCase);
            if (titleMatch.Success) return titleMatch.Groups[1].Value.Trim();

            return null;
        }

        #endregion

        #region CollectBase 구현

        public override void ThrowIfCaptcha(string url, string html)
        {
        }

        public override Task<JToken> GetData(string connectionId, string url, string html)
        {
            var data = new JObject();

            // URL에서 코드 추출 (가장 신뢰도 높음)
            var urlMatch = Regex.Match(url, @"ProductNo=(\d+)", RegexOptions.IgnoreCase);
            if (urlMatch.Success)
                data["code"] = urlMatch.Groups[1].Value;

            ExtractJsVar(html, data, "ProductNo");
            ExtractJsVar(html, data, "produc_price");
            ExtractJsVar(html, data, "RelCardPrice");
            ExtractJsVar(html, data, "SalePrice");
            ExtractJsVar(html, data, "onlyTextOptionChk");

            var cookieMatch = Regex.Match(html, @"todayCookie\s*\(\s*'[^']*'\s*,\s*""(\d{4})-(\d{2})-(\d{2})");
            if (cookieMatch.Success)
            {
                data["imageYear"] = cookieMatch.Groups[1].Value;
                data["imageMonth"] = cookieMatch.Groups[2].Value;
                data["imageDay"] = cookieMatch.Groups[3].Value;
            }

            return Task.FromResult<JToken>(data);
        }

        private void ExtractJsVar(string html, JObject data, string varName)
        {
            var match = Regex.Match(html, $@"var\s+{Regex.Escape(varName)}\s*=\s*([^;\r\n]+);");
            if (match.Success)
            {
                string value = match.Groups[1].Value.Trim().Trim('"', '\'', ' ');
                data[varName] = value;
            }
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string title = null;

            var titleMatch = Regex.Match(html, @"<title>\s*(.+?)\s*</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (titleMatch.Success)
            {
                title = titleMatch.Groups[1].Value.Trim();
                title = Regex.Replace(title, @"\s*[-:]\s*컴퓨존\s*$", "").Trim();
            }

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

        public override List<Name> GetBrand(string html, JToken data)
        {
            var titleMatch = Regex.Match(html, @"<title>\s*(.+?)\s*</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (titleMatch.Success)
            {
                var brandMatch = Regex.Match(titleMatch.Groups[1].Value, @"^\s*\[([^\]]+)\]");
                if (brandMatch.Success)
                    return new List<Name> { new Name(Language.KR, brandMatch.Groups[1].Value.Trim()) };
            }
            return null;
        }

        public override string GetModel(string html, JToken data) => null;
        public override List<Desc> GetDescs(string html, JToken data) => null;
        public override List<Tag> GetTags(string html, JToken data) => null;

        public override List<Price> GetPrice(string html, JToken data)
        {
            double listPrice = 0;
            double salePrice = 0;

            if (data != null)
            {
                double.TryParse(data["produc_price"]?.ToString(), out listPrice);
                double.TryParse(data["SalePrice"]?.ToString(), out salePrice);

                if (salePrice <= 0)
                    double.TryParse(data["RelCardPrice"]?.ToString(), out salePrice);
            }

            if (listPrice <= 0 && salePrice <= 0)
            {
                var priceMatch = Regex.Match(html, @"(\d[\d,]+)\s*원");
                if (priceMatch.Success)
                {
                    string priceStr = priceMatch.Groups[1].Value.Replace(",", "");
                    double.TryParse(priceStr, out salePrice);
                    listPrice = salePrice;
                }
            }

            if (listPrice <= 0 && salePrice <= 0)
                throw new CollectException("가격 정보를 찾을 수 없습니다.");

            if (salePrice <= 0 || salePrice > listPrice)
                salePrice = listPrice;
            if (listPrice <= 0)
                listPrice = salePrice;

            var price = new Price(Currency.KRW, listPrice, salePrice, true);
            price.isoCode = "KRW";
            return new List<Price> { price };
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();

            // mainImg 클래스에서 이미지 추출 (1개: mainImg, 여러개: mainImgThumb)
            var thumbMatches = Regex.Matches(html,
                @"<li[^>]*class=""mainImg[^""]*""[^>]*>.*?<img\s+src=""([^""]+)""",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match match in thumbMatches)
            {
                string imgUrl = match.Groups[1].Value;
                if (!imgUrl.StartsWith("http")) imgUrl = "https:" + imgUrl;
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
            // URL 코드 우선, JS 변수 폴백
            string code = data?["code"]?.ToString();
            if (string.IsNullOrEmpty(code))
                code = data?["ProductNo"]?.ToString();
            if (string.IsNullOrEmpty(code))
            {
                var m = Regex.Match(html, @"ProductNo=(\d+)", RegexOptions.IgnoreCase);
                if (m.Success) code = m.Groups[1].Value;
            }
            if (string.IsNullOrEmpty(code)) return null;

            try
            {
                using var client = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient();
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                client.DefaultRequestHeaders.Add("Referer", $"{BASE_URL}/product/product_detail.htm?ProductNo={code}");

                var response = client.GetAsync($"{BASE_URL}/product/product_detail_ajax.php?actype=getProductDetail&productNo={code}").Result;
                var bytes = response.Content.ReadAsByteArrayAsync().Result;
                string detailHtml = EucKr.GetString(bytes);

                if (string.IsNullOrWhiteSpace(detailHtml)) return null;
                return detailHtml;
            }
            catch
            {
                return null;
            }
        }

        public override Option GetOption(string html, JToken data)
        {
            var selectMatches = Regex.Matches(html,
                @"<select[^>]*name=""opt\[\]""[^>]*>(.*?)</select>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (selectMatches.Count == 0)
            {
                selectMatches = Regex.Matches(html,
                    @"<select[^>]*id=""[^""]*[Oo]pt[^""]*""[^>]*>(.*?)</select>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            double basePrice = 0;
            if (data != null)
            {
                double.TryParse(data["SalePrice"]?.ToString(), out basePrice);
                if (basePrice <= 0)
                    double.TryParse(data["produc_price"]?.ToString(), out basePrice);
            }

            if (selectMatches.Count == 0)
                return GetDefaultOption(basePrice);

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

                string groupName = "옵션";
                var headerMatch = Regex.Match(selectHtml,
                    @"<option[^>]*value=""""[^>]*>(?:[-= ]*)?([^-=<]+?)(?:[-= ]*)?</option>",
                    RegexOptions.IgnoreCase);
                if (headerMatch.Success)
                {
                    string headerText = headerMatch.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(headerText) &&
                        headerText != "옵션선택" &&
                        headerText != "옵션을 선택해 주세요" &&
                        headerText.Length > 1)
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

                    var addPriceMatch = Regex.Match(optText, @"\(\+?\s*([\d,]+)\s*원?\)");
                    if (addPriceMatch.Success)
                    {
                        string addPriceStr = addPriceMatch.Groups[1].Value.Replace(",", "");
                        double.TryParse(addPriceStr, out addPrice);
                        optName = optText.Substring(0, addPriceMatch.Index).Trim();
                    }

                    var subPriceMatch = Regex.Match(optText, @"\(-\s*([\d,]+)\s*원?\)");
                    if (subPriceMatch.Success)
                    {
                        string subPriceStr = subPriceMatch.Groups[1].Value.Replace(",", "");
                        double subPrice;
                        if (double.TryParse(subPriceStr, out subPrice))
                            addPrice = -subPrice;
                        optName = optText.Substring(0, subPriceMatch.Index).Trim();
                    }

                    if (optMatch.Groups[0].Value.Contains("disabled")) continue;
                    if (optText.Contains("품절") || optText.Contains("[품절]")) continue;

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
                return GetDefaultOption(basePrice);

            return new Option
            {
                optionGroups = optionGroups,
                detailOptions = detailOptions
            };
        }

        public override int? GetTotalQuantity(string html, JToken data) => null;

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
        }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            await Task.CompletedTask;
        }

        #endregion

        #region 헬퍼

        private Option GetDefaultOption(double price = 0)
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
            if (price > 0)
            {
                detailOption.price = new List<Price>
                {
                    new Price(Currency.KRW, price, price)
                };
            }

            return new Option
            {
                optionGroups = new List<Option.OptionGroup> { optionGroup },
                detailOptions = new List<Option.DetailOption> { detailOption }
            };
        }

        #endregion
    }
}
