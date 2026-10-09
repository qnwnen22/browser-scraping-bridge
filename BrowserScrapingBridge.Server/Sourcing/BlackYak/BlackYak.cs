#nullable disable warnings
// BlackYak.cs
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

namespace BrowserScrapingBridge.Server.Sourcing.BlackYak
{
    /// <summary>
    /// BlackYak 수집기 (byn.kr)
    /// 핵심 처리 흐름:
    /// 1) 리스트 URL + page 파라미터를 이용해 페이징
    /// 2) 상품 목록(URL/pno)을 수집
    /// 3) 상품 상세에서 정보/가격/옵션 파싱
    /// 4) Product 모델을 반환
    ///
    /// 주의(프로젝트 기준):
    /// - ChatHubService는 크롬 확장 기반 요청(쿠키/JS/옵션 로딩 전제)
    /// - product.option.optionGroups 절대 null 금지 (옵션 없어도 빈 구조 반환)
    /// </summary>
    public class BlackYak : ICollect
    {
        private const string Host = "https://www.byn.kr";

        public string Site => nameof(BlackYak);
        public string Name => "블랙야크";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;

        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        public Task<string> GetLoginUrlAsync(string connectionId) => Task.FromResult<string>(null);

        public string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            var uri = new Uri(url);
            var query = uri.Query;
            if (string.IsNullOrWhiteSpace(query)) return null;

            // byn.kr 상세는 pno=... 가 핵심 키
            string code = GetQueryValue(query, "pno")
                ?? GetQueryValue(query, "goodsno")
                ?? GetQueryValue(query, "goodsNo")
                ?? GetQueryValue(query, "product_no")
                ?? GetQueryValue(query, "no");

            return string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilder = new UriBuilderAdvance(url);
            uriBuilder.SetQuery("page", page.ToString());

            string pageUrl = uriBuilder.ToString();

            // 리스트 페이지 HTML (상품상세 아님)
            string html = await ChatHubService.GetRequest(connectionId, pageUrl);
            if (string.IsNullOrWhiteSpace(html)) return null;

            // 진단용: ChatHubService가 body 대신 status만 주는 케이스 방지
            string trimmed = html.Trim();
            if (trimmed == "200") return null;

            // page > 1: 요청한 페이지가 실제 페이지네이션(#paging_box)에 존재하는지 확인
            // byn.kr는 존재하지 않는 페이지 요청 시 다른 카테고리 HTML을 반환하는데,
            // 그 카테고리 자체는 page=N이 있을 수 있으므로 카테고리 파라미터까지 이중 검증
            if (page > 1)
            {
                string pagingHtml = html.GetValue("//*[@id='paging_box']", TextEx.XPathOption.InnerHtml);
                // 현재 페이지는 활성 상태(링크 아닌 텍스트 <strong>2</strong> 등)로 표시되므로
                // URL 파라미터 page=N 뿐 아니라 표시 텍스트 >N< 도 허용
                if (string.IsNullOrWhiteSpace(pagingHtml) ||
                    (!Regex.IsMatch(pagingHtml, $@"[?&]page={page}[&""'\s]", RegexOptions.IgnoreCase) &&
                     !Regex.IsMatch(pagingHtml, $@">\s*{page}\s*</", RegexOptions.IgnoreCase)))
                {
                    return null;
                }

                // 원본 URL의 카테고리 파라미터(cno1 등)가 pagination 링크에도 포함되어야 함
                // 포함되지 않으면 다른 카테고리의 pagination → 잘못된 페이지
                var qs = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query);
                foreach (string key in qs.AllKeys)
                {
                    if (string.Equals(key, "page", StringComparison.OrdinalIgnoreCase)) continue;
                    string val = qs[key];
                    if (!string.IsNullOrWhiteSpace(val) && !pagingHtml.Contains($"{key}={val}"))
                        return null;
                }
            }

            var pageInfos = new List<CollectPageInfo>();
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 전체 HTML 대신 #prd_basic 영역만 검색
            // → 헤더/풋터/레코픽 추천 등 다른 카테고리 상품 링크가 섞이는 것을 방지
            string productListHtml = html.GetValue("//*[@id='prd_basic']", TextEx.XPathOption.InnerHtml);
            if (string.IsNullOrWhiteSpace(productListHtml)) return null;

            // #prd_basic 안에서 detail.php?pno=... 링크를 수집
            var hrefMatches = Regex.Matches(
                productListHtml,
                "href=[\"'](?<url>[^\"']*detail\\.php\\?[^\"']*pno=[^\"']+)[\"']",
                RegexOptions.IgnoreCase
            );

