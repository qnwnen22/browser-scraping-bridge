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
using System.Text;
using System.Threading.Tasks;
using WebProxy = System.Net.WebProxy;

namespace BrowserScrapingBridge.Server.Sourcing.KoreanSsan
{
    public class KoreanSsan : ICollect
    {
        public readonly string host = "https://koreanssan.com";

        public string Site => nameof(KoreanSsan);

        public string Name => "대한민국농수산";

        public Language Language => Language.KR;

        public Currency Currency => Currency.KRW;

        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public string GetCode(string url)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            var idx = uriBuilderAdvance.GetQuery("idx");
            return idx;
        }

        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }



        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var baseHtml = await ChatHubService.GetRequest(connectionId, url);
            var uriBuilderAdvance = new UriBuilderAdvance(url);

            List<CollectPageInfo> result = null;
            switch (uriBuilderAdvance.Segments[1])
            {
                case "90":
                    result = GetCollectPageInfos90(baseHtml, uriBuilderAdvance, page);
                    break;
                default:
                    result = GetCollectPageInfos(baseHtml, uriBuilderAdvance, page);
                    break;
            }
            return result;
        }
        private List<CollectPageInfo> GetCollectPageInfos(string baseHtml, UriBuilderAdvance uriBuilderAdvance, int page)
        {
            var scripts = baseHtml.SelectNodes("//script", HtmlType.InnerText);
            var script = scripts.Find(x => x.Contains("/ajax/template/widget/shopping.cm"));
            var ajaxPath = script.SingleSplit("url: \"", "\"");

            var ajaxRequest = CreateRequest("POST", ajaxPath);
            var ajaxResponse = ajaxRequest.ReadToEnd();
            var category = ajaxResponse.SingleSplit("'category' : '", "',");

            var sort = uriBuilderAdvance.GetQuery("sort");
            if (string.IsNullOrWhiteSpace(sort)) sort = "like";
            var menu_url = string.Join("", uriBuilderAdvance.Segments);

            var sb = new StringBuilder();
            sb.Append("https://koreanssan.com/ajax/get_shop_list_view.cm?");
            sb.Append($"page={page}&");
            sb.Append("pagesize=95&");
            sb.Append($"category={category}&");
            sb.Append($"sort={sort}&");
            sb.Append($"menu_url={menu_url}&");
            var requestUrl = sb.ToString();
            var webRequest = BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest.CreateHttp(requestUrl);
            webRequest.Referer = "https://koreanssan.com"; // chathub로 Referer를 지정할 수 없어서 서버자원 사용
            var response = webRequest.ReadToEnd();
            JObject data = response.ToJObject();
            var html = data.Value<string>("html");
            List<CollectPageInfo> result = CreateCollectPageInfos(data, html);
            return result;
        }

        private List<CollectPageInfo> GetCollectPageInfos90(string baseHtml, UriBuilderAdvance uriBuilderAdvance, int page)
        {
            var scripts = baseHtml.SelectNodes("//script", HtmlType.OuterHtml);
            var script = scripts.Find(x => x.Contains("/ajax/template/widget/shopping.cm"));
            string qurey = script.SingleSplit("\"/ajax/template/widget/shopping.cm", "\",");
            string address = "https://koreanssan.com/ajax/template/widget/shopping.cm" + qurey;

            var webRequest = BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest.CreateHttp(address);
            webRequest.Referer = "https://koreanssan.com"; // chathub로 Referer를 지정할 수 없어서 서버자원 사용
            var response = webRequest.ReadToEnd();

            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            List<string> data_product_properties = response.SelectNodes("//div[@class='shop-item _shop_item']", "data-product-properties");
            foreach (var data_product_property in data_product_properties)
            {
                var json = data_product_property.ToHtmlDecodeComplete();
                JObject data = json.ToJObject();
                if (data == null) continue;
                string idx = data.Value<string>("idx");
                if (string.IsNullOrWhiteSpace(idx)) continue;
                string name = data.Value<string>("name");
                var collectPageInfo = new CollectPageInfo
                {
                    Code = idx,
                    Url = $"https://koreanssan.com/90/?idx={idx}",
                    Title = name
                };
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }


        private List<CollectPageInfo> CreateCollectPageInfos(JObject data, string html)
        {
            List<string> divs = html.SelectNodes("//div[@class='shop-item _shop_item']", HtmlType.OuterHtml);

            List<CollectPageInfo> result = new List<CollectPageInfo>();
            foreach (var item in divs)
            {
                var dataProductProperties = item.SelectNode("//div[@class='shop-item _shop_item']", "data-product-properties");
                if (string.IsNullOrWhiteSpace(dataProductProperties)) continue;
                dataProductProperties = dataProductProperties.ToHtmlDecodeComplete();
                JObject dataProductProperty = dataProductProperties.ToJObject();
                if (data == null) continue;
                string href = item.GetValue("//a", "href");
                if (string.IsNullOrWhiteSpace(href)) continue;

                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = dataProductProperty.Value<string>("idx");
                collectPageInfo.Title = dataProductProperty.Value<string>("name");
                collectPageInfo.Url = $"{host}{href}";
                result.Add(collectPageInfo);
            }
            return result;
        }

        public BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest CreateRequest(string method, string path, string body = null)
        {
            var request = BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest.CreateHttp($"{host}{path}");
            request.Method = method.ToUpper();
            request.Referer = host;
            if (string.IsNullOrWhiteSpace(body) == false)
            {
                request.Write(body);
            }
            return request;
        }

        public JToken GetData(string html, string url)
        {
            if (string.IsNullOrWhiteSpace(html)) throw new Exception("소켓 연결을 다시 확인해주세요.");
            var result = new JObject();

            var uriBuilderAdvance = new UriBuilderAdvance(url);
            var code = uriBuilderAdvance.GetQuery("idx");

            BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest productRequset = CreateRequest("GET", $"/ajax/oms/OMS_get_product.cm?prod_idx={code}");
            var productJson = productRequset.ReadToEnd();
            if (string.IsNullOrWhiteSpace(productJson)) throw new Exception("상품 정보를 찾을 수 없습니다.");
            var productData = productJson.ToJObject().Value("data");
            if (productData == null) throw new Exception("상품 정보를 찾을 수 없습니다.");
            result["product"] = productData;

            #region 상세 페이지에 사용할려 했으니 의미 없는 데이터라 판단됨
            //var loadProductDetail = html.SingleSplit("SITE_SHOP_DETAIL.loadProductDetail(", ");");
            //var datas = loadProductDetail.Split(",");
            //var menu_code = datas[2].Replace("'", "").Trim();

            //var current_url = url.Substring(host.Length);
            //var sb = new StringBuilder();
            //sb.Append($"idx={code}&");
            //sb.Append($"menu_code={menu_code}&");
            //sb.Append($"current_url={current_url}");

            //BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest detailRequest = CreateRequest("POST", "/shop/load_product_detail.cm", sb.ToString());
            //detailRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            //var detailResponse = detailRequest.ReadToEnd();
            //var detailData = detailResponse.ToJObject();
            //result["detail"] = detailData; 
            #endregion

            return result;
        }

        public List<Name> GetTitle(JToken data)
        {
            var name = data.Value("product").Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(this.Language, name);
        }

        public List<Name> GetBrand(JToken data)
        {
            return null;
        }

        public List<Desc> GetDescs(JToken data)
        {
            var dic = new Dictionary<string, string>(); // 스크립트 및 패킷에서 당최 맵핑되는 구간을 찾을 수가 없음(수작업)
            dic.Add("productname", "제품명");
            dic.Add("producttype", "식품의 유형");
            dic.Add("manufacturer", "생산자 및 소재지");
            dic.Add("date", "제조연월일, 유통기한 또는 소비기한 또는 품질유지기한");
            dic.Add("size", "포장단위별 내용물의 용량(중량), 수량");
            dic.Add("material", "원재료명 (관계 법령에 따른 원산지 표시 포함) 및 함량");
            dic.Add("specifications", "영양성분");
            dic.Add("phrases", "유전자변형식품에 해당하는 경우의 표시");
            dic.Add("precautions", "소비자안전을 위한 주의사항");
            dic.Add("kc", "수입식품의 경우 “관계 법령에 따른 수입신고를 필함”의 문구");
            dic.Add("director", "소비자상담 관련 전화번호");

            List<Desc> result = new List<Desc>();

            JToken prodinfo = data.Value("product", "prodinfo");
            if (prodinfo == null) return result;
            int count = 1;
            foreach (var item in dic)
            {
                string value = prodinfo.Value<string>(item.Key);
                if (string.IsNullOrWhiteSpace(value)) continue;
                result.Add(new Desc(count, this.Language, item.Value, value));
                count++;
            }
            return result;
        }

        public List<Price> GetPrice(JToken data)
        {
            var product = data.Value("product");
            var price = product.Value<long?>("price");
            if (price == null || price == 0) throw new Exception("상품 가격을 확인할 수 없습니다.");
            var price_org = product.Value<long?>("price_org");
            if (price_org == null || price_org == 0)
            {
                price_org = price;
            }
            return ProductHelper.CreatePrice(this.Currency, price_org, price);
        }

        public List<string> GetMainImages(JToken data)
        {
            List<string> result = new List<string>();
            var product = data.Value("product");
            var image_url = product.Value("image_url");
            var children = image_url.Children();
            foreach (var child in children)
            {
                var imageUrl = child.First().Value().ToString();
                if (imageUrl.Contains("?"))
                {
                    var indexOf = imageUrl.IndexOf("?");
                    imageUrl = imageUrl.Substring(0, indexOf);
                }
                result.Add(imageUrl);
            }
            if (result.Count == 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            return result;
        }

        public string GetDetailPageHtml(JToken data)
        {
            var product = data.Value("product");
            var content = product.Value<string>("content");
            var detailHtml = HtmlReader.GetDafaultHtml(content);
            //var detail = data.Value("detail").Value<string>("html");
            //var detailHtml = HtmlReader.GetDafaultHtml(detail);
            //var script = data.Value("detail").Value<string>("script");
            return content;
        }

        public Option GetOption(JToken data)
        {
            var product = data.Value("product");
            var check = product.Value("options");
            if (check == null) return null;
            var optionGroups = new List<Option.OptionGroup>();
            var options = product.Value("options").ToList();
            foreach (JToken option in options)
            {
                var optionGroup = new Option.OptionGroup();
                var code = option.Value<string>("code");
                if (string.IsNullOrWhiteSpace(code)) continue;
                var name = option.Value<string>("name");
                if (string.IsNullOrWhiteSpace(name)) continue;

                optionGroup.optionId = code;
                optionGroup.groupName = ProductHelper.CreateName(this.Language, name);

                optionGroup.groups = new List<Option.OptionGroup.Group>();
                var value_list = option.Value("value_list");
                foreach (var value_item in value_list.Children())
                {
                    var indexOf = value_item.Path.LastIndexOf(".");
                    var key = value_item.Path.Substring(indexOf + 1);
                    var value = value_item.First().Value().ToString();

                    var group = new Option.OptionGroup.Group();
                    group.groupId = key;
                    group.optionName = ProductHelper.CreateName(this.Language, value);
                    optionGroup.groups.Add(group);
                }
                optionGroups.Add(optionGroup);
            }

            string groupId = ProductHelper.GetOptionGroupId(optionGroups);
            string groupName = ProductHelper.GetOptionGroupName(optionGroups);

            var detailOptions = new List<Option.DetailOption>();
            var options_details = product.Value("options_detail").ToList();
            foreach (var options_detail in options_details)
            {
                Option.DetailOption detailOption = new Option.DetailOption();
                detailOption.optionGroupName = ProductHelper.CreateName(this.Language, groupName);

                var option_code_list = options_detail.Value("option_code_list").ToList();
                var value_code_list = options_detail.Value("value_code_list").ToList();

                List<Option.OptionGroup.Group> groups = new List<Option.OptionGroup.Group>();
                foreach (var option_code in option_code_list)
                {
                    var find = optionGroups.Find(x => x.optionId == option_code.ToString());
                    foreach (var value_code in value_code_list)
                    {
                        Option.OptionGroup.Group findGroup = find.groups.Find(x => x.groupId == value_code.ToString());
                        if (findGroup != null)
                        {
                            groups.Add(findGroup);
                        }
                    }
                }

                IEnumerable<string> ids = groups.Select(x => x.groupId);
                detailOption.optionIds = string.Join("ㅡ", ids);

                IEnumerable<string> optionNames = groups.Select(x => x.optionName.First().text);
                detailOption.optionName = ProductHelper.CreateName(this.Language, string.Join("ㅡ", optionNames));

                double? price = options_detail.Value<double?>("price");
                if (price == null || price == 0) continue;
                detailOption.price = ProductHelper.CreatePrice(this.Currency, price, price);

                int? stock = options_detail.Value<int?>("stock");

                detailOption.quantity = stock == null ? 0 : (int)stock;
                detailOption.isSoldOut = detailOption.quantity <= 0;

                if (detailOption.isSoldOut)
                {
                    string status = options_detail.Value<string>("status");
                    if (status.ToUpper() == "SALE")
                    {
                        detailOption.isSoldOut = false;
                        detailOption.quantity = 9;
                    }
                    else
                    {
                        // 추후 발견 시 처리
                    }
                }

                detailOptions.Add(detailOption);
            }

            Option result = new Option();
            result.optionGroups = optionGroups;
            result.detailOptions = detailOptions;
            return result;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var product = new Product();
            product.site = this.Site;
            string code = GetCode(url);
            product.code = code;
            product.url = url;
            product.currency = this.Currency.ToString();
            try
            {
                string html = await ChatHubService.GetRequest(connectionId, url);
                //var html = CreateRequest("GET", "/brand-product/?idx=1152").ReadToEnd();
                var data = GetData(html, url);
                product.title = GetTitle(data);
                product.brand = GetBrand(data);
                product.descs = GetDescs(data);
                product.price = GetPrice(data);
                product.mainImages = GetMainImages(data);
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
            string html = await ChatHubService.GetRequest(connectionId, url);
            var data = GetData(html, url);
            return new CollectRawData(url, data, html);
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
