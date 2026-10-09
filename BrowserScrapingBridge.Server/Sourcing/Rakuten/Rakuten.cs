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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace BrowserScrapingBridge.Server.Sourcing.Rakuten
{
    public partial class Rakuten : ICollect
    {
        public string Site => nameof(Rakuten);
        public string Name => "라쿠텐";
        public Language Language => Language.JP;
        public Currency Currency => Currency.JPY;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public string GetCode(string url)
        {
            string code = null;

            var uri = new Uri(url);
            switch (uri.Host)
            {
                case "brandavenue.rakuten.co.jp":
                case "books.rakuten.co.jp":
                    code = uri.Segments.Last();
                    int lastIndexOf = code.LastIndexOf("/");
                    if (lastIndexOf > 0)
                    {
                        code = code.Substring(0, lastIndexOf);
                    }
                    break;

                default:
                    List<string> paths = uri.Segments.ToList().GetRange(1, 2);
                    string codes = string.Join("", paths);
                    code = codes.Replace("/", "_");
                    break;
            }
            return code;
        }
        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }
        private List<string> GetItemAddressList(string html)
        {
            return html.GetValues("//div[@class='rnkRanking_imageBox']//a", "href");
        }
        private JObject GetSummaryObject(string html)
        {
            List<string> noneAtributeScripts = html.GetValues("//script[count(@*) = 0]", TextEx.XPathOption.InnerHtml);
            string initial_stateText = "window.__INITIAL_STATE__ =";
            string? findScript = noneAtributeScripts.Find(x => x.IndexOf(initial_stateText) >= 0);
            string json = findScript.SingleSplit(initial_stateText, "window.__REWIRED_SCHEMAS__");
            if (string.IsNullOrWhiteSpace(json)) return null;
            var lastIndexOf = json.LastIndexOf(";");
            json = json.Substring(0, lastIndexOf);
            JObject summaryObject = JObject.Parse(json);
            return summaryObject;
        }
        private CollectPageInfo CreateRakutenCollect(string rekutenUri)
        {
            string code = null;
            string urlString = null;
            var uri = new Uri(rekutenUri);
            string[] split = uri.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            string path = split.First().ToLower();

            switch (path)
            {
                case "stylife":
                    code = split[1].ToUpper();
                    urlString = $"https://brandavenue.rakuten.co.jp/item/{code}/";
                    break;

                case "book":
                    code = split[1];
                    urlString = $"https://books.rakuten.co.jp/rb/{code}/";
                    break;

                default:
                    code = uri.LocalPath;
                    urlString = rekutenUri;
                    break;
            }
            var rakutenCollect = new CollectPageInfo();
            rakutenCollect.Code = code;
            rakutenCollect.Url = urlString;
            return rakutenCollect;
        }
        public CollectRequestInfo GetCollectProductInfo(string url)
        {
            CollectRequestInfo collectRequestInfo = new CollectRequestInfo();
            collectRequestInfo.RequestType = RequestType.ChatHub;
            collectRequestInfo.Url = url;
            var uri = new Uri(url);
            switch (uri.Host)
            {
                case "brandavenue.rakuten.co.jp":
                    break;
                default:
                    collectRequestInfo.Encoding = "EUC-JP";
                    break;
            }
            return collectRequestInfo;
        }
        private List<CollectPageInfo> GetBrandPageCollectPageInfos(string html)
        {
            List<string> itemNameLinks = html.GetValues("//a[@class='category_itemnamelink']", "href");
            if (itemNameLinks == null || itemNameLinks.Count <= 0) return null;

            itemNameLinks = itemNameLinks.Distinct().ToList();

            var collectPageInfos = new List<CollectPageInfo>();
            foreach (string itemNameLink in itemNameLinks)
            {
                string code = GetCode(itemNameLink);
                if (string.IsNullOrWhiteSpace(code)) continue;
                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = code;
                collectPageInfo.Url = itemNameLink;
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }
        public List<CollectPageInfo> GetCollectPageInfos(string url, string html)
        {

            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            try
            {
                var uri = new Uri(url);
                switch (uri.Host)
                {
                    case "ranking.rakuten.co.jp":
                        List<string> addressList = GetItemAddressList(html);

                        var scripts = html.GetValues("//script[@language='JavaScript' and @type='text/javascript']", TextEx.XPathOption.InnerHtml);
                        var startText = "var line = (function() {";
                        List<string> scriptHtmls = scripts.FindAll(x => x.IndexOf(startText) >= 0);
                        foreach (string scriptHtml in scriptHtmls)
                        {
                            List<string> codes = GetItemAddressList(scriptHtml);
                            if (codes != null && codes.Count > 0)
                            {
                                addressList.AddRange(codes);
                            }
                        }

                        foreach (string address in addressList)
                        {
                            CollectPageInfo rakutenCollect = CreateRakutenCollect(address);
                            collectPageInfos.Add(rakutenCollect);
                        }
                        break;

                    case "brandavenue.rakuten.co.jp":
                        List<string> noneAtributeScripts = html.GetValues("//script[count(@*) = 0]", TextEx.XPathOption.InnerHtml);
                        string initial_stateText = "window.__INITIAL_STATE__ =";
                        string? findScript = noneAtributeScripts.Find(x => x.IndexOf(initial_stateText) >= 0);
                        string json = findScript.SingleSplit(initial_stateText, ";");
                        JObject summaryObject = JObject.Parse(json);
                        JToken[] dataArray = summaryObject["item"]["data"].ToArray();
                        IEnumerable<string?> model_cds = dataArray.Select(x => x.Value<string>("model_cd"));
                        model_cds = model_cds.Distinct(); // 페이지 화면에 동일한 상품이 다른 옵션으로 여러개가 진열되어 있음 => 중복 제거

                        foreach (string? model_cd in model_cds)
                        {
                            if (string.IsNullOrWhiteSpace(model_cd)) continue;
                            string address = $"https://item.rakuten.co.jp/stylife/{model_cd}/";
                            CollectPageInfo rakutenItem = CreateRakutenCollect(address);
                            collectPageInfos.Add(rakutenItem);
                        }
                        break;

                    default:
                        JObject summaryObject1 = GetSummaryObject(html);
                        if (summaryObject1 != null)
                        {
                            JToken[] itemArray = summaryObject1["state"]["data"]["ichibaSearch"]["items"].ToArray();
                            foreach (JToken item in itemArray)
                            {
                                string code = null;
                                string address = null;

                                string? originalItemUrl = item.Value<string>("originalItemUrl");
                                UriBuilder uriBuilder = new UriBuilder(originalItemUrl);
                                string? shopName = item["shop"].Value<string>("name");

                                CollectPageInfo rakutenItem = null;
                                switch (shopName)
                                {
                                    case "Rakuten Fashion":
                                        rakutenItem = CreateRakutenCollect(uriBuilder.Uri.ToString());
                                        break;

                                    default:
                                        rakutenItem = CreateRakutenCollect(originalItemUrl);
                                        break;
                                }
                                collectPageInfos.Add(rakutenItem);
                            }
                        }
                        else
                        {
                            var getCollectPages = new List<Func<string, List<CollectPageInfo>>>();
                            getCollectPages.Add(GetBrandPageCollectPageInfos);

                            foreach (var getCollectPage in getCollectPages)
                            {
                                List<CollectPageInfo> collectPageInfo = getCollectPage.Invoke(html);
                                if (collectPageInfo == null || collectPageInfo.Count <= 0) continue;
                                collectPageInfos.AddRange(collectPageInfo);
                                break;
                            }
                        }
                        break;
                }
            }
            catch (Exception exception)
            {
                exception.Show();
            }
            return collectPageInfos;
        }
        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            var uri = new Uri(url);
            string encoding = "";
            switch (uri.Host)
            {
                case "brandavenue.rakuten.co.jp":
                    uriBuilderAdvance.SetQuery("page", page.ToString());
                    break;
                case "ranking.rakuten.co.jp":
                    uriBuilderAdvance.SetQuery("p", page.ToString());
                    break;
                default:
                    if (uriBuilderAdvance.Host != "search.rakuten.co.jp")
                    {
                        uriBuilderAdvance.Host = "search.rakuten.co.jp";
                        if (uriBuilderAdvance.Segments.Contains("category/"))
                        {
                            var categoryCode = uriBuilderAdvance.Segments[2];
                            uriBuilderAdvance.Segments = new List<string> { "/", "search/", "mall/", "-/", categoryCode };
                        }
                        else
                        {
                            throw new Exception("검증되지 않은 라쿠텐 페이지 입니다. 관리자에게 문의해주세요.");
                        }
                    }
                    uriBuilderAdvance.SetQuery("p", page.ToString());
                    encoding = "EUC-JP";
                    break;
            }
            string pageUrl = uriBuilderAdvance.ToString();

            string html = await ChatHubService.GetRequest(connectionId, pageUrl, null, encoding);
            List<CollectPageInfo> collectPageInfos = GetCollectPageInfos(pageUrl, html);
            return collectPageInfos;
        }
        protected List<Name> GetTitle(string title)
        {
            List<Name> result = new List<Name>();
            try
            {
                Dictionary<string, string> titleReplaceLists = new Dictionary<string, string>()
                    {
                        { "【" ,"】" },
                        { "★", "★"},
                        { "《" ,"》" },
                        { "※" ,"※" },
                        { "「" ,"」" },
                        { "☆" ,"☆" },
                        { "<" ,">" },
                        {"＼","/ " },
                        {"●","●" },
                        {"＜","＞" },
                        {"『","』" },
                        {"≪","≫" },
                        {"◇","◇" },
                    };


                foreach (KeyValuePair<string, string> item in titleReplaceLists)
                {
                    var titleDel = title.MultiSplit(item.Key, item.Value, TextEx.SplitOption.StartEndWord);
                    if (titleDel.Count > 0)
                    {
                        for (int i = 0; i < titleDel.Count; i++)
                        {
                            title = title.Replace(titleDel[i], "");
                        }
                    }
                }
                title = title.Replace("【楽天市場】", "").Replace("!", "").Replace(":", " ").Replace("α", "A").Replace("&amp;", "&")
                            .Replace("|", "").Replace("minus;", "").Replace("·", " ").Replace("＼", "").Replace("+", "").Replace("■", "").Replace("□", "")
                            .Replace("＼", "").Replace("times;", "").Replace("amp;", "").Replace("℃", "").Replace("※", "").Replace("・", " ").Replace("“", "")
                            .Replace("\"", "").Replace("-", "").Replace("&gt;", "").Replace("～", " ").Replace("~", " ").Replace("☆", "").Replace("&", "").Replace("★", "")
                            .Replace("”", "").Replace("《", "").Replace("》", "").Replace("｜", "").Replace("：", "").Replace("＆", "").Replace("◆", "").Replace(".", "")
                            .Replace("【", "").Replace("Irsquo;d", "").Replace("】", "")
                            ;


                var getTitle = title.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                title = string.Join(" ", getTitle);

                result.Add(new Name(Language.JP, title));
            }
            catch (Exception exception) { exception.Show(); }
            return result;
        }

        public JToken GetData(string html)
        {
            string script = html.SelectNode("//script[@id='item-page-app-data']", HtmlType.InnerHtml);
            if (string.IsNullOrWhiteSpace(script)) throw new Exception("상품 정보를 찾을 수 없습니다.");
            JObject data = script.ToJObject();
            if (data == null) throw new Exception("상품 정보를 찾을 수 없습니다.");
            return data;
        }
        public List<Name> GetTitle(JToken data)
        {
            string title = data.Value("api", "data", "itemInfoSku")?.Value<string>("title");
            if (string.IsNullOrWhiteSpace(title)) throw new Exception("상품명을 찾을 수 없습니다.");
            return GetTitle(title);
        }
        private List<Desc> GetDesc(JToken data)
        {
            List<Desc> result = new List<Desc>();
            List<JToken> skus = data.Value("api", "data", "itemInfoSku", "sku")?.ToList();
            if (skus == null) return null;
            foreach (JToken sku in skus)
            {
                List<JToken> attributes = sku.Value("attributes")?.ToList();
                if (attributes == null) continue;
                for (int i = 0; i < attributes.Count; i++)
                {
                    JToken attribute = attributes[i];
                    string title = attribute.Value<string>("title");
                    if (string.IsNullOrWhiteSpace(title)) continue;
                    string value = attribute.Value<string>("value");
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    Desc? find = result.Find(x => x.title == title);
                    if (find == null)
                    {
                        Desc desc = new Desc(i + 1, Language.JP, title, value);
                        result.Add(desc);
                    }
                    else
                    {
                        if (find.value != value)
                        {
                            find.value += $", {value}";
                        }
                    }
                }
            }
            return result;
        }
        public List<string> GetMainImages(JToken data)
        {
            List<JToken> images = data.Value("api", "data", "itemInfoSku", "media", "images").ToList();

            List<string> result = new List<string>();
            foreach (JToken image in images)
            {
                var location = image.Value<string>("location");
                if (string.IsNullOrWhiteSpace(location)) continue;
                result.Add(location);
            }
            return result;
        }
        private List<Price> GetPrice(string html)
        {
            string priceText = html.GetValue("//meta[@itemprop='price']", "content");
            if (string.IsNullOrWhiteSpace(priceText)) return null;
            double? price = priceText.ParseToDouble();
            if (price == null || price <= 0) return null;
            return new List<Price> { new Price(Currency.JPY, (double)price, (double)price, true) };
        }
        private List<Option.OptionGroup> GetOptionGroup(string html)
        {
            List<Option.OptionGroup> result = new List<Option.OptionGroup>();
            try
            {
                var productData = html.SingleSplit("item-page-app-data\">", "</script>");
                var productModel = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.Model>(productData);

                if (productModel != null)
                {
                    if (productModel.api.data.itemInfoSku.purchaseInfo.axis != null)
                    {
                        var optionIndex = 0;
                        foreach (var optionGroupInfo in productModel.api.data.itemInfoSku.purchaseInfo.axis)
                        {
                            Option.OptionGroup optionGroup = new Option.OptionGroup();

                            optionGroup.groupName = new List<Name>();
                            optionGroup.optionId = (optionIndex + 1).ToString();
                            //var abc = TranslateAdv.GetTranslate("ja", "ko", optionGroupInfo.key.ToString());
                            optionGroup.groupName.Add(new Name(Language.JP, optionGroupInfo.label.ToString()));

                            optionGroup.groups = new List<Option.OptionGroup.Group>();
                            var optionGroupIndex = 0;
                            foreach (var optionGroupDetailInfo in optionGroupInfo.values)
                            {
                                Option.OptionGroup.Group group = new Option.OptionGroup.Group();
                                group.groupId = (optionGroupIndex + 1).ToString();
                                group.optionName = new List<Name>();
                                group.optionName.Add(new Name(Language.JP, optionGroupDetailInfo.label.ToString()));

                                optionGroup.groups.Add(group);
                                optionGroupIndex++;
                            }
                            result.Add(optionGroup);
                            optionIndex++;
                        }
                    }
                }
            }
            catch (Exception exception) { exception.Show(); } //https://item.rakuten.co.jp/biccamera/4957054514761/
            return result;
        }
        private List<Option.DetailOption> GetDetailOption(List<Option.OptionGroup> optionGroups, string html)
        {
            List<Option.DetailOption> result = new List<Option.DetailOption>();
            try
            {
                var tmpPriceData = html.SingleSplit("data-item-data=\"", "]");

                string getPrice = tmpPriceData.SingleSplit("price&quot;:", ",");
                getPrice = Regex.Replace(getPrice, @"[^0-9]", "");

                double price = Convert.ToDouble(getPrice);

                var productData = html.SingleSplit("item-page-app-data\">", "</script>");
                var productModel = JsonConvert.DeserializeObject<Models.Model>(productData);

                if (productModel != null)
                {
                    if (productModel.api.data.itemInfoSku.sku != null)
                    {
                        string optionGroupNameJP = "";
                        switch (optionGroups.Count)
                        {
                            case 1:
                                Name nameJP = optionGroups[0].groupName.Find(x => x.language == "JP");
                                optionGroupNameJP = nameJP.text;
                                break;

                            case 2:
                                Name nameJP1 = optionGroups[0].groupName.Find(x => x.language == "JP");
                                Name nameJP2 = optionGroups[1].groupName.Find(x => x.language == "JP");
                                optionGroupNameJP = nameJP1.text + "ㅡ" + nameJP2.text;
                                break;
                        }

                        foreach (var detailOptionInfo in productModel.api.data.itemInfoSku.sku)
                        {
                            Option.DetailOption detailOption = new Option.DetailOption();

                            detailOption.optionGroupName = new List<Name>();
                            detailOption.optionGroupName.Add(new Name("JP", optionGroupNameJP));

                            detailOption.optionName = new List<Name>();
                            detailOption.optionName.Add(new Name("JP", string.Join("ㅡ", detailOptionInfo.selectorValues)));

                            var optionIdList = new List<string>();
                            for (int i = 0; i < detailOptionInfo.selectorValues.Count; i++)
                            {
                                var optionId = optionGroups[i].groups.Find(x => x.optionName.Find(y => y.language == "JP").text == detailOptionInfo.selectorValues[i]).groupId;
                                if (string.IsNullOrWhiteSpace(optionId) == false) optionIdList.Add(optionId);
                            }

                            detailOption.optionIds = string.Join("ㅡ", optionIdList) + "ㅡ" + detailOptionInfo.variantId;

                            if (productModel.api.data.itemInfoSku.purchaseInfo.variantMappedInventories != null)
                            {
                                detailOption.quantity = productModel.api.data.itemInfoSku.purchaseInfo.variantMappedInventories.Find(x => x.sku == detailOptionInfo.variantId).quantity;
                            }
                            detailOption.isSoldOut = detailOption.quantity == 0;

                            string referencePrice = detailOptionInfo.referencePrice?.value;
                            double optionPrice;
                            bool isTryParse = double.TryParse(referencePrice, out optionPrice);
                            if (isTryParse == false)
                            {
                                optionPrice = detailOptionInfo.taxIncludedPrice;
                            }
                            else
                            {
                                double taxIncludedPrice = detailOptionInfo.taxIncludedPrice;
                                if (optionPrice >= taxIncludedPrice)
                                {
                                    optionPrice = taxIncludedPrice;
                                }
                            }
                            if (optionPrice <= 0) continue;
                            detailOption.price = new List<Price>() { new Price(Currency.JPY, optionPrice, optionPrice) };
                            result.Add(detailOption);
                        }
                    }
                }
            }
            catch (Exception exception) { exception.Show(); }
            return result;
        }
        private Option GetOptions(string html)
        {
            Option result = new Option();
            try
            {
                result.optionGroups = GetOptionGroup(html);
                if (result.optionGroups == null || result.optionGroups.Count == 0) return null;
                result.detailOptions = GetDetailOption(result.optionGroups, html);
                if (result.detailOptions.Count == 0) throw new CollectException("재고가 없는 상품입니다.");
            }
            catch (CollectException collectException) { throw collectException; }
            catch (Exception exception) { exception.Show(); }
            return result;
        }
        public string GetDetailPageHtml(JToken data)
        {
            var newProductDescription = data.Value("api", "data", "itemInfoSku").Value<string>("newProductDescription");
            string detailHtml = HtmlReader.GetDafaultHtml(newProductDescription);
            return detailHtml;
        }
        public Product GetRakutenItem(string url, string html)
        {
            Product product = new Product();
            try
            {
                JToken data = GetData(html);
                product.site = this.Site;
                product.code = GetCode(url);
                product.url = url;
                product.currency = this.Currency.ToString();
                product.title = GetTitle(data);
                product.descs = GetDesc(data);
                product.mainImages = GetMainImages(data);
                product.price = GetPrice(html);
                product.option = GetOptions(html);
                product.detailPageHtml = GetDetailPageHtml(data);

                if (product.price == null)
                {
                    product.price = ProductHelper.GetMinPriceInDetailOption(product.option.detailOptions, Currency.JPY);
                }

                if (product.option != null)
                {
                    if (product.option.optionGroups.IsNotNullGeneric())
                    {
                        product.option.optionGroups.ForEach(x => x.groupName.First().text = HttpUtility.HtmlDecode(x.groupName.First().text));
                        product.option.optionGroups.ForEach(x => x.groups.ForEach(y => y.optionName.First().text = HttpUtility.HtmlDecode(y.optionName.First().text)));
                    }
                }
            }
            catch (Exception exception)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = exception.Message;
            }
            return product;
        }

        public Product GetProduct(string url, string html)
        {
            Product product = null;
            var uri = new Uri(url);
            switch (uri.Host)
            {
                case "brandavenue.rakuten.co.jp":
                    var rakutenBrandavenue = new RakutenBrandavenue();
                    product = rakutenBrandavenue.GetBrandavenueProduct(uri, html);
                    break;

                case "books.rakuten.co.jp":
                    var rakutenBook = new RakutenBook();
                    product = rakutenBook.GetBookProduct(uri, html);
                    break;

                default:
                    product = GetRakutenItem(uri.AbsoluteUri, html);
                    break;
            }
            if (product != null)
            {
                product.regDate = DateTime.Now;
            }
            return product;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var uri = new Uri(url);
            string encoding = "";
            switch (uri.Host)
            {
                case "brandavenue.rakuten.co.jp":
                    break;
                default:
                    encoding = "EUC-JP";
                    break;
            }

            string html = await ChatHubService.GetRequest(connectionId, url, null, encoding);
            return GetProduct(url, html);
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
