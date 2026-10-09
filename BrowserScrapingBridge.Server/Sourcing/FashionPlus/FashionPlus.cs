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

namespace BrowserScrapingBridge.Server.Sourcing.FashionPlus
{
    public class FashionPlus : ICollect
    {
        private string host = "https://www.fashionplus.co.kr";
        public string Site => nameof(FashionPlus);
        public string Name => "패션플러스";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public async Task<string> GetLoginUrlAsync(string connectionId) => null;

        public string GetCode(string url)
        {
            Uri uri = new Uri(url);
            string code = uri.Segments.Last();
            return code;
        }

        private async Task<List<CollectPageInfo>> GetPageDatas1(string connectionId, UriBuilderAdvance uriBuilderAdvance)
        {
            uriBuilderAdvance.Segments.Add("/fetch-goods");
            string pageUrl = uriBuilderAdvance.ToString();
            string html = await ChatHubService.GetRequest(connectionId, pageUrl);
            JObject data = html.ToJObject();
            if (data == null) return null;
            List<JToken> items = data["items"]?.ToList();
            return GetCollectPageInfos(items);
        }

        private async Task<List<CollectPageInfo>> GetPageDatas2(string connectionId, UriBuilderAdvance uriBuilderAdvance)
        {
            uriBuilderAdvance.Segments.Add("/fetch");
            string pageUrl = uriBuilderAdvance.ToString();
            string html = await ChatHubService.GetRequest(connectionId, pageUrl);
            JObject data = html.ToJObject();
            if (data == null) return null;
            List<JToken> items = data["items"]?.ToList();
            return GetCollectPageInfos(items);
        }

        private string CreateQueries(string url, int page)
        {
            url = url.ToHttpUrlDecodeComplete();

            var keys = new Dictionary<string, List<string>>();
            string[] queries = url.SingleSplit("?").Split("&");
            foreach (string query in queries)
            {
                int equal = query.IndexOf("=");
                string value = query.Substring(equal + 1);
                string key = query.Substring(0, equal);
                int idxOf = key.IndexOf("[");
                if (idxOf > 0)
                {
                    key = key.Substring(0, idxOf);
                }

                List<string> values = default;
                bool tryGet = keys.TryGetValue(key, out values);
                if (values == null)
                {
                    values = new List<string>();
                }

                if (key == "page")
                {
                    values.Add(page.ToString());
                }
                else
                {
                    values.Add(value);
                }

                if (tryGet == false)
                {
                    keys.Add(key, values);
                }
            }

            var queryBuilder = new List<string>();
            foreach (var item in keys)
            {
                var finds = item.Value.FindAll(x => string.IsNullOrWhiteSpace(x) == false);
                var join = string.Join(",", finds);
                queryBuilder.Add($"{item.Key}={join}");
            }

            return string.Join("&", queryBuilder);
        }

        private async Task<List<CollectPageInfo>> GetPageDatas3(string connectionId, UriBuilderAdvance uriBuilderAdvance)
        {
            var segments = uriBuilderAdvance.Segments;
            segments[segments.Count - 1] = "fetch";
            string pageUrl = uriBuilderAdvance.ToString();
            string html = await ChatHubService.GetRequest(connectionId, pageUrl);
            JObject data = html.ToJObject();
            if (data == null) return null;
            List<JToken> items = data["goodsPaginator"]?["items"]?.ToList();
            return GetCollectPageInfos(items);
        }

        private async Task<List<CollectPageInfo>> GetPageDatas4(string connectionId, UriBuilderAdvance uriBuilderAdvance)
        {
            var postBody = new JObject();
            Dictionary<string, string> queryDictionary = uriBuilderAdvance.queryDictionary;
            foreach (KeyValuePair<string, string> item in queryDictionary)
            {
                postBody[item.Key] = item.Value;
            }
            string html = await ChatHubService.PostRequest(connectionId, "https://www.fashionplus.co.kr/goods/special-store/fetch", null, postBody.ToString());
            JObject data = html.ToJObject();
            if (data == null) return null;
            List<JToken> items = data?["items"]?.ToList();
            return GetCollectPageInfos(items);
        }

