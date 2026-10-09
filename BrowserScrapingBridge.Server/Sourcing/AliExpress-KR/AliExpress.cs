#nullable disable warnings
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using BrowserScrapingBridge.Server.Sourcing.AliExpress_KR.Models;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Price = BrowserScrapingBridge.Server.Sourcing.Models.Product.Price;

namespace BrowserScrapingBridge.Server.Sourcing.AliExpress_KR
{
    public class AliExpress_Temp : ICollect
    {
        public string Site => "AliExpress-KR";
        public string Name => "알리익스프레스";
        public Language Language => Language.KR;
        public Currency Currency => Currency.USD;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        private string GetMh5tk(List<ExtensionsCookie> extensionsCookie)
        {
            if (extensionsCookie == null || extensionsCookie.Count == 0)
            {
                return null;
            }
            ExtensionsCookie? findExtensionsCookie = extensionsCookie.Find(x => x.name == "_m_h5_tk");
            if (findExtensionsCookie == null) return null;
            string value = findExtensionsCookie.value;
            if (string.IsNullOrWhiteSpace(value)) return null;
            string[] split = value.Split('_');
            if (split == null || split.Length == 0) return null;
            string result = split.First();
            return result;
        }
        public string GetExtParams(string html)
        {
            List<string> scripts = html.GetValues("//script", TextEx.XPathOption.OuterHtml);
            string checkString = "window._d_c_.DCData = ";
            var findScripts = scripts.FindAll(x => x.IndexOf(checkString) >= 0);
            if (findScripts.Count <= 0)
            {
                string json = html.SubstringBetween("{", "}");
                JObject jObject = json.ToJObject();
                string? url = jObject["url"]?.ToString();

                throw new CaptchaException
                {
                    CaptchaInfo = new CaptchaInfo
                    {
                        Message = "알리익스프레스 캡차(Captcha)가 발생했습니다. 캡차를 풀고 다시 진행해주세요.",
                        Url = url,
                    }
                };
            }
            JObject dcData = null;
            for (int i = 0; i < findScripts.Count; i++)
            {
                string findScript = findScripts[i];
                int startIndex = findScript.IndexOf(checkString);
                findScript = findScript.Substring(startIndex + checkString.Length);
                int lastIndex = findScript.LastIndexOf(";");
                findScript = findScript.Substring(0, lastIndex);
                try
                {
                    dcData = JObject.Parse(findScript);
                    break;
                }
                catch
                {
                    dcData = null;
                    continue;
                }
            }
            JToken? extParams = dcData["extParams"];
            if (extParams == null) return null;
            return extParams.ToString();
        }
        public string GetProductInfoUrl(string productCode, string ext, List<ExtensionsCookie> extensionsCookie, int num = 1)
        {
            string mh5tk = GetMh5tk(extensionsCookie);
            if (string.IsNullOrWhiteSpace(mh5tk)) throw new CollectException("");
            var parameters = new NameValueCollection();
            parameters["jsv"] = "2.5.1";
            parameters["appKey"] = "12574478";
            parameters["t"] = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds().ToString();
            parameters["api"] = "mtop.aliexpress.pdp.pc.query";
            parameters["v"] = "1.0";
            parameters["timeout"] = "15000";
            parameters["type"] = "jsonp";
            parameters["dataType"] = "jsonp";
            parameters["callback"] = $"mtopjsonp{num}";
            var data = new
            {
                productId = productCode,
                _lang = "ko_KR",
                _currency = "USD",
                country = "KR",
                province = "",
                city = "",
                channel = "",
                sourceType = "",
                ext = ext
            };
            parameters["data"] = JsonConvert.SerializeObject(data); ;
            parameters["sign"] = ($"{mh5tk}&{parameters["t"]}&{parameters["appKey"]}&{parameters["data"]}").ComputeMD5().ToLower();
            string queryString = parameters.ToQueryString();

            return $"https://acs.aliexpress.com/h5/mtop.aliexpress.pdp.pc.query/1.0/?{queryString}";
        }
        private JToken GetProductData(string mtopjsonpString)
        {
            string startText = "mtopjsonp1(";
            int start = mtopjsonpString.IndexOf(startText);
            mtopjsonpString = mtopjsonpString.Substring(start + startText.Length);
            var lastIndex = mtopjsonpString.LastIndexOf(")");
            var mtopjsonp1String = mtopjsonpString.Substring(0, lastIndex);
            JObject mtopjsonp = JObject.Parse(mtopjsonp1String);
            string captchaUrl = mtopjsonp["data"].Value<string>("url");
            if (string.IsNullOrWhiteSpace(captchaUrl) == false)
            {
                string message = "알리익스프레스 캡차(Captcha)가 발생했습니다. 캡차를 풀고 다시 진행해주세요.";

                DebugConsole.WriteLine(message);
                DebugConsole.WriteLine(captchaUrl);
                var captchaException = new CaptchaException(message);
                captchaException.CaptchaInfo = new CaptchaInfo { Message = message, Url = captchaUrl };
                throw captchaException;
            }
            JToken? result = mtopjsonp["data"]?["result"];
            if (result == null)
            {
                throw new Exception("상품 정보를 불러오는데 실패했습니다.");
            }
            return result;
        }
        public string GetCode(string url)
        {
            Uri uri = new Uri(url);
            string segment = uri.Segments.Last();
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(segment);
            return fileNameWithoutExtension;
        }
        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            string url = "https://login.aliexpress.com/setCommonCookie.htm?fromApp=false&currency=USD&region=KR&bLocale=ko_KR&site=kor&province=&city=";
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Accept", "*/*");
            headers.Add("Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7,ja;q=0.6");
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            headers.Add("Origin", "https://ko.aliexpress.com");
            headers.Add("Referer", "https://ko.aliexpress.com/");
            await ChatHubService.GetRequest(connectionId, url, headers);

            string html = await ChatHubService.GetRequest(connectionId, "https://www.aliexpress.com/fn/an-account/index");
            var data = html.ToJObject();
            var fullName = data.Value("data", "data", "info_1888", "fields", "info")?.Value<string>("fullName");

            string result = "https://login.aliexpress.com/";
            if (string.IsNullOrWhiteSpace(fullName) == false)
            {
                result = null;
            }
            return result;
        }

        private async Task<List<CollectPageInfo>> GetStorePage(string connectionId, string html)
        {
            string shopPageDataApi = html.SingleSplit("window.shopPageDataApi = '", "';");
            if (string.IsNullOrWhiteSpace(shopPageDataApi)) return null;

            var uriBuilderAdvance = new UriBuilderAdvance(shopPageDataApi);
            uriBuilderAdvance.Scheme = "https";
            uriBuilderAdvance.Host = "www.aliexpress.com";
            //uriBuilderAdvance.SetQuery("pageId", "0");
            //uriBuilderAdvance.SetQuery("language", "english");
            //uriBuilderAdvance.SetQuery("currency", "null");
            //uriBuilderAdvance.SetQuery("country", "null");
            //uriBuilderAdvance.SetQuery("pagePath", "index.htm");
            //uriBuilderAdvance.SetQuery("extParams", "null");
            //uriBuilderAdvance.SetQuery("callback", "jsonpCallbackFunction");
            string shopPageUrl = uriBuilderAdvance.ToString();

            var cookies = await ChatHubService.GetCookies(connectionId, "www.aliexpress.com");
            ExtensionsCookie xman_t = cookies.Find(x => x.name == "xman_t");
            if (xman_t != null)
            {
                var cookie = new Cookie(xman_t.name, xman_t.value, xman_t.path, xman_t.domain);
                CookieContainer cookieContainer = new CookieContainer();
                cookieContainer.Add(cookie);


                HttpClientHandler httpClientHandler = new HttpClientHandler();
                httpClientHandler.CookieContainer = cookieContainer;


                using (HttpClient httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(httpClientHandler))
                {
                    httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
                    httpClient.DefaultRequestHeaders.Add("Referer", "https://ko.aliexpress.com/");
                    httpClient.DefaultRequestHeaders.Add("Accept-Language", "ko");
                    httpClient.DefaultRequestHeaders.Remove("traceparent");

                    HttpResponseMessage response = await httpClient.GetAsync(shopPageUrl);
                    string readAsString = await response.Content.ReadAsStringAsync();
                }

                var request = BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest.CreateHttp(shopPageUrl);
                request.Headers["Accept-Language"] = "ko";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";
                request.CookieContainer = cookieContainer;
                var ss = request.ReadToEnd();
            }
            string shopPageJson = await ChatHubService.GetRequest(connectionId, shopPageUrl);
            if (string.IsNullOrWhiteSpace(shopPageJson)) return null;
            string jsonpCallback = shopPageJson.SubstringBetween("jsonpCallbackFunction(", ")", false);
            JObject jsonp = jsonpCallback.ToJObject();
            if (jsonp == null) return null;


            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            return collectPageInfos;
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("page", page.ToString());
            string paegUrl = uriBuilderAdvance.ToString();

            var html = await ChatHubService.GetRequest(connectionId, paegUrl);

            List<string> productList = html.MultiSplit("productId\":\"", "\"");

            var collectPageInfos = new List<CollectPageInfo>();
            if (productList == null || productList.Count <= 0)
            {
                // 너무 오래 걸릴 것 같아서 잠시 보류 
                // https://ko.aliexpress.com/store/1101349065?spm=a2g0o.detail.0.0.26216VWz6VWz6S
                //var getCollectPages = new List<Func<string, int, string, Task<List<CollectPageInfo>>>>();
                //getCollectPages.Add(GetStorePage);
                //foreach (var getCollectPage in getCollectPages)
                //{
                //    List<CollectPageInfo> results = await getCollectPage.Invoke(connectionId, html);
                //    if (results == null || results.Count <= 0) continue;
                //    collectPageInfos.AddRange(results);
                //    break;
                //}
            }
            else
            {
                foreach (string productCode in productList)
                {
                    var collectPageInfo = new CollectPageInfo();
                    collectPageInfo.Code = productCode;
                    collectPageInfo.Url = $"https://ko.aliexpress.com/item/{productCode}.html";
                    collectPageInfos.Add(collectPageInfo);
                }
            }
            return collectPageInfos;
        }
        private string GetProductCode(JToken data)
        {
            string? code = data["PRICE"]?.Value<string>("productId");
            if (string.IsNullOrWhiteSpace(code)) throw new Exception("상품 코드를 찾을 수 없습니다.");
            return code;
        }
        private List<Name> GetTitle(JToken data)
        {
            var result = new List<Name>();
            string title = data?["PRODUCT_TITLE"]?.Value<string>("text");
            if (string.IsNullOrWhiteSpace(title)) throw new Exception("상품명을 찾을 수 없습니다.");
            {
                result.Add(new Name("KR", title));
            }
            return result;
        }
        private List<Desc> GetDescs(JToken data)
        {
            var descs = new List<Desc>();
            JToken[]? showedProps = data["PRODUCT_PROP_PC"]?["showedProps"]?.ToArray();
            if (showedProps != null)
            {
                for (int i = 0; i < showedProps.Length; i++)
                {
                    JToken showedProp = showedProps[i];
                    string attrName = showedProp.Value<string>("attrName");
                    string attrValue = showedProp.Value<string>("attrValue");
                    if (string.IsNullOrWhiteSpace(attrName) || string.IsNullOrWhiteSpace(attrValue)) continue;
                    descs.Add(new Desc(i + 1, Language.KR, attrName, attrValue));
                }
            }
            return descs;
        }
        private double GetDeliveryPrice(List<JToken> deliveryLayoutInfos)
        {
            if (deliveryLayoutInfos == null || deliveryLayoutInfos.Count <= 0) return 0;

            double deliveryPrice = 0;
            try
            {
                string[] deliveryProviderNames = new string[]
                {
                    "AliExpress Standard Shipping",
                    "Aliexpress Selection Standard,"
                };

                JToken deliveryLayoutInfo = null;
                for (int i = 0; i < deliveryProviderNames.Length; i++)
                {
                    JToken? find = deliveryLayoutInfos.Find(x => x["bizData"]?["deliveryProviderName"]?.ToString() == deliveryProviderNames[i]);
                    if (find != null)
                    {
                        deliveryLayoutInfo = find;
                        break;
                    }
                }

                if (deliveryLayoutInfo != null)
                {
                    deliveryPrice = deliveryLayoutInfo["bizData"].Value<double>("displayAmount");
                }
                else
                {
                    float minAmount = deliveryLayoutInfos.Min(x => x["bizData"].Value<float>("displayAmount"));
                    JToken? target = deliveryLayoutInfos.Find(x => x["bizData"].Value<float>("displayAmount") == minAmount);
                    string? currency = target["bizData"].Value<string>("currency");
                    if (currency == "KRW")
                    {
                        string? formattedAmount = target["bizData"].Value<string>("formattedAmount");
                        if (string.IsNullOrWhiteSpace(formattedAmount) == false)
                        {
                            formattedAmount = Regex.Replace(formattedAmount, @"[^0-9.]", "");
                            deliveryPrice = Convert.ToDouble(formattedAmount);
                        }
                    }
                    else
                    {
                        string? displayAmount = target["bizData"].Value<string>("displayAmount");
                        deliveryPrice = Convert.ToDouble(displayAmount);
                    }
                }
            }
            catch (Exception exception)
            {
                exception.Show();
            }
            return deliveryPrice;
        }
        private List<Price> GetPrice(JToken data, Currency currency)
        {
            var price = new List<Price>();
            try
            {
                double originalPrice = 0;

                JToken? originalPriceToken = data["PRICE"]?["targetSkuPriceInfo"]?["originalPrice"];
                string? salePriceString = data["PRICE"]?["targetSkuPriceInfo"]?.Value<string>("salePriceString");
                if (originalPriceToken != null)
                {
                    originalPrice = originalPriceToken.Value<double>("value");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(salePriceString) == false)
                    {
                        originalPrice = Convert.ToDouble(Regex.Replace(salePriceString, @"[^0-9.]", ""));
                    }
                }

                double salePrice = originalPrice;
                if (string.IsNullOrWhiteSpace(salePriceString) == false && salePriceString.Contains("US "))
                {
                    salePrice = Convert.ToDouble(Regex.Replace(salePriceString, @"[^0-9.]", ""));
                }
                price.Add(new Price(currency, originalPrice.Ceiling(2, 1), salePrice.Ceiling(2, 1), true));
                price.Add(new Price(Currency.KRW, 0, 0));

                List<JToken> deliveryLayoutInfos = data["SHIPPING"]?["deliveryLayoutInfo"]?.ToList();
                double deliveryPrice = GetDeliveryPrice(deliveryLayoutInfos);
                if (deliveryPrice > 0)
                {
                    salePrice += deliveryPrice;
                    originalPrice += deliveryPrice;
                }
            }
            catch (Exception exception)
            {
                throw new Exception("상품 가격을 확인할 수 없습니다", exception);
            }
            return price;
        }
        private string GetVideoUrl(JToken data)
        {
            string videoUrl = null;
            try
            {
                string? webUrl = data["HEADER_IMAGE_PC"]?["productVideo"]?["videoPlayInfo"].Value<string>("webUrl");
                videoUrl = webUrl;
            }
            catch { return null; }
            return videoUrl;
        }
        private List<string> GetMainImages(JToken data)
        {
            List<JToken>? mainImages = data["HEADER_IMAGE_PC"]?["mainImages"]?.ToList();
            if (mainImages == null || mainImages.Count <= 0) throw new Exception("메인 이미지를 찾을 수 없습니다.");
            IEnumerable<string> imageUrls = mainImages.Select(x => x["imageUrl"].ToString());
            if (mainImages == null || mainImages.Count <= 0) throw new Exception("메인 이미지를 찾을 수 없습니다.");
            return imageUrls.ToList();
        }
        private Option GetOption(JToken data, Currency currency)
        {
            try
            {
                var option = new Option();
                option.optionGroups = new List<Option.OptionGroup>();

                var tmpOptionIdList = new List<string>();
                if (data["SKU"] != null && data["SKU"]["skuPaths"].ToList().Count > 0 && data["SKU"]["skuProperties"].ToList().Count > 0)
                {
                    foreach (JToken? optionGroupData in data["SKU"]["skuProperties"].ToList())
                    {
                        var optionGroup = new Option.OptionGroup();
                        optionGroup.optionId = optionGroupData["skuPropertyId"].ToString();
                        optionGroup.groupName = new List<Name>();
                        string skuPropertyName = optionGroupData["skuPropertyName"].ToString();
                        if (skuPropertyName == "배송지") continue;
                        optionGroup.groupName.Add(new Name("KR", skuPropertyName));

                        optionGroup.groups = new List<Option.OptionGroup.Group>();
                        foreach (JToken? skuProperty in optionGroupData["skuPropertyValues"].ToList())
                        {
                            var skuGroup = new Option.OptionGroup.Group();
                            skuGroup.groupId = skuProperty["propertyValueIdLong"].ToString();
                            skuGroup.optionName = new List<Name>();
                            skuGroup.optionName.Add(new Name("KR", skuProperty["propertyValueDisplayName"].ToString()));
                            skuGroup.optionImage = skuProperty["skuPropertyImagePath"].ToString();
                            optionGroup.groups.Add(skuGroup);
                        }
                        tmpOptionIdList.Add(optionGroup.groups.Select(x => x.groupId).ToList().Aggregate((text, next) => text + "," + next));
                        option.optionGroups.Add(optionGroup);
                    }

                    option.detailOptions = new List<Option.DetailOption>();

                    if (data["PRICE"] != null && data["PRICE"]["skuPriceInfoMap"] != null)
                    {
                        var skuPriceData = JsonConvert.SerializeObject(data["PRICE"]["skuPriceInfoMap"]);
                        JObject jobject = JObject.Parse(skuPriceData);

                        foreach (JToken skuPath in data["SKU"]["skuPaths"])
                        {
                            string tmpSkuData = jobject[skuPath["skuIdStr"].ToString()].ToString();
                            if (string.IsNullOrWhiteSpace(tmpSkuData)) continue;

                            var pathSplit = skuPath["path"].ToString().Split(';').ToList();
                            var detailOption = new Option.DetailOption();

                            detailOption.optionGroupName = new List<Name>();
                            string optionGroupName = string.Join("ㅡ", option.optionGroups.Select(x => x.groupName.Find(y => y.language == "KR").text));
                            detailOption.optionGroupName.Add(new Name("KR", optionGroupName));

                            List<string> groupIds = pathSplit.Select(x => x.Split(':')[1]).ToList();
                            var groupNames = new List<string>();
                            var detailOptionIds = new List<string>();
                            List<Option.OptionGroup.Group> groups = option.optionGroups.SelectMany(x => x.groups).ToList();
                            for (int i = 0; i < groupIds.Count; i++)
                            {
                                Option.OptionGroup.Group find = groups.Find(x => x.groupId == groupIds[i]);
                                if (find != null)
                                {
                                    string text = find.optionName.First().text;
                                    detailOptionIds.Add(find.groupId);
                                    groupNames.Add(text);
                                }
                            }
                            //if (groupNames.Count != groupIds.Count) continue;
                            detailOption.optionIds = string.Join("ㅡ", detailOptionIds) + "ㅡ" + skuPath["skuIdStr"].ToString();
                            string optionName = string.Join("ㅡ", groupNames);
                            if (string.IsNullOrWhiteSpace(optionName)) continue;

                            detailOption.optionName = new List<Name>();
                            detailOption.optionName.Add(new Name("KR", optionName));

                            var skuData = JsonConvert.DeserializeObject<H5NewDetailItemModel.SkuPriceDetail>(tmpSkuData);

                            detailOption.price = new List<Price>();

                            double optionSalePrice = Convert.ToDouble(Regex.Replace(skuData.salePriceString, @"[^0-9.]", ""));
                            double optionOrigianlPrice = skuData.originalPrice != null ? Convert.ToDouble(skuData.originalPrice.value) : optionSalePrice;

                            detailOption.price.Add(new Price(currency, optionOrigianlPrice.Ceiling(2, 1), optionSalePrice.Ceiling(2, 1)));
                            detailOption.price.Add(new Price("KRW", 0, 0));
                            detailOption.quantity = skuPath.Value<int>("skuStock");
                            detailOption.isSoldOut = detailOption.quantity == 0 ? true : false;

                            option.detailOptions.Add(detailOption);
                        }
                    }
                    for (int i = tmpOptionIdList.Count - 1; i >= 0; i--)
                    {
                        var optionIdLIst = tmpOptionIdList[i].Split(',').ToList();
                        var sortedList = option.detailOptions.OrderBy(item =>
                        {
                            var itemId = item.optionIds.Split('ㅡ')[i];
                            int indexInCustomOrder = optionIdLIst.IndexOf(itemId);
                            return indexInCustomOrder != -1 ? indexInCustomOrder : int.MaxValue;
                        });
                        option.detailOptions = sortedList.ToList();
                    }
                }
                else
                {
                    return null;
                }
                return option;
            }
            catch (Exception exception)
            {
                throw new Exception("옵션 수집에서 문제가 발생했습니다.", exception);
            }
        }
        private string GetDetailPageHtml(string html, JToken data)
        {
            try
            {
                string detailPageUrl = html.SingleSplit("detailDesc\":\"", "\",");
                if (string.IsNullOrWhiteSpace(detailPageUrl) == false)
                {
                    return detailPageUrl;
                }
                string? pcDescUrl = data["DESC"]?.Value<string>("pcDescUrl");
                if (string.IsNullOrWhiteSpace(pcDescUrl) == false)
                {
                    return pcDescUrl;
                }
                return null;
            }
            catch (Exception exception)
            {
                throw new Exception("상세 페이지를 확인할 수 없습니다.", exception);
            }
        }
        //public Product GetProduct(string code, string html)
        //{
        //    Product product = new Product();
        //    try
        //    {
        //        JToken data = GetProductData(html);
        //        if (data == null) throw new Exception("상품 정보를 확인할 수 없습니다.");
        //        Currency currency = Currency.USD;

        //        product.site = this.Site;
        //        product.code = GetProductCode(data);
        //        product.currency = currency.ToString();
        //        product.url = $"https://ko.aliexpress.com/item/{product.code}.html";
        //        product.workNumber = 0;
        //        product.title = GetTitle(data);
        //        product.descs = GetDescs(data);
        //        product.price = GetPrice(data, currency);
        //        product.videoUrl = GetVideoUrl(data);
        //        product.mainImages = GetMainImages(data);
        //        product.option = GetOption(data, currency);
        //        product.detailPageHtml = GetDetailPageHtml(html, data);
        //        if (string.IsNullOrWhiteSpace(product.detailPageHtml)) throw new Exception("상세 페이지 주소를 확인할 수 없습니다.");
        //    }
        //    catch (CaptchaException captchaException)
        //    {
        //        throw captchaException;
        //    }
        //    catch (Exception exception)
        //    {
        //        product.collectStatus = ProductCollectStatus.Failed;
        //        product.memo = exception.Message;
        //    }
        //    return product;
        //}
        public void SetDetailPage(Product product, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                product.memo = "상세 페이지를 확인할 수 없습니다.";
                return;
            }
            string dafaultHtml = HtmlReader.GetDafaultHtml(html);
            if (string.IsNullOrWhiteSpace(dafaultHtml))
            {
                product.memo = "상세 페이지를 확인할 수 없습니다.";
            }
            else
            {
                product.detailPageHtml = dafaultHtml;

                List<string> imgs = dafaultHtml.GetValues("//img", TextEx.XPathOption.OuterHtml);
                if (imgs != null || imgs.Count > 0)
                {
                    product.originalDetailPageUrls = new List<string>();
                    foreach (string img in imgs)
                    {
                        string src = img.GetValue("//img", "src");
                        if (src.IndexOf("http") < 0)
                        {
                            src = "https://" + src;
                        }
                        product.originalDetailPageUrls.Add(src);
                    }
                }
            }
        }
        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string code = GetCode(url);
            Product product = new Product();

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            headers.Add("Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7,ja;q=0.6");
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36");

            string getAliItemHtml = await ChatHubService.GetRequest(connectionId, url, headers);
            if (string.IsNullOrWhiteSpace(getAliItemHtml))
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = "상품 접근 실패";
                return product;
            }

            string extParams = GetExtParams(getAliItemHtml);
            if (string.IsNullOrWhiteSpace(extParams) == false && extParams.Contains("한국에서 발송"))
            {
                product.collectStatus = ProductCollectStatus.Exception;
                product.memo = "국내 배송 상품 제외";
                return product;
            }

            List<ExtensionsCookie> extensionsCookies = await ChatHubService.GetCookies(connectionId, ".aliexpress.com");
            string productInfoUrl = GetProductInfoUrl(code, extParams, extensionsCookies);
            string productHtml = await ChatHubService.GetRequest(connectionId, productInfoUrl, headers);

            try
            {
                JToken data = GetProductData(productHtml);
                if (data == null) throw new Exception("상품 정보를 확인할 수 없습니다.");
                Currency currency = Currency.USD;

                product.site = this.Site;
                product.code = GetProductCode(data);
                product.currency = currency.ToString();
                product.url = $"https://ko.aliexpress.com/item/{product.code}.html";
                product.workNumber = 0;
                product.title = GetTitle(data);
                product.descs = GetDescs(data);
                product.price = GetPrice(data, currency);
                product.videoUrl = GetVideoUrl(data);
                product.mainImages = GetMainImages(data);
                product.option = GetOption(data, currency);
                product.detailPageHtml = GetDetailPageHtml(productHtml, data);
                if (string.IsNullOrWhiteSpace(product.detailPageHtml)) throw new Exception("상세 페이지 주소를 확인할 수 없습니다.");
                string aliExpressDetailPageHtml = await ChatHubService.GetRequest(connectionId, product.detailPageHtml, headers);
                SetDetailPage(product, aliExpressDetailPageHtml);
                await ApplyCollectOptionSetting(connectionId, new CollectRawData { Data = data }, product, collectOptionSetting);
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



        public async Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            CollectRawData rawData = new CollectRawData();
            rawData.Url = url;

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7" },
                { "Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7,ja;q=0.6" },
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36" }
            };

            string itemHtml = await ChatHubService.GetRequest(connectionId, url, headers);
            if (string.IsNullOrWhiteSpace(itemHtml) == false)
            {
                string extParams = GetExtParams(itemHtml);
                if (string.IsNullOrWhiteSpace(extParams) == false && extParams.Contains("한국에서 발송"))
                {
                    throw new Exception("국내 배송 상품 제외");
                    //product.collectStatus = ProductCollectStatus.Exception;
                    //product.memo = "국내 배송 상품 제외";
                    //return product;
                }

                List<ExtensionsCookie> extensionsCookies = await ChatHubService.GetCookies(connectionId, ".aliexpress.com");
                string code = GetCode(url);
                string productInfoUrl = GetProductInfoUrl(code, extParams, extensionsCookies);
                string productHtml = await ChatHubService.GetRequest(connectionId, productInfoUrl, headers);
                rawData.Html = productHtml;

                JToken data = GetProductData(productHtml);
                rawData.Data = data;
                string detailPageHtmlUrl = GetDetailPageHtml(productHtml, data);
                string detailPageHtml = await ChatHubService.GetRequest(connectionId, detailPageHtmlUrl, headers);
                rawData.DetailHtml = detailPageHtml;
            }
            return rawData;
        }

        public Product GetProduct(CollectRawData rawData)
        {
            Product product = new Product();
            try
            {
                if (rawData.Data == null) throw new Exception("상품 정보를 확인할 수 없습니다.");
                Currency currency = Currency.USD;

                product.site = this.Site;
                product.code = GetProductCode(rawData.Data);
                product.currency = currency.ToString();
                product.url = $"https://ko.aliexpress.com/item/{product.code}.html";
                product.title = GetTitle(rawData.Data);
                product.descs = GetDescs(rawData.Data);
                product.price = GetPrice(rawData.Data, currency);
                product.videoUrl = GetVideoUrl(rawData.Data);
                product.mainImages = GetMainImages(rawData.Data);
                product.option = GetOption(rawData.Data, currency);
                product.detailPageHtml = rawData.DetailHtml;
                if (string.IsNullOrWhiteSpace(product.detailPageHtml)) throw new Exception("상세 페이지 주소를 확인할 수 없습니다.");
                SetDetailPage(product, rawData.DetailHtml);
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

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            #region 옵션 중 최대 가격을 기준으로 -50%까지 수집, 나머지는 옵션에서 제거
            if (setting?.AliExpressCollectOption == null) return;
            if (product.option == null) return;

            Option.DetailOption maxOption = null;
            foreach (Option.DetailOption detailOption in product.option.detailOptions)
            {
                if (detailOption.isSoldOut == true) continue;

                double current = detailOption.price.First().salePrice + detailOption.price.First().originalPrice;
                if (current == 0) continue;

                if (maxOption == null)
                {
                    maxOption = detailOption;
                }
                else
                {
                    double before = maxOption.price.First().salePrice + maxOption.price.First().originalPrice;
                    if (current > before)
                    {
                        maxOption = detailOption;
                    }
                }
            }

            double baseSalePrice = maxOption.price.First().salePrice;
            double _base = Math.Ceiling((baseSalePrice / 2d) * 100) / 100;

            for (int i = 0; i < product.option.detailOptions.Count; i++)
            {
                Option.DetailOption detailOption = product.option.detailOptions[i];
                double current = detailOption.price.First().salePrice;
                if (_base > current)
                {
                    product.option.detailOptions[i] = null;
                }
            }
            product.option.detailOptions = product.option.detailOptions.FindAll(x => x != null);
            #endregion
        }

        //private string GetProductH5Html(string productCode, string ext, int num)
        //{
        //    var parameters = new NameValueCollection();
        //    parameters["jsv"] = "2.5.1";
        //    parameters["appKey"] = "12574478";
        //    parameters["t"] = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds().ToString();
        //    parameters["api"] = "mtop.aliexpress.itemdetail.pc.asyncPCDetail";
        //    parameters["v"] = "1.0";
        //    parameters["timeout"] = "15000";
        //    parameters["type"] = "jsonp";
        //    parameters["dataType"] = "jsonp";
        //    parameters["callback"] = $"mtopjsonp{num}";
        //    var data = new
        //    {
        //        productId = productCode,
        //        lang = "ko_KR",
        //        currency = "USD",
        //        country = "KR",
        //        province = "",
        //        city = "",
        //        channel = "",
        //        sourceType = "",
        //        ext = ext
        //    };
        //    parameters["data"] = JsonConvert.SerializeObject(data); ;
        //    parameters["sign"] = ($"{this.mh5tk}&{parameters["t"]}&{parameters["appKey"]}&{parameters["data"]}").ComputeMD5().ToLower();
        //    string queryString = parameters.ToQueryString();
        //    return $"https://acs.aliexpress.com/h5/mtop.aliexpress.itemdetail.pc.asyncpcdetail/1.0/?{queryString}";
        //}
    }
}
