#nullable disable warnings
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
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.TrueReligion
{
    public class TrueReligion : ICollect
    {
        public string Site => nameof(TrueReligion);
        public string Name => "트릴리전";
        public Language Language => Language.EN;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        private const string SITE_BASE = "https://www.truereligion.com";
        private const int PAGE_SIZE = 48;

        public string GetCode(string url)
        {
            // https://www.truereligion.com/true-religion-x-von-dutch-billy-big-t-bootcut-jean/110528C.html?...
            var match = Regex.Match(url, @"/([^/]+)\.html");
            return match.Success ? match.Groups[1].Value : null;
        }

        public Task<string> GetLoginUrlAsync(string connectionId)
        {
            return Task.FromResult<string>(null);
        }

        #region HTTP 요청 (ExtensionsCookie + HttpClient 직접 요청)

        private async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            // 확장에서 브라우저 쿠키 가져오기 (host_permissions 필요)
            List<ExtensionsCookie> cookies = await ChatHubService.GetCookies(connectionId, "www.truereligion.com");
            /*if (cookies == null || cookies.Count == 0)
                throw new Exception("브라우저에서 truereligion.com에 먼저 접속하여 캡챠를 통과해주세요. (쿠키 없음)");*/

            string cookieHeader = string.Join("; ", cookies.Select(c => $"{c.name}={c.value}"));

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                AllowAutoRedirect = true
            };
            if (Proxy != null)
            {
                handler.UseProxy = true;
                handler.Proxy = Proxy;
            }

            using var httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.TryAddWithoutValidation("Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7");
            request.Headers.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
            request.Headers.TryAddWithoutValidation("Referer", SITE_BASE + "/");

            var response = await httpClient.SendAsync(request);
            string html = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(html))
                throw new Exception("페이지를 불러올 수 없습니다.");
            if (html.Contains("px-captcha"))
                throw new Exception("PerimeterX 봇 차단 — 브라우저에서 truereligion.com 캡챠를 다시 통과해주세요.");

            return html;
        }

        #endregion

        #region 카테고리 수집

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            // 페이징: start=0, 48, 96... / sz=48
            int start = (page - 1) * PAGE_SIZE;
            string requestUrl = AppendPagination(url, start, PAGE_SIZE);

            string html = await GetHtmlAsync(connectionId, requestUrl);
            if (string.IsNullOrWhiteSpace(html)) return null;

            // product-tile div 목록 추출
            var tiles = html.GetValues("//div[contains(@class,'product-tile') and @data-master-id]", TextEx.XPathOption.OuterHtml);
            if (tiles == null || tiles.Count == 0) return null;

            var result = new List<CollectPageInfo>();
            foreach (var tile in tiles)
            {
                string masterId = tile.GetValue("//div[@data-master-id]", "data-master-id");
                if (string.IsNullOrWhiteSpace(masterId)) continue;

                string href = tile.GetValue("//a[contains(@class,'product-tile__anchor')]", "href");
                string title = tile.GetValue("//*[contains(@class,'product-tile__name')]", TextEx.XPathOption.InnerText);

                string productUrl = !string.IsNullOrWhiteSpace(href) ? href : $"{SITE_BASE}/{masterId}.html?lang=default";
                if (productUrl.StartsWith("/"))
                    productUrl = SITE_BASE + productUrl;

                result.Add(new CollectPageInfo
                {
                    Code = masterId,
                    Url = productUrl,
                    Title = title?.Trim()
                });

                CollectExtension.CollectSleep();
            }

            return result;
        }

        private string AppendPagination(string url, int start, int sz)
        {
            var uri = new Uri(url);
            string separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
            return $"{url}{separator}start={start}&sz={sz}";
        }

        #endregion

        #region 상품 수집

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var product = new Product();
            try
            {
                string code = GetCode(url);
                product.site = this.Site;
                product.code = code;
                product.url = url;
                product.currency = this.Currency.ToString();

                string html = await GetHtmlAsync(connectionId, url);

                product.title = GetTitle(html);
                product.brand = new List<Name> { new Name(Language.EN, "True Religion") };
                product.price = GetPrice(html);
                product.mainImages = GetMainImages(html);
                product.option = GetOption(html, product.price);
                product.detailPageHtml = GetDetailPageHtml(html);
                product.descs = GetDescs(html);
                product.regDate = DateTime.Now;
            }
            catch (SaleException saleException)
            {
                product.collectStatus = ProductCollectStatus.Success;
                product.saleStatus = saleException.SaleStatus;
                product.memo = saleException.Message;
            }
            catch (Exception exception)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = exception.Message;
            }
            return product;
        }

        private List<Name> GetTitle(string html)
        {
            string title = html.GetValue("//h1[@itemprop='name']", TextEx.XPathOption.InnerText);
            if (string.IsNullOrWhiteSpace(title))
                title = html.GetValue("//h1[contains(@class,'pdp__name')]", TextEx.XPathOption.InnerText);
            if (string.IsNullOrWhiteSpace(title))
                throw new Exception("상품명을 찾을 수 없습니다.");
            return new List<Name> { new Name(Language.EN, title.Trim()) };
        }

        private List<Price> GetPrice(string html)
        {
            // Global-E가 서버 응답에서 KRW로 변환 → itemprop="price"에서 KRW 가격 추출
            string salePriceStr = html.GetValue("//*[@itemprop='price']", "content");
            if (!string.IsNullOrWhiteSpace(salePriceStr) && double.TryParse(salePriceStr, out double salePrice) && salePrice > 0)
            {
                // 가격 규칙: originalPrice와 salePrice 모두 최종 판매가로 동일하게 설정
                var price = new Price(Currency.KRW, salePrice, salePrice, true) { isoCode = "KRW" };
                return new List<Price> { price };
            }

            throw new Exception("가격을 찾을 수 없습니다.");
        }

        private List<string> GetMainImages(string html)
        {
            // Cloudinary kiosk_large 이미지 URL 추출
            var matches = Regex.Matches(html, @"https://media\.truereligion\.com/image/upload/t_kiosk_large[^\s""'<>]+");
            var images = new List<string>();
            var seen = new HashSet<string>();

            foreach (Match m in matches)
            {
                string imgUrl = m.Value;
                if (!seen.Contains(imgUrl))
                {
                    seen.Add(imgUrl);
                    images.Add(imgUrl);
                }
            }

            // kiosk_large 없으면 dfs_large 시도
            if (images.Count == 0)
            {
                var fallback = Regex.Matches(html, @"https://media\.truereligion\.com/image/upload/t_dfs_large[^\s""'<>]+");
                foreach (Match m in fallback)
                {
                    string imgUrl = m.Value;
                    if (!seen.Contains(imgUrl))
                    {
                        seen.Add(imgUrl);
                        images.Add(imgUrl);
                    }
                }
            }

            return images.Count > 0 ? images : null;
        }

        private Option GetOption(string html, List<Price> productPrice)
        {
            var priceList = productPrice != null
                ? new List<Price> { new Price(Currency.KRW, productPrice.First().salePrice, productPrice.First().salePrice) { isoCode = "KRW" } }
                : null;

            // 사이즈 추출 (정규식: data-attr="size" + selectable 패턴)
            var sizes = ParseSizes(html);

            // 컬러 추출
            string currentColor = ParseCurrentColor(html);
            bool hasColor = !string.IsNullOrWhiteSpace(currentColor);
            bool hasSizes = sizes != null && sizes.Count > 0;

            if (!hasSizes) return GetDefaultOption(priceList);

            // 전체 품절 체크
            if (sizes.All(s => s.IsSoldOut))
                throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품입니다.");

            var groupName = new Name(Language.KR, "옵션");

            var optionGroups = new List<Option.OptionGroup>();
            var detailOptions = new List<Option.DetailOption>();

            var optionGroup = new Option.OptionGroup
            {
                optionId = "0",
                groupName = new List<Name> { groupName },
                groups = new List<Option.OptionGroup.Group>()
            };

            int idx = 0;
            foreach (var sizeInfo in sizes)
            {
                string optionName = hasColor ? $"{currentColor} / {sizeInfo.Name}" : sizeInfo.Name;
                string id = idx.ToString();

                optionGroup.groups.Add(new Option.OptionGroup.Group
                {
                    groupId = id,
                    optionName = new List<Name> { new Name(Language.KR, optionName) }
                });
                detailOptions.Add(new Option.DetailOption
                {
                    optionIds = id,
                    isSoldOut = sizeInfo.IsSoldOut,
                    quantity = sizeInfo.IsSoldOut ? 0 : 9,
                    optionGroupName = new List<Name> { groupName },
                    optionName = new List<Name> { new Name(Language.KR, optionName) },
                    price = priceList
                });
                idx++;
            }

            if (optionGroup.groups.Count <= 0) return GetDefaultOption(priceList);
            optionGroups.Add(optionGroup);
            return new Option { optionGroups = optionGroups, detailOptions = detailOptions };
        }

        private class SizeInfo
        {
            public string Name { get; set; }
            public bool IsSoldOut { get; set; }
        }

        /// <summary>
        /// 정규식으로 data-attr="size" 사이즈 추출 (품절 포함)
        /// </summary>
        private List<SizeInfo> ParseSizes(string html)
        {
            // data-attr="size" ... data-attr-value="XX" 패턴 (button/option 태그 공통)
            var matches = Regex.Matches(html,
                @"class=""[^""]*anchor--size[^""]*""[^>]*data-attr=""size""[^>]*data-attr-value=""([^""]+)""",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            var sizes = new List<SizeInfo>();
            var seen = new HashSet<string>();

            foreach (Match m in matches)
            {
                string fullMatch = m.Value;
                string size = m.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(size) || seen.Contains(size)) continue;

                // selectable 또는 unselectable 확인
                if (!fullMatch.Contains("selectable")) continue;

                seen.Add(size);
                sizes.Add(new SizeInfo
                {
                    Name = size,
                    IsSoldOut = fullMatch.Contains("unselectable")
                });
            }

            // 위에서 못 찾으면 역순 패턴 시도
            if (sizes.Count == 0)
            {
                matches = Regex.Matches(html,
                    @"data-attr=""size""[^>]*data-attr-value=""([^""]+)""[^>]*class=""[^""]*selectable[^""]*""",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);

                foreach (Match m in matches)
                {
                    string size = m.Groups[1].Value;
                    if (string.IsNullOrWhiteSpace(size) || seen.Contains(size)) continue;

                    seen.Add(size);
                    sizes.Add(new SizeInfo
                    {
                        Name = size,
                        IsSoldOut = m.Value.Contains("unselectable")
                    });
                }
            }

            return sizes;
        }

        /// <summary>
        /// 정규식으로 현재 선택된 컬러명 추출
        /// </summary>
        private string ParseCurrentColor(string html)
        {
            // title="Color: XXX, selected" 패턴
            var match = Regex.Match(html, @"title=""Color:\s*([^""]+?),\s*selected""", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            // data-attr-color-value 폴백
            var colorMatch = Regex.Match(html, @"data-attr-color-value=""([^""]+)""", RegexOptions.IgnoreCase);
            if (colorMatch.Success)
                return colorMatch.Groups[1].Value.Trim();

            return null;
        }

        private Option GetDefaultOption(List<Price> priceList = null)
        {
            var groupName = new Name(Language.KR, "옵션");
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
                                optionName = new List<Name> { new Name(Language.KR, "원사이즈") }
                            }
                        }
                    }
                },
                detailOptions = new List<Option.DetailOption>
                {
                    new Option.DetailOption
                    {
                        optionIds = "0",
                        isSoldOut = false,
                        quantity = 9,
                        optionGroupName = new List<Name> { groupName },
                        optionName = new List<Name> { new Name(Language.KR, "원사이즈") },
                        price = priceList
                    }
                }
            };
        }

        private string GetDetailPageHtml(string html)
        {
            // Description + Fit & Sizing + Fabric & Care 섹션 결합
            var sections = html.GetValues("//div[contains(@class,'pdp__details-description')]", TextEx.XPathOption.InnerHtml);
            if (sections == null || sections.Count == 0) return null;

            string combined = string.Join("\n", sections.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (string.IsNullOrWhiteSpace(combined)) return null;

            return HtmlReader.GetDafaultHtml(combined);
        }

        private List<Desc> GetDescs(string html)
        {
            var descs = new List<Desc>();
            int order = 1;

            // 브랜드
            descs.Add(new Desc(order++, Language.EN, "Brand", "True Religion"));

            // 카테고리 (breadcrumb)
            var breadcrumbs = html.GetValues("//ol[contains(@class,'breadcrumbs')]//a", TextEx.XPathOption.InnerText);
            if (breadcrumbs != null && breadcrumbs.Count > 0)
            {
                string category = string.Join(" > ", breadcrumbs.Select(b => b.Trim()).Where(b => !string.IsNullOrWhiteSpace(b)));
                if (!string.IsNullOrWhiteSpace(category))
                    descs.Add(new Desc(order++, Language.EN, "Category", category));
            }

            // 컬러
            string color = html.GetValue("//span[@data-attr-color-value]", TextEx.XPathOption.InnerText);
            if (!string.IsNullOrWhiteSpace(color))
                descs.Add(new Desc(order++, Language.EN, "Color", color.Trim()));

            return descs;
        }

        #endregion

        #region 미구현

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

        #endregion
    }
}