        private List<CollectPageInfo> GetCollectPageInfos(List<JToken> items)
        {
            if (items == null || items.Count <= 0) return null;
            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            foreach (JToken item in items)
            {
                string id = item.Value<string>("id");
                if (string.IsNullOrWhiteSpace(id)) continue;
                string itemUrl = $"https://www.fashionplus.co.kr/goods/detail/{id}";
                string name = item.Value<string>("name");
                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = id;
                collectPageInfo.Url = itemUrl;
                collectPageInfo.Title = name;
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }



        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            List<CollectPageInfo> result = null;

            url = url.ToHttpUrlDecodeComplete();
            string address = url.Substring(0, url.IndexOf("?"));
            var createQueries = CreateQueries(url, page);

            UriBuilderAdvance uriBuilderAdvance = new UriBuilderAdvance($"{address}?{createQueries}");
            List<string> segments = uriBuilderAdvance.Segments;

            string segment = segments[2];
            switch (segment)
            {
                case "brand-shop/":
                case "mall/":
                case "group-mall/":
                case "item-shop":
                    result = await GetPageDatas1(connectionId, uriBuilderAdvance);
                    break;
                case "department-store/":
                    result = await GetPageDatas2(connectionId, uriBuilderAdvance);
                    break;
                case "goods/":
                    result = await GetPageDatas3(connectionId, uriBuilderAdvance);
                    break;
                case "special-store/": // post
                    result = await GetPageDatas4(connectionId, uriBuilderAdvance);
                    break;
                default:
                    throw new Exception("서비스하지 않는 패션플러스 페이지 입니다.\n관리자에게 문의 해주세요.");
            }
            return result;
        }

        [Obsolete("이전버전")]
        public JToken GetData_TEMP(string html, string optionHtml)
        {
            try
            {
                List<string> scripts = html.GetValues("//script", TextEx.XPathOption.InnerHtml);
                scripts = scripts.FindAll(x => string.IsNullOrWhiteSpace(x) == false);
                string findScript = scripts.Find(x => x.Contains("var defaultGoodsEventProperties"));
                if (string.IsNullOrWhiteSpace(findScript)) throw new Exception();
                var defaultGoodsEventProperties = findScript.SingleSplit("var defaultGoodsEventProperties = ampli.getDetailPageEvent({", "})");
                if (string.IsNullOrWhiteSpace(defaultGoodsEventProperties)) throw new Exception();
                defaultGoodsEventProperties = $"[{{{defaultGoodsEventProperties}}}]";
                JArray jArray = defaultGoodsEventProperties?.ToJArray();
                if (jArray == null || jArray.Count == 0) throw new Exception();
                JToken data = jArray.First();
                if (string.IsNullOrWhiteSpace(optionHtml) == false)
                {
                    JArray options = optionHtml?.ToJArray();
                    if (options != null && options.Count > 0)
                    {
                        data["optionDatas"] = options;
                    }
                }
                return data;
            }
            catch (Exception exception)
            {
                throw new Exception($"상품 정보를 찾을 수 없습니다.", exception);
            }
        }

        public JToken GetData(string html, string optionHtml)
        {
            try
            {
                JArray datas = html.ToJArray();
                JToken data = datas.FirstOrDefault();
                if (data == null) throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품 입니다.");
                if (string.IsNullOrWhiteSpace(optionHtml) == false)
                {
                    JArray options = optionHtml?.ToJArray();
                    if (options != null && options.Count > 0)
                    {
                        data["optionDatas"] = options;
                    }
                }
                return data;
            }
            catch (SaleException se)
            {
                throw se;
            }
            catch (Exception e)
            {
                throw new Exception("상품 정보를 찾을 수 없습니다.", e);
            }
        }

        public List<Name> GetTitle(JToken data)
        {
            string name = data.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("상품명을 찾을 수 없습니다.");
            return new List<Name> { new Name(Language.KR, name) };
        }

        public List<Name> GetBrand(JToken data)
        {
            string name = data["brand"]?.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) return null;
            return new List<Name> { new Name(Language.KR, name) };
        }

        public string GetModel(JToken data)
        {
            return null;
        }

