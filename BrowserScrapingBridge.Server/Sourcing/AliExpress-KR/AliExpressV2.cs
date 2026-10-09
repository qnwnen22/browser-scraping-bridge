#nullable disable warnings
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.AliExpress_KR.Models;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using BrowserScrapingBridge.Server.Models;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.AliExpress_KR
{
    public class AliExpress : CollectBase
    {
        private const string ProductApiUrl = "https://acs.aliexpress.com/h5/mtop.aliexpress.pdp.pc.query/1.0/";
        public override string Site => "AliExpress-KR";
        public override string Name => "알리익스프레스";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.USD;
        public override bool SupportsCaching { get; set; } = false; // 알리익스프레스는 로그인한 계정 정보에 따라 가격 및 배송정책이 다르게 수집됨

        // CollectBase의 상품/원문 수집도 브라우저 DOM 조회를 사용합니다.
        public override Task<string> GetHtmlAsync(string connectionId, string url)
            => FetchContentAsync(connectionId, url);

        private async Task<string> FetchContentAsync(string connectionId, string url,
            Dictionary<string, string> headers = null, BrowserFetchMode mode = BrowserFetchMode.Dom)
        {
            var context = SourcingExecution.Current;
            var coordinator = context.Coordinator;
            var cancellationToken = context.CancellationToken;
            context.ThrowIfFailed();
            try
            {
                ValidateConnection(context, connectionId);
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                    throw new ArgumentException("수집 요청은 HTTP/HTTPS URL이어야 합니다.");
                var request = context.CreateFetchRequest(uri, headers);
                var result = await coordinator.FetchAsync(uri, request, cancellationToken,
                    connectionId: context.ConnectionId, mode: mode);
                ThrowIfBrowserFailed(result);
                if (mode == BrowserFetchMode.Dom) return result.Html;
                var encoding = BrowserChatHubService.ResponseEncoding(result.ResponseHeaders);
                return encoding.GetString(Convert.FromBase64String(result.BodyBase64 ?? ""));
            }
            catch (Exception exception)
            {
                context.RecordFailure(exception);
                throw;
            }
        }

        private async Task<List<ExtensionsCookie>> ReadBrowserCookiesAsync(string connectionId, string url)
        {
            var context = SourcingExecution.Current;
            context.ThrowIfFailed();
            try
            {
                ValidateConnection(context, connectionId);
                var result = await context.Coordinator.ReadCookiesAsync(url, context.CancellationToken, context.ConnectionId);
                ThrowIfBrowserFailed(result);
                return JsonConvert.DeserializeObject<List<ExtensionsCookie>>(result.CookiesJson ?? "[]") ?? new List<ExtensionsCookie>();
            }
            catch (Exception exception)
            {
                context.RecordFailure(exception);
                throw;
            }
        }

        private static void ValidateConnection(SourcingExecution context, string connectionId)
        {
            if (!string.Equals(connectionId, context.ConnectionId, StringComparison.Ordinal))
                throw new ArgumentException("알리 수집의 브라우저 연결이 실행 범위와 일치하지 않습니다.");
        }

        private static void ThrowIfBrowserFailed(BrowserFetchResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.Error)) throw new BrowserOperationException(result.Error);
        }

        public override async Task<string> GetLoginUrlAsync(string connectionId)
        {
            string url = "https://login.aliexpress.com/setCommonCookie.htm?fromApp=false&currency=USD&region=KR&bLocale=ko_KR&site=kor&province=&city=";
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Accept", "*/*");
            headers.Add("Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7,ja;q=0.6");
            headers.Add("Origin", "https://ko.aliexpress.com");
            headers.Add("Referer", "https://ko.aliexpress.com/");
            await FetchContentAsync(connectionId, url, headers, BrowserFetchMode.Response);

            string accountJson = await FetchContentAsync(connectionId, "https://www.aliexpress.com/fn/an-account/index",
                mode: BrowserFetchMode.Response);
            var data = accountJson.ToJObject();
            if (data is null) throw new CollectException("알리익스프레스 계정 API의 JSON 응답을 해석할 수 없습니다.");
            string fullName = data.Value("data", "data", "info_1888", "fields", "info")?.Value<string>("fullName");

            string result = "https://login.aliexpress.com/";
            if (string.IsNullOrWhiteSpace(fullName) == false)
            {
                result = null;
            }
            return result;
        }

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("page", page.ToString());
            string paegUrl = uriBuilderAdvance.ToString();

            var html = await FetchContentAsync(connectionId, paegUrl);

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

        public override void ThrowIfCaptcha(string url, string html)
        {
            return;
        }

        public override string GetCode(string url)
        {
            Uri uri = new Uri(url);
            string segment = uri.Segments.Last();
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(segment);
            return fileNameWithoutExtension;
        }

        public override string CreateProductUrl(string code)
        {
            return $"https://ko.aliexpress.com/item/{code}.html";
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            string code = GetCode(url);

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            headers.Add("Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7,ja;q=0.6");

            string extParams = GetExtParams(html);
            if (string.IsNullOrWhiteSpace(extParams)) return null;

            // HTML을 가져온 동일 연결에서 실제 H5 요청 URL에 적용되는 쿠키를 조회합니다.
            List<ExtensionsCookie> extensionsCookies = await ReadBrowserCookiesAsync(connectionId, ProductApiUrl);

            string pdpApiUrl = CreateProductPdpApiUrl(code, extParams, extensionsCookies);

            string productJsonp = await FetchContentAsync(connectionId, pdpApiUrl, headers, BrowserFetchMode.Response);
            if (string.IsNullOrWhiteSpace(productJsonp)) return null;

            JToken data = GetProductData(productJsonp);

            string? pcDescUrl = ReadString((data["DESC"] as JObject)?["pcDescUrl"]);
            if (string.IsNullOrWhiteSpace(pcDescUrl) == false)
            {
                // 상품 본문에 저장할 상세 HTML은 응답 원문을 유지합니다.
                string detailHtml = await FetchContentAsync(connectionId, pcDescUrl, mode: BrowserFetchMode.Response);
                if (string.IsNullOrWhiteSpace(detailHtml) == false)
                {
                    data["DetailHtml"] = detailHtml;
                }
            }

            return data;
        }
        public string GetExtParams(string html)
        {
            List<string> scripts = html.GetValues("//script", TextEx.XPathOption.OuterHtml);
            foreach (string script in scripts)
            {
                var match = Regex.Match(script, @"window\._d_c_\.DCData\s*=\s*");
                if (!match.Success) continue;
                try
                {
                    using var textReader = new StringReader(script.Substring(match.Index + match.Length));
                    using var jsonReader = new JsonTextReader(textReader);
                    var dcData = JObject.Load(jsonReader);
                    string extParams = dcData["extParams"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(extParams)) return extParams;
                }
                catch (JsonException) { }
            }
            // 차단 응답의 URL이 있는 경우 사용자에게 브라우저에서 해결할 위치를 전달합니다.
            try
            {
                int jsonStart = html.IndexOf('{');
                if (jsonStart >= 0)
                {
                    using var textReader = new StringReader(html.Substring(jsonStart));
                    using var jsonReader = new JsonTextReader(textReader);
                    var response = JObject.Load(jsonReader);
                    string captchaUrl = ReadString(response["url"]) ?? ReadString((response["data"] as JObject)?["url"]);
                    if (!string.IsNullOrWhiteSpace(captchaUrl))
                    {
                        const string message = "알리익스프레스 캡차(Captcha)가 발생했습니다. 선택한 브라우저에서 캡차를 해결한 후 다시 요청해주세요.";
                        throw new CaptchaException(message)
                        {
                            CaptchaInfo = new CaptchaInfo { Message = message, Url = captchaUrl }
                        };
                    }
                }
            }
            catch (JsonException) { }
            throw new CollectException("알리익스프레스 상품 HTML에서 DCData/extParams를 찾지 못했습니다. 선택한 브라우저의 로그인·차단 화면과 상품 페이지 구조를 확인해주세요.");
        }
        public string CreateProductPdpApiUrl(string productCode, string ext, List<ExtensionsCookie> extensionsCookie, int num = 1)
        {
            string mh5tk = GetMh5tk(extensionsCookie);
            if (string.IsNullOrWhiteSpace(mh5tk))
                throw new CollectException("선택한 브라우저에 알리익스프레스 H5 서명 쿠키(_m_h5_tk)가 없습니다. 해당 브라우저에서 상품 페이지를 열고 로그인·차단 상태를 확인한 후 다시 요청해주세요.");
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
            parameters["data"] = JsonConvert.SerializeObject(data);
            parameters["sign"] = ($"{mh5tk}&{parameters["t"]}&{parameters["appKey"]}&{parameters["data"]}").ComputeMD5().ToLower();
            string queryString = parameters.ToQueryString();

            return $"{ProductApiUrl}?{queryString}";
        }
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
        private JToken GetProductData(string mtopjsonpString)
        {
            if (string.IsNullOrWhiteSpace(mtopjsonpString))
                throw new CollectException("알리익스프레스 상품 API가 빈 응답을 반환했습니다.");
            string json = mtopjsonpString.Trim();
            if (!json.StartsWith("{", StringComparison.Ordinal))
            {
                const string callback = "mtopjsonp1(";
                int start = json.IndexOf(callback, StringComparison.Ordinal);
                int end = json.LastIndexOf(')');
                if (start < 0 || end <= start + callback.Length)
                    throw new CollectException("알리익스프레스 상품 API의 JSONP 응답 형식이 올바르지 않습니다.");
                json = json.Substring(start + callback.Length, end - start - callback.Length);
            }
            JObject mtopjsonp;
            try { mtopjsonp = JObject.Parse(json); }
            catch (JsonException)
            { throw new CollectException("알리익스프레스 상품 API의 JSON 응답을 해석할 수 없습니다."); }
            var responseData = mtopjsonp["data"] as JObject;
            string captchaUrl = ReadString(responseData?["url"]);
            if (string.IsNullOrWhiteSpace(captchaUrl) == false)
            {
                string message = "알리익스프레스 캡차(Captcha)가 발생했습니다. 캡차를 풀고 다시 진행해주세요.";
                var captchaException = new CaptchaException(message);
                captchaException.CaptchaInfo = new CaptchaInfo { Message = message, Url = captchaUrl };
                throw captchaException;
            }
            JObject result = responseData?["result"] as JObject;
            if (result == null)
            {
                string reason = mtopjsonp["ret"]?.ToString(Formatting.None);
                throw new CollectException($"알리익스프레스 상품 API에서 상품 정보를 반환하지 않았습니다. {reason ?? "로그인·서명 쿠키와 차단 상태를 확인해주세요."}");
            }
            return result;
        }

        private static string ReadString(JToken token)
            => token?.Type == JTokenType.String ? token.Value<string>() : null;

        public override List<Name> GetTitle(string html, JToken data)
        {
            var result = new List<Name>();
            string title = data?["PRODUCT_TITLE"]?.Value<string>("text");
            if (string.IsNullOrWhiteSpace(title)) throw new Exception("상품명을 찾을 수 없습니다.");
            {
                result.Add(new Name(this.Language, title));
            }
            return result;
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            return null;
        }

        public override string GetModel(string html, JToken data)
        {
            return null;
        }

        public override List<Desc> GetDescs(string html, JToken data)
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

        public override List<Tag> GetTags(string html, JToken data)
        {
            return null;
        }

        public override List<Price> GetPrice(string html, JToken data)
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
                price.Add(new Price(this.Currency, originalPrice.Ceiling(2, 1), salePrice.Ceiling(2, 1), true));

            }
            catch (Exception exception)
            {
                throw new Exception("상품 가격을 확인할 수 없습니다", exception);
            }
            return price;
        }

        public override List<Delivery> GetDeliveries(string html, JToken data)
        {
            List<JToken> deliveryLayoutInfos = data["SHIPPING"]?["deliveryLayoutInfo"]?.ToList();
            if (deliveryLayoutInfos == null || deliveryLayoutInfos.Count == 0) return null;

            List<Delivery> deliveries = new List<Delivery>();

            List<Price> price = GetPrice(html, data);

            foreach (JToken deliveryLayoutInfoItem in deliveryLayoutInfos)
            {
                JToken bizData = deliveryLayoutInfoItem.Value("bizData");
                if (bizData == null) return null;

                var logisticsComposeThreshold = bizData.Value<string>("logisticsComposeThreshold");
                double? logisticsComposeThresholdValue = logisticsComposeThreshold.ParseToDouble();

                var delivery = new Delivery();
                delivery.name = bizData.Value<string>("deliveryProviderName");
                // n 이상 주문 시 무료배송
                if (price.First().salePrice > logisticsComposeThresholdValue)
                {
                    delivery.price = ProductHelper.CreatePrice(this.Currency, 0, 0);
                }
                else
                {
                    string displayCurrency = bizData.Value<string>("displayCurrency");
                    double displayAmount = bizData.Value<double>("displayAmount");
                    if (displayCurrency == "KRW")
                    {
                        string formattedPreAmount = bizData.Value<string>("formattedPreAmount");
                        displayAmount = (double)formattedPreAmount.ParseToDouble();
                    }
                    delivery.price = ProductHelper.CreatePrice(this.Currency, displayAmount, displayAmount);
                }
                deliveries.Add(delivery);
            }
            return deliveries;
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
                    "Aliexpress Selection Standard",
                    "AliExpress Selection Premium shipping"
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

        public override List<string> GetMainImages(string html, JToken data)
        {
            List<JToken>? mainImages = data["HEADER_IMAGE_PC"]?["mainImages"]?.ToList();
            if (mainImages == null || mainImages.Count <= 0) throw new Exception("메인 이미지를 찾을 수 없습니다.");
            IEnumerable<string> imageUrls = mainImages.Select(x => x["imageUrl"].ToString());
            if (mainImages == null || mainImages.Count <= 0) throw new Exception("메인 이미지를 찾을 수 없습니다.");
            return imageUrls.ToList();
        }

        public override string GetVideoUrl(string html, JToken data)
        {
            return null;
        }

        public override string GetDetailPageHtml(string html, JToken data)
        {
            try
            {
                string detailPageUrl = html.SingleSplit("detailDesc\":\"", "\",");
                if (string.IsNullOrWhiteSpace(detailPageUrl) == false)
                {
                    return detailPageUrl;
                }

                var detailHtml = data.Value<string>("DetailHtml");
                if (string.IsNullOrWhiteSpace(detailHtml) == false)
                {
                    return detailHtml;
                }
                return null;
            }
            catch (Exception exception)
            {
                throw new Exception("상세 페이지를 확인할 수 없습니다.", exception);
            }
        }

        public override Option GetOption(string html, JToken data)
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
                        string skuPriceData = JsonConvert.SerializeObject(data["PRICE"]["skuPriceInfoMap"]);
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

                            detailOption.price.Add(new Price(this.Currency, optionOrigianlPrice.Ceiling(2, 1), optionSalePrice.Ceiling(2, 1)));
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
                            string itemId = item.optionIds.Split('ㅡ')[i];
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

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
            return;
        }

        public override int? GetTotalQuantity(string html, JToken data)
        {
            return null;
        }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting collectOptionSetting)
        {
            var setting = collectOptionSetting?.AliExpressCollectOption;
            if (setting == null) return;

            #region 옵션 중 최대 가격을 기준으로 -50%까지 수집, 나머지는 옵션에서 제거
            if (setting.isMaxPriceOptionCollect == true)
            {
                if (product.option != null)
                {
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
                }
            }

            //if (setting.useDeliveryPrice == true)
            //{
            //    if (product.deliveries != null && product.deliveries.Count > 0)
            //    {
            //        Price deliveryPrice = product.deliveries.First().price.First();

            //        var salePrice = deliveryPrice.salePrice;
            //        var originalPrice = deliveryPrice.originalPrice;

            //        if (product.price != null)
            //        {
            //            product.price.First().salePrice += salePrice;
            //            product.price.First().originalPrice += originalPrice;
            //        }

            //        if (product.option != null)
            //        {
            //            foreach (var detailOption in product.option.detailOptions)
            //            {
            //                detailOption.price.First().salePrice += salePrice;
            //                detailOption.price.First().originalPrice += originalPrice;
            //            }
            //        }
            //    }
            //}
            #endregion
        }
    }
}
