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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Mytheresa
{
    public class Mytheresa : ICollect
    {
        public string Site { get; } = nameof(Mytheresa);
        public string Name => "마이테레사";
        public Language Language => Language.EN;
        public Currency Currency => Currency.EUR;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        public async Task<string> GetHtmlAsync(string url)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Connection", "keep-alive");
            headers.Add("sec-ch-ua", "\"Google Chrome\";v=\"141\", \"Not?A_Brand\";v=\"8\", \"Chromium\";v=\"141\"");
            headers.Add("sec-ch-ua-platform", "\"Windows\"");
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            headers.Add("Sec-Fetch-Dest", "document");
            headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            headers.Add("Accept-Language", "ko,en-US;q=0.9,en;q=0.8,ko-KR;q=0.7");

            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                Proxy = this.Proxy,
            };
            string result = null;
            using (var httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler))
            {
                httpClient.SetHeader(headers);
                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url);
                result = await httpResponseMessage.Content.ReadAsStringAsync();
            }
            return result;
        }
        public string GetCode(string url)
        {
            string code = url.Split("-").Last();
            return code;
        }
        public async Task<string> GetLoginUrlAsync(string connectionId) { return null; }
        private async Task<List<JToken>> GetPageProducts(string uri, int page)
        {
            PageRequestBody pageRequestBody = new PageRequestBody();
            pageRequestBody.variables.page = page;

            UriBuilderAdvance uriBuilderAdvance = new UriBuilderAdvance(uri);
            List<string> getSegments = uriBuilderAdvance.Uri.Segments.ToList().GetRange(4, uriBuilderAdvance.Uri.Segments.Length - 4);
            string slug = string.Join("", getSegments);
            pageRequestBody.variables.slug = $"/{slug}";

            string categories = uriBuilderAdvance.GetQuery("categories");
            if (string.IsNullOrEmpty(categories) == false)
            {
                string[] categoriesArray = categories.Split(",");
                pageRequestBody.variables.categories.AddRange(categoriesArray);
            }

            string colors = uriBuilderAdvance.GetQuery("colors");
            if (string.IsNullOrEmpty(colors) == false)
            {
                string[] colorsArray = colors.Split(",");
                pageRequestBody.variables.colors.AddRange(colorsArray);
            }

            string designers = uriBuilderAdvance.GetQuery("designers");
            if (string.IsNullOrEmpty(designers) == false)
            {
                designers = System.Web.HttpUtility.UrlDecode(designers);
                //designers = System.Web.HttpUtility.HtmlDecode(designers);
                string[] designersArray = designers.Split(",");
                pageRequestBody.variables.designers.AddRange(designersArray);
            }

            string materials = uriBuilderAdvance.GetQuery("materials");
            if (string.IsNullOrEmpty(materials) == false)
            {
                string[] materialsArray = materials.Split(",");
                pageRequestBody.variables.materials.AddRange(materialsArray);
            }

            string patterns = uriBuilderAdvance.GetQuery("patterns");
            if (string.IsNullOrEmpty(patterns) == false)
            {
                string[] patternsArray = patterns.Split(",");
                pageRequestBody.variables.patterns.AddRange(patternsArray);
            }

            string sizesHarmonized = uriBuilderAdvance.GetQuery("sizesHarmonized");
            if (string.IsNullOrEmpty(sizesHarmonized) == false)
            {
                string[] sizesHarmonizedArray = sizesHarmonized.Split(",");
                pageRequestBody.variables.sizesHarmonized.AddRange(sizesHarmonizedArray);
            }

            string sortBy = uriBuilderAdvance.GetQuery("sortBy");
            if (string.IsNullOrEmpty(sortBy) == false)
            {
                pageRequestBody.variables.sort = sortBy;
            }

            string reductionRange = uriBuilderAdvance.GetQuery("reductionRange");
            if (string.IsNullOrEmpty(reductionRange) == false)
            {
                string[] reductionRangeArray = reductionRange.Split(",");
                pageRequestBody.variables.reductionRange.AddRange(reductionRangeArray);
            }

            string storeAndCountry = uriBuilderAdvance.Uri.Segments[1];
            storeAndCountry = storeAndCountry.Substring(0, storeAndCountry.Length - 1);
            storeAndCountry = storeAndCountry.ToUpper();

            string acceptLanguage = uriBuilderAdvance.Uri.Segments[2];
            acceptLanguage = acceptLanguage.Substring(0, acceptLanguage.Length - 1);

            string section = uriBuilderAdvance.Uri.Segments[3];
            section = section.Substring(0, section.Length - 1);

            string body = pageRequestBody.ToSerializeObject();
            string address = "https://api.mytheresa.com/api";

            var headers = new Dictionary<string, string>();
            headers.Add("Connection", "keep-alive");
            headers.Add("X-Store", storeAndCountry);
            headers.Add("X-Section", section);
            headers.Add("Accept-Language", acceptLanguage);
            headers.Add("X-Country", storeAndCountry);
            headers.Add("Referer", "https://www.mytheresa.com/");

            HttpContent content = new StringContent(body);
            using (HttpClient httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient())
            {
                httpClient.SetHeader(headers);
                HttpResponseMessage response = await httpClient.PostAsync(address, content);
                string result = await response.Content.ReadAsStringAsync();
                JObject data = result.ToJObject();
                if (data == null) return null;
                List<JToken> products = data["data"]?["xProductListingPage"]?["products"]?.ToList();
                return products;
            }
        }
        private List<CollectPageInfo> GetCollectPageInfos(List<JToken> products)
        {
            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            if (products != null)
            {
                foreach (JToken product in products)
                {
                    string? department = product.Value<string>("department");
                    string? slug = product.Value<string>("slug");
                    string? name = product.Value<string>("name");
                    if (string.IsNullOrWhiteSpace(department) || string.IsNullOrWhiteSpace(slug))
                    {
                        continue;
                    }
                    string path = department + slug;
                    string code = product.Value<string>("sku");
                    if (string.IsNullOrWhiteSpace(code)) continue;

                    var collectPageInfo = new CollectPageInfo();
                    collectPageInfo.Code = code.ToLower();
                    collectPageInfo.Url = $"https://www.mytheresa.com/nl/en/{path}";
                    collectPageInfo.Title = name;
                    collectPageInfos.Add(collectPageInfo);
                }
            }
            return collectPageInfos;
        }
        /// <summary>
        /// 모든 페이지에 CollectPageInfo 반환
        /// </summary>
        /// <param name="address">페이지 URL 주소</param>
        /// <returns></returns>
        public async Task<List<CollectPageInfo>> GetAllCollectPageInfosAsync(string address)
        {
            string pageHtml = await GetHtmlAsync(address);
            JObject data = GetItemData(pageHtml);
            List<JToken>? trackingActions = data["app"]?["ssr"]?["trackingActions"]?.ToList();
            JToken? tracking_product_list_view = trackingActions.Find(x => x["name"]?.ToString() == "TRACKING_PRODUCT_LIST_VIEW");
            JToken? pagination = tracking_product_list_view["payload"]?["pagination"];
            List<JToken>? products = tracking_product_list_view["payload"]?["products"]?.ToList();

            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            var getCollectPageInfos = GetCollectPageInfos(products);
            collectPageInfos.AddRange(getCollectPageInfos);

            int totalPages = pagination.Value<int>("totalPages");
            for (int i = 2; i <= totalPages; i++)
            {
                List<JToken> nextProducts = await GetPageProducts(address, i);
                List<CollectPageInfo> getNextCollectPageInfos = GetCollectPageInfos(nextProducts);
                if (getNextCollectPageInfos != null)
                {
                    collectPageInfos.AddRange(getNextCollectPageInfos);
                }
            }
            return collectPageInfos;
        }
        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            try
            {
                List<JToken> products = await GetPageProducts(url, page);
                List<CollectPageInfo> getCollectPageInfos = GetCollectPageInfos(products);
                return getCollectPageInfos;
            }
            catch (Exception exception)
            {
                throw new CollectException($"페이지 조회에 실패했습니다. URL을 확인해주세요.", exception);
            }
        }
        public JObject GetItemData(string html)
        {
            string errorMessage = "상품 정보를 조회할 수 없습니다.";

            string script = html.GetValue("//script[@id='preloaded_state']", TextEx.XPathOption.InnerHtml);
            if (string.IsNullOrWhiteSpace(script)) throw new Exception("상품 정보를 찾을 수 없습니다.");
            string start = "window.__PRELOADED_STATE__ = ";
            int indexOf = script.IndexOf(start);
            if (indexOf < 0) { throw new CollectException(errorMessage); }
            string json = script.Substring(indexOf + start.Length);
            int lastIndexOf = json.LastIndexOf(";");
            json = json.Substring(0, lastIndexOf);

            JObject data = json.ToJObject();
            if (data == null) { throw new CollectException(errorMessage); }
            return data;
        }
        private JToken GetPayload(JObject data, string name)
        {
            string msg = "상품 정보를 조회할 수 없습니다.";
            if (data == null) throw new CollectException(msg);
            List<JToken>? trackingActions = data["app"]?["ssr"]?["trackingActions"]?.ToList();
            if (trackingActions == null || trackingActions.Count <= 0) throw new CollectException(msg);
            JToken? trackingProductListClick = trackingActions.Find(x => x["name"]?.ToString() == name);
            if (trackingProductListClick == null) throw new CollectException(msg);
            JObject? trackingProduct = trackingProductListClick.ToObject<JObject>();
            JToken? payload = trackingProduct["payload"];
            if (payload == null) throw new CollectException(msg);
            return payload;
        }
        private string GetCode(JToken tracking_product_list_click)
        {
            string errorMsg = "상품 코드를 찾을 수 없습니다.";
            try
            {
                string? department = tracking_product_list_click.Value<string>("department");
                if (string.IsNullOrWhiteSpace(department)) throw new CollectException(errorMsg);
                string? slug = tracking_product_list_click.Value<string>("slug");
                if (string.IsNullOrWhiteSpace(slug)) throw new CollectException(errorMsg);
                return $"{department}{slug}";
            }
            catch (Exception exception)
            {
                throw new CollectException(errorMsg, exception);
            }
        }
        private List<Name> GetTitle(JToken data)
        {
            try
            {
                string? name = data.Value<string>("name");
                if (string.IsNullOrWhiteSpace(name)) throw new CollectException("상품명을 조회할 수 없습니다.");
                List<Name> names = new List<Name>();
                names.Add(new Name(this.Language, name));
                return names;
            }
            catch (Exception exception)
            {
                throw new CollectException("상품명을 조회할 수 없습니다.", exception);
            }
        }
        private List<Name> GetBrand(JToken data)
        {
            string? designer = data.Value<string>("designer");
            if (string.IsNullOrWhiteSpace(designer)) throw new CollectException("브랜드명을 조회할 수 없습니다.");
            List<Name> names = new List<Name>();
            names.Add(new Name(this.Language, designer));
            return names;
        }

        private string GetModel(JToken productGroup)
        {
            string sku = productGroup.Value<string>("sku");
            if (string.IsNullOrWhiteSpace(sku)) throw new Exception("모델명을 찾을 수 없습니다.");
            return sku;
        }

        private List<Desc> GetDescs(JToken data) { return null; }
        private List<Tag> GetTags(JToken data) { return null; }
        private List<Price> GetPrice(JToken tracking_product_list_click)
        {
            try
            {
                JToken? price = tracking_product_list_click["price"];
                if (price == null) throw new CollectException("가격 정보를 찾을 수 없습니다.");
                double? original = price.Value<double?>("original");
                if (original == null) throw new Exception();
                double? discount = price.Value<double?>("discount");
                if (discount == null)
                {
                    discount = original;
                }
                discount /= 100d;
                original /= 100d;
                List<Price> priceList = new List<Price> { new Price(this.Currency, (double)original, (double)discount, true) };
                return priceList;
            }
            catch (Exception exception)
            {
                throw new CollectException("가격 정보를 찾을 수 없습니다.", exception);

            }

        }
        private List<string> GetMainImages(JToken data)
        {
            try
            {
                List<string> result = new List<string>();

                List<JToken>? displayImages = data["displayImages"]?.ToList();
                if (displayImages == null || displayImages.Count <= 0) throw new CollectException("상품 이미지를 찾을 수 없습니다.");
                foreach (JToken displayImage in displayImages)
                {
                    string image = displayImage.ToString();
                    result.Add(image);
                }
                return result;
            }
            catch (Exception exception)
            {
                throw new CollectException("상품 이미지를 찾을 수 없습니다.", exception);
            }

        }
        private string GetVideoUrl(JToken data) { return null; }
        private List<string> GetOriginalDetailPageUrls(JToken data) { return null; }
        private List<string> GetTranslateDetailPageUrls(JToken data) { return null; }
        private string GetDetailPageHtml(JToken tracking_product_list_click, string html)
        {
            try
            {
                string productinfo__accordion = html.GetValue("//div[@class='productinfo__accordion']", TextEx.XPathOption.OuterHtml);

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(productinfo__accordion);

                string? sku = tracking_product_list_click.Value<string>("sku");
                HtmlNodeCollection lis = htmlDocument.DocumentNode.SelectNodes("//ul//li");
                for (int i = 0; i < lis.Count; i++)
                {
                    if (lis[i].InnerText.Contains(sku))
                    {
                        lis[i].Remove();
                    }
                }

                HtmlNodeCollection pTags = htmlDocument.DocumentNode.SelectNodes("//p");
                for (int i = 0; i < pTags.Count; i++)
                {
                    pTags[i].Remove();
                }

                string xpath = "//div[@data-section='delivery']";
                HtmlNode selectNode = htmlDocument.DocumentNode.SelectSingleNode(xpath);
                selectNode.ParentNode.Remove();
                string outerHtml = htmlDocument.DocumentNode.OuterHtml;

                if (string.IsNullOrWhiteSpace(productinfo__accordion)) throw new CollectException("상세 페이지를 찾을 수 없습니다.");
                string getDafaultHtml = HtmlReader.GetDafaultHtml(outerHtml);
                return getDafaultHtml;
            }
            catch (Exception exception)
            {
                throw new CollectException("상세 페이지 수집에 문제가 발생했습니다.", exception);
            }

        }
        private Option GetOption(JToken data)
        {
            try
            {
                List<JToken>? variants = data["variants"]?.ToList();
                if (variants == null || variants.Count <= 0) return null;

                var option = new Option();

                var optionGroups = new List<Option.OptionGroup>();
                var optionGroup = new Option.OptionGroup();
                optionGroup.optionId = "0";
                optionGroup.groupName = new List<Name>() { new Name(this.Language, "size") };
                var groups = new List<Option.OptionGroup.Group>();
                for (int i = 0; i < variants.Count; i++)
                {
                    string? size = variants[i].Value<string>("size");
                    var group = new Option.OptionGroup.Group();
                    group.groupId = i.ToString();
                    group.optionName = new List<Name> { new Name(this.Language, size) };
                    groups.Add(group);
                }
                optionGroup.groups = groups;
                optionGroups.Add(optionGroup);
                option.optionGroups = optionGroups;

                var detailOptions = new List<Option.DetailOption>();
                for (int i = 0; i < variants.Count; i++)
                {
                    var detailOption = new Option.DetailOption();
                    detailOption.optionIds = i.ToString();

                    JToken? availability = variants[i]["availability"];
                    detailOption.isSoldOut = !availability.Value<bool>("hasStock");
                    int? lastStockQuantityHint = availability.Value<int?>("lastStockQuantityHint");
                    if (lastStockQuantityHint == null)
                    {
                        detailOption.quantity = 9;
                    }
                    else
                    {
                        detailOption.quantity = (int)lastStockQuantityHint;
                    }

                    string? size = variants[i].Value<string>("size");
                    detailOption.optionGroupName = new List<Name> { new Name(this.Language, "size") };
                    detailOption.optionName = new List<Name> { new Name(this.Language, size) };

                    JToken? price = variants[i]["price"];
                    if (price == null) continue;
                    double? originalPrice = price.Value<double?>("original");
                    double? salePrice = price.Value<double?>("discount");
                    if (originalPrice == null || originalPrice <= 0) continue;
                    if (salePrice == null || salePrice <= 0)
                    {
                        salePrice = originalPrice;
                    }
                    salePrice /= 100d;
                    originalPrice /= 100d;

                    detailOption.price = new List<Price> { new Price(this.Currency, (double)originalPrice, (double)salePrice) };
                    detailOptions.Add(detailOption);
                }
                option.detailOptions = detailOptions;
                return option;
            }
            catch (Exception exception)
            {
                throw new CollectException("옵션 정보 수집 중 문제가 발생했습니다.", exception);
            }

        }
        public Product GetProduct(string url, string html)
        {
            var product = new Product();
            try
            {
                product.site = this.Site;
                product.currency = this.Currency.ToString();
                JObject originalData = GetItemData(html);
                JToken tracking_product_list_click = GetPayload(originalData, "TRACKING_PRODUCT_LIST_CLICK");
                JToken tracking_product_details = GetPayload(originalData, "TRACKING_PRODUCT_DETAILS");
                product.code = GetCode(url);
                product.url = url;
                product.title = GetTitle(tracking_product_list_click);
                product.brand = GetBrand(tracking_product_list_click);
                product.model = GetModel(tracking_product_list_click);
                product.descs = GetDescs(originalData);
                product.tags = GetTags(tracking_product_details);
                product.price = GetPrice(tracking_product_list_click);
                product.mainImages = GetMainImages(tracking_product_list_click);
                product.videoUrl = GetVideoUrl(originalData);
                product.originalDetailPageUrls = GetOriginalDetailPageUrls(originalData);
                product.translateDetailPageUrls = GetTranslateDetailPageUrls(originalData);
                product.detailPageHtml = GetDetailPageHtml(tracking_product_list_click, html);
                product.option = GetOption(tracking_product_list_click);
                product.regDate = DateTime.Now;

                if (product?.option?.detailOptions != null && product.option.detailOptions.Count > 0)
                {
                    if (product.price == null)
                    {
                        product.price = ProductHelper.GetMinPriceInDetailOption(product.option.detailOptions, product.currency.ConvertEnum<Currency>());
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

        private string GetScript(string html)
        {
            string script = html.GetValue("//script[@id='preloaded_state']", TextEx.XPathOption.InnerHtml);
            return script;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string html = await GetHtmlAsync(url);
            var script = GetScript(html);
            if (string.IsNullOrWhiteSpace(script))
            {
                DebugConsole.Write("사용자 환경 수집 ");
                html = await ChatHubService.GetRequest(connectionId, url);
                Thread.Sleep(2000);
                script = GetScript(html);
                if (string.IsNullOrWhiteSpace(script)) throw new CaptchaException()
                {
                    CaptchaInfo = new CaptchaInfo()
                    {
                        Url = url,
                        Message = "상품 페이지 접속 후 다시 시도해주세요"
                    }
                };
            }
            return GetProduct(url, html);
        }

        public async Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
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
