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
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BrowserScrapingBridge.Server.Sourcing.Alibaba
{
    public class Taobao : ICollect
    {
        private string jsv = "2.7.2";
        private string appKey = "12574478";
        private Dictionary<string, string> DefaultHeader = new Dictionary<string, string>
        {
            { "Accept", "*/*" },
            { "Accept-Language", "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7,ja;q=0.6" },
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36" }
        };

        public string Site => nameof(Taobao);
        public string Name => "타오바오";
        public Language Language => Language.CN;
        public Currency Currency => Currency.CNY;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public string GetCode(string url)
        {
            var ub = new UriBuilderAdvance(url);
            var code = ub.GetQuery("id");
            return code;
        }

        private string GetParamValue(string url, string key)
        {
            var urlArr = url.Split('?');
            if (urlArr.Length == 2)
            {
                var param = urlArr[1];
                var paramArr = param.Split('&');
                foreach (var data in paramArr)
                {
                    if (data.Split('=')[0] == key)
                    {
                        return data.Split('=')[1];
                    }
                }
            }
            return "";
        }

        public string GetProductList(string token, string url, int page)
        {
            var keyword = GetParamValue(url, "q");
            var parameters = new NameValueCollection();
            parameters["jsv"] = "2.6.2";
            parameters["appKey"] = "12574478";
            parameters["t"] = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds().ToString();
            parameters["api"] = "mtop.relationrecommend.WirelessRecommend.recommend";
            parameters["v"] = "2.0";
            parameters["type"] = "jsonp";
            parameters["dataType"] = "jsonp";
            parameters["callback"] = "mtopjsonp" + page;

            var taobaoModel = new Model.GetTaobaoH5UrlModel();
            taobaoModel.appId = "34385";
            var taobaoJson = new Model.GetTaobaoH5UrlModel.TaobaoJson();
            taobaoJson.device = "HMA-AL00";
            taobaoJson.isBeta = "false";
            taobaoJson.grayHair = "false";
            taobaoJson.from = "nt_history";
            taobaoJson.brand = "HUAWEI";
            taobaoJson.info = "wifi";
            taobaoJson.index = "4";
            taobaoJson.rainbow = "";
            taobaoJson.schemaType = "auction";
            taobaoJson.elderHome = "false";
            taobaoJson.isEnterSrpSearch = "true";
            taobaoJson.newSearch = "false";
            taobaoJson.network = "wifi";
            taobaoJson.subtype = "";
            taobaoJson.hasPreposeFilter = "false";
            taobaoJson.prepositionVersion = "v2";
            taobaoJson.client_os = "Android";
            taobaoJson.gpsEnabled = "false";
            taobaoJson.searchDoorFrom = "false";
            taobaoJson.debug_rerankNewOpenCard = "false";
            taobaoJson.homePageVersion = "v7";
            taobaoJson.searchElderHomeOpen = "false";
            taobaoJson.search_action = "initiative";
            taobaoJson.sugg = "_4_1";
            taobaoJson.sversion = "13.6";
            taobaoJson.style = "list";
            taobaoJson.ttid = "600000@taobaoJson_pc_10.7.0";
            taobaoJson.needTabs = "true";
            taobaoJson.areaCode = "CN";
            taobaoJson.vm = "nw";
            taobaoJson.countryNum = "156";
            taobaoJson.m = "pc";
            taobaoJson.page = page;
            taobaoJson.n = 48;
            taobaoJson.q = keyword;
            taobaoJson.tab = "all";
            taobaoJson.pageSize = "48";
            taobaoJson.totalPage = "100";
            taobaoJson.totalResults = "4800";
            taobaoJson.sourceS = "0";
            taobaoJson.sort = "_coefp";
            taobaoJson.bcoffset = "-3";
            taobaoJson.ntoffset = "3";
            taobaoJson.filterTag = "";
            taobaoJson.service = "";
            taobaoJson.prop = "";
            taobaoJson.loc = "";
            taobaoJson.start_price = null;
            taobaoJson.end_price = null;
            taobaoJson.startPrice = null;
            taobaoJson.endPrice = null;
            taobaoModel.json = taobaoJson.ToSerializeObject();

            parameters["data"] = taobaoModel.ToSerializeObject();
            parameters["data"] = parameters["data"].Replace("\"json\"", "\"params\"");
            parameters["sign"] = ($"{token}&{parameters["t"]}&{parameters["appKey"]}&{parameters["data"]}").ComputeMD5().ToLower();

            string queryString = parameters.ToQueryString();
            return $"https://h5api.m.taobao.com/h5/mtop.relationrecommend.wirelessrecommend.recommend/2.0/?{queryString}";
        }

        private List<string> GetTaobaoItemList(string html, int page)
        {
            string parse = $"{html.SingleSplit($"mtopjsonp{page}(", "})")}" + "}";
            JObject json = JObject.Parse(parse);
            JArray itemsArray = (JArray)json["data"]["itemsArray"];
            var result = itemsArray.Where(x => x["item_id"] != null).Select(x => x["item_id"].ToString()).ToList();
            result = result.Distinct().ToList();
            return result;
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            string token = await GetTokenAsync(connectionId);
            string requestUrl = GetProductList(token, url, page);
            string getTaobaoHtml = await ChatHubService.Request(connectionId, "GetBrowserHtml", requestUrl, DefaultHeader);
            var getTaobaoItemList = GetTaobaoItemList(getTaobaoHtml, page);
            if (getTaobaoItemList == null || getTaobaoItemList.Count <= 0) return null;
            var collectPageInfos = new List<CollectPageInfo>();
            foreach (string taobaoItemCode in getTaobaoItemList)
            {
                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = taobaoItemCode;
                collectPageInfo.Url = $"https://item.taobao.com/item.htm?id={taobaoItemCode}";
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
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
            if (parameters == null || parameters.Count == 0) return "";
            var queryString = HttpUtility.ParseQueryString("");
            queryString.Add(parameters);

            return queryString.ToString();
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
            parameters["callback"] = "mtopjsonpliblogin2";
            parameters["data"] = "{}";
            var signString = $"{token}&{parameters["t"]}&{parameters["appKey"]}&{parameters["data"]}";
            parameters["sign"] = ComputeMD5(signString).ToLower();
            string queryString = ToQueryString(parameters);
            return $"https://h5api.m.taobao.com/h5/mtop.user.getusersimple/1.0/?{queryString}";
        }

        private async Task<string> GetTokenAsync(string connectionId)
        {
            string getTaobaoCookie = await ChatHubService.ExtensionsCookie(connectionId, ".taobao.com");
            if (string.IsNullOrWhiteSpace(getTaobaoCookie)) return null;
            List<JToken> taobaoCookies = getTaobaoCookie?.ToJArray()?.ToList();
            if (taobaoCookies == null || taobaoCookies.Count <= 0) return null;
            JToken _m_h5_tk = taobaoCookies.Find(x => x.Value<string>("name") == "_m_h5_tk");
            if (_m_h5_tk == null || _m_h5_tk.HasValues == false) return null;
            string token = _m_h5_tk.Value<string>("value")?.Split("_")?.First();
            return token;
        }

        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            string loginUrl = "https://login.taobao.com/member/login.jhtml?redirectURL=https%3A%2F%2Fworld.taobao.com";

            string token = await GetTokenAsync(connectionId);
            if (string.IsNullOrWhiteSpace(token)) return loginUrl;

            string userSimpleUrl = GetUserSimpleUrl(token);
            string html = await ChatHubService.GetRequest(connectionId, userSimpleUrl, DefaultHeader);
            if (string.IsNullOrWhiteSpace(html) == false)
            {
                if (html.Contains("SUCCESS::调用成功"))
                {
                    loginUrl = null;
                }
            }
            return loginUrl;
        }

        public async Task<string> GetItem(string itemId, string apiToken)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10,
                    AutomaticDecompression =
                        DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                using (var httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler))
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    for (int attempt = 1; attempt <= 2; attempt++)
                    {
                        using (var request = new HttpRequestMessage(
                            HttpMethod.Get,
                            $"http://api.tmapi.top/taobao/item_detail?apiToken={apiToken}&item_id={itemId}"
                        ))
                        {
                            request.Version = HttpVersion.Version11;
                            request.Headers.Accept.ParseAdd("*/*");
                            request.Headers.Connection.Add("keep-alive");
                            request.Headers.CacheControl =
                                new System.Net.Http.Headers.CacheControlHeaderValue
                                {
                                    MaxAge = TimeSpan.Zero
                                };

                            using (var response = await httpClient.SendAsync(request))
                            {
                                var result = await response.Content.ReadAsStringAsync();


                                // Get item description error가 포함되어 있으면 재시도
                                if (!string.IsNullOrEmpty(result) && !result.Contains("Get item description error"))
                                    return result;
                            }
                        }

                        // 첫 시도 실패 시에만 재시도
                        if (attempt == 1)
                        {
                            await Task.Delay(500); // 0.5초 쉬고 재시도 (선택)
                        }
                    }
                }

                return null;
            }
            catch (TaskCanceledException ex) // timeout
            {
                ex.Show();
                return null;
            }
            catch (HttpRequestException ex)
            {
                ex.Show();
                return null;
            }
            catch (Exception ex)
            {
                ex.Show();
                return null;
            }

        }

        public async Task<string> GetDetailPage(string itemId, string apiToken, string type = "pc")
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10,
                    AutomaticDecompression =
                        DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                using (var httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler))
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    for (int attempt = 1; attempt <= 2; attempt++)
                    {
                        using (var request = new HttpRequestMessage(
                            HttpMethod.Get,
                            $"http://api.tmapi.top/taobao/item_desc?apiToken={apiToken}&item_id={itemId}"
                        ))
                        {
                            request.Version = HttpVersion.Version11;
                            request.Headers.Accept.ParseAdd("*/*");
                            request.Headers.Connection.Add("keep-alive");
                            request.Headers.CacheControl =
                                new System.Net.Http.Headers.CacheControlHeaderValue
                                {
                                    MaxAge = TimeSpan.Zero
                                };

                            using (var response = await httpClient.SendAsync(request))
                            {
                                var result = await response.Content.ReadAsStringAsync();

                                // Get item description error가 포함되어 있으면 재시도
                                if (!string.IsNullOrEmpty(result) && !result.Contains("Get item description error"))
                                    return result;
                            }
                        }

                        // 첫 실패 후에만 재시도
                        if (attempt == 1)
                            await Task.Delay(500);
                    }
                }

                return null;
            }
            catch (TaskCanceledException ex) // timeout
            {
                ex.Show();
                return null;
            }
            catch (HttpRequestException ex)
            {
                ex.Show();
                return null;
            }
            catch (Exception ex)
            {
                ex.Show();
                return null;
            }

        }

        public async Task<JToken> GetData(string code)
        {
            string privateAPIToken = SourcingExecution.Current.GetSetting("taobaoApiToken") ?? throw new ArgumentException("apiSettings.taobaoApiToken이 필요합니다.");
            string itemJson = await GetItem(code, privateAPIToken);
            string detailPage = await GetDetailPage(code, privateAPIToken);

            var itemData = itemJson.ToJObject();
            string msg = itemData.Value<string>("msg");
            if (msg != "success") throw new Exception("상품 정보를 찾을 수 없습니다.");

            var detailPageData = detailPage.ToJObject();
            msg = detailPageData.Value<string>("msg");
            if (msg != "success") throw new Exception("상품 상세페이지 정보를 찾을 수 없습니다.");

            var data = itemData.Value("data") as JObject;
            var datailData = detailPageData.Value("data");
            if (datailData != null)
            {
                data.Merge(datailData);
            }
            return data;
        }

        public List<Name> GetTitle(JToken data)
        {
            string title = data.Value<string>("title");
            if (string.IsNullOrWhiteSpace(title)) throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(this.Language, title);
        }

        public List<Name> GetBrand(JToken data)
        {
            var shop_name = data.Value("shop_info")?.Value<string>("shop_name");
            if (string.IsNullOrWhiteSpace(shop_name)) return null;
            return ProductHelper.CreateName(this.Language, shop_name);
        }

        public List<Desc> GetDescs(JToken data)
        {
            List<JToken> product_props = data.Value("product_props")?.ToList();
            if (product_props == null) return null;
            List<Desc> descs = new List<Desc>();

            int index = 1;
            foreach (JToken product_prop in product_props)
            {
                string path = product_prop.First().Path;
                string indexTxt = "].";
                int lastIndexOf = path.LastIndexOf(indexTxt);
                string key = path.Substring(lastIndexOf + indexTxt.Length);
                if (string.IsNullOrWhiteSpace(key)) continue;
                string value = product_prop.Value<string>(key);
                if (string.IsNullOrWhiteSpace(value)) continue;
                Desc desc = new Desc(index, this.Language, key, value);
                descs.Add(desc);
                index++;
            }
            return descs;
        }

        public List<Price> GetPrice(JToken data)
        {
            JToken price_info = data.Value("price_info");
            if (price_info == null) throw new Exception("가격 정보를 찾을 수 없습니다.");
            double? salePrice = price_info.Value<double?>("price");
            double? originalPrice = price_info.Value<double?>("origin_price");
            if (salePrice == null && originalPrice == null) throw new Exception("가격 정보를 찾을 수 없습니다.");
            if (originalPrice == null) originalPrice = salePrice;
            return ProductHelper.CreatePrice(this.Currency, originalPrice, salePrice);
        }

        public List<string> GetMainImages(JToken data)
        {
            List<JToken> main_imgs = data.Value("main_imgs")?.ToList();
            if (main_imgs == null || main_imgs.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            var mainImages = new List<string>();
            foreach (var main_img in main_imgs)
            {
                var imageUrl = main_img.ToString();
                if (string.IsNullOrWhiteSpace(imageUrl)) continue;
                mainImages.Add(imageUrl);
            }
            if (mainImages.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            return mainImages;
        }

        public string GetVideoUrl(JToken data)
        {
            return data.Value<string>("video_url");
        }

        public string GetDetailPageHtml(JToken data)
        {
            var detail_html = data.Value<string>("detail_html");
            if (string.IsNullOrWhiteSpace(detail_html)) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            var dafaultHtml = HtmlReader.GetDafaultHtml(detail_html);
            List<string> srcs = dafaultHtml.GetValues("//img", "src");
            srcs = srcs.Distinct().ToList();
            foreach (var src in srcs)
            {
                string http = "https:";
                if (src.IndexOf(http) != 0)
                {
                    string replaceUrl = $"{http}{src}";
                    dafaultHtml = dafaultHtml.Replace(src, replaceUrl);
                }
            }
            return dafaultHtml;
        }

        public Option GetOption(JToken data)
        {
            JToken sku_props = data.Value("sku_props");
            JToken skus = data.Value("skus");
            if (sku_props == null || sku_props.HasValues == false || skus == null || skus.HasValues == false) return null;

            Option option = new Option();
            option.optionGroups = new List<Option.OptionGroup>();
            foreach (JToken sku_prop in sku_props.ToList())
            {
                string pid = sku_prop.Value<string>("pid");
                if (string.IsNullOrWhiteSpace(pid)) continue;
                string prop_name = sku_prop.Value<string>("prop_name");
                if (string.IsNullOrWhiteSpace(prop_name)) continue;

                List<JToken> values = sku_prop.Value("values")?.ToList();
                if (values == null || values.Count <= 0) continue;
                var groups = new List<Option.OptionGroup.Group>();
                foreach (JToken value in values)
                {
                    var vid = value.Value<string>("vid");
                    var name = value.Value<string>("name");
                    var imageUrl = value.Value<string>("imageUrl");

                    var group = new Option.OptionGroup.Group();
                    group.groupId = $"{pid}:{vid}";
                    group.optionName = ProductHelper.CreateName(this.Language, name);
                    group.optionImage = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl;
                    groups.Add(group);
                }
                if (groups.Count <= 0) continue;
                var optionGroup = new Option.OptionGroup();
                optionGroup.optionId = pid;
                optionGroup.groupName = ProductHelper.CreateName(this.Language, prop_name);
                optionGroup.groups = groups;
                option.optionGroups.Add(optionGroup);
            }

            var optionGroupName = ProductHelper.GetOptionGroupName(option.optionGroups);

            option.detailOptions = new List<Option.DetailOption>();
            foreach (JToken sku in skus.ToList())
            {
                string skuid = sku.Value<string>("skuid");
                if (string.IsNullOrWhiteSpace(skuid)) continue;

                string props_ids = sku.Value<string>("props_ids");
                if (string.IsNullOrWhiteSpace(props_ids)) continue;

                var codes = props_ids.Split(";").ToList();
                codes.Add(skuid);

                string props_names = sku.Value<string>("props_names");
                if (string.IsNullOrWhiteSpace(props_names)) continue;

                var optionValues = new List<string>();
                List<string> props_nameList = props_names.Split(";")?.ToList();
                if (props_nameList == null || props_nameList.Count <= 0) continue;
                foreach (string props_name in props_nameList)
                {
                    var indexOf = props_name.IndexOf(":");
                    if (indexOf <= 0) continue;
                    var optionValue = props_name.Substring(indexOf + 1);
                    if (string.IsNullOrWhiteSpace(optionValue)) continue;
                    optionValues.Add(optionValue);
                }
                if (optionValues.Count <= 0) continue;
                string optionName = string.Join("ㅡ", optionValues);

                string sale_price = sku.Value<string>("sale_price");
                if (string.IsNullOrWhiteSpace(sale_price)) continue;
                string origin_price = sku.Value<string>("origin_price");
                if (string.IsNullOrWhiteSpace(origin_price))
                {
                    origin_price = sale_price;
                }
                double? salePrice = sale_price.ParseToDouble();
                double? originalPrice = origin_price.ParseToDouble();

                int? stock = sku.Value<int?>("stock");

                var detailOption = new Option.DetailOption();
                detailOption.optionIds = string.Join("ㅡ", codes);
                detailOption.optionGroupName = ProductHelper.CreateName(this.Language, optionGroupName);
                detailOption.optionName = ProductHelper.CreateName(this.Language, optionName);
                detailOption.price = ProductHelper.CreatePrice(this.Currency, originalPrice, salePrice);
                detailOption.quantity = stock == null ? 0 : (int)stock;
                detailOption.isSoldOut = detailOption.quantity <= 0;
                option.detailOptions.Add(detailOption);
            }
            if (option.optionGroups.Count <= 0) return null;
            if (option.detailOptions.Count <= 0) return null;
            return option;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var product = new Product();
            try
            {
                string code = GetCode(url);
                JToken data = await GetData(code);
                product.site = this.Site;
                product.code = code;
                product.url = url;
                product.currency = this.Currency.ToString();
                product.title = GetTitle(data);
                product.brand = GetBrand(data);
                product.descs = GetDescs(data);
                product.price = GetPrice(data);
                product.mainImages = GetMainImages(data);
                product.videoUrl = GetVideoUrl(data);
                product.detailPageHtml = GetDetailPageHtml(data);
                product.option = GetOption(data);
            }
            catch (Exception e)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = e.Message;
            }
            return product;
        }

        public async Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            string code = GetCode(url);
            JToken data = await GetData(code);
            return new CollectRawData { Url = url, Data = data };
        }

        public Product GetProduct(CollectRawData rawData)
        {
            var product = new Product();
            try
            {
                string code = GetCode(rawData.Url);
                product.site = this.Site;
                product.code = code;
                product.url = rawData.Url;
                product.currency = this.Currency.ToString();
                product.title = GetTitle(rawData.Data);
                product.brand = GetBrand(rawData.Data);
                product.descs = GetDescs(rawData.Data);
                product.price = GetPrice(rawData.Data);
                product.mainImages = GetMainImages(rawData.Data);
                product.videoUrl = GetVideoUrl(rawData.Data);
                product.detailPageHtml = GetDetailPageHtml(rawData.Data);
                product.option = GetOption(rawData.Data);
            }
            catch (Exception e)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = e.Message;
            }
            return product;
        }

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            return;
        }
    }
}
