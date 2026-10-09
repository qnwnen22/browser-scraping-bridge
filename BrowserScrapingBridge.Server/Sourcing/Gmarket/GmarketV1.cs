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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Gmarket
{
    public class GmarketV1 : ICollect
    {
        public string Site => nameof(Gmarket);
        public string Name => "지마켓스타배송";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public string GetCode(string url)
        {
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

            return query.Get("goodscode");
        }

        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null; // 로그인 
        }

        private string FormatUrl(string rawImage)
        {
            return !string.IsNullOrWhiteSpace(rawImage)
                ? (rawImage.StartsWith("//") ? "https:" + rawImage : rawImage)
                : null;
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
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

        private void CheckAccess(string url, string html)
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

        public JToken GetData(string html)
        {
            var scripts = html.GetValues("//script[@type='text/javascript']", TextEx.XPathOption.OuterHtml);

            var a = string.Join("\n\n", scripts);

            var goodsScript = scripts.Find(x => x.Contains("var goods"));
            var goods = goodsScript.SingleSplit("var goods =", ";");
            if (string.IsNullOrWhiteSpace(goods)) throw new Exception("상품 정보를 확인할 수 없습니다.");
            JObject data = goods.ToJObject();

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

            var deliveryScript = scripts.Find(x => x.Contains("GmktItem.DeliveryInfo"));
            if (string.IsNullOrWhiteSpace(deliveryScript) == false)
            {
                string deliveryJson = deliveryScript.SingleSplit("GmktItem.DeliveryInfo = ", ";");
                data["deliveryInfo"] = deliveryJson.ToJObject();
            }

            return data;
        }

        public List<Name> GetTitle(JToken data)
        {
            string goodsName = data.Value<string>("GoodsName");
            if (string.IsNullOrWhiteSpace(goodsName)) throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(this.Language, goodsName);
        }

        public List<Name> GetBrand(JToken data)
        {
            string brandName = data.Value<string>("BrandName");
            if (string.IsNullOrWhiteSpace(brandName)) return null;
            return ProductHelper.CreateName(this.Language, brandName);
        }

        public List<Desc> GetDescs(string html)
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

        public List<Tag> GetTags()
        {
            return null;
        }

        public List<Price> GetPrice(JToken data)
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

        public List<string> GetMainImages(JToken data)
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

        public async Task<string> GetDetailPageHtmlAsync(string connectionId, string html)
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

            return HtmlReader.GetDafaultHtml(detailHtml);
        }

        public Option GetOption(JToken data)
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

        public void ApplyCollectOptionSetting(Product product, JToken data, CollectOptionSetting setting)
        {
            if (product == null) return;
            if (data == null) return;
            if (data.HasValues == false) return;
            if (setting == null) return;
            if (setting.GmarketCollectOption == null) return;

            if (setting.GmarketCollectOption.UseDeliveryPrice == true)
            {
                var deliveryFee = data.Value("deliveryInfo")?.Value<double?>("deliveryFee");
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

        private string CreateGoodsOptions(string code, string html)
        {
            var optionSelects = html.SelectNode($"//ul[contains(@class,'type_minishop') and contains(@class,'select-itemoption-list')]//li//a[@data-goodscode='{code}']");
            var attributes = optionSelects.Attributes;

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
            sb.Append($"isAboveOptionLayer={isAboveOptionLayer}&");
            sb.Append($"groupIndex={groupIndex}&");
            sb.Append($"isGroupVip={isGroupVip}&");
            sb.Append($"isPcs={isPcs}&");
            sb.Append($"item={item}&");
            sb.Append($"delivery={delivery}&");
            sb.Append($"discount={discount}&");
            sb.Append($"couponMarketInfo={couponMarketInfo}");
            var address = sb.ToString();
            return address;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            Thread.Sleep(1000);
            CheckAccess(url, html);
            var product = new Product();
            try
            {
                string code = GetCode(url);
                var aasd = CreateGoodsOptions(code, html);
                var optionHtml = await ChatHubService.GetRequest(connectionId, aasd);
                JToken data = GetData(html);
                product.site = this.Site;
                product.code = code;
                product.url = url;
                product.currency = Currency.KRW.ToString();
                product.title = GetTitle(data);
                product.brand = GetBrand(data);
                product.descs = GetDescs(html);
                product.price = GetPrice(data);
                product.mainImages = GetMainImages(data);
                product.detailPageHtml = await GetDetailPageHtmlAsync(connectionId, html);
                product.option = GetOption(data);
                ApplyCollectOptionSetting(product, data, collectOptionSetting);
            }
            catch (SaleException se)
            {
                product.collectStatus = ProductCollectStatus.Success;
                product.saleStatus = se.SaleStatus;
                product.memo = se.Message;
            }
            catch (Exception exception)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = exception.Message;
            }

            return product;
        }

        public async Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            Thread.Sleep(1000);
            CheckAccess(url, html);
            JToken data = GetData(html);
            return new CollectRawData(url, data, html);
        }

        public Product GetProduct(CollectRawData rawData)
        {
            var product = new Product();
            try
            {
                product.site = this.Site;
                string code = GetCode(rawData.Url);
                product.code = code;
                product.url = rawData.Url;
                product.currency = Currency.KRW.ToString();
                product.title = GetTitle(rawData.Data);
                product.brand = GetBrand(rawData.Data);
                product.descs = GetDescs(rawData.Html);
                product.price = GetPrice(rawData.Data);
                product.mainImages = GetMainImages(rawData.Data);
                //product.detailPageHtml = await GetDetailPageHtmlAsync(connectionId, html);
                product.option = GetOption(rawData.Data);
            }
            catch (Exception exception)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = exception.Message;
            }
            return product;
        }

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
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
