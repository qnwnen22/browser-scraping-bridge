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

namespace BrowserScrapingBridge.Server.Sourcing.Yes24
{
    public class Yes24 : CollectBase
    {
        public override string Site => nameof(Yes24);
        public override string Name => "YES24";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = false;

        private const string BASE_URL = "https://www.yes24.com";
        private static readonly HttpClient _httpClient;

        static Yes24()
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
            var match = Regex.Match(url, @"/Goods/(\d+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : url;
        }

        public override string CreateProductUrl(string code)
        {
            return $"{BASE_URL}/Product/Goods/{code}";
        }

        public override Task<string> GetLoginUrlAsync(string connectionId)
        {
            return Task.FromResult<string>(null);
        }

        public override async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync();
            // Yes24가 invalid charset 반환 시 ReadAsStringAsync 실패 → 바이트로 읽어서 UTF-8 디코딩
            return Encoding.UTF8.GetString(bytes);
        }

        #region 카테고리 수집

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            string requestUrl = BuildCategoryUrl(url, page);
            string html = await GetHtmlAsync(connectionId, requestUrl);
            if (string.IsNullOrWhiteSpace(html)) return null;

            var result = new List<CollectPageInfo>();
            var seenCodes = new HashSet<string>();

            // 상품 링크 추출: href="/product/goods/12345" 또는 href="/Product/Goods/12345"
            var matches = Regex.Matches(html, @"href=""(/[Pp]roduct/[Gg]oods/(\d+))""", RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                string code = match.Groups[2].Value;
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

        private string BuildCategoryUrl(string url, int page)
        {
            // More URL → AJAX 엔드포인트 변환
            // /category/more/001001003?ElemNo=208&ElemSeq=3
            // → CategoryProductMoreContents?dispNo=001001003&elemNo=208&elemSeq=3&page=1&size=24
            var moreMatch = Regex.Match(url, @"/category/more/(\d+)", RegexOptions.IgnoreCase);
            if (moreMatch.Success)
            {
                string dispNo = moreMatch.Groups[1].Value;
                string elemNo = Regex.Match(url, @"ElemNo=(\d+)", RegexOptions.IgnoreCase) is { Success: true } en ? en.Groups[1].Value : "0";
                string elemSeq = Regex.Match(url, @"ElemSeq=(\d+)", RegexOptions.IgnoreCase) is { Success: true } es ? es.Groups[1].Value : "0";
                return $"{BASE_URL}/product/category/CategoryProductMoreContents?dispNo={dispNo}&order=SINDEX_ONLY&page={page}&size=24&elemNo={elemNo}&elemSeq={elemSeq}&seriesNumber=0";
            }

            // BestSeller URL → AJAX 엔드포인트 변환
            // /category/bestseller?categoryNumber=001001003
            // → BestSellerContents?categoryNumber=001001003&pageNumber=1&pageSize=24
            if (url.Contains("bestseller", StringComparison.OrdinalIgnoreCase))
            {
                var catMatch = Regex.Match(url, @"categoryNumber=(\d+)", RegexOptions.IgnoreCase);
                string categoryNumber = catMatch.Success ? catMatch.Groups[1].Value : "001";
                return $"{BASE_URL}/product/category/BestSellerContents?categoryNumber={categoryNumber}&pageNumber={page}&pageSize=24";
            }

            // Display URL → BestSeller AJAX로 변환
            var displayMatch = Regex.Match(url, @"/Display/(\d+)", RegexOptions.IgnoreCase);
            if (displayMatch.Success)
            {
                string categoryNumber = displayMatch.Groups[1].Value;
                return $"{BASE_URL}/product/category/BestSellerContents?categoryNumber={categoryNumber}&pageNumber={page}&pageSize=24";
            }

            // Search URL → page 파라미터 교체
            if (url.Contains("Search", StringComparison.OrdinalIgnoreCase))
            {
                string baseUrl = Regex.Replace(url, @"[&?]page=\d+", "", RegexOptions.IgnoreCase);
                string separator = baseUrl.Contains("?") ? "&" : "?";
                return $"{baseUrl}{separator}page={page}";
            }

            // CategoryNumber 쿼리 파라미터가 있으면 BestSeller AJAX로
            var catNumMatch = Regex.Match(url, @"categoryNumber=(\d+)", RegexOptions.IgnoreCase);
            if (catNumMatch.Success)
            {
                string categoryNumber = catNumMatch.Groups[1].Value;
                return $"{BASE_URL}/product/category/BestSellerContents?categoryNumber={categoryNumber}&pageNumber={page}&pageSize=24";
            }

            // 기본: 원본 URL에 pageNumber 추가
            string sep = url.Contains("?") ? "&" : "?";
            return $"{url}{sep}pageNumber={page}";
        }

        private string ExtractTitleNear(string html, int linkIndex)
        {
            int searchStart = Math.Max(0, linkIndex - 200);
            int searchEnd = Math.Min(html.Length, linkIndex + 500);
            string context = html.Substring(searchStart, searchEnd - searchStart);

            // alt 속성에서 제목 추출
            var altMatch = Regex.Match(context, @"alt=""([^""]{5,})""", RegexOptions.IgnoreCase);
            if (altMatch.Success) return altMatch.Groups[1].Value.Trim();

            // title 속성에서 제목 추출
            var titleMatch = Regex.Match(context, @"title=""([^""]{5,})""", RegexOptions.IgnoreCase);
            if (titleMatch.Success) return titleMatch.Groups[1].Value.Trim();

            // 링크 텍스트에서 제목 추출
            var linkTextMatch = Regex.Match(context, @">[^<]{5,}<", RegexOptions.IgnoreCase);
            if (linkTextMatch.Success) return linkTextMatch.Value.Trim('>', '<').Trim();

            return null;
        }

        #endregion

        #region CollectBase 구현

        public override void ThrowIfCaptcha(string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new CollectException("페이지를 불러올 수 없습니다.");

            if (html.Contains("전시제한된 상품입니다"))
                throw new CollectException("전시제한된 상품입니다.");
        }

        public override Task<JToken> GetData(string connectionId, string url, string html)
        {
            var data = new JObject();

            // JSON-LD 추출
            var ldMatch = Regex.Match(html,
                @"<script\s+type=""application/ld\+json""[^>]*>\s*(\{.*?\})\s*</script>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (ldMatch.Success)
            {
                try
                {
                    var jsonLd = JObject.Parse(ldMatch.Groups[1].Value);
                    data["jsonLd"] = jsonLd;
                }
                catch { }
            }

            // JS 전역변수 추출 (var 선언 + static_variables. 둘 다)
            ExtractJsVar(html, data, "goods_no");
            ExtractJsVar(html, data, "g_GoodsSalePrice");
            ExtractJsVar(html, data, "g_GoodsShopPrice");
            ExtractJsVar(html, data, "isOptionGoods");

            return Task.FromResult<JToken>(data);
        }

        private void ExtractJsVar(string html, JObject data, string varName)
        {
            // var goods_no = "175122641"; 형태
            var match = Regex.Match(html, $@"var\s+{Regex.Escape(varName)}\s*=\s*([^;\r\n]+);");
            if (match.Success)
            {
                string value = match.Groups[1].Value.Trim().Trim('"', '\'', ' ');
                data[varName] = value;
            }

            // static_variables.isOptionGoods = "N"; 형태
            var staticMatch = Regex.Match(html, $@"static_variables\.{Regex.Escape(varName)}\s*=\s*([^;\r\n]+);");
            if (staticMatch.Success && !data.ContainsKey(varName))
            {
                string value = staticMatch.Groups[1].Value.Trim().Trim('"', '\'', ' ');
                data[varName] = value;
            }
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string title = null;

            // JSON-LD name
            title = data?["jsonLd"]?["name"]?.ToString();

            // og:title 폴백
            if (string.IsNullOrWhiteSpace(title))
            {
                var ogMatch = Regex.Match(html, @"<meta\s+property=""og:title""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
                if (ogMatch.Success)
                    title = ogMatch.Groups[1].Value.Trim();
            }

            // <h2 class="gd_name"> 폴백
            if (string.IsNullOrWhiteSpace(title))
            {
                var h2Match = Regex.Match(html, @"<h2[^>]*class=""gd_name""[^>]*>([^<]+)</h2>", RegexOptions.IgnoreCase);
                if (h2Match.Success)
                    title = h2Match.Groups[1].Value.Trim();
            }

            if (string.IsNullOrWhiteSpace(title))
                throw new CollectException("상품명을 찾을 수 없습니다.");

            return ProductHelper.CreateName(Language.KR, title);
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            // 도서: 출판사를 브랜드로
            string publisher = data?["jsonLd"]?["publisher"]?["name"]?.ToString();
            if (!string.IsNullOrWhiteSpace(publisher))
                return ProductHelper.CreateName(Language.KR, publisher);

            return null;
        }

        public override string GetModel(string html, JToken data) => null;
        public override List<Desc> GetDescs(string html, JToken data) => null;
        public override List<Tag> GetTags(string html, JToken data) => null;

        public override List<Price> GetPrice(string html, JToken data)
        {
            double originalPrice = 0;  // 정가 (g_GoodsShopPrice)
            double salePrice = 0;      // 판매가 (g_GoodsSalePrice)

            // JS 변수: g_GoodsShopPrice = 정가, g_GoodsSalePrice = 판매가
            string shopPriceStr = data?["g_GoodsShopPrice"]?.ToString();
            if (!string.IsNullOrWhiteSpace(shopPriceStr))
                double.TryParse(shopPriceStr.Replace(",", ""), out originalPrice);

            string salePriceStr = data?["g_GoodsSalePrice"]?.ToString();
            if (!string.IsNullOrWhiteSpace(salePriceStr))
                double.TryParse(salePriceStr.Replace(",", ""), out salePrice);

            // JSON-LD 폴백 (offers.price = 판매가)
            if (salePrice <= 0)
            {
                var offersPrice = data?["jsonLd"]?["offers"]?["price"];
                if (offersPrice != null)
                    double.TryParse(offersPrice.ToString(), out salePrice);
            }

            // HTML 폴백: 정가 <em class="yes_m">19,000원</em>
            if (originalPrice <= 0)
            {
                var shopPriceMatch = Regex.Match(html,
                    @"정가\s*</th>\s*<td[^>]*>\s*(?:<[^>]+>)*\s*([\d,]+)",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (shopPriceMatch.Success)
                    double.TryParse(shopPriceMatch.Groups[1].Value.Replace(",", ""), out originalPrice);
            }

            // HTML 폴백: 판매가 <span class="nor_price"><em class="yes_m">17,100</em>
            if (salePrice <= 0)
            {
                var norPriceMatch = Regex.Match(html,
                    @"class=""[^""]*nor_price[^""]*""[^>]*>\s*(?:<[^>]+>)*\s*([\d,]+)",
                    RegexOptions.IgnoreCase);
                if (norPriceMatch.Success)
                    double.TryParse(norPriceMatch.Groups[1].Value.Replace(",", ""), out salePrice);
            }

            if (salePrice <= 0)
                throw new CollectException("가격 정보를 찾을 수 없습니다.");

            // 정가가 없으면 판매가와 동일
            if (originalPrice <= 0)
                originalPrice = salePrice;

            // Price.originalPrice = 정가, Price.salePrice = 판매가
            var price = ProductHelper.CreatePrice(Currency.KRW, originalPrice, salePrice, true);
            price[0].isoCode = "KRW";
            return price;
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();

            string code = data?["goods_no"]?.ToString();
            if (string.IsNullOrWhiteSpace(code))
            {
                // JSON-LD에서 코드 추출 시도
                string jsonLdUrl = data?["jsonLd"]?["url"]?.ToString();
                if (!string.IsNullOrWhiteSpace(jsonLdUrl))
                    code = GetCode(jsonLdUrl);
            }

            // Yes24 표준 이미지 URL
            if (!string.IsNullOrWhiteSpace(code))
                images.Add($"https://image.yes24.com/goods/{code}/XL");

            // og:image 폴백
            if (images.Count == 0)
            {
                var ogMatch = Regex.Match(html, @"<meta\s+property=""og:image""\s+content=""([^""]+)""", RegexOptions.IgnoreCase);
                if (ogMatch.Success)
                    images.Add(ogMatch.Groups[1].Value);
            }

            // JSON-LD image 폴백
            if (images.Count == 0)
            {
                string ldImage = data?["jsonLd"]?["image"]?.ToString();
                if (!string.IsNullOrWhiteSpace(ldImage))
                    images.Add(ldImage);
            }

            if (images.Count == 0)
                throw new CollectException("메인 이미지를 찾을 수 없습니다.");

            return images;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            // <textarea class="txtContentText"> 안의 HTML 추출
            var textareaMatch = Regex.Match(html,
                @"<textarea[^>]*class=""txtContentText""[^>]*>(.*?)</textarea>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (textareaMatch.Success && textareaMatch.Groups[1].Length > 10)
                return textareaMatch.Groups[1].Value;

            // #infoset_introduce 영역 폴백
            var introMatch = Regex.Match(html,
                @"id=""infoset_introduce""[^>]*>(.*?)<div[^>]*id=""infoset_",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (introMatch.Success && introMatch.Groups[1].Length > 10)
                return introMatch.Groups[1].Value;

            return null;
        }

        public override Option GetOption(string html, JToken data)
        {
            string isOptionGoods = data?["isOptionGoods"]?.ToString();

            // 옵션 상품이 아니면 원사이즈 기본 옵션
            if (isOptionGoods != "Y")
                return GetDefaultOption();

            // 옵션 상품: select 태그에서 파싱
            var selectMatch = Regex.Match(html,
                @"<select[^>]*id=""[^""]*[Oo]ption[^""]*""[^>]*>(.*?)</select>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!selectMatch.Success)
                return GetDefaultOption();

            var optionGroup = new Option.OptionGroup();
            optionGroup.optionId = "0";
            optionGroup.groupName = new List<Name> { new Name(Language.KR, "옵션") };
            optionGroup.groups = new List<Option.OptionGroup.Group>();

            var detailOptions = new List<Option.DetailOption>();

            var optionMatches = Regex.Matches(selectMatch.Groups[1].Value,
                @"<option\s+value=""([^""]+)""[^>]*>([^<]+)</option>",
                RegexOptions.IgnoreCase);

            int idx = 0;
            foreach (Match optMatch in optionMatches)
            {
                string optValue = optMatch.Groups[1].Value.Trim();
                string optText = optMatch.Groups[2].Value.Trim();

                if (string.IsNullOrWhiteSpace(optValue) || optValue == "0") continue;
                if (optText.Contains("선택")) continue;

                bool isSoldOut = optText.Contains("품절") || optMatch.Value.Contains("disabled");
                string optName = Regex.Replace(optText, @"\s*[\[\(]품절[\]\)]\s*", "").Trim();

                string id = idx.ToString();

                optionGroup.groups.Add(new Option.OptionGroup.Group
                {
                    groupId = id,
                    optionName = new List<Name> { new Name(Language.KR, optName) }
                });

                var detailOption = new Option.DetailOption();
                detailOption.optionIds = id;
                detailOption.optionGroupName = new List<Name> { new Name(Language.KR, "옵션") };
                detailOption.optionName = new List<Name> { new Name(Language.KR, optName) };
                detailOption.isSoldOut = isSoldOut;
                detailOption.quantity = isSoldOut ? 0 : 9;
                detailOptions.Add(detailOption);

                idx++;
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

        public override void ThrowIfOutOfStock(string html, JToken data, Product product) { }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            // detailOption.price가 null이면 product.price로 채움 (CheckCollectLimit에서 .First() 호출)
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
