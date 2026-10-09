#nullable disable warnings
using Newtonsoft.Json;
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
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Gmarket
{
    public class Gmarket : CollectBase
    {
        public override string Site => nameof(Gmarket);
        public override string Name => "지마켓스타배송";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = true;

        public override Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null; // 로그인 
        }

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("p", page);
            var page_url = uriBuilderAdvance.ToString();
            string html = await ChatHubService.GetRequest(connectionId, page_url);
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var script_content = html.GetValue("//script[@id='__NEXT_DATA__' and @type='application/json']", TextEx.XPathOption.InnerHtml);
            if (string.IsNullOrWhiteSpace(script_content))
            {
                throw new Exception("상품 목록을 찾을 수 없습니다.");
            }

            JToken root = JsonConvert.DeserializeObject<JToken>(script_content);
            var item_cards = root.SelectToken("props.pageProps.initialStates.itemPage.itemListingContentArea.itemCards");
            if (item_cards == null || !item_cards.HasValues)
            {
                throw new Exception("상품 상세 데이터를 찾을 수 없습니다.");
            }

            var collect_page_infos = new List<CollectPageInfo>();

            foreach (var item in item_cards)
            {
                string href = item.SelectToken("itemUrl")?.ToString();
                if (string.IsNullOrWhiteSpace(href))
                {
                    continue;
                }

                var match = System.Text.RegularExpressions.Regex.Match(href, @"goodscode=(\d+)");
                if (!match.Success)
                {
                    continue;
                }

                string goods_no = match.Groups[1].Value;

                var collect_page_info = new CollectPageInfo
                {
                    Code = goods_no,
                    Url = $"https://item.gmarket.co.kr/Item?goodscode={goods_no}",
                    Title = item.SelectToken("title")?.ToString()?.Trim()
                };

                collect_page_infos.Add(collect_page_info);
            }

            return collect_page_infos;
        }

        public override void ThrowIfCaptcha(string url, string html)
        {
            var h1 = html.GetValue("//title", TextEx.XPathOption.InnerText);
            if (h1.ToUpper() == "ACCESS DENIED")
            {
                var errorHtml = html.ToHtmlDecodeComplete();
                var errorMsg = errorHtml.GetValue("//body", TextEx.XPathOption.InnerText)?.Trim();

                throw new CaptchaException()
                {
                    CaptchaInfo = new CaptchaInfo()
                    {
                        Url = url,
                        Message = errorMsg
                    }
                };
            }

            var p_txt1 = html.GetValue("//p[@class='p_txt1']", TextEx.XPathOption.InnerText);
            if (p_txt1?.Contains("고객님의 사이트 내 활동이 봇의 동작과 유사해 보입니다") == true)
            {
                throw new CaptchaException()
                {
                    CaptchaInfo = new CaptchaInfo()
                    {
                        Url = url,
                        Message = $"{this.Name} 캡차가 발생했습니다."
                    }
                };
            }
        }

        public override string GetCode(string url)
        {
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

            return query.Get("goodscode");
        }

        public override string CreateProductUrl(string code)
        {
            return $"https://item.gmarket.co.kr/Item?goodscode={code}";
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            var scripts = html.GetValues("//script[@type='text/javascript']", TextEx.XPathOption.OuterHtml);

            var goodsScript = scripts.Find(x => x.Contains("var goods"));
            var goods = goodsScript.SingleSplit("var goods =", ";");
            if (string.IsNullOrWhiteSpace(goods)) throw new Exception("상품 정보를 확인할 수 없습니다.");
            JObject data = goods.ToJObject();
            data["priceInfo"] = ParsingPriceInfo(scripts);
            data["optionData"] = ParsingOptionData(scripts);
            data["htmlOption"] = ParsingHtmlOption(html);
            data["deliveryInfo"] = ParsingDeliveryInfo(scripts);
            data["detailPageData"] = await GetDetailPageHtmlAsync(connectionId, html);
            return data;
        }

        private JToken ParsingPriceInfo(List<string> scripts)
        {
            string couponAppliedPriceInfoScript = scripts.Find(x => x.Contains("var couponAppliedPriceInfo"));
            if (string.IsNullOrWhiteSpace(couponAppliedPriceInfoScript) == false)
            {
                string couponAppliedPriceInfo = couponAppliedPriceInfoScript.SingleSplit("var couponAppliedPriceInfo = ", ";");
                if (string.IsNullOrWhiteSpace(couponAppliedPriceInfo) == false)
                {
                    JObject priceInfo = couponAppliedPriceInfo.ToJObject();
                    return priceInfo;
                }
            }
            return null;
        }

        private JToken ParsingOptionData(List<string> scripts)
        {
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
                    return optionData;
                }
            }
            return null;
        }

        private JToken ParsingHtmlOption(string html)
        {
            var section_option_areas = html.SelectNodes("//div[@class='section_option_area']", HtmlType.OuterHtml);
            string section_option_area = html.SelectNode("//div[@class='section_option_area']", HtmlType.OuterHtml);
            if (string.IsNullOrWhiteSpace(section_option_area)) return null;
            List<string> item_options = section_option_area.SelectNodes("//div[contains(@class,'item_options')]", HtmlType.OuterHtml);
            if (item_options == null || item_options.Count <= 0) return null;

            JArray htmlOption = new JArray();
            foreach (var item_option in item_options)
            {
                var optionData = new JObject();

                var optionName = item_option.SelectNode("//button//span", HtmlType.InnerText);
                if (string.IsNullOrWhiteSpace(optionName)) continue;

                optionData["optionName"] = optionName.Trim();

                var optionValues = new JArray();
                List<string> lis = item_option.SelectNodes("//ul//li", HtmlType.OuterHtml);
                if (lis != null)
                {
                    foreach (var li in lis)
                    {
                        JObject optValue = new JObject();
                        string _class = li.SelectNode("//li", "class");
                        bool soldUut = false;
                        if (string.IsNullOrWhiteSpace(_class))
                        {
                        }
                        else if (_class.Contains("soldout"))
                        {
                            soldUut = true;
                        }
                        optValue["soldOut"] = soldUut;
                        HtmlAgilityPack.HtmlNode aNode = li.SelectNode("//a");
                        HtmlAgilityPack.HtmlAttributeCollection attributes = aNode.Attributes;
                        optValue["optionTitle"] = attributes["data-optiontitle"]?.Value;
                        optValue["optionNm"] = attributes["data-optionnm"]?.Value;

                        optValue["optionNo"] = attributes["data-optionno"]?.Value;
                        optValue["optionPrice"] = attributes["data-optionprice"]?.Value;
                        optValue["optionValue1"] = attributes["data-optionvalue1"]?.Value;
                        optionValues.Add(optValue);
                    }
                }
                optionData["optionValues"] = optionValues;
                htmlOption.Add(optionData);
            }
            return htmlOption;
        }

        private JToken ParsingDeliveryInfo(List<string> scripts)
        {
            var deliveryScript = scripts.Find(x => x.Contains("GmktItem.DeliveryInfo"));
            if (string.IsNullOrWhiteSpace(deliveryScript) == false)
            {
                string deliveryJson = deliveryScript.SingleSplit("GmktItem.DeliveryInfo = ", ";");
                return deliveryJson.ToJObject();
            }
            else
            {
                return null;
            }
        }

        [Obsolete("ChatHub GetRequest의 인코딩 문제 해결 시 사용 가능")]
        private async Task<JToken> CreateGoodsOptions(string connectionId, string code, string html)
        {
            HtmlAgilityPack.HtmlNode optionSelects = html.SelectNode($"//ul[contains(@class,'type_minishop') and contains(@class,'select-itemoption-list')]//li//a[@data-goodscode='{code}']");
            if (optionSelects == null) return null;

            HtmlAgilityPack.HtmlAttributeCollection attributes = optionSelects.Attributes;
            string iscore = attributes["data-iscore"]?.Value;
            string isAboveOptionLayer = attributes["data-isaboveoptionlayer"]?.Value;
            string groupIndex = attributes["data-groupindex"]?.Value;
            string isGroupVip = attributes["data-isgroupvip"]?.Value;
            string isPcs = attributes["data-ispcs"]?.Value;
            string item = attributes["data-item"]?.Value;
            string delivery = attributes["data-delivery"]?.Value;
            string discount = attributes["data-discount"]?.Value;
            string couponMarketInfo = attributes["data-couponmarketinfo"]?.Value;

            var sb = new StringBuilder();
            sb.Append($"https://item.gmarket.co.kr/Item/GoodsOptions?");
            sb.Append($"goodsCode={code}&");
            sb.Append($"isCore={iscore}&");
            sb.Append($"isAboveOptionLayer=false&");
            sb.Append($"groupIndex={groupIndex}&");
            sb.Append($"isGroupVip={isGroupVip}&");
            sb.Append($"isPcs={isPcs}&");
            sb.Append($"item={item}&");
            sb.Append($"delivery={delivery}&");
            sb.Append($"discount={discount}&");
            sb.Append($"couponMarketInfo={couponMarketInfo}");
            var address = sb.ToString();

            string response = await this.ChatHubService.GetRequest(connectionId, address);
            var aaasd = Encoding.GetEncoding("euc-kr").GetBytes(response);
            var qwewqe = Encoding.Default.GetString(aaasd);
            return address;
        }

        private async Task<string> GetDetailPageHtmlAsync(string connectionId, string html)
        {
            string iframeSrc = html.GetValue("//iframe[@id='detail1']", "src");

            if (string.IsNullOrWhiteSpace(iframeSrc))
            {
                return null;
            }

            string baseUrl = "https://item.gmarket.co.kr";
            string detailUrl = iframeSrc.StartsWith("http") ? iframeSrc : baseUrl + iframeSrc;

            string detailHtmlPage = await this.ChatHubService.GetRequest(connectionId, detailUrl);

            if (string.IsNullOrWhiteSpace(detailHtmlPage))
            {
                detailHtmlPage = html.GetValue("//div[@id='basic_detail_html']", TextEx.XPathOption.OuterHtml);
                if (string.IsNullOrWhiteSpace(detailHtmlPage))
                {
                    return null;
                }
            }

            var detailHtml = detailHtmlPage.GetValue("//div[@id='basic_detail_html']", TextEx.XPathOption.OuterHtml);

            if (string.IsNullOrWhiteSpace(detailHtml))
            {
                return null;
            }

            return detailHtml;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string goodsName = data.Value<string>("GoodsName");
            if (string.IsNullOrWhiteSpace(goodsName)) throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(this.Language, goodsName);
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            string brandName = data.Value<string>("BrandName");
            if (string.IsNullOrWhiteSpace(brandName)) return null;
            return ProductHelper.CreateName(this.Language, brandName);
        }

        public override string GetModel(string html, JToken data)
        {
            return null;
        }

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
                result.Add(new Desc(index, this.Language, th, td));
                index++;
            }
            if (result.Count <= 0) return null;
            return result;
        }

        public override List<Tag> GetTags(string html, JToken data)
        {
            return null;
        }

        public override List<Price> GetPrice(string html, JToken data)
        {
            JToken priceInfo = data.Value("priceInfo");
            if (priceInfo != null)
            {
                var prices = new List<double>();
                double couponAppliedPrice = priceInfo.Value<double>("couponAppliedPrice");
                if (couponAppliedPrice > 0)
                {
                    prices.Add(couponAppliedPrice);
                }
                double targetSellPrice = priceInfo.Value<double>("targetSellPrice");
                if (targetSellPrice > 0)
                {
                    prices.Add(targetSellPrice);
                }
                if (prices.Count == 0)
                {
                    throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품 입니다.");
                }
                var salePrice = prices.Min();
                var originalPrice = prices.Max();
                if (salePrice == 0 || originalPrice == 0) throw new Exception("상품 가격을 확인할 수 없습니다.");
                return ProductHelper.CreatePrice(this.Currency, originalPrice, salePrice, true);
            }
            else
            {
                double? originalPrice = data.Value<double?>("OriginPrice");
                if (originalPrice == null) throw new Exception("상품 가격을 확인할 수 없습니다.");
                return ProductHelper.CreatePrice(this.Currency, originalPrice, originalPrice, true);
            }
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var mainImages = new List<string>();

            var rawMainImage = data["ImageUrl"]?.ToString();
            var mainImage = FormatUrl(rawMainImage);

            if (!string.IsNullOrWhiteSpace(mainImage))
            {
                mainImages.Add(mainImage);
            }
            if (data["MoreImages"] != null && data["MoreImages"].Type == JTokenType.Array)
            {
                foreach (var img in data["MoreImages"])
                {
                    var rawPath = img["FullDefaultImagePath"]?.ToString();
                    var image = FormatUrl(rawPath);
                    if (!string.IsNullOrWhiteSpace(image))
                    {
                        mainImages.Add(image);
                    }

                }
            }
            return mainImages.Distinct().ToList();
        }

        private string FormatUrl(string rawImage)
        {
            return !string.IsNullOrWhiteSpace(rawImage)
                ? (rawImage.StartsWith("//") ? "https:" + rawImage : rawImage)
                : null;
        }

        public override string GetVideoUrl(string html, JToken data)
        {
            return null;
        }

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var detailPageData = data.Value<string>("detailPageData");
            if (string.IsNullOrWhiteSpace(detailPageData)) return null;
            return HtmlReader.GetDafaultHtml(detailPageData);
        }
        
        public override Option GetOption(string html, JToken data)
        {
            var optionData = data.Value("optionData");
            var getOption1 = GetOption1(data);
            if (getOption1 != null)
            {
                return getOption1;
            }
            else if (getOption1 == null && CheckBundleItem(data))
            {
                return GetOption2(data);
            }
            else
            {
                return null;
            }
        }
        private Option GetOption1(JToken data)
        {
            var optionData = data.Value("optionData");
            if (optionData == null) return null;

            var optionGroups = new List<Option.OptionGroup>();
            int optionDepth = optionData.Value<int>("OptionDepth");
            for (int depth = 1; depth <= optionDepth; depth++)
            {
                var optionGroup = new Option.OptionGroup();

                string key = $"CombinationalOptionData{depth}";
                var combinationalOptionData = optionData.Value(key);
                if (combinationalOptionData == null) continue;

                optionGroup.optionId = depth.ToString();
                var optionName = combinationalOptionData.Value<string>("OptionName");
                optionGroup.groupName = ProductHelper.CreateName(this.Language, optionName);

                optionGroup.groups = new List<Option.OptionGroup.Group>();
                List<JToken> optionValues = combinationalOptionData.Value("OptionValues").ToList();
                foreach (JToken optionValue in optionValues)
                {
                    var group = new Option.OptionGroup.Group();
                    var groupOptionName = optionValue.Value<string>($"OptionValue{depth}");

                    Option.OptionGroup.Group find = optionGroup.groups.Find(x => x.optionName.First().text == groupOptionName);
                    if (find != null) continue;

                    group.groupId = groupOptionName;
                    group.optionName = ProductHelper.CreateName(this.Language, groupOptionName);
                    var matchingImageUrl = optionValue.Value<string>("MatchingImageUrl");
                    if (matchingImageUrl.Contains("no_img") == false)
                    {
                        group.optionImage = FormatUrl(matchingImageUrl);
                    }
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
                var detailOption = new Option.DetailOption();

                JToken lastOptionValue = lastOptionValues[i];
                var names = new List<string>();
                for (int depth = 0; depth <= optionDepth; depth++)
                {
                    string optionName = lastOptionValue.Value<string>($"OptionValue{depth}");
                    if (string.IsNullOrWhiteSpace(optionName)) continue;
                    names.Add(optionName);
                }

                var optionStatus = lastOptionValue.Value<bool?>("OptionStatus");

                string sellPriceText = lastOptionValue.Value<string>("SellPriceText");
                double? originalPrice = sellPriceText.ParseToDouble();
                string couponAppliedOptionPriceText = lastOptionValue.Value<string>("CouponAppliedOptionPriceText");
                double? salePrice = couponAppliedOptionPriceText.ParseToDouble();
                if (salePrice == null || salePrice <= 0)
                {
                    salePrice = originalPrice;
                }

                string join = string.Join("ㅡ", names);
                detailOption.optionIds = join;
                detailOption.optionGroupName = ProductHelper.CreateName(this.Language, optionGroupName);
                detailOption.optionName = ProductHelper.CreateName(this.Language, join);
                if (optionStatus == true)
                {
                    detailOption.isSoldOut = false;
                    string InventoryCount = lastOptionValue.Value<string>("InventoryCount");
                    int quantity = 0;
                    if (int.TryParse(InventoryCount, out quantity) && quantity > 0)
                    {
                        detailOption.quantity = quantity;
                    }
                    else
                    {
                        detailOption.quantity = 9;
                    }
                }
                else
                {
                    detailOption.isSoldOut = true;
                    detailOption.quantity = 0;
                }
                detailOption.price = ProductHelper.CreatePrice(this.Currency, originalPrice, salePrice);
                detailOptions.Add(detailOption);
            }

            var option = new Option();
            option.optionGroups = optionGroups;
            option.detailOptions = detailOptions;
            return option;
        }
        private bool CheckBundleItem(JToken data)
        {
            List<JToken> groupGoodscodeList = data.Value("GroupGoodscodeList")?.ToList();
            return groupGoodscodeList != null && groupGoodscodeList.Count > 0;
        }
        private Option GetOption2(JToken data)
        {
            var htmlOption = data.Value("htmlOption");
            if (htmlOption == null) return null;
            // 옵션이 2개 이상인 경우 스크립트 기반으로 동작하는 옵션정보를 수집해야함
            // 정상적인 상품의 경우 data.optionData를 통해 수집이 가능함
            // 추후에 수집이 가능한 형태의 상품 샘플이 생길 경우 작업
            throw new CollectException(ProductCollectStatus.Exception, "비정상적인 옵션이 섞여있는 번들 상품입니다.");
        }


        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
            return;
        }

        public override int? GetTotalQuantity(string html, JToken data)
        {
            return null;
        }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            if (product == null) return;
            if (rawData.Data == null) return;
            if (rawData.Data.HasValues == false) return;
            if (setting == null) return;
            if (setting.GmarketCollectOption == null) return;

            if (setting.GmarketCollectOption.UseDeliveryPrice == true)
            {
                var deliveryFee = rawData.Data.Value("deliveryInfo")?.Value<double?>("deliveryFee");
                if (deliveryFee != null)
                {
                    foreach (var price in product.price)
                    {
                        price.salePrice += (double)deliveryFee;
                        price.originalPrice += (double)deliveryFee;
                    }
                }
                if (product.option != null)
                {
                    foreach (var detailOption in product.option.detailOptions)
                    {
                        foreach (var price in detailOption.price)
                        {
                            price.salePrice += (double)deliveryFee;
                            price.originalPrice += (double)deliveryFee;
                        }
                    }
                }
            }
        }
    }
}
