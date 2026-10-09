#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Amazon.Common.Models;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Amazon
{
    public abstract class AmazonV2 : CollectBase
    {
        /// <summary>
        /// 호스트
        /// </summary>
        public abstract string Host { get; }
        /// <summary>
        /// 수집 시 사용할 Zipcode
        /// </summary>
        public abstract string Zipcode { get; }
        /// <summary>
        /// customer-preferences 언어 설정
        /// </summary>
        public abstract string Lop { get; }
        /// <summary>
        /// customer-preferences 통화 설정
        /// </summary>
        public abstract string Cop { get; }
        public override bool SupportsCaching { get; set; } = false;

        public virtual string DefaultPostData()
        {
            var result = new
            {
                locationType = "LOCATION_INPUT",
                zipCode = this.Zipcode,
                storeContext = "generic",
                deviceType = "web",
                pageType = "Gateway",
                actionSource = "glow",
                almBrandId = "undefined"
            };
            return result.ToSerializeObject();
        }

        #region GetLoginUrlAsync
        public override async Task<string> GetLoginUrlAsync(string connectionId)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("Accept", "text/html,*/*");
            header.Add("Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7,ja;q=0.6");
            header.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            header.Add("Origin", $"https://{Host}");
            header.Add("Referer", $"https://{Host}/");

            string baseUrl = $"https://{Host}/";
            string defaultAmazonUs = await ChatHubService.GetRequest(connectionId, baseUrl, header);
            if (string.IsNullOrWhiteSpace(defaultAmazonUs)) return null;
            string getAntiCsrfUs = GetAntiCsrf(defaultAmazonUs);
            if (getAntiCsrfUs.IsNullOrWhiteSpace() == false)
            {
                string csrfUrl = $"https://{Host}/portal-migration/hz/glow/get-rendered-address-selections?deviceType=desktop&pageType=Gateway&storeContext=NoStoreName&actionSource=desktop-modal";
                Dictionary<string, string> csrfHeader = GetCsrfHeaders(Host, getAntiCsrfUs);
                string getCsrfHtmlUs = await ChatHubService.GetRequest(connectionId, csrfUrl, csrfHeader);

                string csrfUs = GetCsrf(getCsrfHtmlUs);
                if (string.IsNullOrWhiteSpace(csrfUs) == false)
                {
                    var defaultSetHeader = DefaultSetHeaders(Host, csrfUs);

                    await ChatHubService.PostRequest(connectionId,
                        $"https://{Host}/portal-migration/hz/glow/address-change?actionSource=glow",
                        defaultSetHeader, DefaultPostData());

                    await ChatHubService.GetRequest(connectionId,
                        $"https://{Host}/portal-migration/hz/glow/get-location-label?storeContext=furniture&pageType=Detail&actionSource=desktop-modal",
                        DefaultSetHeaders(Host, ""));
                }

                var applyResult = await ApplyCustomerPreferences(connectionId);
                return null;
            }
            else
            {
                return baseUrl;
            }
        }
        private string GetAntiCsrf(string html)
        {
            if (html.IsNullOrWhiteSpace()) return null;
            return html.SingleSplit("anti-csrftoken-a2z&quot;:&quot;", "&quot;");
        }
        private Dictionary<string, string> GetCsrfHeaders(string host, string antiCsrf)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("Accept", "text/html,*/*");
            result.Add("Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7,ja;q=0.6");
            result.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            if (antiCsrf.IsNullOrWhiteSpace() == false) result.Add("anti-csrftoken-a2z", antiCsrf);
            result.Add("Origin", $"https://{host}");
            result.Add("Referer", $"https://{host}/");
            return result;
        }
        private string GetCsrf(string html)
        {
            if (html.IsNullOrWhiteSpace()) return null;
            return html.SingleSplit("CSRF_TOKEN : \"", "\",");
        }
        private Dictionary<string, string> DefaultSetHeaders(string host, string csrf)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("Accept", "text/html,*/*");
            result.Add("Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7,ja;q=0.6");
            result.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            result.Add("Content-Type", "application/json");
            if (csrf.IsNullOrWhiteSpace() == false) result.Add("anti-csrftoken-a2z", csrf);
            result.Add("Origin", $"https://{host}");
            result.Add("Referer", $"https://{host}/");
            return result;
        }
        /// <summary>
        /// 아마존 페이지 언어 및 통화 설정
        /// </summary>
        private async Task<string> ApplyCustomerPreferences(string connectionId)
        {
            if (string.IsNullOrWhiteSpace(this.Lop)) return null;
            if (string.IsNullOrWhiteSpace(this.Cop)) return null;

            var customerPreferencesHtml = await ChatHubService.GetRequest(connectionId, $"https://{Host}/customer-preferences/edit?ref_=icp_cop_flyout_change&preferencesReturnUrl=%2F-%2Fen%2Fref%3Dnav_logo");
            if (string.IsNullOrWhiteSpace(customerPreferencesHtml)) return null;
            var anti_csrftoken_a2z = customerPreferencesHtml.SelectNode("//input[@name='anti-csrftoken-a2z']", "value");
            if (string.IsNullOrWhiteSpace(anti_csrftoken_a2z)) return null;

            var header = new Dictionary<string, string>();
            header.Add("Content-Type", "application/json; charset=UTF-8");
            header.Add("anti-csrftoken-a2z", anti_csrftoken_a2z);

            string body = new { lop = this.Lop, cop = this.Cop }.ToSerializeObject();

            string result = await ChatHubService.PostRequest(connectionId,
                $"https://{Host}/customer-preferences/ajax/save-preferences",
                header, body);
            return result;
        }
        #endregion

        #region GetCollectPageInfosAsync
        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            UriBuilderAdvance uriBuilderAdvance = new UriBuilderAdvance(url);
            bool isBestsellers = uriBuilderAdvance.Segments.Exists(x => x == "bestsellers/");
            if (isBestsellers)
            {
                uriBuilderAdvance.SetQuery("pg", page.ToString());
            }
            else
            {
                uriBuilderAdvance.SetQuery("page", page.ToString());
            }
            uriBuilderAdvance.DeleteQuery("srs");

            string pageUrl = uriBuilderAdvance.ToString();
            string html = await ChatHubService.GetRequest(connectionId, pageUrl);

            List<CollectPageInfo> result = new List<CollectPageInfo>();
            try
            {
                var funcList = new List<Func<string, List<CollectPageInfo>>>();
                funcList.Add(GetPageHtmlToAsinCodes);
                //funcList.Add(GetPageHtmlToAsinCodes1);
                funcList.Add(GetPageHtmlToAsinCodes2);
                funcList.Add(GetPageHtmlToAsinCodes3);
                for (int i = 0; i < funcList.Count; i++)
                {
                    List<CollectPageInfo> collectPageInfos = funcList[i].Invoke(html);
                    if (collectPageInfos != null && collectPageInfos.Count > 0)
                    {
                        result = collectPageInfos;
                        break;
                    }
                }

                if (result == null || result.Count == 0)
                {
                    List<CollectPageInfo> storeCollectInfos = await GetPageHtmlToAsinCodes4(html, connectionId);
                    if (storeCollectInfos != null && storeCollectInfos.Count > 0)
                    {
                        result = storeCollectInfos;
                    }
                }
            }
            catch (Exception exception) { exception.Show(); }
            return result;
        }
        private List<CollectPageInfo> GetPageHtmlToAsinCodes(string html)
        {
            List<string> listitems = null;

            string[] xPaths = new string[]
            {
                "//div[@role='listitem']",
                "//span[@data-component-type='s-search-results']//div[@role='listitem']",
            };
            foreach (var xPath in xPaths)
            {
                listitems = html.GetValues(xPath, TextEx.XPathOption.OuterHtml);
                if (listitems != null && listitems.Count > 0)
                {
                    break;
                }
            }

            if (listitems == null || listitems.Count == 0) { return null; }

            var collectPageInfos = new List<CollectPageInfo>();
            foreach (var item in listitems)
            {
                string micro = item.GetValue("//div[contains(@class,'a-spacing-micro')]", TextEx.XPathOption.OuterHtml); // 스폰/광고 상품일 경우 제외
                if (string.IsNullOrWhiteSpace(micro) == false) continue;
                string asin = item.GetValue("//div", "data-asin");
                if (string.IsNullOrWhiteSpace(asin)) continue;
                string title = item.GetValue("//h2[@aria-label and normalize-space(@aria-label) != '']//span", TextEx.XPathOption.OuterHtml);
                if (string.IsNullOrWhiteSpace(title) == false)
                {
                    title = WebUtility.HtmlDecode(title);
                    title = title.Trim();
                }

                string button = item.GetValue("//button", TextEx.XPathOption.OuterHtml);
                if (string.IsNullOrWhiteSpace(button))
                {
                    string priceTag = item.GetValue("//div[@data-cy='price-recipe']//span[@class='a-price']");
                    if (string.IsNullOrWhiteSpace(priceTag))
                    {
                        continue;
                    }
                }

                var collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = asin;
                collectPageInfo.Url = $"https://{Host}/-/dp/{asin}";
                collectPageInfo.Title = title;
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }
        private List<CollectPageInfo> GetPageHtmlToAsinCodes2(string html)
        {
            string dataClientRecsList = html.GetValue("//div[@class='p13n-desktop-grid']", "data-client-recs-list");
            string jsonString = WebUtility.HtmlDecode(dataClientRecsList);
            if (string.IsNullOrWhiteSpace(jsonString)) return null;
            JArray jArray = JArray.Parse(jsonString);

            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            foreach (JToken jToken in jArray)
            {
                string? asin = jToken?.Value<string>("id");
                if (string.IsNullOrWhiteSpace(asin)) continue;
                var collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = asin;
                collectPageInfo.Url = $"https://{Host}/-/dp/{asin}";
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }
        private List<CollectPageInfo> GetPageHtmlToAsinCodes3(string html)
        {
            List<CollectPageInfo> collectPageInfos = null;

            string startText = "var config = ";
            string widgetDOM = "var widgetDOM";
            string endText = ";";

            List<string> scripts = html.GetValues("//script", TextEx.XPathOption.InnerText);
            if (scripts == null || scripts.Count <= 0) return null;
            List<string> dataScripts = scripts.FindAll(x => x.IndexOf(startText) >= 0);
            if (dataScripts == null || dataScripts.Count <= 0) return null;

            foreach (string dataScript in dataScripts)
            {
                try
                {
                    string text = dataScript.SingleSplit(startText, widgetDOM);
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    int endIndex = text.LastIndexOf(";");
                    var json = text.Substring(0, endIndex);
                    JObject data = JObject.Parse(json);
                    JToken? asinList = data["content"]?["ASINList"];
                    if (asinList == null) continue;
                    JToken[] asinArray = asinList.ToArray();
                    if (asinArray != null && asinArray.Count() >= 0)
                    {
                        collectPageInfos = new List<CollectPageInfo>();
                        foreach (JToken asinToken in asinArray)
                        {
                            string asin = asinToken.ToString();
                            if (string.IsNullOrWhiteSpace(asin)) continue;
                            var collectPageInfo = new CollectPageInfo();
                            collectPageInfo.Code = asin;
                            collectPageInfo.Url = $"https://{Host}/-/dp/{asin}";
                            collectPageInfos.Add(collectPageInfo);
                        }
                        break;
                    }
                }
                catch { }
            }
            return collectPageInfos;
        }
        private async Task<List<CollectPageInfo>> GetPageHtmlToAsinCodes4(string html, string connectionId)
        {
            List<CollectPageInfo> result = new List<CollectPageInfo>();

            var scripts = html.GetValues("//script", TextEx.XPathOption.InnerHtml);
            var slotScript = scripts.Find(x => x.Contains("var slots = "));
            var slots = slotScript.SubstringBetween("var slots = ", ".map(", false);

            List<string> widgetIds = null;

            JArray slotArray = slots.ToJArray();
            if (slotArray != null)
            {
                widgetIds = slotArray.Select(x => x["widgetId"].ToString())?.ToList();
            }

            List<string> finds = scripts.FindAll(x => x.Contains("var config = "));
            foreach (var find in finds)
            {
                var config = find.SubstringBetween("var config = {", "};", false);
                config = $"{{{config}}}";
                JObject data = config.ToJObject();

                List<string> configAsins = GetConfigAsins(find);
                var queryParameterMap = data?["requestContext"]?["queryParameterMap"];
                var appendedParameters = data?["requestContext"]?["appendedParameters"];
                foreach (var widgetId in widgetIds)
                {
                    var uriBuilder = new UriBuilderAdvance($"https://{Host}/stores/slot/{widgetId}");
                    foreach (var queryParameter in queryParameterMap)
                    {
                        var path = queryParameter.First().Path;
                        var lastIdx = path.LastIndexOf(".");
                        var key = path.Substring(lastIdx + 1);
                        var value = queryParameter.First().First().ToString();
                        uriBuilder.SetQuery(key, value);
                    }

                    foreach (var appendedParameter in appendedParameters)
                    {
                        var path = appendedParameter.First().Path;
                        var lastIdx = path.LastIndexOf(".");
                        var key = path.Substring(lastIdx + 1);
                        var value = appendedParameter.First().ToString();
                        uriBuilder.SetQuery(key, value);
                    }
                    uriBuilder.DeleteQuery("page");

                    string slotUrl = uriBuilder.ToString();
                    var slotHtml = await this.ChatHubService.GetRequest(connectionId, slotUrl);
                    var getConfigAsins = GetConfigAsins(slotHtml);
                    configAsins.AddRange(getConfigAsins);
                }

                foreach (var configAsin in configAsins)
                {
                    if (string.IsNullOrWhiteSpace(configAsin)) continue;
                    bool isExists = result.Exists(x => x.Code == configAsin);
                    if (isExists) continue;
                    CollectPageInfo collectPageInfo = new CollectPageInfo();
                    collectPageInfo.Code = configAsin;
                    collectPageInfo.Url = $"https://{Host}/-/dp/{configAsin}";
                    result.Add(collectPageInfo);
                }
            }
            return result;
        }
        private List<string> GetConfigAsins(string html)
        {
            List<string> asins = new List<string>();
            var config = html.SubstringBetween("var config = {", "};", false);
            config = $"{{{config}}}";
            JObject data = config.ToJObject();
            if (data != null)
            {
                JToken tiles = data["tiles"];
                if (tiles != null && tiles.HasValues)
                {
                    List<JToken> tileList = tiles.ToList();
                    foreach (JToken tile in tileList)
                    {
                        List<JToken> ASINList = tile["content"]?["ASINList"]?.ToList();
                        if (ASINList == null || ASINList.Count == 0) { continue; }
                        foreach (var ASIN in ASINList)
                        {
                            string asin = ASIN.ToString();
                            if (string.IsNullOrWhiteSpace(asin)) continue;
                            asins.Add(asin);
                        }
                    }
                }

                JToken content = data["content"];
                if (content != null && content.HasValues)
                {
                    List<JToken> ASINList = content?["ASINList"]?.ToList();
                    if (ASINList != null && ASINList.Count > 0)
                    {
                        foreach (var ASIN in ASINList)
                        {
                            string asin = ASIN.ToString();
                            if (string.IsNullOrWhiteSpace(asin)) continue;
                            asins.Add(asin);
                        }
                    }
                }
            }
            return asins;
        }
        #endregion

        public override void ThrowIfCaptcha(string url, string html)
        {
            return;
        }

        public override string GetCode(string url)
        {
            try
            {
                var uri = new Uri(url);
                var segments = uri.Segments.ToList();
                int findIndex = segments.FindIndex(x => x == "dp/");
                string code = null;
                if (findIndex >= 0)
                {
                    code = uri.Segments[findIndex + 1];
                }
                else
                {
                    code = uri.Segments.Last();
                }
                if (code.Contains("/"))
                {
                    code = code.Replace("/", "");
                }
                if (string.IsNullOrWhiteSpace(code)) throw new Exception();
                return code;
            }
            catch (Exception exception)
            {
                throw new CollectException("상품 코드를 찾을 수 없습니다.", exception);
            }
        }

        public override string CreateProductUrl(string code)
        {
            return $"https://{Host}/dp/{code}?th=1&psc=1";
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            JObject data = new JObject();

            JObject productData = GetProductData(html);
            data["productData"] = productData;

            JObject optionData = GetOptionData(html);
            if (optionData != null)
            {
                data["optionData"] = optionData;

                List<OptionModel> optionModels = await GetOptionModels(connectionId, html, optionData);
                data["optionModels"] = JToken.FromObject(optionModels);

                Option option = ParseOption(optionData, productData);
                if (option != null)
                {
                    List<Option.DetailOption> detailOptions = GetDetailOptions(this.Currency.ToString(), option, optionModels);
                    option.detailOptions = detailOptions;
                    data["option"] = JToken.FromObject(option);

                    Option.DetailOption minDetailOption = ProductHelper.FindMinDetailOption(detailOptions);
                    string asin = minDetailOption.optionIds.Split("ㅡ").Last();
                    string newUrl = $"https://{Host}/dp/{asin}?th=1&psc=1";
                    data["newUrl"] = newUrl;

                    data["newHtml"] = await ChatHubService.GetRequest(connectionId, newUrl);
                }
            }
            JObject nonOptionData = GetNonOptionData(html);
            data["nonOption"] = nonOptionData;

            return data;
        }
        private async Task<List<OptionModel>> GetOptionModels(string connectionId, string html, JToken optionData)
        {
            if (optionData == null) return null;

            var dimensionToAsinMap = optionData.Value("dimensionToAsinMap");
            List<string> optionAsins = new List<string>();
            foreach (var child in dimensionToAsinMap.Children())
            {
                var first = child?.First()?.ToString();
                if (string.IsNullOrWhiteSpace(first)) continue;
                optionAsins.Add(first);
            }
            List<List<string>> divisions = optionAsins.Division(5);

            var optionModels = new List<OptionModel>();
            for (int i = 0; i < divisions.Count; i++)
            {
                CollectRequestInfo requestInfo = GetOptionPriceRequestInfo(html, divisions[i]);
                if (requestInfo == null) continue;
                string optionHtml = await ChatHubService.GetRequest(connectionId, requestInfo.Url, requestInfo.Headers);

                List<OptionModel> optionDatas = ConvertOptionModels(optionHtml);
                if (optionDatas != null && optionDatas.Count > 0)
                {
                    optionModels.AddRange(optionDatas);
                    DebugConsole.WriteLine($"옵션 가격 수집 중 : {optionModels.Count}/{optionAsins.Count}");
                }
            }
            return optionModels;
        }
        public CollectRequestInfo GetOptionPriceRequestInfo(string html, List<string> codes)
        {
            CollectRequestInfo result = null;
            try
            {
                var asins = new List<string>();
                foreach (string code in codes)
                {
                    if (code.Contains("ㅡ"))
                    {
                        string asin = code.Split("ㅡ").Last();
                        if (string.IsNullOrWhiteSpace(asin)) continue;
                        asins.Add(asin);
                    }
                    else
                    {
                        asins.Add(code);
                    }
                }
                if (asins.Count == 0) return null;
                //List<string> scripts = html.GetValues("//script[@type='text/javascript']", TextEx.XPathOption.InnerHtml);
                //string? find = scripts.Find(x => x.IndexOf("P.register('twister-js-init-dpx-data'") >= 0);
                //if (find == null) { return null; }
                //string startText = "dataToReturn = ";
                //string lastText = "return dataToReturn";
                //int first = find.IndexOf(startText);
                //if (first >= 0)
                //{
                //    first += startText.Length;
                //}
                //string text = find.Substring(first);
                //int last = text.LastIndexOf(lastText);
                //text = text.Substring(0, last);
                //int endIndex = text.LastIndexOf(";");
                //string json = text.Substring(0, endIndex);
                //string exceptionString = "\" + \"";
                //if (json.Contains(exceptionString))
                //{
                //    json = json.Replace(exceptionString, "");
                //}
                //JObject data = JObject.Parse(json);
                JObject optionData = GetOptionData(html);

                string? parentAsin = null;
                string? productTypeDefinition = null;
                string? productGroupId = null;

                if (optionData != null)
                {
                    JToken? optionUrlData = TryGetToken(optionData, "twisterUpdateURLInfo", "twisterUpdateURLAppend");
                    JToken? immutableParams = optionUrlData["immutableParams"];
                    Dictionary<string, object>? dictionary = immutableParams.ToObject<Dictionary<string, object>>();
                    //string? dpEnvironment = data["dpEnvironment"]?.ToString();
                    parentAsin = dictionary["parentAsin"].ToString();
                    productTypeDefinition = dictionary["ptd"].ToString();
                    productGroupId = dictionary["pgid"].ToString();
                }
                else
                {
                    JObject productData = GetProductData(html);
                    parentAsin = productData.Value<string>("parentAsin");
                    productGroupId = productData.Value<string>("productGroupID");
                }

                string asinList = string.Join(",", asins);

                var sb = new StringBuilder();
                //sb.Append($"https://{Host}/gp/product/ajax/twisterDimensionSlotsDefault?");
                sb.Append($"https://{Host}/gp/product/ajax/twisterDimensionSlotsDefault?");
                sb.Append("isDimensionSlotsAjax=1&");
                sb.Append($"asinList={asinList}&");
                sb.Append("vs=1&");
                //sb.Append("experienceId=twisterDimensionSlotsDefault&");
                sb.Append($"asin={parentAsin}&");
                sb.Append($"productTypeDefinition={productTypeDefinition}&");
                sb.Append($"productGroupId={productGroupId}&");
                sb.Append("storeId=&");
                sb.Append($"parentAsin={parentAsin}&");
                sb.Append("isPrime=0&");
                sb.Append("webpSupport=1&");
                sb.Append("isProductReel=0&");
                sb.Append("deviceOs=android&");
                sb.Append($"landingAsin={parentAsin}&");
                sb.Append("deviceType=mobile&");
                sb.Append("showFancyPrice=false&");
                sb.Append("twisterFlavor=twisterPlusInlineTwister2D");

                var header = new Dictionary<string, string>();
                header.Add("Accept", "text/html,*/*");
                header.Add("Accept-Encoding", "gzip, deflate, br, zstd");
                header.Add("Accept-Language", "ko,en-US;q=0.9,en;q=0.8,ko-KR;q=0.7");
                header.Add("User-Agent", "Mozilla/5.0 (Linux; Android 4.4.2; Nexus 4 Build/KOT49H) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.114 Mobile Safari/537.36");

                result = new CollectRequestInfo
                {
                    RequestType = RequestType.ChatHub,
                    Method = "GET",
                    Url = sb.ToString(),
                    Headers = header
                };
            }
            catch (Exception exception)
            {
                DebugConsole.WriteLine("옵션 정보가 있는 scripts를 찾을 수 없습니다.");
            }
            return result;
        }
        public List<OptionModel> ConvertOptionModels(string html)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(html)) return null;
                var result = new List<OptionModel>();
                string replaceHtml = html.Replace("&&&", ",");
                replaceHtml = $"[{replaceHtml}]";
                JArray array = replaceHtml.ToJArray();
                if (array == null) return null;
                foreach (JToken item in array)
                {
                    string json = item.ToString();
                    var optionModel = json.ToDeserializeObject<OptionModel>();
                    if (optionModel != null)
                    {
                        result.Add(optionModel);
                    }
                }

                return result;
            }
            catch (Exception exception)
            {
                DebugConsole.WriteLine(exception.StackTrace);
                return null;
            }
        }
        private JToken TryGetToken(JObject json, params string[] keys)
        {
            JToken result = null;

            foreach (string key in keys)
            {
                JToken? token = json[key];
                if (token != null)
                {
                    result = token;
                    break;
                }
            }
            return result;
        }
        private JObject GetNonOptionData(string html)
        {
            var scripts = html.GetValues("//script[@type='text/javascript']", TextEx.XPathOption.OuterHtml);
            string json = null;
            foreach (string script in scripts)
            {
                if (script.IndexOf("ImageBlockATF") >= 0)
                {
                    json = script;
                    json = json.SubstringBetween("var data = {", "};", false);
                    json = $"{{{json}}}";
                    json = json.Replace("'", "\"");
                    json = json.Replace("Date.now()", "null");
                    break;
                }
            }
            JObject data = json.ToJObject();
            return data;
        }
        public List<Option.DetailOption> GetDetailOptions(string currency, Option option, List<OptionModel> optionModels)
        {
            var result = new List<Option.DetailOption>();
            try
            {
                for (int i = 0; i < option.detailOptions.Count; i++)
                {
                    Option.DetailOption detailOption = option.detailOptions[i];
                    string asin = detailOption.optionIds.Split("ㅡ").Last();
                    if (string.IsNullOrWhiteSpace(asin)) continue;
                    OptionModel? findOptionModel = optionModels.Find(x => x.ASIN == asin);

                    string twisterSlotDiv = findOptionModel?.value?.content?.twisterSlotDiv;
                    if (string.IsNullOrWhiteSpace(twisterSlotDiv))
                    {
                        detailOption.isSoldOut = true;
                        if (detailOption.price == null)
                        {
                            detailOption.price = new List<Price> { new Price(currency, 0, 0) };
                        }
                        else
                        {
                            detailOption.price.Find(x => x.currency == currency).originalPrice = 0;
                            detailOption.price.Find(x => x.currency == currency).salePrice = 0;
                        }
                        detailOption.quantity = 0;
                    }
                    else
                    {
                        //string partten = @"-?\d+(\.\d+)?|\.\d+";
                        List<string> priceTexts = twisterSlotDiv.GetValues("//span[contains(@class,'a-size-medium')]", TextEx.XPathOption.InnerText);
                        var priceValues = new List<double>();
                        foreach (string priceText in priceTexts)
                        {
                            if (priceText.Contains(",") && currency == "EUR")
                            {
                                string replacePriceText = priceText.Replace(".", "");
                                replacePriceText = priceText.Replace(",", ".");
                                string price = Regex.Replace(replacePriceText, @"[^0-9.]", "");
                                if (string.IsNullOrWhiteSpace(price) == false)
                                {
                                    priceValues.Add(Convert.ToDouble(price));
                                }
                            }
                            else
                            {
                                string price = Regex.Replace(priceText, @"[^0-9.]", "");
                                if (string.IsNullOrWhiteSpace(price) == false)
                                {
                                    priceValues.Add(Convert.ToDouble(price));
                                }
                            }
                        }

                        if (priceValues.Count <= 0)
                        {
                            double salePrice = 0;
                            string? price = findOptionModel?.value?.content?.twisterSlotJson?.price;
                            if (string.IsNullOrWhiteSpace(price) == false)
                            {
                                salePrice = Convert.ToDouble(price);
                            }

                            double originalPrice = 0;
                            List<string>? priceSpanTags = findOptionModel?.value?.content?.twisterSlotDiv.GetValues("//span[@data-a-strike='true']//span[@class='a-offscreen']", TextEx.XPathOption.OuterHtml);
                            foreach (var spanTag in priceSpanTags)
                            {
                                string innerText = spanTag.GetSelectNodeInnerText("//span[@class='a-offscreen']");
                                if (string.IsNullOrWhiteSpace(innerText) == false)
                                {
                                    double? parsePrice = innerText.ParseToDouble();
                                    if (parsePrice != null && originalPrice < (double)parsePrice)
                                    {
                                        originalPrice = (double)parsePrice;
                                    }
                                }
                            }

                            if (salePrice > originalPrice)
                            {
                                originalPrice = salePrice;
                            }

                            if (detailOption.price == null)
                            {
                                detailOption.price = new List<Price> { new Price(currency, originalPrice, salePrice) };
                            }
                            else
                            {
                                detailOption.price.Find(x => x.currency == currency).originalPrice = originalPrice;
                                detailOption.price.Find(x => x.currency == currency).salePrice = salePrice;
                            }

                            string? twisterAvailability = findOptionModel?.value?.content?.twisterSlotDiv.GetValue("//span[@id='twisterAvailability']", TextEx.XPathOption.InnerText);
                            if (string.IsNullOrWhiteSpace(twisterAvailability) == false && twisterAvailability.ToLower().Contains("in stock"))
                            {
                                detailOption.isSoldOut = false;
                                detailOption.quantity = 999;
                            }
                            else
                            {
                                detailOption.isSoldOut = true;
                                detailOption.quantity = 0;
                            }
                        }
                        else if (priceValues.Count > 2)
                        {
                            throw new Exception("옵션 가격 부분 확인이 필요한 에러");
                        }
                        else
                        {
                            priceValues = priceValues.OrderBy(x => x).ToList();

                            double originalPrice = priceValues.Max();
                            double salePrice = priceValues.Min();
                            if (salePrice <= 0 || originalPrice <= 0)
                            {
                                continue;
                            }
                            detailOption.isSoldOut = false;
                            if (detailOption.price == null)
                            {
                                detailOption.price = new List<Price> { new Price(currency, originalPrice, salePrice) };
                            }
                            else
                            {
                                detailOption.price.Find(x => x.currency == currency).originalPrice = originalPrice;
                                detailOption.price.Find(x => x.currency == currency).salePrice = salePrice;
                            }
                            detailOption.quantity = 999;
                        }
                    }
                    result.Add(detailOption);
                }
                // 아마존은 옵션이 과도하게 많이 때문에 수집 단에서 일부 삭제
                result.RemoveAll(x => x.isSoldOut);
                result.RemoveAll(x => x.price.Find(x => x.currency == currency).salePrice == 0);
            }
            catch (Exception exception)
            {
                DebugConsole.WriteLine(exception.Message);
                return null;
            }
            return result;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            JToken productData = data.Value("productData");
            string? title = productData.Value<string>("title");
            if (string.IsNullOrWhiteSpace(title)) throw new CollectException("상품명을 찾을 수 없습니다.");
            title = WebUtility.HtmlDecode(title).Trim();
            List<Name> names = new List<Name>() { new Name(this.Language, title) };
            return names;
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            string rhapsodyARIngressViewModel = html.SingleSplit("var rhapsodyARIngressViewModel = {", "};");
            if (string.IsNullOrWhiteSpace(rhapsodyARIngressViewModel)) return null;
            rhapsodyARIngressViewModel = "{" + rhapsodyARIngressViewModel + "}";
            rhapsodyARIngressViewModel = rhapsodyARIngressViewModel.Replace("`", "\"");
            JObject rhapsodyARIngressView = rhapsodyARIngressViewModel.ToJObject();
            if (rhapsodyARIngressView == null) return null;
            string? brand = rhapsodyARIngressView.Value<string>("brand");
            if (string.IsNullOrWhiteSpace(brand)) return null;
            List<Name> names = new List<Name>() { new Name(this.Language, brand) };
            return names;
        }

        public override string GetModel(string html, JToken data) => null;

        public override List<Desc> GetDescs(string html, JToken data)
        {
            List<string> divs = html.GetValues("//div[@class='a-fixed-left-grid product-facts-detail']", TextEx.XPathOption.OuterHtml);
            if (divs == null || divs.Count <= 0) return null;
            List<Desc> descs = new List<Desc>();
            for (int i = 0; i < divs.Count; i++)
            {
                string div = divs[i];
                List<string> spans = div.GetValues("//span[@class='a-color-base']", TextEx.XPathOption.OuterHtml);
                if (spans == null || spans.Count != 2) continue;
                string title = spans[0].GetSelectNodeInnerText("//span[@class='a-color-base']");
                if (string.IsNullOrWhiteSpace(title)) continue;
                string value = spans[1].GetSelectNodeInnerText("//span[@class='a-color-base']");
                if (string.IsNullOrWhiteSpace(value)) continue;
                Desc desc = new Desc(i + 1, this.Language, title, value);
                descs.Add(desc);
            }
            return descs;
        }

        public override List<Tag> GetTags(string html, JToken data) => null;

        public override List<Price> GetPrice(string html, JToken data)
        {
            JToken option = data.Value("option");
            if (option != null)
            {
                var deserializeOption = option.ToString().ToDeserializeObject<Option>();
                return ProductHelper.GetMinPriceInDetailOption(deserializeOption.detailOptions, this.Currency);
            }
            else
            {
                double salePrice = 0d;
                double originalPrice = 0d;

                string priceValue = string.Empty;
                string[] tryXPaths = new string[]
                {
                    "//input[@name='priceValue']",
                    "//input[@id='attach-base-product-price']",
                    "//input[@name='items[0.base][customerVisiblePrice][amount]']",
                };
                for (int i = 0; i < tryXPaths.Length; i++)
                {
                    string getValue = html.GetValue(tryXPaths[i], "value");
                    if (string.IsNullOrWhiteSpace(getValue) == false)
                    {
                        priceValue = getValue;
                        break;
                    }
                }

                if (double.TryParse(priceValue, out salePrice))
                {
                    if (string.IsNullOrWhiteSpace(priceValue)) return null;
                    string originalPriceString = html.GetSelectNodeInnerText("//div[@id='corePriceDisplay_desktop_feature_div']//span[@data-a-strike='true']//span[@class='a-offscreen']");

                    double? value = originalPriceString?.ParseToDouble();

                    if (value != null && value > 0)
                    {
                        originalPrice = (double)value;
                    }
                }
                else
                {
                    throw new SaleException(ProductSaleStatus.SoldOut, "테스트");
                }

                if (salePrice > originalPrice)
                {
                    originalPrice = salePrice;
                }

                var price = new List<Price> { new Price(this.Currency, originalPrice, salePrice, true) };
                return price;
            }
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            List<string> result = new List<string>();

            var productData = data.Value("productData");
            var nonOption = data.Value("nonOption");

            var datas = new JArray();
            datas.Add(productData);
            datas.Add(nonOption);

            foreach (JObject item in datas)
            {
                if (item == null) continue;
                string? landingAsinColor = item.Value<string>("landingAsinColor");
                List<JToken>? mainImages = null;
                if (string.IsNullOrWhiteSpace(landingAsinColor) == false)
                {
                    mainImages = item["colorImages"]?[landingAsinColor]?.ToList();
                }
                else
                {
                    mainImages = item["colorImages"]?["initial"]?.ToList();
                }
                if (mainImages == null || mainImages.Count <= 0) continue;
                foreach (var mainImage in mainImages)
                {
                    //JObject? main = mainImage["main"].ToObject<JObject>();
                    //if (main == null) continue;
                    //string imageUrl = main.Properties().First().Name;
                    string imageUrl = mainImage.Value<string>("hiRes");
                    if (string.IsNullOrWhiteSpace(imageUrl))
                    {
                        imageUrl = mainImage.Value<string>("large");
                        if (string.IsNullOrWhiteSpace(imageUrl)) continue;
                    }
                    result.Add(imageUrl);
                }
                if (result.Count > 0) break;
            }

            // 폴백: JSON 이미지가 1개 이하일 때, HTML DOM에서 추출
            if (result.Count <= 1)
            {
                List<string> htmlImages = GetMainImagesFromHtml(html);
                if (htmlImages != null && htmlImages.Count > result.Count)
                {
                    result = htmlImages;
                }
            }

            if (result.Count <= 0) throw new Exception("메인 이미지를 찾을 수 없습니다.");
            return result;
        }
        private List<string> GetMainImagesFromHtml(string html)
        {
            var result = new List<string>();
            try
            {
                List<string> thumbSrcs = html.GetValues(
                    "//div[@id='altImages']//li[contains(@class,'imageThumbnail')]//img", "src");
                if (thumbSrcs != null && thumbSrcs.Count > 0)
                {
                    foreach (string src in thumbSrcs)
                    {
                        if (string.IsNullOrWhiteSpace(src)) continue;
                        if (src.Contains("play-button") || src.Contains("video")) continue;
                        // 썸네일 → 원본: ._AC_US40_. 같은 크기 접미사 제거
                        string hiRes = Regex.Replace(src, @"\._[A-Z0-9_,]+_\.", ".");
                        if (result.Contains(hiRes) == false)
                            result.Add(hiRes);
                    }
                }
            }
            catch { }
            return result;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            string result = null;
            try
            {
                string container = html.GetValue("//div[@id='dp-container']", TextEx.XPathOption.OuterHtml);

                var detailPageBuilder = new StringBuilder();
                // 수집 설정 옵션으로 빠짐, CollectGroupModel.useMainImageInDetailPage 참조
                //detailPageBuilder.AppendLine("<div>"); 
                //foreach (string mainImage in mainImages)
                //{
                //    detailPageBuilder.AppendLine($"<img style='max-width: 600px; display: block; margin: auto;' src='{mainImage}'>");
                //}
                //detailPageBuilder.AppendLine("</div>");
                DetailPageBuild(detailPageBuilder, container, "//div[@id='featurebullets_feature_div']");
                DetailPageBuild(detailPageBuilder, container, "//div[@id='productFactsDesktop_feature_div']");
                DetailPageBuild(detailPageBuilder, container, "//div[@id='detailBulletsWrapper_feature_div']");
                DetailPageBuild(detailPageBuilder, container, "//div[@id='detailBullets_feature_div']");
                DetailPageBuild(detailPageBuilder, container, "//div[@id='productDescription_feature_div']");
                DetailPageBuild(detailPageBuilder, container, "//div[@id='aplus']");
                string detailPage = detailPageBuilder.ToString();
                string getDafaultHtml = HtmlReader.GetDafaultHtml(detailPage);
                if (string.IsNullOrWhiteSpace(getDafaultHtml)) throw new Exception("상세페이지 수집에 실패했습니다.");

                string[] escapeStrings = new string[] { "&#155;", "&sup1;", "&nbsp;" };
                for (int i = 0; i < escapeStrings.Length; i++)
                {
                    if (getDafaultHtml.Contains(escapeStrings[i]) == false) continue;
                    getDafaultHtml = getDafaultHtml.Replace(escapeStrings[i], "");
                }

                result = getDafaultHtml;
            }
            catch (Exception e) { throw e; }
            return result;
        }
        protected void DetailPageBuild(StringBuilder stringBuilder, string html, string xpath)
        {
            if (string.IsNullOrWhiteSpace(html)) return;
            string element = html.GetValue(xpath, TextEx.XPathOption.InnerHtml);
            if (string.IsNullOrWhiteSpace(element) == false)
            {
                string comparison_table_scroller = element.GetValue("//div[contains(@class,'comparison-table-scroller')]", TextEx.XPathOption.OuterHtml);
                if (string.IsNullOrWhiteSpace(comparison_table_scroller) == false)
                {
                    element = element.Replace(comparison_table_scroller, "");
                }

                switch (xpath)
                {
                    case "//div[@id='detailBulletsWrapper_feature_div']":
                        var uls = element.GetValues("//ul", TextEx.XPathOption.OuterHtml);
                        for (int i = 0; i < uls.Count; i++)
                        {
                            if (i == 0)
                            {
                                List<string> lis = uls[i].GetValues("//li", TextEx.XPathOption.OuterHtml);
                                foreach (string li in lis)
                                {
                                    var spans = li.GetValues("//span/span", TextEx.XPathOption.InnerText);
                                    if (spans.Count != 2)
                                    {
                                        element = element.Replace(li, "");
                                    }
                                    else
                                    {
                                        var htmlDecode = WebUtility.HtmlDecode(spans[0]);
                                        string regexReplace = Regex.Replace(htmlDecode, @"[^A-Za-z0-9가-힣\s]", "");
                                        string key = regexReplace.Trim();
                                        switch (key.ToUpper())
                                        {
                                            case "ASIN":
                                            case "BEST SELLERS RANK":
                                                element = element.Replace(li, string.Empty);
                                                continue;
                                            default:
                                                break;
                                        }
                                        string value = spans[1].Trim();
                                        element = element.Replace(li, $"<li>{key} : {value}</li>");
                                    }
                                }
                            }
                            else
                            {
                                element = element.Replace(uls[i], string.Empty);
                            }
                        }
                        break;
                    case "//div[@id='productFactsDesktop_feature_div']":
                        element = element.GetValue("//div", TextEx.XPathOption.InnerHtml);
                        break;
                    default:
                        break;
                }
                stringBuilder.AppendLine(element);
            }
        }

        public override Option GetOption(string html, JToken data)
        {
            try
            {
                var optionObject = data.Value("option");
                if (optionObject == null) return null;
                if (optionObject.HasValues == false) return null;
                var optionJson = optionObject.ToString();
                if (string.IsNullOrWhiteSpace(optionJson)) return null;
                Option deserializeOption = optionJson.ToDeserializeObject<Option>();
                return deserializeOption;
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine($"옵션 수집 실패: {ex.Message}");
                return null;
            }
        }
        protected JObject GetProductData(string html)
        {
            string productJson = html.SingleSplit("jQuery.parseJSON('", "')");
            JObject data = productJson.ToJObject();
            return data;
        }
        protected JObject GetOptionData(string html)
        {
            var scripts = html.GetValues("//script[@type='text/javascript']", TextEx.XPathOption.OuterHtml);
            string json = null;
            string matchedScript = null;

            // 1순위: twister-js-init-dpx-data
            foreach (var script in scripts)
            {
                if (script.IndexOf("twister-js-init-dpx-data") >= 0)
                {
                    matchedScript = script;
                    json = script;
                    json = json.SubstringBetween("var dataToReturn = ", "return dataToReturn;", false);
                    if (json != null)
                    {
                        var lastIndexOf = json.LastIndexOf(";");
                        json = json.Substring(0, lastIndexOf);
                        string exceptionString = "\" + \"&landingAsin=";
                        if (json.Contains(exceptionString))
                        {
                            json = json.Replace(exceptionString, "");
                        }
                    }
                    break;
                }
            }

            JObject data = json.ToJObject();

            // twister 스크립트는 찾았지만 "var dataToReturn" 패턴이 아닌 경우
            if (data == null && matchedScript != null)
            {
                string altJson = matchedScript.SubstringBetween("var data = ", "return data;", false);
                if (string.IsNullOrWhiteSpace(altJson) == false)
                {
                    var lastIdx = altJson.LastIndexOf(";");
                    if (lastIdx > 0) altJson = altJson.Substring(0, lastIdx);
                    string exStr = "\" + \"&landingAsin=";
                    if (altJson.Contains(exStr)) altJson = altJson.Replace(exStr, "");
                    data = altJson.ToJObject();
                }
            }

            // 2순위: dimensionToAsinMap + variationValues 키워드 기반 검색
            if (data == null)
            {
                foreach (var script in scripts)
                {
                    if (script.IndexOf("dimensionToAsinMap") >= 0
                        && script.IndexOf("variationValues") >= 0)
                    {
                        // "var dataToReturn" 패턴 시도
                        string extracted = script.SubstringBetween(
                            "var dataToReturn = ", "return dataToReturn;", false);
                        // "var data" 패턴 시도
                        if (string.IsNullOrWhiteSpace(extracted))
                            extracted = script.SubstringBetween(
                                "var data = ", "return data;", false);
                        if (string.IsNullOrWhiteSpace(extracted) == false)
                        {
                            var lastIdx = extracted.LastIndexOf(";");
                            if (lastIdx > 0) extracted = extracted.Substring(0, lastIdx);
                            string exStr = "\" + \"&landingAsin=";
                            if (extracted.Contains(exStr)) extracted = extracted.Replace(exStr, "");
                            data = extracted.ToJObject();
                            if (data != null) break;
                        }
                    }
                }
            }

            return data;
        }
        public Option ParseOption(JToken optionData, JToken data)
        {
            if (optionData == null) return null;
            try
            {
                var optionGroups = new List<Option.OptionGroup>();
                List<JToken> dimensions = optionData["dimensions"].ToList();
                foreach (JToken dimension in dimensions)
                {
                    Option.OptionGroup optionGroup = new Option.OptionGroup();
                    string? dimensionString = dimension?.ToString();
                    optionGroup.optionId = dimensionString;
                    JToken? lable = optionData?["variationDisplayLabels"]?[dimensionString];
                    string? groupName = lable?.ToString();
                    optionGroup.groupName = new List<Name> { new Name(this.Language, groupName) };

                    JToken? variation = optionData?["variationValues"]?[dimensionString];
                    List<JToken> variations = variation.ToList();

                    optionGroup.groups = new List<Option.OptionGroup.Group>();

                    for (int i = 0; i < variations.Count; i++)
                    {
                        Option.OptionGroup.Group group = new Option.OptionGroup.Group();
                        group.groupId = i.ToString();
                        string optionName = variations[i].ToString();
                        group.optionName = new List<Name> { new Name(this.Language, optionName) };
                        string key = optionName;
                        if (key.Contains("/"))
                        {
                            key = key.Replace("/", "\\/");
                        }
                        JToken? colorImages = data?["colorImages"]?[key];
                        if (colorImages != null)
                        {
                            JToken colorImage = colorImages.First();
                            string? image = colorImage?["hiRes"]?.ToString();
                            if (string.IsNullOrWhiteSpace(image))
                            {
                                image = colorImage?["large"]?.ToString();
                            }
                            group.optionImage = image;
                        }
                        optionGroup.groups.Add(group);
                    }
                    optionGroups.Add(optionGroup);
                }

                var detailOptions = new List<Option.DetailOption>();

                var optionGroupNames = new List<string>();
                JToken? variationDisplayLabels = optionData["variationDisplayLabels"];
                foreach (JToken variationDisplayLabel in variationDisplayLabels)
                {
                    string label = variationDisplayLabel.First().ToString();
                    optionGroupNames.Add(label);
                }

                string optionGroupName = string.Join("ㅡ", optionGroupNames);

                JToken? dimensionToAsinMap = optionData["dimensionToAsinMap"];
                JToken? dimensionValuesDisplayData = optionData["dimensionValuesDisplayData"];
                JEnumerable<JToken> children = dimensionToAsinMap.Children();
                foreach (JToken child in children)
                {
                    JProperty prop = (JProperty)child;
                    string id = prop.Name;
                    id = id.Replace("_", "ㅡ");

                    string asin = prop.Value.ToString();
                    id += $"ㅡ{asin}";
                    if (string.IsNullOrWhiteSpace(asin)) throw new Exception();

                    List<JToken>? detailOptionValues = dimensionValuesDisplayData?[asin]?.ToList();
                    if (detailOptionValues == null || detailOptionValues.Count <= 0) throw new Exception();
                    string optionName = string.Join("ㅡ", detailOptionValues);
                    if (string.IsNullOrWhiteSpace(optionName)) throw new Exception();

                    Option.DetailOption detailOption = new Option.DetailOption();
                    detailOption.optionIds = id;
                    detailOption.optionGroupName = new List<Name> { new Name(this.Language, optionGroupName) };
                    detailOption.optionName = new List<Name> { new Name(this.Language, optionName) };

                    var colorImagesKey = string.Join(" ", detailOptionValues);
                    var colorImages = data?.Value("colorImages", colorImagesKey);
                    if (colorImages != null)
                    {
                        JToken colorImage = colorImages.First();
                        string? image = colorImage?["hiRes"]?.ToString();
                        if (string.IsNullOrWhiteSpace(image))
                        {
                            image = colorImage?["large"]?.ToString();
                        }
                        if (string.IsNullOrWhiteSpace(image) == false)
                        {
                            detailOption.optionImage = image;
                        }
                    }
                    detailOptions.Add(detailOption);
                }

                var option = new Option();
                option.optionGroups = optionGroups;
                option.detailOptions = detailOptions;

                #region 아마존 옵션 이미지 처리, 첫번째 옵션에만 이미지가 있다고 가정
                foreach (Option.DetailOption detailOption in option.detailOptions)
                {
                    if (string.IsNullOrWhiteSpace(detailOption.optionImage)) continue;
                    string firstOptionId = detailOption.optionIds.Split("ㅡ")?.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(firstOptionId)) continue;
                    Option.OptionGroup.Group group = option.optionGroups.First()?.groups.Find(x => x.groupId == firstOptionId);
                    if (group == null) continue;
                    if (string.IsNullOrWhiteSpace(group.optionImage))
                    {
                        group.optionImage = detailOption.optionImage;
                        detailOption.optionImage = null;
                    }
                }
                #endregion
                return option;
            }
            catch (Exception exception)
            {
                throw new Exception("옵션 수집 과정에서 문제가 발생했습니다.", exception);
            }
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
            return;
        }

        public override int? GetTotalQuantity(string html, JToken data) => null;

        public override async Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            var html = await GetHtmlAsync(connectionId, url);
            var data = await GetData(connectionId, url, html);

            var newUrl = data.Value<string>("newUrl");
            if (string.IsNullOrWhiteSpace(newUrl) == false) url = newUrl;
            var newHtml = data.Value<string>("newHtml");
            if (string.IsNullOrWhiteSpace(html) == false) html = newHtml;

            CollectRawData collectRawData = new CollectRawData(url, data, html);
            return collectRawData;
        }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            return;
        }
    }
}