        public List<Desc> GetDescs_TEMP(JToken data)
        {
            var mandatories = data["mandatories"];
            if (mandatories == null || mandatories.HasValues == false) return null;
            var descs = new List<Desc>();
            int count = 1;
            foreach (JToken item in mandatories.ToList())
            {
                string label = item.Value<string>("label");
                string value = item.Value<string>("value");
                if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value)) continue;
                Desc desc = new Desc(count, Language.KR, label, value);
                descs.Add(desc);
                count++;
            }
            return descs;
        }

        public List<Desc> GetDescs(string html)
        {
            var descs = new List<Desc>();

            int count = 1;
            List<string> tables = html.SelectNodes("//section//table", HtmlType.OuterHtml);
            if (tables == null) return descs;
            foreach (string table in tables)
            {
                List<string> trs = table.SelectNodes("//tr", HtmlType.OuterHtml);
                foreach (string tr in trs)
                {
                    string th = tr.SelectNode("//th", HtmlType.InnerText);
                    if (string.IsNullOrWhiteSpace(th)) continue;

                    string td = tr.SelectNode("//td", HtmlType.InnerText);
                    if (string.IsNullOrWhiteSpace(td)) continue;

                    th = th.NormalizeSpaces();
                    td = td.NormalizeSpaces();

                    Desc desc = new Desc(count, Language.KR, th, td);
                    descs.Add(desc);
                    count++;
                }
            }
            return descs;
        }

        public List<Tag> GetTags(JToken data)
        {
            return null;
        }

        public List<Price> GetPrice(JToken data)
        {
            var prices = new List<double>();
            double? consumerPrice = data.Value<double?>("consumerPrice");
            if (consumerPrice != null && consumerPrice > 0) prices.Add((double)consumerPrice);
            double? salePrice = data.Value<double?>("salePrice");
            if (salePrice != null && salePrice > 0) prices.Add((double)salePrice);
            if (prices.Count == 0) throw new Exception("상품 가격을 찾을 수 없습니다.");
            return new List<Price>() { new Price(Currency.KRW, prices.Max(), prices.Min(), true) };
        }

        public List<string> GetMainImages(JToken data)
        {
            var thumbnailUrl = data.Value<string>("thumbnailUrl");
            if (string.IsNullOrWhiteSpace(thumbnailUrl)) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            var quertStartIndex = thumbnailUrl.IndexOf("?");
            if (quertStartIndex >= 0)
            {
                thumbnailUrl = thumbnailUrl.Substring(0, quertStartIndex);
            }
            return new List<string> { thumbnailUrl };
        }

        [Obsolete("패션 플러스에서 상품 이미지 수집 시 추가이미지에서 호스팅 문제가 발생하는 이미지가 빈번히 발생됨에 따라 추후에 정상화 이후 사용")]
        public List<string> GetMainImages_TEMP(string html)
        {
            var dataPreloads = html.SelectNodes("//div[@class='m_prodetail-head-thumbnail']//button[@class='btn_thumbnail']//i", "data-preload");
            if (dataPreloads == null || dataPreloads.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            var mainImages = new List<string>();
            foreach (string dataPreload in dataPreloads)
            {
                JObject data = dataPreload.ToJObject();
                if (data == null) continue;
                var _src = data.Value<string>("_src");
                if (string.IsNullOrWhiteSpace(_src) == false)
                {
                    int queryStartIndex = _src.IndexOf("?");
                    if (queryStartIndex > 0)
                    {
                        _src = _src.Substring(0, queryStartIndex);
                    }
                    mainImages.Add(_src);
                }
            }
            if (mainImages.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            return mainImages;
        }

        public string GetDetailPageHtml_TEMP(JToken data)
        {
            string description = data.Value<string>("description");
            if (string.IsNullOrWhiteSpace(description)) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            var dataPreloads = description.GetValues("//img", "data-preload");

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("<div>");
            foreach (var dataPreload in dataPreloads)
            {
                JObject detailData = dataPreload.ToJObject();
                if (detailData == null) continue;
                string src = detailData.Value<string>("_src");
                if (string.IsNullOrWhiteSpace(src)) continue;
                stringBuilder.AppendLine($"<img src=\"{src}\">");
            }
            stringBuilder.Append("</div>");

            var detailHtml = stringBuilder.ToString();
            return detailHtml;
        }
        public string GetDetailPageHtml(string html)
        {
            List<string> imgs = html.SelectNodes("//img", HtmlType.OuterHtml);

            var sb = new StringBuilder();
            sb.AppendLine("<div>");

            for (int i = 0; i < imgs.Count; i++)
            {
                string img = imgs[i];

                string src = img.SelectNode("//img", "src");
                if (src.IndexOf("http") == 0)
                {
                    string newImg = $"<img src='{src}'>";
                    sb.AppendLine(newImg);
                }
                else
                {
                    string preload = img.SelectNode("//img", "data-preload");
                    if (string.IsNullOrWhiteSpace(preload)) continue;
                    JObject data = preload.ToJObject();
                    if (data == null) continue;
                    string _src2 = data.Value<string>("_src2");
                    if (string.IsNullOrWhiteSpace(_src2)) _src2 = data.Value<string>("_src");
                    if (string.IsNullOrWhiteSpace(_src2)) continue;
                    if (_src2.IndexOf("data") == 0) continue;
                    string newSrc = _src2.IndexOf("http") == 0 ? _src2 : $"{host}{_src2}";
                    string newImg = $"<img src='{newSrc}'>";
                    sb.AppendLine(newImg);
                }
            }
            sb.AppendLine("</div>");
            string detailHtml = sb.ToString();
            return detailHtml;
        }

        public Option GetOption(JToken data)
        {
            JToken optionDatas = data["optionDatas"];
            if (optionDatas == null || optionDatas.HasValues == false) return null;
            JToken optionData = optionDatas?.ToList()?.First();
            if (optionData == null || optionData.HasValues == false) return null;
            List<JToken> options = optionData["options"]?.ToList();
            if (options == null || options.Count <= 0) return null;

            var option = new Option();
            option.optionGroups = new List<Option.OptionGroup>();
            var optionGroup = new Option.OptionGroup();
            optionGroup.optionId = "optionGroupId";
            var groupName = new List<Name> { new Name(Language.KR, "옵션을 선택하세요") };
            optionGroup.groupName = groupName;
            optionGroup.groups = new List<Option.OptionGroup.Group>();
            option.detailOptions = new List<Option.DetailOption>();

            double defaultPrice = optionData.Value<double>("_price");
            foreach (JToken optionItem in options)
            {
                string _id = optionItem.Value<string>("_id");
                if (string.IsNullOrWhiteSpace(_id)) continue;
                string _name = optionItem.Value<string>("_name");
                if (string.IsNullOrWhiteSpace(_name)) continue;
                double optionPrice = optionItem.Value<double>("_price");
                int? stock = optionItem.Value<int?>("_stock");

                var group = new Option.OptionGroup.Group();
                group.groupId = _id;
                var optionName = new List<Name> { new Name(Language.KR, _name.Trim()) };
                group.optionName = optionName;
                optionGroup.groups.Add(group);

                double salePrice = defaultPrice < optionPrice ? defaultPrice : optionPrice;
                double originalPrice = defaultPrice >= optionPrice ? defaultPrice : optionPrice;

                var detailOption = new Option.DetailOption();
                detailOption.optionIds = _id;
                detailOption.optionGroupName = groupName;
                detailOption.optionName = optionName;
                detailOption.price = new List<Price> { new Price(Currency.KRW, originalPrice, salePrice) };
                if (stock == null || stock <= 0)
                {
                    detailOption.isSoldOut = true;
                    detailOption.quantity = 0;
                }
                else
                {
                    detailOption.isSoldOut = false;
                    detailOption.quantity = (int)stock;
                }
                option.detailOptions.Add(detailOption);
            }
            option.optionGroups.Add(optionGroup);
            return option;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);

            var text_sale = html.SelectNode("//div[@class='m_prodetail']//p[@class='text_sale']", HtmlType.InnerText);


            CollectExtension.CollectSleep();

            string code = GetCode(url);
            string body = new { goodsIdConcat = code }.ToSerializeObject();
            string goodsJson = await ChatHubService.PostRequest(connectionId, $"https://www.fashionplus.co.kr/global/shopping/add-recent-view-goods", null, body);
            CollectExtension.CollectSleep();

            string optionHtml = await ChatHubService.GetRequest(connectionId, $"https://www.fashionplus.co.kr/goods/detail/{code}/fetch-option-data");
            CollectExtension.CollectSleep();

            string detailHtml = await ChatHubService.GetRequest(connectionId, $"https://www.fashionplus.co.kr/goods/detail/{code}/description-iframe");
            CollectExtension.CollectSleep();

            Product product = new Product();
            try
            {
                product.code = code;
                product.site = this.Site;
                product.url = $"https://www.fashionplus.co.kr/goods/detail/{code}";
                product.currency = Currency.KRW.ToString();
                JToken data = GetData(goodsJson, optionHtml);
                product.title = GetTitle(data);
                product.brand = GetBrand(data);
                product.model = GetModel(data);
                product.descs = GetDescs(html);
                product.tags = GetTags(data);
                product.mainImages = GetMainImages(data);
                product.detailPageHtml = GetDetailPageHtml(detailHtml);
                product.price = GetPrice(data);
                product.option = GetOption(data);
                product.regDate = DateTime.Now;

                if (string.IsNullOrWhiteSpace(text_sale) == false)
                {
                    text_sale = text_sale.Trim().ToUpper();
                    if (text_sale == "SOLD OUT")
                    {
                        throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품 입니다.");
                    }
                }
            }
            catch (SaleException se)
            {
                product.saleStatus = se.SaleStatus;
                product.collectStatus = ProductCollectStatus.Success;
                product.memo = se.Message;
            }
            catch (Exception exception)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = exception.Message;
            }
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
