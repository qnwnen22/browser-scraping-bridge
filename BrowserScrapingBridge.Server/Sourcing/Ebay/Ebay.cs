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
using System.Net.Http;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Ebay
{
    public class Ebay : ICollect
    {
        public string Site => nameof(Ebay);
        public string Name => "이베이";
        public Language Language { get; private set; } = Language.EN;
        public Currency Currency => Currency.USD;
        public string CheckLoginUrl => "https://www.ebay.com/";
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }

        public bool SupportsCaching { get; set; }
        public async Task<string> GetHtmlAsync(string url, Dictionary<string, string> headers = null)
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler() { Proxy = this.Proxy };
                using (HttpClient httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler))
                {
                    if (headers != null)
                    {
                        httpClient.SetHeader(headers);
                    }
                    HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url);
                    string html = await httpResponseMessage.Content.ReadAsStringAsync();
                    return html;
                }
            }
            catch (Exception exception)
            {
                DebugConsole.WriteLine(exception.Message);
                return null;
            }
        }
        public string GetCode(string url)
        {
            var uri = new Uri(url);
            string code = uri.Segments.Last();
            if (code.Contains("/"))
            {
                code = code.Replace("/", "");
            }
            return code;
        }
        public async Task<string> GetLoginUrlAsync(string connectionId) { return null; }
        private List<CollectPageInfo> GetCollectPageInfos1(string html)
        {
            List<CollectPageInfo> collectPageInfos = null;
            List<string> scripts = html.GetValues("//script[@type='application/ld+json']", TextEx.XPathOption.InnerHtml);
            for (int i = 0; i < scripts.Count; i++)
            {
                JObject data = scripts[i].ToJObject();
                if (data == null) continue;
                JToken? itemOffered = data?["about"]?["offers"]?["itemOffered"];
                if (itemOffered != null)
                {
                    collectPageInfos = new List<CollectPageInfo>();

                    List<JToken> itemOffereds = itemOffered.ToList();
                    for (int j = 0; j < itemOffereds.Count; j++)
                    {
                        string? name = itemOffereds[j].Value<string>("name");
                        string? url = itemOffereds[j].Value<string>("url");
                        if (string.IsNullOrWhiteSpace(url)) continue;
                        string code = GetCode(url);
                        if (string.IsNullOrWhiteSpace(code)) continue;
                        collectPageInfos.Add(new CollectPageInfo
                        {
                            Code = code,
                            Url = url,
                            Title = name
                        });
                    }
                    break;
                }
            }
            return collectPageInfos;
        }
        private List<CollectPageInfo> GetCollectPageInfos2(string html)
        {
            try
            {
                string[] scriptStartTexts = new string[] { "$MC", "$M_" };

                List<string> scripts = html.GetValues("//script", TextEx.XPathOption.InnerHtml);
                string? mcScript = null;
                foreach (var text in scriptStartTexts)
                {
                    mcScript = scripts.Find(x => x.Contains(text));
                    if (string.IsNullOrWhiteSpace(mcScript) == false) break;
                }
                
                string json = mcScript.SubstringBetween("concat(", ")", false);
                JObject data = json.ToJObject();
                if (data == null) return null;

                JToken findModel = null;
                List<JToken> wList = data["o"]["w"].ToList();
                foreach (JToken wItem in wList)
                {
                    try
                    {
                        List<JToken> w = wItem.ToList();
                        if (w.Count < 4) continue;

                        JToken? w2 = w[2];
                        if (w2.HasValues == false) continue;

                        JToken? type = w2["model"]?["_type"];
                        if (type == null) continue;
                        if (type.ToString() == "BOS_PLACEHOLDER")
                        {
                            findModel = w[2]?["model"];
                            break;
                        }
                    }
                    catch (Exception e) { }
                }

                if (findModel == null) { return null; }
                else
                {
                    List<JToken>? listings = findModel["POST"]?["mainSearchContext"]?["listings"]?.ToList();
                    var collectPageInfos = new List<CollectPageInfo>();
                    foreach (JToken listing in listings)
                    {
                        string? itemId = listing.Value<string>("itemId");
                        string url = $"https://www.ebay.com/itm/{itemId}";
                        CollectPageInfo collectPageInfo = new CollectPageInfo();
                        collectPageInfo.Code = itemId;
                        collectPageInfo.Url = url;
                        collectPageInfos.Add(collectPageInfo);
                    }
                    return collectPageInfos;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine(ex.Message);
                DebugConsole.WriteLine(ex.StackTrace);
                return null;
            }
        }

        private async Task<List<CollectPageInfo>> GetCollectPageInfosAsync2(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("_pgn", page.ToString());
            string pageUrl = uriBuilderAdvance.ToString();

            List<CollectPageInfo> result = null;
            string html = await ChatHubService.GetRequest(connectionId, pageUrl);
            if (string.IsNullOrWhiteSpace(html) == false)
            {
                var getPageInfos = new List<Func<string, List<CollectPageInfo>>>();
                getPageInfos.Add(GetCollectPageInfos1);
                getPageInfos.Add(GetCollectPageInfos2);

                foreach (Func<string, List<CollectPageInfo>> getPageInfo in getPageInfos)
                {
                    List<CollectPageInfo> collectPageInfos = getPageInfo.Invoke(html);
                    if (collectPageInfos != null && collectPageInfos.Count > 0)
                    {
                        result = collectPageInfos;
                        break;
                    }
                }
            }
            return result;
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("_pgn", page.ToString());
            string pageUrl = uriBuilderAdvance.ToString();

            List<CollectPageInfo> result = null;
            string html = await GetHtmlAsync(pageUrl);
            if (string.IsNullOrWhiteSpace(html) == false)
            {
                var getPageInfos = new List<Func<string, List<CollectPageInfo>>>();
                getPageInfos.Add(GetCollectPageInfos1);
                getPageInfos.Add(GetCollectPageInfos2);

                foreach (Func<string, List<CollectPageInfo>> getPageInfo in getPageInfos)
                {
                    List<CollectPageInfo> collectPageInfos = getPageInfo.Invoke(html);
                    if (collectPageInfos != null && collectPageInfos.Count > 0)
                    {
                        result = collectPageInfos;
                        break;
                    }
                }
            }
            if (result == null || result.Count <= 0)
            {
                result = await GetCollectPageInfosAsync2(connectionId, url, page); // 프록시로 페이지 수집 시 오류 페이지가 떠서 크롬 확장프로그램으로 재시도
            }
            return result;
        }
        private JObject GetData(string html)
        {
            try
            {
                List<string> scripts = html.GetValues("//script", TextEx.XPathOption.OuterHtml);
                string? script = scripts.Find(x => x.IndexOf("window._NUTS_EVENT_CACHE") >= 0);
                string json = script.SubstringBetween("$MC=(window.$MC||[]).concat(", ")", false);
                if (string.IsNullOrWhiteSpace(json))
                {
                    script = scripts.Find(x => x.IndexOf("Object.assign($i18n") >= 0);
                    json = script.SubstringBetween("concat(", ")", false);
                }
                JObject data = json.ToJObject();
                if (data == null) return null;
                JToken? productData = data["o"]?["w"]?.ToList()?.First()?.Last();
                if (productData == null) return null;
                return productData.ToObject<JObject>();
            }
            catch (Exception exception)
            {
                throw new Exception("상품 정보를 조회할 수 없습니다.", exception);
            }
        }

        public Language GetLanguage(string html)
        {
            Language language = Language.EN;
            string lang = html.GetValue("//html", "lang");
            switch (lang)
            {
                case "en":
                    language = Language.EN;
                    break;
                case "ko":
                    language = Language.KR;
                    break;
                default:
                    language = this.Language;
                    break;
            }
            return language;
        }

        public List<Name> GetTitle(JObject data, string html)
        {
            try
            {
                JToken? product = data["model"]?["modules"]?["JSONLD"]?["product"];
                if (product == null) throw new Exception();
                string? name = product.Value<string>("name");
                if (string.IsNullOrWhiteSpace(name)) throw new Exception();
                List<Name> title = new List<Name>();
                title.Add(new Name(this.Language, name));
                return title;
            }
            catch (Exception exception)
            {
                throw new Exception("상품명을 찾을 수 없습니다.", exception);
            }
        }
        public List<Name> GetBrand(JObject data)
        {
            try
            {
                JToken? textSpan = data["model"]?["modules"]?["STORE_INFORMATION_ON_OVERLAY"]?["title"]?["textSpans"]?.First();
                if (textSpan == null) throw new Exception();
                string? text = textSpan.Value<string>("text");
                if (string.IsNullOrWhiteSpace(text)) throw new Exception();
                List<Name> brand = new List<Name>();
                brand.Add(new Name(this.Language, text));
                return brand;
            }
            catch (Exception exception)
            {
                throw new Exception("브랜드명을 찾을 수 없습니다.", exception);
            }
        }
        public List<Desc> GetDescs(JObject data)
        {
            try
            {
                List<JToken>? dataItems = data["model"]?["modules"]?["ABOUT_THIS_ITEM"]?["sections"]?["features"]?["dataItems"]?.ToList();
                if (dataItems == null || dataItems.Count <= 0) throw new Exception();

                List<Desc> descs = new List<Desc>();
                for (int i = 0; i < dataItems.Count; i++)
                {
                    JToken dataItem = dataItems[i];
                    string name = dataItem.First().Path.Split(".").Last();
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    JToken? value = dataItem?.First()?["values"]?.ToList()?.First();
                    string? text = value?["textSpans"]?.FirstOrDefault()?.Value<string>("text");
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    Desc desc = new Desc(i + 1, this.Language, name, text);
                    descs.Add(desc);
                }
                return descs;
            }
            catch (Exception exception)
            {
                return null;
            }
        }
        public List<Tag> GetTags(JObject data)
        {
            List<Tag> result = new List<Tag>();
            try
            {
                List<JToken>? breadcrumbsList = data["model"]?["modules"]?["SEOBREADCRUMBS"]?["modules"]?["breadcrumbsModule"]?["breadcrumbsList"]?.ToList();
                if (breadcrumbsList != null || breadcrumbsList.Count > 0)
                {
                    var tag = new Tag();
                    tag.key = "seobreadcrumbs";
                    tag.values = new List<string>();
                    for (int i = 0; i < breadcrumbsList.Count; i++)
                    {
                        string? text = breadcrumbsList[i]?["textSpans"]?.First().Value<string>("text");
                        if (string.IsNullOrWhiteSpace(text)) continue;
                        tag.values.Add(text);
                    }
                    result.Add(tag);
                }
            }
            catch (Exception exception)
            {
                exception.Show();
            }
            return result;
        }
        public List<string> GetMainImages(JObject data)
        {
            try
            {
                JToken? product = data["model"]?["modules"]?["JSONLD"]?["product"];
                if (product == null) throw new Exception();
                JToken? images = product["image"];
                if (images == null) throw new Exception();

                List<string> result = new List<string>();
                foreach (JToken item in images)
                {
                    string? image = item.ToString();
                    if (string.IsNullOrWhiteSpace(image) == false)
                    {
                        result.Add(image);
                    }
                }
                return result;
            }
            catch (Exception exception)
            {
                throw new Exception("메인 이미지를 찾을 수 없습니다.", exception);
            }
        }
        public string GetVideoUrl() => null;
        public List<string> GetOriginalDetailPageUrls() => null;
        public List<string> GetTranslateDetailPageUrls() => null;
        public string GetDetailPageHtml(JObject data)
        {
            try
            {
                JToken? url = data["model"]?["modules"]?["ITEM_DESCRIPTION_MIN_VIEW_MODULE"]?["sections"]?.First()?["action"]?["URL"];
                if (url == null)
                {
                    url = data["model"]?["modules"]?["ITEM_DESCRIPTION_MIN_VIEW_MODULE"]?["sections"]?.First()?["dataItems"]?.First()?["action"]?["URL"];
                }
                if (url == null) throw new Exception();
                string detailPageUrl = url.ToString();

                using (HttpClient httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient())
                {
                    string html = httpClient.GetStringAsync(detailPageUrl).Result;
                    if (string.IsNullOrWhiteSpace(html)) throw new Exception();
                    string container = html.GetValue("//div[@class='container']", TextEx.XPathOption.OuterHtml);
                    if (string.IsNullOrWhiteSpace(container) == false)
                    {
                        html = container;
                    }
                    html = WebUtility.HtmlDecode(html);
                    if (string.IsNullOrWhiteSpace(html)) throw new Exception();
                    string detailHtml = HtmlReader.GetDafaultHtml(html);
                    return detailHtml;
                }
            }
            catch (Exception exception)
            {
                throw new Exception("상세 페이지를 찾을 수 없습니다", exception);
            }
        }
        public Option GetOption(JObject data)
        {
            try
            {
                JToken? msku = data["model"]?["modules"]?["MSKU"];
                if (msku == null || msku.HasValues == false)
                {
                    return null;
                }

                var optionGroups = new List<Option.OptionGroup>();
                List<JToken>? selectMenus = msku["selectMenus"]?.ToList();
                foreach (JToken selectMenu in selectMenus)
                {
                    Option.OptionGroup optionGroup = new Option.OptionGroup();
                    string? id = selectMenu.Value<string>("id");
                    if (string.IsNullOrWhiteSpace(id)) throw new Exception();
                    string? displayLabel = selectMenu.Value<string>("displayLabel");
                    if (string.IsNullOrWhiteSpace(displayLabel)) throw new Exception();

                    optionGroup.optionId = id;
                    optionGroup.groupName = new List<Name> { new Name(this.Language, displayLabel) };
                    optionGroup.groups = new List<Option.OptionGroup.Group>();

                    List<JToken>? menuItemValueIds = selectMenu["menuItemValueIds"]?.ToList();
                    if (menuItemValueIds == null || menuItemValueIds.Count <= 0) throw new Exception();

                    foreach (JToken menuItemValueId in menuItemValueIds)
                    {
                        string valueId = menuItemValueId.ToString();
                        if (string.IsNullOrWhiteSpace(valueId)) throw new Exception();

                        Option.OptionGroup.Group group = new Option.OptionGroup.Group();
                        group.groupId = valueId;
                        JToken? menuItemMap = msku["menuItemMap"]?[valueId];
                        if (menuItemMap == null) throw new Exception();
                        string? valueName = menuItemMap?.Value<string>("valueName");
                        if (string.IsNullOrWhiteSpace(valueName)) throw new Exception();
                        group.optionName = new List<Name>() { new Name(this.Language, valueName) };
                        optionGroup.groups.Add(group);
                    }
                    optionGroups.Add(optionGroup);
                }

                JToken? variationCombinations = msku["variationCombinations"];
                if (variationCombinations == null) throw new Exception();

                IEnumerable<string> groupNames = optionGroups.SelectMany(x => x.groupName).Select(x => x.text);
                string detailOptionOptionGroupName = string.Join("ㅡ", groupNames);
                if (string.IsNullOrWhiteSpace(detailOptionOptionGroupName)) throw new Exception();

                var detailOptions = new List<Option.DetailOption>();
                foreach (JToken variationCombination in variationCombinations)
                {
                    string? code = variationCombination?.First()?.Path?.Split(".")?.Last();
                    if (string.IsNullOrWhiteSpace(code)) throw new Exception();
                    string? value = variationCombinations[code]?.ToString();
                    if (string.IsNullOrWhiteSpace(value)) throw new Exception();
                    List<string> codes = code.Split("_").ToList();

                    List<string> optionNames = new List<string>();
                    foreach (var item in codes)
                    {
                        foreach (Option.OptionGroup optionGroup in optionGroups)
                        {
                            Option.OptionGroup.Group? find = optionGroup.groups.Find(x => x.groupId == item);
                            if (find == null) continue;
                            string findOptionName = find.optionName.First().text;
                            if (string.IsNullOrWhiteSpace(findOptionName)) continue;
                            optionNames.Add(findOptionName);
                        }
                    }
                    string optionName = string.Join("ㅡ", optionNames);
                    if (string.IsNullOrWhiteSpace(optionName)) throw new Exception();

                    codes.Add(value);
                    string id = string.Join("ㅡ", codes);
                    if (string.IsNullOrWhiteSpace(id)) throw new Exception();

                    JToken? variationsMap = msku["variationsMap"]?[value];
                    if (variationsMap == null) throw new Exception();
                    JToken? price = variationsMap?["binModel"]?["price"]?["value"];
                    if (price == null) throw new Exception();

                    double? salePrice = GetTrySalePrice(price);
                    if (salePrice == null) throw new Exception();

                    List<JToken>? priceDiscounts = variationsMap?["binModel"]?["priceDiscount"]?["textSpans"]?.ToList();
                    double? originalPrice = GetTryOriginalPrice(priceDiscounts);
                    if (originalPrice == null)
                    {
                        var additionalInfos = variationsMap?["binModel"]?["additionalInfo"]?.ToList();
                        if (additionalInfos != null)
                        {
                            foreach (var additionalInfo in additionalInfos)
                            {
                                List<JToken>? additionalText = additionalInfo["additionalText"]?["textSpans"]?.ToList();
                                originalPrice = GetTryOriginalPrice(additionalText);
                                if (originalPrice != null) break;
                            }
                        }
                        else
                        {
                            List<JToken>? originalBinModelPrice = variationsMap?["binModel"]?["price"]["textSpans"]?.ToList();
                            originalPrice = GetTryOriginalPrice2(originalBinModelPrice);
                        }
                    }

                    if (originalPrice == null || originalPrice <= 0)
                    {
                        originalPrice = salePrice;
                    }

                    bool? outOfStock = msku["variationsMap"]?[value]?["quantity"]?.Value<bool>("outOfStock");
                    if (outOfStock == null) throw new Exception();

                    var detailOption = new Option.DetailOption();
                    detailOption.optionIds = id;
                    detailOption.optionGroupName = new List<Name>() { new Name(this.Language, detailOptionOptionGroupName) };
                    detailOption.optionName = new List<Name> { new Name(this.Language, optionName) };
                    detailOption.price = new List<Price>() { new Price(this.Currency, (double)originalPrice, (double)salePrice) };
                    if (outOfStock == true)
                    {
                        detailOption.isSoldOut = true;
                    }
                    else
                    {
                        detailOption.isSoldOut = false;
                        detailOption.quantity = 9;
                    }
                    detailOptions.Add(detailOption);
                }

                Option option = new Option();
                option.optionGroups = optionGroups;
                option.detailOptions = detailOptions;
                return option;
            }
            catch (Exception exception)
            {
                throw new Exception("옵션 정보를 찾을 수 없습니다", exception);
            }
        }
        private double? GetTrySalePrice(JToken price)
        {
            string priceValue = null;
            string? currency = price.Value<string>("convertedFromCurrency");
            if (string.IsNullOrWhiteSpace(currency) || currency != "USD")
            {
                priceValue = "value";
                currency = price.Value<string>("currency");
                if (string.IsNullOrWhiteSpace(currency)) return null;
            }
            else
            {
                priceValue = "convertedFromValue";
            }

            string? originalPriceString = price.Value<string>(priceValue);
            if (string.IsNullOrWhiteSpace(originalPriceString)) return null;

            double originalPrice = 0;
            if (double.TryParse(originalPriceString, out originalPrice) == false) return null;
            return originalPrice;
        }
        public double? GetTryOriginalPrice(List<JToken> jTokens)
        {
            if (jTokens == null || jTokens.Count <= 0) return null;
            double? originalPrice = null;
            foreach (JToken? jToken in jTokens)
            {
                List<JToken>? styles = jToken["styles"]?.ToList();
                if (styles == null || styles.Count <= 0) continue;
                JToken? strikeThrough = styles.Find(x => x.ToString() == "STRIKETHROUGH");
                if (strikeThrough != null)
                {
                    string? text = jToken.Value<string>("text");
                    originalPrice = text.ParseToDouble();
                    break;
                }
            }
            return originalPrice;
        }
        public double? GetTryOriginalPrice2(List<JToken> jTokens)
        {
            if (jTokens == null || jTokens.Count <= 0) return null;
            double? originalPrice = null;
            foreach (JToken? jToken in jTokens)
            {
                string? text = jToken.Value<string>("text");
                if (string.IsNullOrWhiteSpace(text)) continue;
                originalPrice = text.ParseToDouble();
                break;
            }
            return originalPrice;
        }
        public List<Price> GetPrice(JObject data)
        {
            try
            {
                JToken? buy_box = data["model"]?["modules"]?["BUY_BOX"];
                if (buy_box == null) throw new Exception();
                JToken? price = buy_box?["binModel"]?["price"]?["value"];
                if (price == null)
                {
                    price = buy_box["bidPrice"]?["value"];
                }
                if (price == null) throw new Exception();
                double? salePrice = GetTrySalePrice(price);
                if (salePrice == null) throw new Exception();

                List<JToken>? textSpans = buy_box?["binModel"]?["priceDiscount"]?["textSpans"]?.ToList();
                double? originalPrice = GetTryOriginalPrice(textSpans);
                if (originalPrice == null)
                {
                    var additionalInfos = buy_box?["binModel"]?["additionalInfo"]?.ToList();
                    if (additionalInfos != null)
                    {
                        foreach (var additionalInfo in additionalInfos)
                        {
                            var additionalText = additionalInfo?["additionalText"]?["textSpans"]?.ToList();
                            originalPrice = GetTryOriginalPrice(additionalText);
                            if (originalPrice != null) { break; }
                        }
                    }
                    else
                    {
                        List<JToken>? priceTextSpans = buy_box?["binModel"]?["price"]?["textSpans"]?.ToList();
                        originalPrice = GetTryOriginalPrice2(priceTextSpans);

                    }
                }

                if (originalPrice == null || originalPrice <= 0)
                {
                    originalPrice = salePrice;
                }

                List<Price> prices = new List<Price>()
                {
                    new Price(this.Currency, (double)originalPrice, (double)salePrice, true)
                };
                return prices;
            }
            catch (Exception exception)
            {
                return null;
            }

        }
        public Product GetProduct(string url, string html)
        {
            var product = new Product();
            try
            {
                JObject data = GetData(html);
                if (data == null)
                {
                    string title = html.GetValue("//title", TextEx.XPathOption.InnerText);
                    if (title.Contains("Pardon Our Interruption"))
                    {
                        throw new Exception("상품 정보를 조회할 수 없습니다."); // 프록시를 사용하기 때문에 캡차 발생 시 예외로 처리

                        throw new CaptchaException("ebay에서 캡차가 발생했습니다. 해체 후 다시 시도해주세요.")
                        {
                            CaptchaInfo = new CaptchaInfo()
                            {
                                Message = "Checking your browser before you access eBay",
                                Url = url
                            }
                        };
                    }
                    else
                    {
                        throw new Exception("상품 정보를 조회할 수 없습니다.");
                    }
                }
                this.Language = GetLanguage(html);
                product.site = this.Site;
                product.currency = this.Currency.ToString();
                product.code = GetCode(url);
                product.url = url;
                product.title = GetTitle(data, html);
                product.brand = GetBrand(data);
                product.descs = GetDescs(data);
                product.tags = GetTags(data);
                product.mainImages = GetMainImages(data);
                product.videoUrl = GetVideoUrl();
                product.originalDetailPageUrls = GetOriginalDetailPageUrls();
                product.translateDetailPageUrls = GetTranslateDetailPageUrls();
                product.detailPageHtml = GetDetailPageHtml(data);
                product.price = GetPrice(data);
                product.option = GetOption(data);
                if (product.option != null)
                {
                    if (product.price == null)
                    {
                        product.price = ProductHelper.GetMinPriceInDetailOption(product.option.detailOptions, this.Currency);
                    }
                }
                product.regDate = DateTime.Now;

            }
            catch (CaptchaException captchaException)
            {
                throw captchaException;
            }
            catch (Exception exception)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = exception.Message;
            }
            return product;
        }
        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string html = await GetHtmlAsync(url);
            Product product = GetProduct(url, html);
            return product;
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
