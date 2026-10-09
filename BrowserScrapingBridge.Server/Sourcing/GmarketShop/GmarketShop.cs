#nullable disable warnings
using Newtonsoft.Json;
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
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.GmarketShop
{
    /// <summary>
    /// 지마켓샵 — 브랜드샵 + 미니샵 수집
    /// - 브랜드샵: gmarket.co.kr/n/brandshop/{shopId} (__NEXT_DATA__ + POST API 페이징)
    /// - 미니샵: minishop.gmarket.co.kr/{alias}/List (레거시 HTML + 서버사이드 페이징)
    /// - 상세: item.gmarket.co.kr/Item?goodscode={code} (기존 지마켓 동일)
    /// </summary>
    public class GmarketShop : CollectBase
    {
        public override string Site => nameof(GmarketShop);
        public override string Name => "지마켓샵";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = false;

        private static readonly HttpClient _httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient();

        static GmarketShop()
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
        }

        public override string GetCode(string url)
        {
            var m = Regex.Match(url, @"goods[cC]ode=(\d+)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;
            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query);
                return query.Get("goodscode") ?? query.Get("goodsCode");
            }
            catch { return null; }
        }

        public override string CreateProductUrl(string code)
            => $"https://item.gmarket.co.kr/Item?goodscode={code}";

        public override Task<string> GetLoginUrlAsync(string connectionId)
            => Task.FromResult<string>(null);

        #region 카테고리 수집

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uri = new Uri(url);

            // minishop.gmarket.co.kr → 미니샵
            if (uri.Host.Contains("minishop"))
                return await GetMinishopPageAsync(connectionId, url, page);

            // gmarket.co.kr/n/brandshop/ → 브랜드샵
            if (uri.AbsolutePath.Contains("/brandshop/"))
                return await GetBrandshopPageAsync(connectionId, url, page);

            return null;
        }

        // ── 브랜드샵 ──

        private async Task<List<CollectPageInfo>> GetBrandshopPageAsync(string connectionId, string url, int page)
        {
            var uri = new Uri(url);
            bool isHomePage = !uri.AbsolutePath.Contains("/list") && !uri.AbsolutePath.Contains("/all");

            if (page == 1)
            {
                string html = await ChatHubService.GetRequest(connectionId, url);
                if (string.IsNullOrWhiteSpace(html)) return null;

                var nextData = ExtractNextData(html);
                if (nextData == null) return null;

                JArray items;
                if (isHomePage)
                {
                    // 홈: homePage.components 에서 전체 아이템 추출 (페이징 없음)
                    items = ExtractHomePageItems(nextData);
                }
                else
                {
                    // list/all: itemPage.pageItemContents.items
                    items = nextData.SelectToken("props.pageProps.initialStates.itemPage.pageItemContents.items") as JArray;
                }

                return ItemsToPageInfos(items);
            }

            // 홈페이지는 단일 페이지
            if (isHomePage) return null;

            // page 2+: POST API
            return await GetBrandshopApiPageAsync(url, page);
        }

        private async Task<List<CollectPageInfo>> GetBrandshopApiPageAsync(string url, int page)
        {
            string shopId = ExtractShopId(url);
            if (string.IsNullOrWhiteSpace(shopId)) return null;

            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

            string apiName = uri.AbsolutePath.Contains("/all")
                ? "all-lp/page-item-contents"
                : "category-lp/page-item-contents";

            var body = new JObject
            {
                ["shopId"] = shopId,
                ["apiName"] = apiName,
                ["options"] = new JObject
                {
                    ["pageNo"] = page,
                    ["sortOption"] = "Popularity",
                    ["categoryCode"] = query.Get("categoryCode") ?? "0",
                    ["categoryLevel"] = query.Get("categoryLevel") ?? "L",
                    ["categoryType"] = query.Get("categoryType") ?? "CORE"
                }
            };

            try
            {
                var content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://www.gmarket.co.kr/n/brandshop/api/pagination-api", content);
                string json = await response.Content.ReadAsStringAsync();
                var items = JObject.Parse(json).SelectToken("data.items") as JArray;
                return ItemsToPageInfos(items);
            }
            catch { return null; }
        }

        private JArray ExtractHomePageItems(JObject nextData)
        {
            var components = nextData.SelectToken("props.pageProps.initialStates.homePage.components") as JArray;
            if (components == null) return null;

            var allItems = new JArray();
            foreach (var comp in components)
            {
                var items = comp["items"] as JArray;
                if (items == null) continue;
                foreach (var item in items) allItems.Add(item);
            }
            return allItems.Count > 0 ? allItems : null;
        }

        // ── 미니샵 ──

        private async Task<List<CollectPageInfo>> GetMinishopPageAsync(string connectionId, string url, int page)
        {
            var uriBuilder = new UriBuilderAdvance(url);
            uriBuilder.SetQuery("Page", page);
            uriBuilder.SetQuery("PageSize", 60);
            string pageUrl = uriBuilder.ToString();

            string html = await ChatHubService.GetRequest(connectionId, pageUrl);
            if (string.IsNullOrWhiteSpace(html)) return null;

            var result = new List<CollectPageInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var matches = Regex.Matches(html, @"goods[cC]ode=(\d+)", RegexOptions.IgnoreCase);
            foreach (Match m in matches)
            {
                var code = m.Groups[1].Value;
                if (!seen.Add(code)) continue;
                result.Add(new CollectPageInfo
                {
                    Code = code,
                    Url = CreateProductUrl(code),
                    Title = ""
                });
            }

            return result.Count > 0 ? result : null;
        }

        // ── 공통 ──

        private List<CollectPageInfo> ItemsToPageInfos(JArray items)
        {
            if (items == null || !items.Any()) return null;

            var result = new List<CollectPageInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                string itemNo = item.Value<string>("itemNo");
                if (string.IsNullOrWhiteSpace(itemNo) || !seen.Add(itemNo)) continue;
                result.Add(new CollectPageInfo
                {
                    Code = itemNo,
                    Url = CreateProductUrl(itemNo),
                    Title = item.Value<string>("itemName") ?? ""
                });
            }

            return result.Count > 0 ? result : null;
        }

        private string ExtractShopId(string url)
        {
            var m = Regex.Match(url, @"/brandshop/([^/\?]+)");
            return m.Success ? m.Groups[1].Value : null;
        }

        private JObject ExtractNextData(string html)
        {
            var m = Regex.Match(html,
                @"<script\s+id=""__NEXT_DATA__""\s+type=""application/json"">(.*?)</script>",
                RegexOptions.Singleline);
            if (!m.Success) return null;
            try { return JObject.Parse(m.Groups[1].Value); }
            catch { return null; }
        }

        #endregion

        #region 상품 수집

        public override void ThrowIfCaptcha(string url, string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return;
            var title = html.GetValue("//title", TextEx.XPathOption.InnerText)?.Trim();
            if (!string.IsNullOrWhiteSpace(title) && title.Equals("ACCESS DENIED", StringComparison.OrdinalIgnoreCase))
            {
                var errorMsg = html.ToHtmlDecodeComplete().GetValue("//body", TextEx.XPathOption.InnerText)?.Trim();
                throw new CaptchaException
                {
                    CaptchaInfo = new CaptchaInfo { Url = "https://www.gmarket.co.kr", Message = errorMsg }
                };
            }
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            // 멀티(번들/세트) 상품 감지 — 수집 제외
            if (IsBundleDealPage(html))
                throw new Exception("번들(딜/세트) 상품은 수집할 수 없습니다.");

            // var goods 파싱
            var scripts = html.GetValues("//script[@type='text/javascript']", TextEx.XPathOption.OuterHtml);
            var goodsScript = scripts.Find(x => x.Contains("var goods"));
            var goods = goodsScript.SingleSplit("var goods =", ";");
            if (string.IsNullOrWhiteSpace(goods)) throw new Exception("상품 정보를 확인할 수 없습니다.");
            JObject data = goods.ToJObject();

            // aplus-cpvdata 메타에서 가격 추출 (가장 신뢰도 높음)
            string cpvContent = html.GetValue("//meta[@name='aplus-cpvdata']", "content");
            if (!string.IsNullOrWhiteSpace(cpvContent))
            {
                try
                {
                    var cpv = JObject.Parse(cpvContent);
                    if (double.TryParse(cpv.Value<string>("promotion_price"), out double promoPrice) && promoPrice > 0)
                        data["_promotionPrice"] = promoPrice;
                    if (double.TryParse(cpv.Value<string>("coupon_price"), out double cpPrice) && cpPrice > 0)
                        data["_couponPrice"] = cpPrice;
                }
                catch { }
            }

            // fallback: OrderSet.SellPrice (promotion_price 없을 때)
            if (data["_promotionPrice"] == null)
            {
                string sellPriceScript = scripts.Find(x => x.Contains("OrderSet.SellPrice"));
                if (!string.IsNullOrWhiteSpace(sellPriceScript))
                {
                    string spStr = sellPriceScript.SingleSplit("OrderSet.SellPrice =", ";");
                    if (double.TryParse(spStr?.Trim(), out double sp) && sp > 0)
                        data["_promotionPrice"] = sp;
                }
            }

            // optionData
            data["optionData"] = null;
            string[] keys = { "GmktItem.OptionParamCoreAbove.combOptionObj", "GmktItem.OptionParamCoreWing.combOptionObj" };
            foreach (var key in keys)
            {
                string optScript = scripts.Find(x => x.Contains(key));
                if (!string.IsNullOrWhiteSpace(optScript))
                {
                    string startTxt = $"{key} =";
                    int start = optScript.IndexOf(startTxt);
                    optScript = optScript.Substring(start + startTxt.Length);
                    int last = optScript.IndexOf("\n");
                    optScript = optScript.Substring(0, last);
                    JObject optionData = optScript.ToJObject();
                    if (optionData != null) { data["optionData"] = optionData; break; }
                }
            }

            // fallback: regex
            if (data["optionData"] == null)
            {
                var m = Regex.Match(html,
                    @"GmktItem\.OptionParamCore(?:Above|Wing)?\.combOptionObj\s*=\s*(\{[\s\S]*?\});",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    try { data["optionData"] = m.Groups[1].Value.Trim().TrimEnd(';').ToJObject(); }
                    catch { }
                }
            }

            // 선택형 옵션 (combOptionObj가 null일 때)
            if (data["optionData"] == null || data["optionData"].Type == JTokenType.Null)
            {
                var selOptions = ParseSelectableOptions(html);
                if (selOptions != null && selOptions.Count > 0)
                    data["_selectableOptions"] = selOptions;
            }

            // iframe 상세페이지 미리 로드 (GetDetailPageHtml은 동기이므로 여기서 비동기 처리)
            string iframeSrc = html.GetValue("//iframe[@id='detail1']", "src");
            if (!string.IsNullOrWhiteSpace(iframeSrc))
            {
                string detailUrl = iframeSrc.StartsWith("http") ? iframeSrc : "https://item.gmarket.co.kr" + iframeSrc;
                try
                {
                    string detailPage = await ChatHubService.GetRequest(connectionId, detailUrl);
                    if (!string.IsNullOrWhiteSpace(detailPage))
                        data["_detailHtml"] = detailPage.GetValue("//div[@id='basic_detail_html']", TextEx.XPathOption.OuterHtml);
                }
                catch { }
            }

            if (data["_detailHtml"] == null)
                data["_detailHtml"] = html.GetValue("//div[@id='basic_detail_html']", TextEx.XPathOption.OuterHtml);

            return data;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string name = data.Value<string>("GoodsName");
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(Language, name);
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            string brand = data.Value<string>("BrandName");
            if (string.IsNullOrWhiteSpace(brand)) return null;
            return ProductHelper.CreateName(Language, brand);
        }

        public override string GetModel(string html, JToken data) => null;

        public override List<Delivery> GetDeliveries(string html, JToken data)
        {
            // "배송비 3,000원" 패턴에서 금액 추출
            var match = Regex.Match(html, @"배송비\s*([\d,]+)\s*원");
            if (!match.Success) return null;

            string amountStr = match.Groups[1].Value.Replace(",", "");
            if (!double.TryParse(amountStr, out double fee) || fee <= 0) return null;

            return new List<Delivery>
            {
                new Delivery { price = new List<Price> { new Price(Currency, fee, fee) } }
            };
        }

        public override List<Price> GetPrice(string html, JToken data)
        {
            // 쿠폰적용가 우선 (기본: 쿠폰가 사용)
            double? couponPrice = data.Value<double?>("_couponPrice");
            if (couponPrice > 0)
                return ProductHelper.CreatePrice(Currency, couponPrice, couponPrice, true);

            // 할인가 (promotion_price)
            double? promoPrice = data.Value<double?>("_promotionPrice");
            if (promoPrice > 0)
                return ProductHelper.CreatePrice(Currency, promoPrice, promoPrice, true);

            // fallback: OriginPrice
            double? originPrice = data.Value<double?>("OriginPrice");
            if (originPrice == null || originPrice <= 0) throw new Exception("상품 가격을 확인할 수 없습니다.");
            return ProductHelper.CreatePrice(Currency, originPrice, originPrice, true);
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();
            string main = FormatUrl(data["ImageUrl"]?.ToString());
            if (!string.IsNullOrWhiteSpace(main)) images.Add(main);

            if (data["MoreImages"] is JArray moreImages)
            {
                foreach (var img in moreImages)
                {
                    string formatted = FormatUrl(img["FullDefaultImagePath"]?.ToString());
                    if (!string.IsNullOrWhiteSpace(formatted)) images.Add(formatted);
                }
            }

            return images.Distinct().ToList();
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override List<Desc> GetDescs(string html, JToken data)
        {
            var result = new List<Desc>();
            int index = 1;
            var trs = html.GetValues("//table[@class='table_productinfo']//tbody//tr", TextEx.XPathOption.OuterHtml);
            foreach (var tr in trs)
            {
                var th = tr.GetValue("//th", TextEx.XPathOption.InnerText)?.Trim();
                var td = tr.GetValue("//td", TextEx.XPathOption.InnerText)?.Trim();
                if (string.IsNullOrWhiteSpace(th) || string.IsNullOrWhiteSpace(td)) continue;
                result.Add(new Desc(index++, Language, th, td));
            }
            return result.Count > 0 ? result : null;
        }

        public override List<Tag> GetTags(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            string detailHtml = data.Value<string>("_detailHtml");
            if (string.IsNullOrWhiteSpace(detailHtml)) return null;
            return HtmlReader.GetDafaultHtml(detailHtml);
        }

        public override Option GetOption(string html, JToken data)
        {
            var optionData = data.Value("optionData");
            if (optionData == null || optionData.Type == JTokenType.Null)
            {
                // 선택형 옵션
                var selOpts = data["_selectableOptions"] as JArray;
                if (selOpts != null && selOpts.Count > 0)
                    return BuildSelectableOption(data, selOpts);

                var prices = GetPrice(html, data);
                return GetDefaultOption(prices);
            }

            var optionGroups = new List<Option.OptionGroup>();
            int optionDepth = optionData.Value<int>("OptionDepth");

            if (optionDepth >= 3)
                throw new Exception("3단계 이상 옵션 상품은 수집할 수 없습니다.");

            for (int depth = 1; depth <= optionDepth; depth++)
            {
                var combData = optionData.Value($"CombinationalOptionData{depth}");
                if (combData == null) continue;

                var optionGroup = new Option.OptionGroup
                {
                    optionId = depth.ToString(),
                    groupName = ProductHelper.CreateName(Language, combData.Value<string>("OptionName")),
                    groups = new List<Option.OptionGroup.Group>()
                };

                var optValues = combData.Value("OptionValues").ToList();
                foreach (var ov in optValues)
                {
                    string groupOptName = ov.Value<string>($"OptionValue{depth}");
                    if (string.IsNullOrWhiteSpace(groupOptName)) continue;
                    if (optionGroup.groups.Any(x => x.optionName.First().text == groupOptName)) continue;

                    var group = new Option.OptionGroup.Group
                    {
                        groupId = groupOptName,
                        optionName = ProductHelper.CreateName(Language, groupOptName)
                    };

                    string matchImg = ov.Value<string>("MatchingImageUrl");
                    if (!string.IsNullOrWhiteSpace(matchImg) && !matchImg.Contains("no_img"))
                        group.optionImage = FormatUrl(matchImg);

                    optionGroup.groups.Add(group);
                }

                optionGroups.Add(optionGroup);
            }

            // 쿠폰 비율 계산: coupon_price / promotion_price
            double couponRatio = 1.0;
            double cpPrice = data.Value<double?>("_couponPrice") ?? 0;
            double promoPrice = data.Value<double?>("_promotionPrice") ?? 0;
            if (cpPrice > 0 && promoPrice > 0)
                couponRatio = cpPrice / promoPrice;

            var detailOptions = new List<Option.DetailOption>();
            string optGroupName = ProductHelper.GetOptionGroupName(optionGroups);
            var lastData = optionData.Value($"CombinationalOptionData{optionDepth}");
            var lastValues = lastData.Value("OptionValues").ToList();

            foreach (var lv in lastValues)
            {
                var names = new List<string>();
                for (int d = 0; d <= optionDepth; d++)
                {
                    string n = lv.Value<string>($"OptionValue{d}");
                    if (!string.IsNullOrWhiteSpace(n)) names.Add(n);
                }

                string join = string.Join("ㅡ", names);
                bool? status = lv.Value<bool?>("OptionStatus");

                string sellPriceText = lv.Value<string>("SellPriceText");
                double? originalPrice = sellPriceText.ParseToDouble();
                string couponText = lv.Value<string>("CouponAppliedOptionPriceText");
                double? salePrice = couponText.ParseToDouble();
                if (salePrice == null || salePrice <= 0)
                {
                    // CouponAppliedOptionPriceText가 없으면 쿠폰 비율 적용
                    if (originalPrice > 0 && couponRatio < 1.0)
                        salePrice = Math.Round((double)originalPrice * couponRatio);
                    else
                        salePrice = originalPrice;
                }
                double finalPrice = (double)(salePrice ?? originalPrice ?? 0);

                var detailOption = new Option.DetailOption
                {
                    optionIds = join,
                    optionGroupName = ProductHelper.CreateName(Language, optGroupName),
                    optionName = ProductHelper.CreateName(Language, join),
                    price = ProductHelper.CreatePrice(Currency, finalPrice, finalPrice)
                };

                if (status == true)
                {
                    detailOption.isSoldOut = false;
                    string inv = lv.Value<string>("InventoryCount");
                    detailOption.quantity = int.TryParse(inv, out int qty) && qty > 0 ? qty : 9;
                }
                else
                {
                    detailOption.isSoldOut = true;
                    detailOption.quantity = 0;
                }

                detailOptions.Add(detailOption);
            }

            return new Option { optionGroups = optionGroups, detailOptions = detailOptions };
        }

        public override int? GetTotalQuantity(string html, JToken data) => null;

        public override Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            if (setting?.GmarketGeneralCollectOption?.UseCouponAppliedPrice == false && rawData?.Data != null)
            {
                // 쿠폰적용가 미사용: 일반 판매가(promotion_price)로 재계산
                var data = rawData.Data;

                double baseSellPrice = data.Value<double?>("_promotionPrice") ?? 0;
                if (baseSellPrice <= 0)
                    baseSellPrice = data.Value<double?>("OriginPrice") ?? 0;
                if (baseSellPrice > 0)
                    product.price = ProductHelper.CreatePrice(Currency, baseSellPrice, baseSellPrice, true);

                // 옵션 가격 재계산 (쿠폰가 → 일반 판매가)
                if (product.option?.detailOptions != null)
                {
                    var optionData = data.Value("optionData");
                    if (optionData != null && optionData.Type != JTokenType.Null)
                    {
                        // 조합형 옵션: SellPriceText에서 가격 추출
                        int optionDepth = optionData.Value<int>("OptionDepth");
                        var lastData = optionData.Value($"CombinationalOptionData{optionDepth}");
                        var lastValues = lastData?.Value("OptionValues")?.ToList();
                        if (lastValues != null)
                        {
                            foreach (var detailOption in product.option.detailOptions)
                            {
                                var lv = lastValues.FirstOrDefault(v =>
                                {
                                    var names = new List<string>();
                                    for (int d = 0; d <= optionDepth; d++)
                                    {
                                        string n = v.Value<string>($"OptionValue{d}");
                                        if (!string.IsNullOrWhiteSpace(n)) names.Add(n);
                                    }
                                    return string.Join("ㅡ", names) == detailOption.optionIds;
                                });

                                if (lv != null)
                                {
                                    string sellPriceText = lv.Value<string>("SellPriceText");
                                    double? sellPrice = sellPriceText.ParseToDouble();
                                    if (sellPrice != null && sellPrice > 0)
                                        detailOption.price = ProductHelper.CreatePrice(Currency, (double)sellPrice, (double)sellPrice);
                                }
                            }
                        }
                    }
                    else
                    {
                        // 선택형 옵션: baseSellPrice + optionPrice로 재계산
                        var selOpts = data["_selectableOptions"] as JArray;
                        if (selOpts != null)
                        {
                            foreach (var detailOption in product.option.detailOptions)
                            {
                                var opt = selOpts.FirstOrDefault(o => o.Value<string>("optionValue") == detailOption.optionIds);
                                double optPrice = opt?.Value<double>("optionPrice") ?? 0;
                                double finalPrice = baseSellPrice + optPrice;
                                detailOption.price = ProductHelper.CreatePrice(Currency, finalPrice, finalPrice);
                            }
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }

        #endregion

        #region Helpers

        private JArray ParseSelectableOptions(string html)
        {
            // 옵션그룹명: <input ... value="사이즈" ... id="optOrderSelOptNm_0"/> (속성 순서 무관)
            var nameMatch = Regex.Match(html, @"value=""([^""]+)""[^>]*id=""optOrderSelOptNm_0""");
            if (!nameMatch.Success)
                nameMatch = Regex.Match(html, @"id=""optOrderSelOptNm_0""[^>]*value=""([^""]+)""");
            string groupName = nameMatch.Success ? nameMatch.Groups[1].Value : "옵션";

            // 옵션 링크: <a ... data-optionValue1="95" ... data-optionPrice="0" ...>95 - 9개 남음</a>
            var linkMatches = Regex.Matches(html, @"<a\s[^>]*data-optionValue1=""[^""]+""[^>]*>.*?</a>", RegexOptions.Singleline);
            if (linkMatches.Count == 0) return null;

            var result = new JArray();
            var seen = new HashSet<string>();
            foreach (Match linkMatch in linkMatches)
            {
                string tag = linkMatch.Value;
                var valMatch = Regex.Match(tag, @"data-optionValue1=""([^""]+)""");
                if (!valMatch.Success) continue;
                string optValue = valMatch.Groups[1].Value;
                if (!seen.Add(optValue)) continue; // 중복 제거 (Above/Wing)

                var priceMatch = Regex.Match(tag, @"data-optionPrice=""([^""]+)""");
                double.TryParse(priceMatch.Success ? priceMatch.Groups[1].Value : "0", out double optPrice);

                string displayText = Regex.Replace(Regex.Match(tag, @">(.+)</a>", RegexOptions.Singleline).Groups[1].Value, @"<[^>]+>", "").Trim();

                int qty = 9;
                bool isSoldOut = false;
                if (displayText.Contains("품절"))
                {
                    isSoldOut = true;
                    qty = 0;
                }
                else
                {
                    var stockMatch = Regex.Match(displayText, @"(\d+)개\s*남음");
                    if (stockMatch.Success) qty = int.Parse(stockMatch.Groups[1].Value);
                }

                result.Add(new JObject
                {
                    ["groupName"] = groupName,
                    ["optionValue"] = optValue,
                    ["optionPrice"] = optPrice,
                    ["quantity"] = qty,
                    ["isSoldOut"] = isSoldOut
                });
            }

            return result.Count > 0 ? result : null;
        }

        private Option BuildSelectableOption(JToken data, JArray selOpts)
        {
            string groupName = selOpts[0].Value<string>("groupName");
            double basePrice = data.Value<double?>("_couponPrice")
                            ?? data.Value<double?>("_promotionPrice")
                            ?? data.Value<double?>("OriginPrice")
                            ?? 0;

            var optionGroup = new Option.OptionGroup
            {
                optionId = "1",
                groupName = ProductHelper.CreateName(Language, groupName),
                groups = new List<Option.OptionGroup.Group>()
            };

            var detailOptions = new List<Option.DetailOption>();
            foreach (var opt in selOpts)
            {
                string optValue = opt.Value<string>("optionValue");
                double optPrice = opt.Value<double>("optionPrice");
                double finalPrice = basePrice + optPrice;
                int qty = opt.Value<int>("quantity");
                bool isSoldOut = opt.Value<bool>("isSoldOut");

                optionGroup.groups.Add(new Option.OptionGroup.Group
                {
                    groupId = optValue,
                    optionName = ProductHelper.CreateName(Language, optValue)
                });

                detailOptions.Add(new Option.DetailOption
                {
                    optionIds = optValue,
                    optionGroupName = ProductHelper.CreateName(Language, groupName),
                    optionName = ProductHelper.CreateName(Language, optValue),
                    price = ProductHelper.CreatePrice(Currency, finalPrice, finalPrice),
                    isSoldOut = isSoldOut,
                    quantity = qty
                });
            }

            return new Option { optionGroups = new List<Option.OptionGroup> { optionGroup }, detailOptions = detailOptions };
        }

        private bool IsBundleDealPage(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return false;
            if (html.IndexOf("class=\"seloption\"", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (Regex.IsMatch(html, @"상품\s*(<[^>]+>)?\s*(0[1-9]|[1-9]\d)", RegexOptions.IgnoreCase)) return true;
            return false;
        }

        private string FormatUrl(string rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl)) return null;
            return rawUrl.StartsWith("//") ? "https:" + rawUrl : rawUrl;
        }

        private Option GetDefaultOption(List<Price> prices)
        {
            var groupName = new Name(Language.KR, "사이즈");
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
                            new Option.OptionGroup.Group { groupId = "0", optionName = new List<Name> { optionName } }
                        }
                    }
                },
                detailOptions = new List<Option.DetailOption>
                {
                    new Option.DetailOption
                    {
                        id = "0",
                        isSoldOut = false,
                        quantity = 9,
                        optionGroupName = new List<Name> { groupName },
                        optionName = new List<Name> { optionName },
                        price = prices
                    }
                }
            };
        }

        #endregion
    }
}
