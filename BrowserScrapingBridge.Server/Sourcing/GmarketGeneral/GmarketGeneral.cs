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

namespace BrowserScrapingBridge.Server.Sourcing.GmarketGeneral
{
    /// <summary>
    /// 지마켓 "일반상품(organictier)" 수집
    /// - 리스트: https://www.gmarket.co.kr/n/list?... (NextData에서 organictier만 추출)
    /// - 상세: https://item.gmarket.co.kr/Item?goodscode=...
    /// </summary>
    /// .... 
    public class GmarketGeneral : ICollect
    {
        public string Site => nameof(GmarketGeneral);
        public string Name => "지마켓_일반";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;

        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            // goodscode=123
            var m = Regex.Match(url, @"goodscode=(\d+)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;

            try
            {
                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                return query.Get("goodscode");
            }
            catch
            {
                return null;
            }
        }

        public Task<string> GetLoginUrlAsync(string connectionId)
        {
            // 로그인 불필요
            return Task.FromResult<string>(null);
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilder = new UriBuilderAdvance(url);
            uriBuilder.SetQuery("p", page); // G마켓 리스트는 p=page 로 동작하는 케이스가 많음(이미 쓰던 방식 유지)
            string pageUrl = uriBuilder.ToString();

            string html = await ChatHubService.GetRequest(connectionId, pageUrl);
            if (string.IsNullOrWhiteSpace(html))
                throw new Exception("페이지 HTML을 가져오지 못했습니다.");

            // organictier 섹션(일반상품) 블록만 추출
            //  - 실제 소스에서는 organictier가 여러 번 나옴(요청하신 내용)
            //  - 닫힘이 깊어서 정교한 DOM 파서가 제일 좋지만, 현재 프로젝트 스타일에 맞춰 Regex 근사 처리
            var organictierBlocks = new List<string>();
            var starts = System.Text.RegularExpressions.Regex.Matches(
                html,
                @"<div[^>]+spmc=""organictier""[^>]*>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            for (int i = 0; i < starts.Count; i++)
            {
                int startIdx = starts[i].Index;
                int endIdx = (i + 1 < starts.Count) ? starts[i + 1].Index : html.Length;
                if (endIdx <= startIdx) continue;

                organictierBlocks.Add(html.Substring(startIdx, endIdx - startIdx));
            }

            if (organictierBlocks.Count == 0)
                throw new Exception("organictier 섹션을 찾을 수 없습니다.");

            var codes = new HashSet<string>();

            foreach (var block in organictierBlocks)
            {
                // 1) href에서 goodscode
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
                             block, @"goodscode=(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    if (m.Success) codes.Add(m.Groups[1].Value);
                }

                // 2) data-montelena-goodscode="123"
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
                             block, @"data-montelena-goodscode=""(\d+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    if (m.Success) codes.Add(m.Groups[1].Value);
                }

                // 3) JSON 문자열 안의 "goodscode":"123"
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
                             block, @"""goodscode"":""(\d+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    if (m.Success) codes.Add(m.Groups[1].Value);
                }
            }

            if (codes.Count == 0)
                throw new Exception("organictier 섹션에서 goodscode를 찾을 수 없습니다.");

            var result = new List<CollectPageInfo>();
            foreach (var goodsNo in codes)
            {
                result.Add(new CollectPageInfo
                {
                    Code = goodsNo,
                    Url = $"https://item.gmarket.co.kr/Item?goodscode={goodsNo}",
                    Title = "" // 리스트에서 제목도 뽑고 싶으면 block에서 text__item title=""로 추가 파싱 가능
                });
            }

            return result;
        }


        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            if (string.IsNullOrWhiteSpace(html))
            {
                return new Product
                {
                    site = Site,
                    code = GetCode(url) ?? "",
                    url = url,
                    currency = Currency.KRW.ToString(),
                    collectStatus = ProductCollectStatus.Failed,
                    memo = "상품 접근 실패(응답이 비었습니다).",
                    title = new List<Name> { new Name(Language.KR, "") },
                    price = new List<Price> { new Price(Currency.KRW, 0, 0, true), new Price("KRW", 0, 0) },
                    mainImages = new List<string>(),
                    descs = new List<Desc>()
                };
            }

            CheckAccess(url, html);

            var product = new Product
            {
                site = this.Site,
                url = url,
                code = GetCode(url) ?? "",
                currency = this.Currency.ToString(),
                collectStatus = ProductCollectStatus.Failed,

                // ✅ CollectService .First() 방지용 기본값
                title = new List<Name> { new Name(this.Language, "") },
                price = new List<Price>
                {
                    new Price(this.Currency, 0, 0, true),
                    new Price("KRW", 0, 0)
                },
                mainImages = new List<string>(),
                descs = new List<Desc>(),
                tags = new List<Tag>()
            };

            try
            {
                JToken data = GetData(html);

                // GetProductAsync 내부 (html 가져온 직후 추천)
                if (IsBundleDealPage(html))
                {
                    product.collectStatus = ProductCollectStatus.Failed;
                    product.memo = "번들(딜/세트) 상품 제외";
                    return product; // 번들 상품 제외 사유를 수집 결과에 반환
                }

                product.site = Site;
                product.code = GetCode(url) ?? "";
                product.url = url;
                product.currency = Currency.KRW.ToString();

                product.title = GetTitle(data);
                product.brand = GetBrand(data);
                product.descs = GetDescs(html);
                product.price = GetPrice(data, collectOptionSetting?.GmarketGeneralCollectOption);
                product.mainImages = GetMainImages(data);

                product.detailPageHtml = await GetDetailPageHtmlAsync(connectionId, html);
                product.option = GetOption(data);

                if (product.option == null)
                {
                    product.option = CreateSingleDefaultOption(product.price);
                }

                product.collectStatus = ProductCollectStatus.Success;
            }
            catch (Exception ex)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = ex.Message;

                // CollectService 보호용 기본값
                product.site ??= Site;
                product.code ??= GetCode(url) ?? "";
                product.url ??= url;
                product.currency ??= Currency.KRW.ToString();
                product.title ??= new List<Name> { new Name(Language.KR, "") };
                product.price ??= new List<Price> { new Price(Currency.KRW, 0, 0, true), new Price("KRW", 0, 0) };
                product.mainImages ??= new List<string>();
                product.descs ??= new List<Desc>();
            }
            return product;
        }

        #region Internals (same parsing logic as existing Gmarket)


        private bool IsBundleDealPage(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return false;

            // 번들(세트) UI 흔적: seloption + "상품 01/02..." 패턴
            if (html.IndexOf("class=\"seloption\"", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            // 좀 더 보수적으로: "상품<em>01</em>" / "상품 <strong>01</strong>" 같은 표기도 포함
            if (System.Text.RegularExpressions.Regex.IsMatch(html, @"상품\s*(<[^>]+>)?\s*(0[1-9]|[1-9]\d)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return true;

            return false;
        }


        private Option CreateSingleDefaultOption(List<Price> productPrices)
        {
            // 안전장치: productPrices가 없으면 최소 0원이라도 넣기
            var safePrices = (productPrices != null && productPrices.Count > 0)
                ? productPrices
                : ProductHelper.CreatePrice(this.Currency, 0, 0, true);

            var option = new Option
            {
                optionGroups = new List<Option.OptionGroup>
        {
            new Option.OptionGroup
            {
                optionId = "1",
                groupName = ProductHelper.CreateName(this.Language, "옵션"),
                groups = new List<Option.OptionGroup.Group>
                {
                    new Option.OptionGroup.Group
                    {
                        groupId = "1",
                        optionName = ProductHelper.CreateName(this.Language, "단일상품"),
                        quantity = 9,
                        optionImage = null,
                        price = safePrices // ✅ 여기까지 같이 채우면 더 안전
                    }
                }
            }
        },
                detailOptions = new List<Option.DetailOption>
        {
            new Option.DetailOption
            {
                optionIds = "1",
                isSoldOut = false,
                optionGroupName = ProductHelper.CreateName(this.Language, "옵션"),
                optionName = ProductHelper.CreateName(this.Language, "단일상품"),
                quantity = 9,
                optionImage = null,
                price = safePrices // ✅ 핵심: null 금지
            }
        }
            };
            return option;
        }



        private void CheckAccess(string url, string html)
        {
            var title = html.GetValue("//title", TextEx.XPathOption.InnerText)?.Trim();
            if (!string.IsNullOrWhiteSpace(title) && title.Equals("ACCESS DENIED", StringComparison.OrdinalIgnoreCase))
            {
                var errorHtml = html.ToHtmlDecodeComplete();
                var errorMsg = errorHtml.GetValue("//body", TextEx.XPathOption.InnerText)?.Trim();

                throw new CaptchaException
                {
                    CaptchaInfo = new CaptchaInfo
                    {
                        Url = url,
                        Message = errorMsg
                    }
                };
            }
        }

        private string FormatUrl(string rawImage)
        {
            return !string.IsNullOrWhiteSpace(rawImage)
                ? (rawImage.StartsWith("//") ? "https:" + rawImage : rawImage)
                : null;
        }

        private JToken GetData(string html)
        {
            var scripts = html.GetValues("//script[@type='text/javascript']", TextEx.XPathOption.OuterHtml);
            var goodsScript = scripts.Find(x => x.Contains("var goods"));
            var goods = goodsScript.SingleSplit("var goods =", ";");
            if (string.IsNullOrWhiteSpace(goods)) throw new Exception("상품 정보를 확인할 수 없습니다.");

            JObject data = goods.ToJObject();

            // priceInfo (쿠폰가 등)
            string couponAppliedPriceInfoScript = scripts.Find(x => x.Contains("var couponAppliedPriceInfo"));
            if (string.IsNullOrWhiteSpace(couponAppliedPriceInfoScript) == false)
            {
                string couponAppliedPriceInfo = couponAppliedPriceInfoScript.SingleSplit("var couponAppliedPriceInfo = ", ";");
                if (string.IsNullOrWhiteSpace(couponAppliedPriceInfo) == false)
                {
                    JObject priceInfo = couponAppliedPriceInfo.ToJObject();
                    data["priceInfo"] = priceInfo;
                }
            }

            // optionData
            data["optionData"] = null;
            string[] keys = new string[] { "GmktItem.OptionParamCoreAbove.combOptionObj", "GmktItem.OptionParamCoreWing.combOptionObj" };
            foreach (var key in keys)
            {
                string optionScript = scripts.Find(x => x.Contains(key));
                if (string.IsNullOrWhiteSpace(optionScript) == false)
                {
                    string startTxt = $"{key} =";
                    int start = optionScript.IndexOf(startTxt);
                    optionScript = optionScript.Substring(start + startTxt.Length);
                    int last = optionScript.IndexOf("\n");
                    optionScript = optionScript.Substring(0, last);

                    JObject optionData = optionScript.ToJObject();
                    if (optionData != null)
                    {
                        data["optionData"] = optionData;
                        break;
                    }
                }
            }


            // (기존 keys loop 끝난 다음)
            if (data["optionData"] == null)
            {
                // html 전체에서 combOptionObj 직접 캐치 (Above/Wing/일반 모두)
                var m = System.Text.RegularExpressions.Regex.Match(
                    html,
                    @"GmktItem\.OptionParamCore(?:Above|Wing)?\.combOptionObj\s*=\s*(\{[\s\S]*?\});",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (m.Success)
                {
                    var objText = m.Groups[1].Value;

                    // JS object -> JSON 비슷하지만 대부분은 Newtonsoft가 처리 가능
                    // (혹시 마지막 세미콜론/줄바꿈 문제 방지)
                    objText = objText.Trim().TrimEnd(';');

                    try
                    {
                        JObject optionData = objText.ToJObject();
                        if (optionData != null)
                            data["optionData"] = optionData;
                    }
                    catch { /* 무시 */ }
                }
            }

            return data;
        }

        private List<Name> GetTitle(JToken data)
        {
            string goodsName = data.Value<string>("GoodsName");
            if (string.IsNullOrWhiteSpace(goodsName)) throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(Language, goodsName);
        }

        private List<Name> GetBrand(JToken data)
        {
            string brandName = data.Value<string>("BrandName");
            if (string.IsNullOrWhiteSpace(brandName)) return null;
            return ProductHelper.CreateName(Language, brandName);
        }

        private List<Desc> GetDescs(string html)
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

            return result.Count <= 0 ? null : result;
        }

        private List<Price> GetPrice(JToken data, GmarketGeneralCollectOption option = null)
        {
            JToken priceInfo = data.Value("priceInfo");
            if (priceInfo != null)
            {
                var prices = new List<double>();

                // 쿠폰적용가 선택(기본 선택): true 또는 null 이면 쿠폰적용가 사용, false 이면 일반가만
                if (option?.UseCouponAppliedPrice != false)
                {
                    double couponAppliedPrice = priceInfo.Value<double>("couponAppliedPrice");
                    if (couponAppliedPrice > 0) prices.Add(couponAppliedPrice);
                }

                double targetSellPrice = priceInfo.Value<double>("targetSellPrice");
                if (targetSellPrice > 0) prices.Add(targetSellPrice);

                if (prices.Count == 0) throw new Exception("상품 가격을 확인할 수 없습니다.");
                var salePrice = prices.Min();
                var originalPrice = prices.Max();
                if (salePrice == 0 || originalPrice == 0) throw new Exception("상품 가격을 확인할 수 없습니다.");

                return ProductHelper.CreatePrice(Currency, originalPrice, salePrice, true);
            }
            else
            {
                double? originalPrice = data.Value<double?>("OriginPrice");
                if (originalPrice == null) throw new Exception("상품 가격을 확인할 수 없습니다.");
                return ProductHelper.CreatePrice(Currency, originalPrice, originalPrice, true);
            }
        }

        private List<string> GetMainImages(JToken data)
        {
            var mainImages = new List<string>();

            var rawMainImage = data["ImageUrl"]?.ToString();
            var mainImage = FormatUrl(rawMainImage);
            if (!string.IsNullOrWhiteSpace(mainImage)) mainImages.Add(mainImage);

            if (data["MoreImages"] != null && data["MoreImages"].Type == JTokenType.Array)
            {
                foreach (var img in data["MoreImages"])
                {
                    var rawPath = img["FullDefaultImagePath"]?.ToString();
                    var image = FormatUrl(rawPath);
                    if (!string.IsNullOrWhiteSpace(image)) mainImages.Add(image);
                }
            }

            return mainImages.Distinct().ToList();
        }

        private async Task<string> GetDetailPageHtmlAsync(string connectionId, string html)
        {
            string iframeSrc = html.GetValue("//iframe[@id='detail1']", "src");
            if (string.IsNullOrWhiteSpace(iframeSrc)) return null;

            string baseUrl = "https://item.gmarket.co.kr";
            string detailUrl = iframeSrc.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? iframeSrc : baseUrl + iframeSrc;

            string detailHtmlPage = await ChatHubService.GetRequest(connectionId, detailUrl);

            if (string.IsNullOrWhiteSpace(detailHtmlPage))
            {
                detailHtmlPage = html.GetValue("//div[@id='basic_detail_html']", TextEx.XPathOption.OuterHtml);
                if (string.IsNullOrWhiteSpace(detailHtmlPage)) return null;
            }

            var detailHtml = detailHtmlPage.GetValue("//div[@id='basic_detail_html']", TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(detailHtml)) return null;

            return HtmlReader.GetDafaultHtml(detailHtml);
        }

        private Option GetOption(JToken data)
        {
            var optionData = data.Value("optionData");
            if (optionData == null) return null;

            var optionGroups = new List<Option.OptionGroup>();
            int optionDepth = optionData.Value<int>("OptionDepth");

            for (int depth = 1; depth <= optionDepth; depth++)
            {
                string key = $"CombinationalOptionData{depth}";
                var combinationalOptionData = optionData.Value(key);
                if (combinationalOptionData == null) continue;

                var optionGroup = new Option.OptionGroup
                {
                    optionId = depth.ToString(),
                    groupName = ProductHelper.CreateName(Language, combinationalOptionData.Value<string>("OptionName")),
                    groups = new List<Option.OptionGroup.Group>()
                };

                List<JToken> optionValues = combinationalOptionData.Value("OptionValues").ToList();
                foreach (JToken optionValue in optionValues)
                {
                    var groupOptionName = optionValue.Value<string>($"OptionValue{depth}");
                    if (string.IsNullOrWhiteSpace(groupOptionName)) continue;

                    if (optionGroup.groups.Any(x => x.optionName.First().text == groupOptionName)) continue;

                    var group = new Option.OptionGroup.Group
                    {
                        groupId = groupOptionName,
                        optionName = ProductHelper.CreateName(Language, groupOptionName)
                    };

                    var matchingImageUrl = optionValue.Value<string>("MatchingImageUrl");
                    if (!string.IsNullOrWhiteSpace(matchingImageUrl) && matchingImageUrl.Contains("no_img") == false)
                        group.optionImage = FormatUrl(matchingImageUrl);

                    optionGroup.groups.Add(group);
                }

                optionGroups.Add(optionGroup);
            }

            var detailOptions = new List<Option.DetailOption>();
            string optionGroupName = ProductHelper.GetOptionGroupName(optionGroups);

            JToken lastOptionData = optionData.Value($"CombinationalOptionData{optionDepth}");
            var lastOptionValues = lastOptionData.Value("OptionValues").ToList();

            for (int i = 0; i < lastOptionValues.Count; i++)
            {
                JToken lastOptionValue = lastOptionValues[i];

                var names = new List<string>();
                for (int depth = 0; depth <= optionDepth; depth++)
                {
                    string optionName = lastOptionValue.Value<string>($"OptionValue{depth}");
                    if (!string.IsNullOrWhiteSpace(optionName)) names.Add(optionName);
                }

                string join = string.Join("ㅡ", names);

                var optionStatus = lastOptionValue.Value<bool?>("OptionStatus");

                string sellPriceText = lastOptionValue.Value<string>("SellPriceText");
                double? originalPrice = sellPriceText.ParseToDouble();

                string couponAppliedOptionPriceText = lastOptionValue.Value<string>("CouponAppliedOptionPriceText");
                double? salePrice = couponAppliedOptionPriceText.ParseToDouble();
                if (salePrice == null || salePrice <= 0) salePrice = originalPrice;

                var detailOption = new Option.DetailOption
                {
                    optionIds = join,
                    optionGroupName = ProductHelper.CreateName(Language, optionGroupName),
                    optionName = ProductHelper.CreateName(Language, join),
                    price = ProductHelper.CreatePrice(Currency, originalPrice, salePrice)
                };

                if (optionStatus == true)
                {
                    detailOption.isSoldOut = false;

                    string inv = lastOptionValue.Value<string>("InventoryCount");
                    int qty = 0;
                    if (int.TryParse(inv, out qty) && qty > 0) detailOption.quantity = qty;
                    else detailOption.quantity = 9;
                }
                else
                {
                    detailOption.isSoldOut = true;
                    detailOption.quantity = 0;
                }

                detailOptions.Add(detailOption);
            }

            return new Option
            {
                optionGroups = optionGroups,
                detailOptions = detailOptions
            };
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

        #endregion
    }

}

