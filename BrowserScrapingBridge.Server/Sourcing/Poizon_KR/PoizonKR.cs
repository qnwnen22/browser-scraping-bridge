#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
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
using Price = BrowserScrapingBridge.Server.Sourcing.Models.Product.Price;

namespace BrowserScrapingBridge.Server.Sourcing.Poizon_KR
{
    public class PoizonKR : ICollect
    {
        // ✅ CollectFactory에서 nameof(Poizon_KR.PoizonKR)만 남겼으니 Site도 동일하게
        public string Site => nameof(PoizonKR); // "PoizonKR"
        public string Name => "포이즌KR";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;

        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }

        private const string Host = "https://kr.poizon.com";
        public bool SupportsCaching { get; set; }
        public Task<string> GetLoginUrlAsync(string connectionId) => Task.FromResult<string>(null);

        public string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            // /product/xxx-579838442 or ...-61030189
            var m = Regex.Match(url, @"-(\d+)(?:\?|$)");
            if (m.Success) return m.Groups[1].Value;

            try
            {
                var uri = new Uri(url);
                var last = uri.Segments.LastOrDefault()?.Trim('/');
                if (string.IsNullOrWhiteSpace(last)) return null;

                var m2 = Regex.Match(last, @"(\d+)$");
                if (m2.Success) return m2.Groups[1].Value;
                return last;
            }
            catch { return null; }
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            // ✅ page 적용
            var uri = new UriBuilderAdvance(url);
            uri.SetQuery("page", page.ToString());
            string pageUrl = uri.ToString();

            var headers = GetDefaultHeaders(pageUrl);
            string html = await ChatHubService.GetRequest(connectionId, pageUrl, headers);

            if (string.IsNullOrWhiteSpace(html))
                return new List<CollectPageInfo>();

            if (LooksBlocked(html))
            {
                throw new CaptchaException
                {
                    CaptchaInfo = new CaptchaInfo
                    {
                        Message = "Poizon 페이지 접근이 차단되었습니다. 브라우저에서 정상 표시되게 한 후 다시 수집해주세요.",
                        Url = pageUrl
                    }
                };
            }

