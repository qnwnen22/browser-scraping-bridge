#nullable disable warnings
using HtmlAgilityPack;
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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.VVIC
{
    public class VVIC : ICollect
    {
        public string Site => nameof(VVIC);
        public string Name => nameof(VVIC);
        public Language Language { get; set; } = Language.CN;
        public Currency Currency { get; set; } = Currency.CNY;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        public string GetCode(string url)
        {
            var uri = new Uri(url);
            return uri.Segments.Last();
        }
        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            string html = await ChatHubService.GetRequest(connectionId, "https://www.vvic.com/apic/user/login/info?user_key=&city=gz");
            if (string.IsNullOrWhiteSpace(html)) throw new Exception("소켓 연결이 끊겼습니다. 새로고침을 진행해주세요.");
            string result = "https://www.vvic.com/login.html";
            try
            {
                JObject data = html.ToJObject();
                if (data != null)
                {
                    string? user = data["data"]?.Value<string>("userId");
                    if (string.IsNullOrWhiteSpace(user) == false && user != "0")
                    {
                        result = null;
                    }
                }
            }
            catch (Exception exception)
            {
                exception.Show();
            }
            return result;
        }
        public string GetPageUrl(string url, int page)
        {
            string result = null;

            var uriBuilderAdvance = new UriBuilderAdvance(url);
            var isUser = uriBuilderAdvance.Segments.Exists(x => x.Contains("user"));
            if (isUser)
            {
                uriBuilderAdvance.Segments = new List<string> { "/", "apic/", "user/", "favoriteData" };
            }
            else
            {
                bool isKor = uriBuilderAdvance.Segments.Contains("ko/");
                StringBuilder sb = new StringBuilder();
                string sort = uriBuilderAdvance.GetQuery("sort");
                if (string.IsNullOrWhiteSpace(sort) == false)
                {
                    if (sort == "default")
                    {
                        uriBuilderAdvance.DeleteQuery("sort");
                    }
                }
                //if (isKor)
                //{
                //    uriBuilderAdvance.SetQuery("merge", "0");
                //}
                //else
                //{
                //    uriBuilderAdvance.SetQuery("merge", "1");
                //}
                uriBuilderAdvance.SetQuery("merge", "1");

                string queryPath = "set-";
                string? set = uriBuilderAdvance.Segments.Find(x => x.Contains(queryPath));
                string[]? sets = set?.Split("_");
                if (string.IsNullOrWhiteSpace(set) == false)
                {
                    uriBuilderAdvance.SetQuery("algo", "0");
                    int? indexOf = sets?[0]?.IndexOf(queryPath);
                    string? pid = sets?[0]?.Substring((int)indexOf + queryPath.Length);
                    if (pid != "0") uriBuilderAdvance.SetQuery("pid", pid);
                    string? tagid = sets?[1];
                    if (tagid != "0") uriBuilderAdvance.SetQuery($"tagid", tagid);
                    string? vcid = sets?[2]?.Split("-")?.First();
                    if (vcid != "0") uriBuilderAdvance.SetQuery("vcid", vcid);
                }
                else
                {
                    uriBuilderAdvance.SetQuery("algo", "false");
                }

                string queryPath2 = "collect-";
                string? collect = uriBuilderAdvance.Segments.Find(x => x.Contains(queryPath2));
                string[] collects = collect?.Split("-");
                if (collects.Length >= 5)
                {
                    var isSpFront = collects[4];
                    if (isSpFront == "1")
                    {
                        uriBuilderAdvance.SetQuery("isSpFront", isSpFront);
                    }
                }

                uriBuilderAdvance.SetQuery("currentPage", page.ToString());
                if (isKor)
                {
                    uriBuilderAdvance.SetQuery("goodsInStock", "1");
                    uriBuilderAdvance.SetQuery("lang", "ko");
                }
                else
                {
                    uriBuilderAdvance.SetQuery("lang", "cn");
                }
                uriBuilderAdvance.SetQuery("currency", "CNY");
                uriBuilderAdvance.Segments = new() { "/", "apif/", "searchPage" };
            }
            result = uriBuilderAdvance.ToString();
            return result;
        }

        private JToken TryGetData(JObject data, params string[] keys)
        {
            JToken result = data;
            foreach (string key in keys)
            {
                JToken current = result[key];
                if (current == null || current.HasValues == false) return null;
                result = current;
            }
            return result;
        }

        public List<CollectPageInfo> GetCollectPageInfos(string html)
        {
            JObject data = html.ToJObject();
            if (data == null) return null;
            string? statusCode = data.Value<string>("code");
            if (statusCode != "200")
            {
                string? message = data.Value<string>("message");

                throw new CaptchaException()
                {
                    CaptchaInfo = new CaptchaInfo
                    {
                        Url = "https://www.vvic.com/",
                        Message = message,
                    }
                };
            }

            List<string[]> keysList = new List<string[]>();
            keysList.Add(new string[] { "data", "search_page", "recordList" });
            keysList.Add(new string[] { "data", "ads_bottom" });
            keysList.Add(new string[] { "data", "page", "recordList" });

            JToken pageData = null;
            foreach (string[] keys in keysList)
            {
                pageData = TryGetData(data, keys);
                if (pageData != null) break;
            }

            List<JToken>? recordList = pageData?.ToList();
            if (recordList == null || recordList.Count <= 0) return null;

            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            foreach (JToken record in recordList)
            {
                string? vid = record.Value<string>("vid");
                if (string.IsNullOrWhiteSpace(vid)) continue;
                string itemUrl = $"https://www.vvic.com/item/{vid}";
                string? title = record.Value<string>("title");
                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = vid;
                collectPageInfo.Url = itemUrl;
                collectPageInfo.Title = title;
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }
        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            string pageUrl = GetPageUrl(url, page);
            string html = await ChatHubService.GetRequest(connectionId, pageUrl);
            return GetCollectPageInfos(html);
        }
        private string DecryptByDES(string encryptedBase64, string key = "item_detail_6493d374179f8c0008cdde7a", string iv = "vvicItem")
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] keys = keyBytes.ToList().GetRange(0, 8).ToArray();
            byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
            byte[] cipherBytes = Convert.FromBase64String(encryptedBase64);

            // DES.Create()로 대체
            using (var des = DES.Create())
            {
                des.Mode = CipherMode.CBC;
                des.Padding = PaddingMode.PKCS7;
                des.IV = ivBytes;
                des.Key = keys;

                using (ICryptoTransform decryptor = des.CreateDecryptor())
                {
                    byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }
        public List<Name> GetTitle(JToken data)
        {
            try
            {
                List<Name> names = new List<Name>();
                string? title = data["data"]?.Value<string>("title");
                names.Add(new Name(Language.CN, title));
                return names;
            }
            catch (Exception exception)
            {
                throw new Exception("상품명을 찾을 수 없습니다.", exception);
            }
        }
        public List<Name> GetBrand() => null;
        public List<Desc> GetDescs(JToken data)
        {
            try
            {
                List<Desc> descs = new List<Desc>();
                string? attrs = data["data"].Value<string>("attrs");
                List<string> attrList = attrs.Split(",").ToList();
                for (int i = 0; i < attrList.Count; i++)
                {
                    try
                    {
                        string attr = attrList[i];
                        string[] split = attr.Split(":");
                        string key = split[0].Trim();
                        if (string.IsNullOrWhiteSpace(key)) continue;
                        string value = split[1].Trim();
                        if (string.IsNullOrWhiteSpace(value)) continue;
                        Desc desc = new Desc(i + 1, Language.CN, key, value);
                        descs.Add(desc);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                return descs;
            }
            catch (Exception exception)
            {
                throw new Exception("상품 상세정보를 찾을 수 없습니다.", exception);
            }
        }
        public List<Price> GetPrice(JToken data)
        {
            try
            {
                string? basePriceString = data["data"]?.Value<string>("basePrice");
                string? baseDiscountPriceString = data["data"]?.Value<string>("baseDiscountPrice");
                if (string.IsNullOrWhiteSpace(basePriceString)) throw new Exception();
                double? basePrice = basePriceString.ParseToDouble();
                double? baseDiscountPrice = baseDiscountPriceString.ParseToDouble();
                if (basePrice == null || basePrice <= 0) throw new Exception();
                if (baseDiscountPrice == null || baseDiscountPrice <= 0)
                {
                    baseDiscountPrice = basePrice;
                }
                Price price = new Price(Currency.CNY, (double)basePrice, (double)baseDiscountPrice, true);
                List<Price> prices = new List<Price>();
                prices.Add(price);
                return prices;
            }
            catch (Exception exception)
            {
                throw new Exception("가격 정보를 확인할 수 없습니다.", exception);

            }
        }
        private string GetUrl(string url)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            string? find = uriBuilderAdvance.Segments.Find(x => x.Contains("ko"));
            if (string.IsNullOrWhiteSpace(find) == false)
            {
                uriBuilderAdvance.Segments.RemoveAll(x => x.Contains("ko"));
                url = uriBuilderAdvance.ToString();
            }
            return url;
        }
        public List<string> GetMainImages(JToken data)
        {
            try
            {
                string? imgsString = data["data"].Value<string>("imgs");
                if (string.IsNullOrWhiteSpace(imgsString)) throw new Exception();
                string[] imgs = imgsString.Split(",");
                if (imgs.Length <= 0) throw new Exception();

                List<string> images = new List<string>();
                for (int i = 0; i < imgs.Length; i++)
                {
                    string img = imgs[i];
                    try
                    {
                        var uri = new Uri(img);
                        if (uri.IsFile)
                        {
                            img = $"https:{img}";
                        }
                        images.Add(img);
                    }
                    catch (Exception exception)
                    {
                        DebugConsole.WriteLine(exception.Message);
                    }
                }
                if (images.Count <= 0) throw new Exception();
                return images;
            }
            catch (Exception exception)
            {
                throw new Exception("메인 이미지를 찾을 수 없습니다.", exception);
            }
        }
        public string GetDetailPageHtml(JToken data)
        {
            try
            {
                string? desc = data["data"]?["item_desc"]?.Value<string>("desc");
                string? tags_desc = data["data"]?["item_desc"]?.Value<string>("tags_desc");

                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(desc);

                HtmlNodeCollection htmlNodeCollection = htmlDocument.DocumentNode.SelectNodes("//img");
                if (htmlNodeCollection == null) throw new Exception();
                for (int i = 0; i < htmlNodeCollection.Count; i++)
                {
                    HtmlNode htmlNode = htmlNodeCollection[i];
                    HtmlAttribute htmlAttribute = htmlNode.Attributes["src"];
                    string srcValue = htmlAttribute.Value;
                    var uri = new Uri(srcValue);
                    if (uri.IsFile)
                    {
                        htmlAttribute.Value = "https:" + htmlAttribute.Value;
                    }
                }
                string dafaultHtml = HtmlReader.GetDafaultHtml(htmlDocument.DocumentNode.OuterHtml);
                return dafaultHtml;
            }
            catch (Exception exception)
            {
                throw new Exception("상세 페이지를 찾을 수 없습니다.", exception);
            }
        }
        public Option GetOption(JToken data, List<JToken> skumaps)
        {
            try
            {
                var option = new Option();

                var optionGroups = new List<Option.OptionGroup>();
                string? color_idString = data["data"]?.Value<string>("color_id");
                if (string.IsNullOrWhiteSpace(color_idString) == false)
                {
                    var optionGroup = new Option.OptionGroup();
                    optionGroup.optionId = "color_id";
                    optionGroup.groupName = new List<Name>() { new Name(Language.CN, "Color") };
                    optionGroup.groups = new List<Option.OptionGroup.Group>();
                    string[]? color_ids = color_idString?.Split(",");
                    for (int i = 0; i < color_ids.Length; i++)
                    {
                        string color_id = color_ids[i];
                        JToken? find = skumaps.Find(x => x["color_id"]?.ToString() == color_id);
                        if (find == null) continue;
                        string? color_pic = find.Value<string>("color_pic");
                        try
                        {
                            Uri uri = new Uri(color_pic);
                            if (uri.IsFile)
                            {
                                color_pic = "https:" + color_pic;
                            }
                        }
                        catch { }
                        string? color_name = find.Value<string>("color_name");

                        Option.OptionGroup.Group group = new Option.OptionGroup.Group();
                        group.groupId = color_id;
                        group.optionImage = color_pic;
                        group.optionName = new List<Name>() { new Name(Language.CN, color_name) };
                        optionGroup.groups.Add(group);
                    }
                    optionGroups.Add(optionGroup);
                }

                string? size_idString = data["data"]?.Value<string>("size_id");
                if (string.IsNullOrWhiteSpace(size_idString) == false)
                {
                    var optionGroup = new Option.OptionGroup();
                    optionGroup.optionId = "size_id";
                    optionGroup.groupName = new List<Name>() { new Name(Language.CN, "Size") };
                    optionGroup.groups = new List<Option.OptionGroup.Group>();
                    string[]? size_ids = size_idString?.Split(",");
                    for (int i = 0; i < size_ids.Length; i++)
                    {
                        string size_id = size_ids[i];
                        JToken? find = skumaps.Find(x => x["size_id"]?.ToString() == size_id);
                        if (find == null) continue;
                        string? size_name = find.Value<string>("size_name");

                        Option.OptionGroup.Group group = new Option.OptionGroup.Group();
                        group.groupId = size_id;
                        group.optionName = new List<Name>() { new Name(Language.CN, size_name) };
                        optionGroup.groups.Add(group);
                    }
                    optionGroups.Add(optionGroup);
                }

                option.optionGroups = optionGroups;
                IEnumerable<Name> groupNames = option.optionGroups.SelectMany(x => x.groupName);
                IEnumerable<string> texts = groupNames.Select(x => x.text);
                string optionGroupName = string.Join("ㅡ", texts);

                var detailOptions = new List<Option.DetailOption>();
                for (int i = 0; i < skumaps.Count; i++)
                {
                    JToken skumap = skumaps[i];
                    string? skuid = skumap.Value<string>("skuid");
                    var skuids = skuid.Split(";", StringSplitOptions.RemoveEmptyEntries);
                    string id = string.Join("ㅡ", skuids);

                    var optionNameList = new List<string>();
                    string[] tryOptionKeys = new string[] { "color_name", "size_name" };
                    foreach (string optionKey in tryOptionKeys)
                    {
                        string? value = skumap.Value<string>(optionKey);
                        if (string.IsNullOrWhiteSpace(value)) continue;
                        optionNameList.Add(value);
                    }
                    string optionName = string.Join("ㅡ", optionNameList);

                    string? discount_priceString = skumap.Value<string>("discount_price");
                    string? priceString = skumap.Value<string>("price");

                    double? salePrice = discount_priceString.ParseToDouble();
                    double? originalPrice = priceString.ParseToDouble();

                    if (originalPrice == null || originalPrice <= 0) throw new Exception();
                    if (salePrice == null || salePrice <= 0)
                    {
                        salePrice = originalPrice;
                    }

                    var detailOption = new Option.DetailOption();
                    detailOption.optionIds = id;
                    detailOption.optionGroupName = new List<Name>() { new Name(Language.CN, optionGroupName) };
                    detailOption.optionName = new List<Name>() { new Name(Language.CN, optionName) };
                    detailOption.price = new List<Price> { new Price(Currency.CNY, (double)originalPrice, (double)salePrice) };
                    detailOption.isSoldOut = false;
                    detailOption.quantity = 9;
                    detailOptions.Add(detailOption);
                }
                option.detailOptions = detailOptions;
                return option;
            }
            catch (Exception exception)
            {
                throw new Exception("옵션 정보를 찾을 수 없습니다.", exception);
            }
        }
        public Product GetProduct(string url, string html)
        {
            var product = new Product();
            try
            {
                JObject data = html.ToJObject();
                if (data == null) throw new CaptchaException()
                {
                    CaptchaInfo = new CaptchaInfo { Url = url, Message = "VVIC 캡차(Captcha)가 발생했습니다. 캡차를 풀고 다시 진행해주세요.", }
                };
                product.site = this.Site;
                product.title = GetTitle(data);
                product.descs = GetDescs(data);
                product.code = GetCode(url);
                product.currency = Currency.CNY.ToString();
                product.price = GetPrice(data);
                product.url = GetUrl(url);
                product.mainImages = GetMainImages(data);
                product.detailPageHtml = GetDetailPageHtml(data);
                string? skumap = data["data"]?.Value<string>("skumap");
                if (string.IsNullOrWhiteSpace(skumap) == false)
                {
                    skumap = DecryptByDES(skumap);
                    List<JToken> skumaps = skumap.ToJArray().ToList();
                    product.option = GetOption(data, skumaps);
                    if (product.price == null || product.price.Count <= 0)
                    {
                        product.price = ProductHelper.GetMinPriceInDetailOption(product.option.detailOptions, Currency.CNY);
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
            string code = GetCode(url);
            string apiUrl = $"https://www.vvic.com/apif/item/{code}/detail";
            string html = await ChatHubService.GetRequest(connectionId, apiUrl);
            return GetProduct(url, html);
        }

        public async Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            string code = GetCode(url);
            string apiUrl = $"https://www.vvic.com/apif/item/{code}/detail";
            string html = await ChatHubService.GetRequest(connectionId, apiUrl);
            JObject data = html.ToJObject();

            throw new NotImplementedException();
        }

        public Product GetProduct(CollectRawData rawData)
        {
            var product = new Product();
            try
            {
                if (rawData.Data == null)
                {
                    throw new CaptchaException()
                    {
                        CaptchaInfo = new CaptchaInfo { Url = rawData.Url, Message = "VVIC 캡차(Captcha)가 발생했습니다. 캡차를 풀고 다시 진행해주세요.", }
                    };
                }
                product.site = this.Site;
                product.title = GetTitle(rawData.Data);
                product.descs = GetDescs(rawData.Data);
                product.code = GetCode(rawData.Url);
                product.currency = Currency.CNY.ToString();
                product.price = GetPrice(rawData.Data);
                product.url = GetUrl(rawData.Url);
                product.mainImages = GetMainImages(rawData.Data);
                product.detailPageHtml = GetDetailPageHtml(rawData.Data);
                string? skumap = rawData.Data["data"]?.Value<string>("skumap");
                if (string.IsNullOrWhiteSpace(skumap) == false)
                {
                    skumap = DecryptByDES(skumap);
                    List<JToken> skumaps = skumap.ToJArray().ToList();
                    product.option = GetOption(rawData.Data, skumaps);
                    if (product.price == null || product.price.Count <= 0)
                    {
                        product.price = ProductHelper.GetMinPriceInDetailOption(product.option.detailOptions, Currency.CNY);
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

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            return;
        }
    }
}