            if (hrefMatches.Count == 0) return null;

            foreach (Match match in hrefMatches)
            {
                string href = match.Groups["url"]?.Value;
                if (string.IsNullOrWhiteSpace(href)) continue;

                string itemUrl = ToAbsoluteUrl(href);
                // 리스트 페이지에는 p.code 없음 → URL에서만 추출. 상세 수집 시 GetProductAsync에서 상세 HTML의 p.code 사용
                string code = GetCode(itemUrl);
                if (string.IsNullOrWhiteSpace(code)) continue;
                if (!unique.Add(code)) continue;

                string title = ExtractTitleNearLink(html, href);

                pageInfos.Add(new CollectPageInfo
                {
                    Code = code,
                    Url = itemUrl,
                    Title = title
                });
            }

            return pageInfos.Count == 0 ? null : pageInfos;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);

            var product = new Product();

            try
            {
                if (string.IsNullOrWhiteSpace(html)) throw new Exception("상품 상세 HTML을 가져오지 못했습니다.");
                if (html.Trim() == "200") throw new Exception("상품 상세 HTML 대신 상태코드만 반환되었습니다.(200)");

                // 상품코드: 상세 페이지에서 h3.name ~ div.Price 사이 p.code 텍스트 취합, 없으면 URL에서 추출
                string code = ExtractCodeBetweenNameAndPrice(html, null) ?? GetCode(url);
                if (string.IsNullOrWhiteSpace(code)) throw new Exception("상품 코드를 찾을 수 없습니다.(pno)");

                product.site = Site;
                product.code = code;
                product.url = url;
                product.currency = Currency.ToString();

                string title = GetTitle(html);

                // option1 컬러는 title 뒤에 합침
                string color = GetOption1ValueFromHiddenRow(html); // fallback

                if (!string.IsNullOrWhiteSpace(color))
                {
                    title = $"{title} {color}".Trim();
                }

                product.title = ProductHelper.CreateName(Language.KR, title);

                string brand = GetBrand(html);
                if (!string.IsNullOrWhiteSpace(brand))
                {
                    product.brand = new List<Name> { new Name(Language.KR, brand) };
                }

                product.price = GetPrice(html);
                product.mainImages = GetMainImages(html);
                product.detailPageHtml = GetDetailPageHtml(html);

                double defaultSalePrice = product.price?.FirstOrDefault()?.salePrice ?? 0d;
                product.option = GetOption(html, defaultSalePrice); // ✅ optionGroups 절대 null 금지
                product.regDate = DateTime.Now;
            }
            catch (Exception exception)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = exception.Message;
            }

            return product;
        }


        private static string GetOption1ValueFromHiddenRow(string html)
        {
            // 1순위: 숨김 컬러 tr 내부의 text_option 첫 a 텍스트
            string v = html.GetValue("//tr[contains(@style,'display:none')][.//th[contains(.,'컬러')]]//ul[contains(@class,'text_option')]//a",
                                     TextEx.XPathOption.InnerText);

            if (!string.IsNullOrWhiteSpace(v))
                return WebUtility.HtmlDecode(v).Trim();

            // 2순위: hidden input option1의 option-data='BEIGE::0.00::...'
            string optionData = html.GetValue("//input[@name='option1']", "option-data");
            if (!string.IsNullOrWhiteSpace(optionData))
            {
                var parts = optionData.Split(new[] { "::" }, StringSplitOptions.None);
                if (parts.Length > 0)
                    return WebUtility.HtmlDecode(parts[0]).Trim();
            }

            return null;
        }


        private static string GetTitle(string html)
        {
            string title = html.GetValue("//input[@name='product_name']", "value");
            if (string.IsNullOrWhiteSpace(title)) title = html.GetValue("//title", TextEx.XPathOption.InnerText);
            if (string.IsNullOrWhiteSpace(title)) title = html.GetValue("//p[contains(@class,'name')]//a", TextEx.XPathOption.InnerText);
            if (string.IsNullOrWhiteSpace(title)) title = html.GetValue("//h3", TextEx.XPathOption.InnerText);
            if (string.IsNullOrWhiteSpace(title)) throw new Exception("상품명을 찾을 수 없습니다.");

            return title.Trim();
        }

        private static string GetBrand(string html)
        {
            // og:site_name이 없거나 리스트 페이지인 경우도 있으므로 기본값 보강
            string brand = html.GetValue("//meta[@property='og:site_name']", "content");
            if (string.IsNullOrWhiteSpace(brand)) brand = "BLACKYAK";

            // 3️⃣ 불필요 문구 제거
            string[] removeWords =
            {
                "공식몰",
                "공식",
                "몰",
                "비와이엔",
                "BYN",
                "㈜",
                "(주)"
            };

            foreach (string word in removeWords)
            {
                brand = brand.Replace(word, "", StringComparison.OrdinalIgnoreCase);
            }

            return brand.Trim();
        }

        private static List<Price> GetPrice(string html)
        {
            // ✅ 상세 페이지 기준: data-rel이 가장 신뢰도 높음
            double? original = TryParseNumber(html.GetValue("//dd[contains(@class,'normal_price')]", "data-rel"))
                            ?? TryParseNumber(html.GetValue("//dd[contains(@class,'normal_price')]", TextEx.XPathOption.InnerText));

            double? sale = TryParseNumber(html.GetValue("//dd[contains(@class,'sell_price')]", "data-rel"))
                        ?? TryParseNumber(html.GetValue("//dd[contains(@class,'sell_price')]", TextEx.XPathOption.InnerText));

            if (original == null && sale == null)
                throw new Exception("상품 가격을 찾을 수 없습니다. (normal_price/sell_price)");

            double salePrice = sale ?? original!.Value;
            double originalPrice = original ?? salePrice;

            //return new List<Price> { new Price(Currency.KRW, originalPrice, salePrice, true) };
            return ProductHelper.CreatePrice(Currency.KRW, originalPrice, salePrice, true);

        }

        private static double? TryParseNumber(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            string digits = Regex.Replace(raw, @"[^\d]", ""); // 콤마/원/%/공백 제거
            if (!double.TryParse(digits, out var v)) return null;
            if (v <= 0) return null;
            return v;
        }

        private static List<string> GetMainImages(string html)
        {
            var result = new List<string>();
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1) 상세 썸네일 영역(가장 정확)
            // #product_add_image_list 내부의 img src만 수집
            var thumbSrcs = html.GetValues("//*[@id='product_add_image_list']//img", "src");

            if (thumbSrcs != null)
            {
                foreach (var src in thumbSrcs)
                {
                    if (string.IsNullOrWhiteSpace(src)) continue;

                    // noimg / placeholder 제거
                    if (src.Contains("/_image/_default/prd/noimg.jpg", StringComparison.OrdinalIgnoreCase)) continue;

                    var imgUrl = ToAbsoluteUrl(src);

                    // 중복 제거
                    if (!unique.Add(imgUrl)) continue;

                    result.Add(imgUrl);
                    if (result.Count >= 20) break;
                }
            }

            // 2) 썸네일이 없으면 og:image
            if (result.Count == 0)
            {
                string ogImage = html.GetValue("//meta[@property='og:image']", "content");
                if (!string.IsNullOrWhiteSpace(ogImage))
                {
                    var imgUrl = ToAbsoluteUrl(ogImage);
                    if (unique.Add(imgUrl)) result.Add(imgUrl);
                }
            }

            // 3) 그래도 없으면 마지막 fallback (기존 방식 최소 적용)
            if (result.Count == 0)
            {
                var imageSrcs = html.GetValues("//img", "src") ?? new List<string>();
                foreach (string src in imageSrcs)
                {
                    if (string.IsNullOrWhiteSpace(src)) continue;
                    if (src.Contains("logo", StringComparison.OrdinalIgnoreCase)) continue;
                    if (src.Contains("icon", StringComparison.OrdinalIgnoreCase)) continue;
                    if (src.Contains("/_image/_default/prd/noimg.jpg", StringComparison.OrdinalIgnoreCase)) continue;

                    var imgUrl = ToAbsoluteUrl(src);
                    if (!unique.Add(imgUrl)) continue;

                    result.Add(imgUrl);
                    if (result.Count >= 10) break;
                }
            }

            if (result.Count == 0)
                throw new Exception("상품 이미지를 찾을 수 없습니다.");

            return result;
        }

        private static string GetDetailPageHtml(string html)
        {
            // ✅ BlackYak 상세 정답 영역
            string detailHtml = html.GetValue(
                "//div[contains(@class,'tabcnt_detail') and contains(@class,'tabcnt_detail0')]",
                TextEx.XPathOption.InnerHtml
            );

            if (string.IsNullOrWhiteSpace(detailHtml))
                throw new Exception("상세정보(tabcnt_detail0)를 찾을 수 없습니다.");

            // 원본 스타일 유지: GetDafaultHtml는 모든 속성(style, class 등)을 제거하므로 사용하지 않음
            return detailHtml.Trim();
        }


        private static Option GetOption(string html, double defaultSalePrice)
        {
            var option = new Option
            {
                optionGroups = new List<Option.OptionGroup>(),
                detailOptions = new List<Option.DetailOption>()
            };

            // ✅ 사이즈 tr(두번째) 안의 text_option만 수집
            var aHtmlList = html.GetValues(
                "//tr[.//th[contains(.,'사이즈')]]//ul[contains(@class,'text_option')]//a",
                TextEx.XPathOption.OuterHtml
            );

            if (aHtmlList == null || aHtmlList.Count == 0)
                return option;

            var items = new List<(string id, string text, bool soldout, double addPrice)>();

            foreach (var aHtml in aHtmlList)
            {
                if (string.IsNullOrWhiteSpace(aHtml)) continue;

                string text = WebUtility.HtmlDecode(Regex.Replace(aHtml, "<.*?>", "", RegexOptions.Singleline)).Trim();
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (text.Contains("선택")) continue;

                bool soldout = aHtml.Contains("soldout", StringComparison.OrdinalIgnoreCase);

                // id: data="371298"
                string id = null;
                var mData = Regex.Match(aHtml, @"\sdata\s*=\s*[""'](?<v>[^""']+)[""']", RegexOptions.IgnoreCase);
                if (mData.Success) id = mData.Groups["v"].Value.Trim();
                if (string.IsNullOrWhiteSpace(id)) id = text;

                // 추가금: '095::0.00::...'
                double addPrice = 0d;
                var mPayload = Regex.Match(aHtml, @"selectOptionChip\([^,]+,\s*'(?<p>[^']+)'", RegexOptions.IgnoreCase);
                if (mPayload.Success)
                {
                    var parts = mPayload.Groups["p"].Value.Split(new[] { "::" }, StringSplitOptions.None);
                    if (parts.Length >= 2) addPrice = TryParseDouble(parts[1]) ?? 0d;
                }

                if (items.Any(x => string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase))) continue;
                items.Add((id, text, soldout, addPrice));
            }

            if (items.Count == 0)
                return option;

            // 단일 그룹(사이즈)
            var groupName = ProductHelper.CreateName(Language.KR, "사이즈");
            var group = new Option.OptionGroup
            {
                optionId = "2",
                groupName = groupName,
                groups = new List<Option.OptionGroup.Group>()
            };

            foreach (var it in items)
            {
                group.groups.Add(new Option.OptionGroup.Group
                {
                    groupId = it.id,
                    optionName = ProductHelper.CreateName(Language.KR, it.text)
                });
            }

            option.optionGroups.Add(group);

            // detailOptions (사이즈 단일)
            foreach (var it in items)
            {
                double salePrice = defaultSalePrice + it.addPrice;
                double originalPrice = salePrice;

                option.detailOptions.Add(new Option.DetailOption
                {
                    optionIds = it.id,
                    optionGroupName = groupName,
                    optionName = ProductHelper.CreateName(Language.KR, it.text),
                    isSoldOut = it.soldout,
                    quantity = it.soldout ? 0 : 9,
                    price = ProductHelper.CreatePrice(Currency.KRW, originalPrice, salePrice, true)
                });
            }

            return option;
        }


        private static double? TryParseDouble(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            string cleaned = Regex.Replace(s, @"[^0-9\.\-\,]", "").Replace(",", "");
            if (double.TryParse(cleaned, out var v)) return v;
            return null;
        }

        private static string ToAbsoluteUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            if (url.StartsWith("//")) return $"https:{url}";
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return url;

            // 상대경로 처리
            if (!url.StartsWith('/')) return $"{Host}/{url}";
            return $"{Host}{url}";
        }

        private static string GetQueryValue(string query, string key)
        {
            if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(key)) return null;

            string normalized = query.TrimStart('?');

            foreach (string pair in normalized.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int idx = pair.IndexOf('=');
                if (idx <= 0) continue;

                string k = pair.Substring(0, idx);
                if (!string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) continue;

                return Uri.UnescapeDataString(pair.Substring(idx + 1));
            }

            return null;
        }

        /// <summary>
        /// 상세 페이지 HTML에서 h3 class="name" 과 div class="Price" 사이에 있는 p class="code" 의 텍스트를 취합.
        /// 리스트 페이지에는 해당 구조가 없음. anchorNearItem이 null이면 첫 번째 구간 사용(상세 1건).
        /// </summary>
        /// <param name="html">상품 상세 페이지 HTML</param>
        /// <param name="anchorNearItem">null이면 상세 1개 블록, 있으면 해당 anchor 이전 마지막 h3.name~div.Price 구간</param>
        private static string ExtractCodeBetweenNameAndPrice(string html, string anchorNearItem = null)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;

            // 시작: h3 class="name"
            const string nameTagPattern = @"<h3[^>]*\bclass\s*=\s*[""'][^""']*\bname\b[^""']*[""'][^>]*>";

            string searchRegion;
            if (!string.IsNullOrWhiteSpace(anchorNearItem))
            {
                int anchorIndex = html.IndexOf(anchorNearItem, StringComparison.OrdinalIgnoreCase);
                if (anchorIndex < 0) return null;
                // anchor 이전에서 마지막 h3 class="name" 찾기
                int nameEnd = -1;
                int lastNameEnd = -1;
                int i = 0;
                while (i < anchorIndex)
                {
                    var m = Regex.Match(html.Substring(i), nameTagPattern, RegexOptions.IgnoreCase);
                    if (!m.Success) break;
                    lastNameEnd = i + m.Index + m.Length;
                    i = lastNameEnd;
                }
                if (lastNameEnd < 0) return null;
                // nameEnd 이후에서 첫 div class="Price" 찾기
                var mPrice = Regex.Match(html.Substring(lastNameEnd), @"<div[^>]*\bclass\s*=\s*[""']\s*[^""']*\bPrice\b[^""']*[""'][^>]*>", RegexOptions.IgnoreCase);
                if (!mPrice.Success) return null;
                int priceStart = lastNameEnd + mPrice.Index;
                searchRegion = html.Substring(lastNameEnd, priceStart - lastNameEnd);
            }
            else
            {
                var mName = Regex.Match(html, nameTagPattern, RegexOptions.IgnoreCase);
                if (!mName.Success) return null;
                int nameEnd = mName.Index + mName.Length;
                var mPrice = Regex.Match(html.Substring(nameEnd), @"<div[^>]*\bclass\s*=\s*[""'][^""']*\bPrice\b[^""']*[""'][^>]*>", RegexOptions.IgnoreCase);
                if (!mPrice.Success) return null;
                searchRegion = html.Substring(nameEnd, mPrice.Index);
            }

            // 구간 안의 모든 p class="code" 텍스트 취합 (내부 태그 제거)
            var codeTexts = new List<string>();
            var regex = new Regex(@"<p[^>]*\bclass\s*=\s*[""'][^""']*\bcode\b[^""']*[""'][^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match m in regex.Matches(searchRegion))
            {
                string raw = m.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string text = WebUtility.HtmlDecode(Regex.Replace(raw, "<[^>]+>", "").Trim()).Trim();
                if (!string.IsNullOrWhiteSpace(text)) codeTexts.Add(text);
            }
            return codeTexts.Count == 0 ? null : string.Join(" ", codeTexts);
        }

        private static string ExtractTitleNearLink(string html, string href)
        {
            // 간단한 휴리스틱: 링크 주변 alt/title 우선, 없으면 null
            if (string.IsNullOrWhiteSpace(html) || string.IsNullOrWhiteSpace(href)) return null;

            int index = html.IndexOf(href, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;

            int start = Math.Max(0, index - 300);
            int length = Math.Min(600, html.Length - start);
            string nearby = html.Substring(start, length);

            Match altMatch = Regex.Match(nearby, "alt=[\"'](?<title>[^\"']+)[\"']", RegexOptions.IgnoreCase);
            if (altMatch.Success)
            {
                return altMatch.Groups["title"].Value?.Trim();
            }

            Match titleMatch = Regex.Match(nearby, "title=[\"'](?<title>[^\"']+)[\"']", RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                return titleMatch.Groups["title"].Value?.Trim();
            }

            // byn 리스트 구조에서는 <p class="name"><a>가 같이 있으니
            // 링크 주변에서 텍스트 뽑기(과한 파싱 없이)
            Match nameMatch = Regex.Match(nearby, @"<p[^>]*class=['""][^'""]*name[^'""]*['""][^>]*>\s*<a[^>]*>\s*(?<t>[^<]+)\s*</a>",
                RegexOptions.IgnoreCase);
            if (nameMatch.Success)
            {
                return WebUtility.HtmlDecode(nameMatch.Groups["t"].Value)?.Trim();
            }

            return null;
        }

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