            // ✅ 리스트는 <a href="/product/...-61030189">
            var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Regex.Matches(html, @"href=""(?<u>/product/[^""]+)""", RegexOptions.IgnoreCase))
            {
                var u = m.Groups["u"].Value;
                if (string.IsNullOrWhiteSpace(u)) continue;
                urls.Add(ToAbsolute(u));
            }

            var result = new List<CollectPageInfo>();
            foreach (var pu in urls)
            {
                string code = GetCode(pu);
                if (string.IsNullOrWhiteSpace(code)) continue;

                result.Add(new CollectPageInfo
                {
                    Code = code,
                    Url = pu,
                    Title = ""
                });
            }

            return result;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            // ✅ 실패해도 CollectService에서 .First()로 안 죽게 기본값 세팅
            var product = new Product
            {
                site = Site,
                url = url,
                currency = Currency.ToString(),
                collectStatus = ProductCollectStatus.Failed,
                code = GetCode(url) ?? "",
                title = new List<Name> { new Name(Language.KR, "") },
                price = new List<Price> { new Price(Currency.KRW, 0, 0, true), new Price("KRW", 0, 0) },
                mainImages = new List<string>(),
                descs = new List<Desc>(),
                tags = new List<Tag>(),
                detailPageHtml = ""
            };

            try
            {
                var headers = GetDefaultHeaders(url);
                string html = await ChatHubService.GetRequest(connectionId, url, headers);

                if (string.IsNullOrWhiteSpace(html))
                {
                    product.memo = "상품 접근 실패(응답이 비었습니다)";
                    return product;
                }

                if (LooksBlocked(html))
                {
                    throw new CaptchaException
                    {
                        CaptchaInfo = new CaptchaInfo
                        {
                            Message = "Poizon 상품 페이지 접근이 차단되었습니다. 브라우저에서 정상 표시되게 한 후 다시 시도해주세요.",
                            Url = url
                        }
                    };
                }

                JObject next = TryExtractNextData(html);
                if (next == null)
                {
                    product.memo = "__NEXT_DATA__ 파싱 실패";
                    return product;
                }

                // ✅ 1) 제목: props.pageProps.goodsDetail.detail.title
                string title =
                    next.SelectToken("props.pageProps.goodsDetail.detail.title")?.ToString()
                    ?? TryGetOgMeta(html, "og:title")
                    ?? TryGetHtmlTitle(html)
                    ?? "";

                product.title = new List<Name> { new Name(Language.KR, title) };

                // ✅ 2) 최저가: props.pageProps.priceInfo.skuMinPriceInfoDTO.minPrice.money.amount
                double minPrice = ParseKrwNumber(
                    next.SelectToken("props.pageProps.priceInfo.skuMinPriceInfoDTO.minPrice.money.amount")?.ToString()
                );

                if (minPrice <= 0)
                {
                    // fallback: props.pageProps.goodsDetail.price.money.amount
                    minPrice = ParseKrwNumber(
                        next.SelectToken("props.pageProps.goodsDetail.price.money.amount")?.ToString()
                    );
                }

                if (minPrice <= 0)
                {
                    // fallback: HTML에서 원
                    minPrice = TryGetPriceFromHtmlKRW(html);
                }

                product.price = new List<Price>
                {
                    new Price(Currency.KRW, minPrice, minPrice, true),
                    new Price("KRW", 0, 0)
                };

                // ✅ 3) 메인 이미지: props.pageProps.goodsDetail.imageModels[].url
                var imageUrls = new List<string>();
                var imgs = next.SelectToken("props.pageProps.goodsDetail.imageModels") as JArray;
                if (imgs != null)
                {
                    foreach (var it in imgs)
                    {
                        var u = it["url"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(u)) imageUrls.Add(u);
                    }
                }

                // fallback: saleImages[색상ID] 배열
                if (imageUrls.Count == 0)
                {
                    var saleImages = next.SelectToken("props.pageProps.goodsDetail.saleImages") as JObject;
                    if (saleImages != null)
                    {
                        foreach (var prop in saleImages.Properties())
                        {
                            var arr = prop.Value as JArray;
                            if (arr == null) continue;
                            foreach (var it in arr)
                            {
                                var u = it["url"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(u)) imageUrls.Add(u);
                            }
                            if (imageUrls.Count > 0) break;
                        }
                    }
                }

                product.mainImages = imageUrls
                    .Select(NormalizeUrl)
                    .Distinct()
                    .Take(10)
                    .ToList();

                if (product.mainImages == null || product.mainImages.Count == 0)
                {
                    product.memo = "메인 이미지를 찾을 수 없습니다.";
                    product.collectStatus = ProductCollectStatus.Failed;
                    return product;
                }

                // ✅ 4) Desc: baseProperties + propertyModule.propertyBlocks
                var descs = new List<Desc>();
                int di = 1;

                var baseProps = next.SelectToken("props.pageProps.goodsDetail.baseProperties") as JArray;
                if (baseProps != null)
                {
                    foreach (var p in baseProps)
                    {
                        string k = p["key"]?.ToString();
                        string v = p["value"]?.ToString();
                        if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(v)) continue;
                        descs.Add(new Desc(di++, Language.KR, k, v));
                    }
                }

                var blocks = next.SelectToken("props.pageProps.goodsDetail.propertyModule.propertyBlocks") as JArray;
                if (blocks != null)
                {
                    foreach (var b in blocks)
                    {
                        var plist = b["propertyList"] as JArray;
                        if (plist == null) continue;
                        foreach (var p in plist)
                        {
                            string k = p["name"]?.ToString();
                            string v = p["value"]?.ToString();
                            if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(v)) continue;
                            descs.Add(new Desc(di++, Language.KR, k, v));
                        }
                    }
                }

                product.descs = descs;

                // ✅ 5) 상세(텍스트): goodsDetail.detailTextModule.detailTextList[].content
                var detailTextList = next.SelectToken("props.pageProps.goodsDetail.detailTextModule.detailTextList") as JArray;
                if (detailTextList != null && detailTextList.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("<div>");
                    foreach (var t in detailTextList)
                    {
                        var subTitle = t["subTitle"]?.ToString();
                        var content = t["content"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(subTitle))
                            sb.AppendLine($"<h3>{WebUtility.HtmlEncode(subTitle)}</h3>");
                        if (!string.IsNullOrWhiteSpace(content))
                            sb.AppendLine($"<div>{content}</div>"); // content는 이미 HTML
                    }
                    sb.AppendLine("</div>");
                    product.detailPageHtml = sb.ToString();
                }
                else
                {
                    product.detailPageHtml = "<div></div>";
                }

                // ✅ 6) 옵션 전체(색상/사이즈) + detailOptions 구성
                product.option = BuildOptionsFromNext(next, product.price);

                product.collectStatus = ProductCollectStatus.Success;
                product.memo = null;
                return product;
            }
            catch (CaptchaException)
            {
                throw;
            }
            catch (Exception ex)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = ex.Message;

                // 안전망
                if (product.title == null || product.title.Count == 0)
                    product.title = new List<Name> { new Name(Language.KR, "") };
                if (product.mainImages == null)
                    product.mainImages = new List<string>();
                if (product.price == null)
                    product.price = new List<Price> { new Price(Currency.KRW, 0, 0, true), new Price("KRW", 0, 0) };

                return product;
            }
        }

        private Option BuildOptionsFromNext(JObject next, List<Price> productPrices)
        {
            // saleProperties 에 "색상"(level=1) + "사이즈: KR"(level=2) 모두 들어있음
            var saleProps = next.SelectToken("props.pageProps.goodsDetail.saleProperties") as JArray;
            var skuArr = next.SelectToken("props.pageProps.priceInfo.skus") as JArray;

            if (saleProps == null || skuArr == null) return null;

            // level별 propertyValueId -> value(표시값) 매핑
            var levelMap = new Dictionary<int, Dictionary<long, string>>();
            var levelName = new Dictionary<int, string>(); // 그룹명

            foreach (var sp in saleProps)
            {
                int lvl = sp.Value<int?>("level") ?? 0;
                string gName = sp["name"]?.ToString();
                if (lvl == 0 || string.IsNullOrWhiteSpace(gName)) continue;

                levelName[lvl] = gName;
                if (!levelMap.ContainsKey(lvl)) levelMap[lvl] = new Dictionary<long, string>();

                var propList = sp["propertyList"] as JArray;
                if (propList == null) continue;

                foreach (var pl in propList)
                {
                    var items = pl["propertyItemModels"] as JArray;
                    if (items == null) continue;

                    foreach (var it in items)
                    {
                        long id = it.Value<long?>("propertyValueId") ?? 0;
                        string val = it["value"]?.ToString();
                        if (id == 0 || string.IsNullOrWhiteSpace(val)) continue;
                        levelMap[lvl][id] = val;
                    }
                }
            }

            // 옵션 그룹 생성 (level 1, 2)
            var option = new Option
            {
                optionGroups = new List<Option.OptionGroup>(),
                detailOptions = new List<Option.DetailOption>()
            };

            foreach (var kv in levelName.OrderBy(x => x.Key))
            {
                int lvl = kv.Key;
                string gName = kv.Value;

                var og = new Option.OptionGroup
                {
                    optionId = lvl.ToString(),
                    groupName = new List<Name> { new Name(Language.KR, gName) },
                    groups = new List<Option.OptionGroup.Group>()
                };

                if (levelMap.TryGetValue(lvl, out var map))
                {
                    foreach (var item in map.OrderBy(x => x.Value))
                    {
                        og.groups.Add(new Option.OptionGroup.Group
                        {
                            groupId = item.Key.ToString(),
                            optionName = new List<Name> { new Name(Language.KR, item.Value) },
                            price = null,
                            quantity = 0,
                            optionImage = null
                        });
                    }
                }

                option.optionGroups.Add(og);
            }

            // detailOptions = priceInfo.skus 기반으로 생성
            // sku.properties -> propertyValueId 로 색상/사이즈를 찾아 조합
            foreach (var sku in skuArr)
            {
                string skuId = sku["skuId"]?.ToString();
                int status = sku.Value<int?>("status") ?? 0;

                // 가격: sku.minPrice.money.amount
                double skuPrice = ParseKrwNumber(sku.SelectToken("minPrice.money.amount")?.ToString());
                if (skuPrice <= 0)
                {
                    skuPrice = ParseKrwNumber(sku.SelectToken("skuSpeedInfo[0].speedPrice.money.amount")?.ToString());
                }

                // propertyValueId들 찾기
                long colorId = 0;
                long sizeId = 0;
                var props = sku["properties"] as JArray;
                if (props != null)
                {
                    foreach (var p in props)
                    {
                        int lvl = p.Value<int?>("level") ?? 0;
                        long vid = p.Value<long?>("propertyValueId") ?? 0;
                        if (lvl == 1) colorId = vid;
                        if (lvl == 2) sizeId = vid;
                    }
                }

                string color = (colorId != 0 && levelMap.ContainsKey(1) && levelMap[1].ContainsKey(colorId)) ? levelMap[1][colorId] : "";
                string size = (sizeId != 0 && levelMap.ContainsKey(2) && levelMap[2].ContainsKey(sizeId)) ? levelMap[2][sizeId] : "";

                string groupNameJoin = string.Join("ㅡ", option.optionGroups.Select(x => x.groupName.First().text));
                string optionNameJoin = string.Join("ㅡ", new[] { color, size }.Where(x => !string.IsNullOrWhiteSpace(x)));

                int qty = status == 1 ? 9 : 0;

                option.detailOptions.Add(new Option.DetailOption
                {
                    optionIds = skuId ?? Guid.NewGuid().ToString(),
                    optionGroupName = new List<Name> { new Name(Language.KR, groupNameJoin) },
                    optionName = new List<Name> { new Name(Language.KR, optionNameJoin) },
                    price = new List<Price>
                    {
                        new Price(Currency.KRW, skuPrice, skuPrice),
                        new Price("KRW", 0, 0)
                    },
                    quantity = qty,
                    isSoldOut = qty <= 0,
                    optionImage = null
                });
            }

            return option;
        }

        // ---- helpers ----

        private Dictionary<string, string> GetDefaultHeaders(string referer)
        {
            return new Dictionary<string, string>
            {
                ["accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8",
                ["accept-language"] = "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7",
                ["user-agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36",
                ["referer"] = string.IsNullOrWhiteSpace(referer) ? Host + "/" : referer
            };
        }

        private bool LooksBlocked(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return true;
            if (html.IndexOf("Forbidden", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (html.IndexOf("Access denied", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (html.IndexOf("captcha", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private string ToAbsolute(string href)
        {
            if (string.IsNullOrWhiteSpace(href)) return href;
            if (href.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return href;
            if (href.StartsWith("//")) return "https:" + href;
            if (href.StartsWith("/")) return Host.TrimEnd('/') + href;
            return Host.TrimEnd('/') + "/" + href.TrimStart('/');
        }

        private string NormalizeUrl(string u)
        {
            if (string.IsNullOrWhiteSpace(u)) return u;
            if (u.StartsWith("//")) return "https:" + u;
            return u;
        }

        private string TryGetOgMeta(string html, string propertyOrName)
        {
            var m = Regex.Match(html,
                $@"<meta[^>]+(?:property|name)=""{Regex.Escape(propertyOrName)}""[^>]+content=""(?<v>[^""]+)""",
                RegexOptions.IgnoreCase);
            if (m.Success) return WebUtility.HtmlDecode(m.Groups["v"].Value);
            return null;
        }

        private string TryGetHtmlTitle(string html)
        {
            var m = Regex.Match(html, @"<title>(?<v>[^<]+)</title>", RegexOptions.IgnoreCase);
            if (m.Success) return WebUtility.HtmlDecode(m.Groups["v"].Value).Trim();
            return null;
        }

        private JObject TryExtractNextData(string html)
        {
            var m = Regex.Match(html, @"<script[^>]+id=""__NEXT_DATA__""[^>]*>(?<v>[\s\S]*?)</script>",
                RegexOptions.IgnoreCase);
            if (m.Success)
            {
                try { return JObject.Parse(m.Groups["v"].Value); } catch { }
            }
            return null;
        }

        private double TryGetPriceFromHtmlKRW(string html)
        {
            var m = Regex.Match(html, @"([\d,]+)\s?원", RegexOptions.IgnoreCase);
            if (!m.Success) return 0;
            return ParseKrwNumber(m.Value);
        }

        private double ParseKrwNumber(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = Regex.Replace(s, @"[^\d]", "");
            return double.TryParse(s, out var v) ? v : 0;
        }

        public Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            throw new NotImplementedException();
        }

        public Product GetProduct(CollectRawData data)
        {
            throw new NotImplementedException();
        }

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            throw new NotImplementedException();
        }
    }
}
