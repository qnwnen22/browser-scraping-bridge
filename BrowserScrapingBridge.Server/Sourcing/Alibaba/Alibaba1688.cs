#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Alibaba.H5ApiModels;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BrowserScrapingBridge.Server.Sourcing.Alibaba
{
    public class Alibaba1688 : CollectBase
    {
        private string jsv = "2.7.2";
        private string appKey = "12574478";
        public override string Site => "1688";
        public override string Name => "1688";
        public override Language Language { get; set; } = Language.CN;
        public override Currency Currency { get; set; } = Currency.CNY;
        public override bool SupportsCaching { get; set; } = true;

        public override async Task<string> GetLoginUrlAsync(string connectionId)
        {
            string loginUrl = "https://login.taobao.com/?\r\nredirect_url=https%3A%2F%2Flogin.1688.com%2Fmember%2Fjump.htm%3Ftarget%3Dhttps%253A%252F%252Flogin.1688.com%252Fmember%252FmarketSigninJump.htm%253FDone%253Dhttps%25253A%25252F%25252Fwww.1688.com%25252F%25253FpcLoginAbJumpBeforeTimeStr%25253D%252526scene%25253Da260k.home2025.topbar.login%252526from%25253Dlogin-modal&style=tao_custom&from=1688web";
            try
            {
                string token = await GetToken(connectionId);
                var userSimpleUrl = GetUserSimpleUrl(token);
                string loginCheckHtml = await ChatHubService.GetRequest(connectionId, userSimpleUrl);
                if (string.IsNullOrWhiteSpace(loginCheckHtml) == false)
                {
                    string json = loginCheckHtml.SubstringBetween(" mtopjsonpliblogin1(", ")", false);
                    JObject data = json.ToJObject();
                    if (data != null)
                    {
                        JToken ret = data["ret"];
                        if (ret != null)
                        {
                            var message = ret?.First()?.ToString();
                            if (message == "SUCCESS::调用成功")
                            {
                                loginUrl = string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                exception.Show();
            }
            return loginUrl;
        }
        private async Task<string> GetToken(string connectionId)
        {
            List<ExtensionsCookie> getCookies = await ChatHubService.GetCookies(connectionId, ".1688.com");
            if (getCookies == null || getCookies.Count <= 0) throw new Exception();
            ExtensionsCookie _m_h5_tk = getCookies.Find(x => x.name == "_m_h5_tk");
            if (_m_h5_tk == null)
            {
                List<ExtensionsCookie> h5ApiResult = await ChatHubService.GetCookies(connectionId, "h5api.m.taobao.com");
                if (h5ApiResult == null || h5ApiResult.Count <= 0) throw new Exception();
                _m_h5_tk = h5ApiResult.Find(x => x.name == "_m_h5_tk");
                if (_m_h5_tk == null) throw new Exception();
            }
            string token = _m_h5_tk.value?.Split("_")?.First();
            return token;
        }
        private string GetUserSimpleUrl(string token)
        {
            var parameters = new NameValueCollection();
            parameters["jsv"] = jsv;
            parameters["appKey"] = appKey;
            parameters["t"] = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds().ToString();
            parameters["api"] = "mtop.user.getUserSimple";
            parameters["v"] = "1.0";
            parameters["ecode"] = "1";
            parameters["sessionOption"] = "AutoLoginOnly";
            parameters["jsonpIncPrefix"] = "liblogin";
            parameters["type"] = "jsonp";
            parameters["dataType"] = "jsonp";
            parameters["callback"] = "mtopjsonpliblogin1";
            parameters["data"] = "{}";
            string sign = $"{token}&{parameters["t"]}&{parameters["appKey"]}&{parameters["data"]}";
            var computeMD5 = ComputeMD5(sign);
            parameters["sign"] = computeMD5.ToLower();
            string queryString = ToQueryString(parameters);
            return $"https://h5api.m.1688.com/h5/mtop.user.getusersimple/1.0/?{queryString}";
        }
        private string ComputeMD5(string text)
        {
            using (MD5 md5 = MD5.Create())
            {
                return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
            }
        }
        private string ToQueryString(NameValueCollection parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return "";
            var queryString = HttpUtility.ParseQueryString("");
            queryString.Add(parameters);

            return queryString.ToString();
        }

        private void CheckCaptcha(JToken data)
        {
            if (data == null) return;

            JToken ret = data.Value("ret");
            if (ret != null)
            {
                List<JToken> rets = ret.ToList();
                var find = rets.Find(x => x.ToString().Contains("FAIL_SYS_USER_VALIDATE"));
                if (find != null)
                {
                    string captchaUrl = data.Value("data").Value<string>("url");
                    if (string.IsNullOrWhiteSpace(captchaUrl) == false)
                    {
                        throw new CaptchaException()
                        {
                            CaptchaInfo = new CaptchaInfo
                            {
                                Message = "1688 페이지 수집 도중 캡차가 발생했습니다.",
                                Url = captchaUrl,
                            }
                        };
                    }
                }
            }
        }

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            UriBuilderAdvance inputUrl = new UriBuilderAdvance(url);
            var keywords = inputUrl.GetQuery("keywords");
            var featurePair = inputUrl.GetQuery("featurePair");

            var h5apiData = new H5ApiData();
            h5apiData.appId = 32517;

            GetOfferList getOfferList = new GetOfferList(page);
            getOfferList.spm = inputUrl.GetQuery("spm");
            if (string.IsNullOrWhiteSpace(keywords) == false)
            {
                getOfferList.keywords = keywords;
            }
            if (string.IsNullOrWhiteSpace(featurePair) == false)
            {
                getOfferList.featurePair = featurePair;
            }
            h5apiData.Params = getOfferList.ToParams();
            var paramsJson = h5apiData.ToSerializeObject();

            string token = await GetToken(connectionId);
            string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            string sign = CreateSign(token, timestamp, appKey, paramsJson);

            var uriBuilderAdvance = new UriBuilderAdvance("https://h5api.m.1688.com/h5/mtop.relationrecommend.wirelessrecommend.recommend/2.0/");
            uriBuilderAdvance.SetQuery("jsv", "2.7.4");
            uriBuilderAdvance.SetQuery("appKey", appKey);
            uriBuilderAdvance.SetQuery("t", timestamp);
            uriBuilderAdvance.SetQuery("sign", sign);
            uriBuilderAdvance.SetQuery("api", "mtop.relationrecommend.WirelessRecommend.recommend");
            uriBuilderAdvance.SetQuery("ignoreLogin", "true");
            uriBuilderAdvance.SetQuery("prefix", "h5api");
            uriBuilderAdvance.SetQuery("v", "2.0");
            uriBuilderAdvance.SetQuery("dataType", "jsonp");
            uriBuilderAdvance.SetQuery("jsonpIncPrefix", "fetchTpp_32517_getOfferList");
            uriBuilderAdvance.SetQuery("timeout", "20000");
            uriBuilderAdvance.SetQuery("type", "jsonp");
            uriBuilderAdvance.SetQuery("callback", "mtopjsonpfetchTpp_32517_getOfferList3");
            uriBuilderAdvance.SetQuery("data", paramsJson.ToHttpUrlEncoding());

            string requestUrl = uriBuilderAdvance.ToString();
            string mtopjson = await ChatHubService.GetRequest(connectionId, requestUrl);
            if (string.IsNullOrWhiteSpace(mtopjson))
            {
                throw new CaptchaException
                {
                    CaptchaInfo = new CaptchaInfo
                    {
                        Message = "1688 페이지 수집 중 캡차가 발생했습니다.\n해제 후 재수집해주세요.",
                        Url = url
                    }
                };
            }
            string json = mtopjson.SubstringBetween("(", ")", false);
            if (string.IsNullOrWhiteSpace(json)) return null;
            JObject responseData = json.ToJObject();
            if (responseData == null) { return null; }

            CheckCaptcha(responseData);

            List<JToken> items = responseData["data"]?["data"]?["OFFER"]?["items"]?.ToList();
            if (items == null || items.Count <= 0) return null;

            var collectPageInfos = new List<CollectPageInfo>();
            foreach (JToken item in items)
            {
                JToken data = item["data"];
                if (data == null || data.HasValues == false) continue;
                string offerId = data.Value<string>("offerId");
                if (string.IsNullOrWhiteSpace(offerId))
                {
                    continue;
                }
                var collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = offerId;
                collectPageInfo.Url = $"https://detail.1688.com/offer/{offerId}.html";
                //collectPageInfo.Title = title;
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }
        public string CreateSign(string token, string timestamp, string appId, string paramsJson)
        {
            string join = string.Join("&", token, timestamp, appId, paramsJson);
            var sign = NewComputeMD5(join);
            return sign;
        }
        private string NewComputeMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input); // JS와 동일하게 UTF-8 사용
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // 16진수 문자열로 변환 (소문자)
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString(); // 32자리 소문자 해시
            }
        }

        public override void ThrowIfCaptcha(string url, string html)
        {
            List<string> scripts = html.GetValues("//script", TextEx.XPathOption.InnerHtml);
            string window_config = scripts.Find(x => x.Contains("window._config_"));
            if (string.IsNullOrWhiteSpace(window_config)) return;
            string captchaJson = window_config.SubstringBetween("window._config_ = {", "};", false);
            if (string.IsNullOrWhiteSpace(captchaJson)) throw new Exception("1688사이트 수집중 알 수 없는 오류가 발생했습니다.\n관리자에게 문의 해주세요.");
            captchaJson = $"{{{captchaJson}}}";
            JObject captcha = captchaJson.ToJObject();
            if (captcha == null) throw new Exception("1688사이트 수집중 알 수 없는 오류가 발생했습니다.\n관리자에게 문의 해주세요.");
            string action = captcha.Value<string>("action");
            if (action == "captcha")
            {
                throw new CaptchaException("1688 상품 수집 중 캡차가 발생했습니다.\n해제 후 재수집해주세요.")
                {
                    CaptchaInfo = new CaptchaInfo()
                    {
                        Message = "1688 상품 수집 중 캡차가 발생했습니다.\n해제 후 재수집해주세요.",
                        Url = url
                    }
                };
            }
        }

        public override string GetCode(string url)
        {
            var uri = new Uri(url);
            string segment = uri.Segments?.Last();
            if (string.IsNullOrWhiteSpace(segment)) return null;
            int index = segment.IndexOf(".");
            if (index < 0) return null;
            string code = segment.Substring(0, index);
            return code;
        }

        public override string CreateProductUrl(string code)
        {
            return $"https://detail.1688.com/offer/{code}.html";
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            if (html.Contains("FAIL_SYS_USER_VALIDATE"))
            {
                JObject res = html.ToJObject();
                CheckCaptcha(res);
            }

            string json = html.SingleSplit("window.__INIT_DATA=", "</script>");
            if (string.IsNullOrWhiteSpace(json))
            {
                List<string> scripts = html.GetValues("//script", TextEx.XPathOption.OuterHtml);
                var contextPath = scripts.Find(x => x.Contains("window.contextPath"));
                json = contextPath.SubstringBetween("(window.contextPath,", ");", false);
            }

            JObject data = json.ToJObject();
            if (data == null) throw new Exception("상품 데이터를 찾을 수 없습니다.");

            var detailUrl = data["result"]?["data"]?["description"]?["fields"]?.Value<string>("detailUrl");
            if (string.IsNullOrWhiteSpace(detailUrl) == false)
            {
                string detailJson = await ChatHubService.GetRequest(connectionId, detailUrl);
                if (string.IsNullOrWhiteSpace(detailJson) == false)
                {
                    detailJson = detailJson.SubstringBetween("{", "}");
                    JObject datailData = detailJson.ToJObject();
                    data["detailData"] = datailData;
                }
            }
            return data;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string subject = data["result"]?["data"]?["gallery"]?["fields"]?.Value<string>("subject");
            if (string.IsNullOrWhiteSpace(subject)) throw new Exception("상품명을 찾을 수 없습니다.");
            List<Name> title = new List<Name>() { new Name(Language.CN, subject) };
            return title;
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
            List<JToken> featureAttributes = data["result"]?["global"]?["globalData"]?["model"]?["offerDetail"]?["featureAttributes"]?.ToList();
            if (featureAttributes == null || featureAttributes.Count <= 0) return null;
            List<Desc> descs = new List<Desc>();
            for (int i = 0; i < featureAttributes.Count; i++)
            {
                JToken featureAttribute = featureAttributes[i];
                string name = featureAttribute.Value<string>("name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                string value = null;
                List<JToken> values = featureAttribute["values"]?.ToList();
                if (values == null || values.Count <= 0)
                {
                    value = featureAttribute.Value<string>("value");
                }
                else
                {
                    value = string.Join(", ", values);
                }
                Desc desc = new Desc(i + 1, Language.CN, name, value);
                descs.Add(desc);
            }
            return descs;
        }

        public override List<Tag> GetTags(string html, JToken data)
        {
            return null;
        }

        public override List<Price> GetPrice(string html, JToken data)
        {
            var priceString = data["result"]?["data"]?["shippingServices"]?["fields"]?.Value<string>("price");
            if (string.IsNullOrWhiteSpace(priceString)) return null;
            double? originalPrice = priceString.ParseToDouble();
            List<Price> price = new List<Price>();

            JToken disPriceRanges = data?["result"]?["global"]?["globalData"]?["model"]?["tradeModel"]?["disPriceRanges"];
            if (disPriceRanges != null && disPriceRanges.HasValues)
            {
                List<JToken> disPriceRangeList = disPriceRanges?.ToList();
                if (disPriceRangeList != null && disPriceRangeList.Count > 0)
                {
                    disPriceRangeList = disPriceRangeList?.OrderBy(x => x.Value<int>("beginAmount"))?.ToList();
                    var disPriceRange = disPriceRangeList.FirstOrDefault();
                    double? discountPrice = disPriceRange?.Value<string>("discountPrice")?.ParseToDouble();
                    int? beginAmount = disPriceRange?.Value<int>("beginAmount");
                    if (beginAmount != null && beginAmount > 1)
                    {
                        discountPrice *= beginAmount;
                    }
                    if (discountPrice != null && discountPrice > originalPrice)
                    {
                        originalPrice = discountPrice;
                    }
                }
            }
            price.Add(new Price(Currency.CNY, (double)originalPrice, (double)originalPrice, true));
            return price;
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            List<JToken> offerImgList = data["result"]?["data"]?["gallery"]?["fields"]?["offerImgList"].ToList();
            if (offerImgList == null || offerImgList.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            List<string> mainImages = new List<string>();
            foreach (var offerImg in offerImgList)
            {
                mainImages.Add(offerImg.ToString());
            }
            return mainImages;
        }

        public override string GetVideoUrl(string html, JToken data)
        {
            try
            {
                var videoUrl = data["result"]?["data"]?["gallery"]?["fields"]?["video"]?.Value<string>("videoUrl");
                return videoUrl;
            }
            catch
            {
                return null;
            }
        }

        public override string GetDetailPageHtml(string html, JToken data)
        {
            string content = data.Value("detailData")?.Value<string>("content");
            if (string.IsNullOrWhiteSpace(content)) return null;
            var detailHtml = HtmlReader.GetDafaultHtml(content);
            return detailHtml;
        }

        public override Option GetOption(string html, JToken data)
        {
            List<JToken> skuProps = data["result"]?["data"]?["Root"]?["fields"]?["dataJson"]?["skuModel"]?["skuProps"]?.ToList();
            if (skuProps == null || skuProps.Count <= 0) return null;
            try
            {
                Option option = new Option();
                option.optionGroups = new List<Option.OptionGroup>();
                for (int i = 0; i < skuProps.Count; i++)
                {
                    var optionGroup = new Option.OptionGroup();
                    JToken skuProp = skuProps[i];
                    var prop = skuProp.Value<string>("prop");
                    optionGroup.optionId = skuProp.Value<string>("fid");
                    optionGroup.groupName = new List<Name>() { new Name(Language.CN, prop) };
                    List<JToken> values = skuProp["value"]?.ToList();
                    if (values == null || values.Count <= 0) throw new Exception();
                    optionGroup.groups = new List<Option.OptionGroup.Group>();
                    for (int j = 0; j < values.Count; j++)
                    {
                        JToken value = values[j];
                        var group = new Option.OptionGroup.Group();
                        group.groupId = j.ToString();
                        string name = value.Value<string>("name");
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        group.optionName = new List<Name>() { new Name(Language.CN, name) };
                        group.optionImage = value.Value<string>("imageUrl");
                        optionGroup.groups.Add(group);
                    }
                    option.optionGroups.Add(optionGroup);
                }
                string getOptionGroupName = ProductHelper.GetOptionGroupName(option.optionGroups);
                JToken skuInfoMap = data["result"]?["data"]?["Root"]?["fields"]?["dataJson"]?["skuModel"]?["skuInfoMap"];
                if (skuInfoMap == null || skuInfoMap.HasValues == false) throw new Exception();
                option.detailOptions = new List<Option.DetailOption>();
                JEnumerable<JToken> children = skuInfoMap.Children();
                foreach (JToken child in children)
                {
                    JToken first = child.First();
                    var specAttrs = first.Value<string>("specAttrs");
                    string optionName = specAttrs.Replace("&gt;", "ㅡ");
                    string[] optNames = optionName.Split("ㅡ");
                    List<string> ids = new List<string>();
                    foreach (var optName in optNames)
                    {
                        var groups = option.optionGroups.SelectMany(x => x.groups).ToList();
                        Option.OptionGroup.Group find = groups.Find(x => x.optionName.First().text == optName);
                        if (find == null) throw new Exception();
                        ids.Add(find.groupId);
                    }
                    string skuId = first.Value<string>("skuId");
                    ids.Add(skuId);
                    string price = first.Value<string>("price");
                    if (string.IsNullOrWhiteSpace(price))
                    {
                        price = data["result"]?["data"]?["Root"]?["fields"]?["dataJson"]?["skuModel"].Value<string>("skuPriceScale");
                        if (string.IsNullOrWhiteSpace(price) == false && price.Contains("-"))
                        {
                            price = price.Split("-").Last(); // 범위 가격일 경우 최대 가격으로 설정
                        }
                    }
                    string discountPrice = first.Value<string>("discountPrice");
                    int? canBookCount = first.Value<int?>("canBookCount");

                    double? originalPrice = price.ParseToDouble();
                    if (originalPrice == null)
                    {
                        throw new Exception();
                    }
                    double? salePrice = discountPrice.ParseToDouble();
                    if (salePrice == null || salePrice <= 0)
                    {
                        salePrice = originalPrice;
                    }

                    Option.DetailOption detailOption = new Option.DetailOption();
                    detailOption.optionIds = string.Join("ㅡ", ids);
                    detailOption.optionGroupName = new List<Name>() { new Name(Language.CN, getOptionGroupName) };
                    detailOption.optionName = new List<Name>() { new Name(Language.CN, optionName) };
                    detailOption.price = new List<Price>() { new Price(Currency.CNY, (double)originalPrice, (double)salePrice) };
                    if (canBookCount == null || canBookCount <= 0)
                    {
                        detailOption.isSoldOut = true;
                        detailOption.quantity = 0;
                    }
                    else
                    {
                        detailOption.isSoldOut = false;
                        detailOption.quantity = (int)canBookCount;
                    }
                    option.detailOptions.Add(detailOption);
                }
                option.detailOptions = option.detailOptions.OrderBy(x => x.optionIds).ToList();
                return option;
            }
            catch (Exception exception)
            {
                throw new Exception($"옵션 수집 중 문제가 발생했습니다.{exception.Message}", exception);
            }
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {

        }

        public override int? GetTotalQuantity(string html, JToken data)
        {
            return null;
        }

        public override async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            Product product = new Product();
            try
            {
                ThrowIfCaptcha(url, html);
                var data = await GetData(connectionId, url, html);
                product.site = this.Site;
                product.code = GetCode(url);
                product.url = CreateProductUrl(product.code);
                product.currency = Currency.CNY.ToString();
                product.title = GetTitle(html, data);
                product.descs = GetDescs(html, data);
                product.tags = GetTags(html, data);
                product.price = GetPrice(html, data);
                product.mainImages = GetMainImages(html, data);
                product.videoUrl = GetVideoUrl(html, data);
                product.detailPageHtml = GetDetailPageHtml(html, data);
                product.option = GetOption(html, data);
                product.regDate = DateTime.Now;
                await ApplyCollectOptionSetting(connectionId, new CollectRawData(url, data, html), product, collectOptionSetting);
            }
            catch (CaptchaException captchaException)
            {
                captchaException.CaptchaInfo = new CaptchaInfo()
                {
                    Message = captchaException.Message,
                    Url = url
                };
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
            string html = await ChatHubService.GetRequest(connectionId, url);
            var data = await GetData(connectionId, url, html);
            return new CollectRawData(url, data, html);
        }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            JToken disPriceRanges = rawData.Data?["result"]?["global"]?["globalData"]?["model"]?["tradeModel"]?["disPriceRanges"];
            if (disPriceRanges != null)
            {
                List<JToken> disPriceRangeList = disPriceRanges?.ToList();
                disPriceRangeList = disPriceRangeList?.OrderBy(x => x.Value<int>("beginAmount"))?.ToList();
                var disPriceRange = disPriceRangeList.FirstOrDefault();
                double? discountPrice = disPriceRange?.Value<string>("discountPrice")?.ParseToDouble();
                int? beginAmount = disPriceRange?.Value<int>("beginAmount");
                if (beginAmount != null && beginAmount > 1)
                {
                    var minOrderQuantity = (int)beginAmount;

                    product.title.First().text += $" ({minOrderQuantity}个)";

                    product.price.First().salePrice *= minOrderQuantity;
                    product.price.First().originalPrice *= minOrderQuantity;

                    if (product.option != null)
                    {
                        foreach (var detailOption in product.option.detailOptions)
                        {
                            detailOption.price.First().salePrice *= minOrderQuantity;
                            detailOption.price.First().originalPrice *= minOrderQuantity;
                        }
                    }
                }
            }
        }
    }
}
