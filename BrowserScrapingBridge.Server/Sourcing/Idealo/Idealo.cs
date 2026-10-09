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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Idealo
{
    public class Idealo : ICollect
    {
        public string Site => nameof(Idealo);
        public string Name => "이데알로";
        public Language Language => Language.DE;
        public Currency Currency => Currency.EUR;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        public async Task<string> GetHtml(string url)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Connection", "keep-alive");
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36");
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            headers.Add("Accept-Language", "ko,en-US;q=0.9,en;q=0.8,ko-KR;q=0.7");

            using (var httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient())
            {
                httpClient.SetHeader(headers);
                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url);
                string response = await httpResponseMessage.Content.ReadAsStringAsync();
                return response;
            }
        }

        public string GetCode(string url)
        {
            var uri = new Uri(url);
            string segment = uri.Segments.Last();
            int indexOf = segment.IndexOf("_");
            if (indexOf > 0)
            {
                segment = segment.Substring(0, indexOf);
            }
            if (string.IsNullOrWhiteSpace(segment)) throw new Exception("상품 코드를 찾을 수 없습니다.");
            string code = segment;
            return code;
        }
        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }

        private async Task<List<CollectPageInfo>> GetCollectPageInfo1(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            var fileName = Path.GetFileNameWithoutExtension(url);
            int categoryCode;
            if (int.TryParse(fileName, out categoryCode) == false)
            {
                categoryCode = 100;
            }
            int next = (page - 1) * 15;
            string pageName = $"{categoryCode}I16-{next}";
            uriBuilderAdvance.Segments[uriBuilderAdvance.Segments.Count - 1] = $"{pageName}.html";
            string pageUrl = uriBuilderAdvance.ToString();

            string html = await ChatHubService.GetRequest(connectionId, pageUrl);
            string tagManagerDataLayer = html.GetValue("//script[@id='tagManagerDataLayer']", TextEx.XPathOption.InnerHtml);
            string utag_data = tagManagerDataLayer.SubstringBetween("var utag_data = [", "];", false);
            JObject data = utag_data.ToJObject();
            if (data == null) return null;
            List<JToken> products = data["products"]?.ToList();
            if (products == null || products.Count <= 0) return null;
            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            foreach (JToken product in products)
            {
                string id = product.Value<string>("id");
                if (string.IsNullOrWhiteSpace(id)) continue;
                string itemUrl = $"https://www.idealo.de/preisvergleich/OffersOfProduct/{id}";
                string name = product.Value<string>("name");
                var collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = id;
                collectPageInfo.Url = itemUrl;
                collectPageInfo.Title = name;
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfo2(string connectionId, string url, int page)
        {
            var uriBuilderAdvance1 = new UriBuilderAdvance(url);
            var query = uriBuilderAdvance1.GetQuery("q");
            if (string.IsNullOrWhiteSpace(query)) return null;

            //page = 3;
            var uriBuilderAdvance2 = new UriBuilderAdvance("https://www.idealo.de/csr/api/v2/modules/mainSearchResult");
            uriBuilderAdvance2.SetQuery("locale", "de_DE");
            if (page > 1)
            {
                uriBuilderAdvance2.SetQuery("pageIndex", (page - 1).ToString());
            }
            uriBuilderAdvance2.SetQuery("itemsPerPage", "60");
            //uriBuilderAdvance2.SetQuery("itemStates", "EXCLUDE_USED");
            uriBuilderAdvance2.SetQuery("query", query);
            uriBuilderAdvance2.SetQuery("allowDelphi", "true");
            string requestUrl = uriBuilderAdvance2.ToString();
            string json = await ChatHubService.GetRequest(connectionId, requestUrl);
            JObject data = json.ToJObject();
            if (data == null) return null;
            List<JToken> items = data["items"]?.ToList();
            if (items == null || items.Count <= 0) return null;

            List<CollectPageInfo> result = new List<CollectPageInfo>();
            foreach (JToken item in items)
            {
                var id = item.Value<string>("id");
                if (string.IsNullOrWhiteSpace(id)) continue;
                var title = item.Value<string>("title");

                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = id;
                collectPageInfo.Url = $"https://www.idealo.de/preisvergleich/OffersOfProduct/{id}";
                collectPageInfo.Title = title;
                result.Add(collectPageInfo);
            }
            return result;
        }
        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            List<CollectPageInfo> collectPageInfo1 = await GetCollectPageInfo1(connectionId, url, page);
            if (collectPageInfo1 != null) return collectPageInfo1;

            var html = await ChatHubService.GetRequest(connectionId, url);

            List<CollectPageInfo> collectPageInfo2 = await GetCollectPageInfo2(connectionId, url, page);
            if (collectPageInfo2 != null) return collectPageInfo2;

            return null;
        }

        private JObject GetData(string html)
        {
            var data = new JObject();

            string product = html.GetValue("//script[@data-testid=\"Product\"]");
            if (string.IsNullOrEmpty(product) == false) data["product"] = product.ToJObject();

            string productGroup = html.GetValue("//script[@data-testid=\"ProductGroup\"]");
            if (string.IsNullOrWhiteSpace(productGroup) == false) data["productGroup"] = productGroup.ToJObject();

            var astroSlots = html.SelectNodes("//astro-island", HtmlType.OuterHtml);
            if (astroSlots != null)
            {
                var astroSlotArray = new JArray();
                for (int i = 0; i < astroSlots.Count; i++)
                {
                    string astroSlot = astroSlots[i];
                    var astroSlotItem = new JObject();
                    astroSlotItem[$"props"] = astroSlot.SelectNode("//astro-island", "props").ToHtmlDecodeComplete().ToJObject();
                    astroSlotItem[$"opts"] = astroSlot.SelectNode("//astro-island", "opts").ToHtmlDecodeComplete().ToJObject();
                    astroSlotArray.Add(astroSlotItem);
                }
                data["astro-island"] = astroSlotArray;
            }

            var oopStageDeltaFilters = html.GetValues("//div[@class='oopStage-deltaFilter']", TextEx.XPathOption.OuterHtml);
            if (oopStageDeltaFilters != null && oopStageDeltaFilters.Count > 0)
            {
                data["oopStage-deltaFilter"] = new JObject();
                for (int i = 0; i < oopStageDeltaFilters.Count; i++)
                {
                    string oopStageDeltaFilter = oopStageDeltaFilters[i];

                    var label = oopStageDeltaFilter.GetSelectNodeInnerText("//div[@class='oopStage-deltaFilterLabel']//strong");
                    var idx = label.IndexOf(":");
                    if (idx > 0) label = label.Substring(0, idx).Trim();
                    var deltaFilters = new JArray();
                    List<string> deltaFilterButtons = oopStageDeltaFilter.GetValues("//div[@class='oopStage-deltaFilterButtons']//div", TextEx.XPathOption.OuterHtml);
                    for (int j = 0; j < deltaFilterButtons.Count; j++)
                    {
                        var deltaFilterButton = deltaFilterButtons[j];
                        var link = deltaFilterButton.GetValue("//div", "data-async-link");
                        link = $"https://www.idealo.de{link}";

                        var post = deltaFilterButton.GetValue("//div", "data-async-data");
                        var attribute = deltaFilterButton.GetValue("//div", "data-trx-click-data").ToJObject();
                        if (attribute == null)
                        {
                            continue;
                        }
                        attribute["post"] = post;
                        attribute["link"] = link;
                        deltaFilters.Add(attribute);
                    }
                    data["oopStage-deltaFilter"][label] = deltaFilters;
                }
            }
            return data;
        }

        public List<Name> GetTitle(JObject data)
        {
            string name = data["productGroup"].Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("상품명을 찾을 수 없습니다.");
            List<Name> result = new List<Name>() { new Name(this.Language, name) };
            return result;
        }

        public List<Name> GetBrand(JObject data)
        {
            string name = data["productGroup"]?["brand"]?.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("브랜드를 찾을 수 없습니다.");
            List<Name> result = new List<Name>() { new Name(this.Language, name) };
            return result;
        }

        public List<Desc> GetDescs()
        {
            return null;
        }

        public List<Tag> GetTags()
        {
            return null;
        }

        public List<Price> GetPrice(JObject data)
        {
            JToken offers = data["product"]?["offers"];
            if (offers == null) throw new Exception("가격 정보를 확인할 수 없습니다.");

            double? highPrice = offers?.Value<double>("highPrice");
            if (highPrice == null || highPrice <= 0) throw new Exception("가격 정보를 확인할 수 없습니다.");

            double? lowPrice = offers?.Value<double>("lowPrice");
            if (lowPrice == null || lowPrice <= 0) throw new Exception("가격 정보를 확인할 수 없습니다.");

            var result = ProductHelper.CreatePrice(this.Currency, highPrice, lowPrice, true);
            return result;
        }

        public List<string> GetMainImages(JObject data)
        {
            var images = data["product"]?["image"]?.ToList();
            if (images == null || images.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");

            var result = new List<string>();
            foreach (JToken image in images)
            {
                result.Add(image.ToString());
            }
            return result;
        }

        public string GetVideoUrl()
        {
            return null;
        }

        public string GetDetailPageHtml(string html)
        {
            string datasheet = html.SelectNode("//div[@id='datasheet']", HtmlType.OuterHtml);
            if (string.IsNullOrWhiteSpace(datasheet)) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            var dafaultHtml = HtmlReader.GetDafaultHtml(datasheet);
            return dafaultHtml;
        }

        public async Task<Option> GetOption(string connectionId, JObject data, string html)
        {
            try
            {
                string name = data["productGroup"]?.Value<string>("name");
                if (string.IsNullOrWhiteSpace(name)) return null;
                string productGroupID = data["productGroup"]?.Value<string>("productGroupID");
                if (string.IsNullOrWhiteSpace(productGroupID)) return null;

                Option option = new Option();
                option.optionGroups = new List<Option.OptionGroup>();
                option.detailOptions = new List<Option.DetailOption>();

                var optionGroup = new Option.OptionGroup();
                optionGroup.optionId = productGroupID;
                optionGroup.groupName = ProductHelper.CreateName(this.Language, "Variant");
                optionGroup.groups = new List<Option.OptionGroup.Group>();

                var optionHtml = await ChatHubService.GetRequest(connectionId, $"https://www.idealo.de/offerpage/variantoverlay/product/{productGroupID}/filters/none");
                List<string> listItems = optionHtml.SelectNodes("//div[contains(@class,'productVariants-listItem small-6 large-4 xlarge-3 columns')]", HtmlType.OuterHtml);
                for (int i = 0; i < listItems.Count; i++)
                {
                    Option.OptionGroup.Group group = new Option.OptionGroup.Group();
                    group.groupId = i.ToString();
                    string listItem = listItems[i];
                    var src = listItem.SelectNode("//img", "src").GetUriString();
                    if (string.IsNullOrWhiteSpace(src) == false) group.optionImage = src;
                    var title = listItem.GetSelectNodeInnerText("//span[@class='productVariants-listItemWrapperTitle-short']")?.Trim();
                    if (string.IsNullOrWhiteSpace(title)) continue;
                    group.optionName = ProductHelper.CreateName(this.Language, title);
                    optionGroup.groups.Add(group);
                }
                option.optionGroups.Add(optionGroup);

                var oopStage_deltaFilter = data["oopStage-deltaFilter"];
                if (oopStage_deltaFilter != null)
                {
                    var children = oopStage_deltaFilter.Children();

                    List<JToken> deltaFilters = new List<JToken>();
                    foreach (JToken child in children)
                    {
                        List<JToken> items = child.First()?.ToList();
                        if (items == null || items.Count <= 0) continue;
                        deltaFilters.AddRange(items);
                    }

                    for (int i = 0; i < deltaFilters.Count; i++)
                    {
                        var deltaFilter = deltaFilters[i];
                        var product_attribute_id = deltaFilter.Value<string>("product_attribute_id");
                        if (string.IsNullOrWhiteSpace(product_attribute_id) || product_attribute_id.Contains("delta")) continue;

                        var product_attribute_name = deltaFilter.Value<string>("product_attribute_name");
                        var product_attribute_value = deltaFilter.Value<string>("product_attribute_value").ToHtmlDecodeComplete();
                        if (product_attribute_value.Contains("½"))
                        {
                            product_attribute_value = product_attribute_value.Replace("½", ".5");
                        }
                        Option.OptionGroup.Group group = new Option.OptionGroup.Group();
                        group.groupId = i.ToString();
                        group.optionName = ProductHelper.CreateName(this.Language, product_attribute_value);

                        var find = option.optionGroups.Find(x => x.groupName.First().text == product_attribute_name);
                        if (find == null)
                        {
                            var optionGroup2 = new Option.OptionGroup();
                            optionGroup2.optionId = product_attribute_id;
                            optionGroup2.groupName = ProductHelper.CreateName(this.Language, product_attribute_name);
                            optionGroup2.groups = new List<Option.OptionGroup.Group>();
                            group.optionName = ProductHelper.CreateName(this.Language, product_attribute_value);
                            optionGroup2.groups.Add(group);
                            option.optionGroups.Add(optionGroup2);
                        }
                        else
                        {
                            find.groups.Add(group);
                        }
                    }
                }

                JToken offers = data["product"]?["offers"];
                double? highPrice = offers?.Value<double>("highPrice");
                double? lowPrice = offers?.Value<double>("lowPrice");
                string getOptionGroupName = ProductHelper.GetOptionGroupName(option.optionGroups);
                
                List<JToken> hasVariants = data["productGroup"]?["hasVariant"]?.ToList();
                hasVariants = hasVariants.FindAll(x => x["name"] != null);
                
                switch (option.optionGroups.Count)
                {
                    case 0:
                        return null;

                    case 1:
                        Option.OptionGroup firstOptionGroup = option.optionGroups.First();
                        var optionGroupName = firstOptionGroup.groupName.First().text;
                        foreach (Option.OptionGroup.Group group in optionGroup.groups)
                        {
                            var detailOption = new Option.DetailOption();
                            detailOption.optionIds = group.groupId;
                            detailOption.optionGroupName = ProductHelper.CreateName(this.Language, optionGroupName);
                            detailOption.optionName = ProductHelper.CreateName(this.Language, group.optionName.First().text);
                            JToken find = hasVariants.Find(x => x["name"].ToString().Contains(group.optionName.First().text));
                            if (find != null)
                            {
                                var variantOffers = find["offers"];
                                if (variantOffers == null)
                                {
                                    detailOption.price = ProductHelper.CreatePrice(this.Currency, highPrice, lowPrice);
                                }
                                else
                                {
                                    var variantLowPrice = variantOffers.Value<double?>("lowPrice");
                                    var variantHighPrice = variantOffers.Value<double?>("highPrice");
                                    detailOption.price = ProductHelper.CreatePrice(this.Currency, variantHighPrice, variantLowPrice);
                                }
                            }
                            detailOption.isSoldOut = false;
                            detailOption.quantity = 100;
                            option.detailOptions.Add(detailOption);
                        }
                        break;

                    case 2:
                        Option.OptionGroup option1 = option.optionGroups[0];
                        Option.OptionGroup option2 = option.optionGroups[1];

                        string groupName1 = option1.groupName.First().text;
                        string groupName2 = option2.groupName.First().text;
                        string groupName = string.Join("ㅡ", groupName1, groupName2);

                        for (int i = 0; i < option1.groups.Count; i++)
                        {
                            for (int j = 0; j < option2.groups.Count; j++)
                            {
                                Option.OptionGroup.Group group1 = option1.groups[i];
                                Option.OptionGroup.Group group2 = option2.groups[j];
                                var detailOption = new Option.DetailOption();
                                detailOption.optionIds = string.Join("ㅡ", group1.groupId, group2.groupId);
                                detailOption.optionGroupName = ProductHelper.CreateName(this.Language, groupName);
                                var optionName = string.Join("ㅡ", group1.optionName.First().text, group2.optionName.First().text);
                                detailOption.optionName = ProductHelper.CreateName(this.Language, optionName);

                                JToken find = hasVariants.Find(x => x["name"].ToString().Contains(group1.optionName.First().text));
                                if (find != null)
                                {
                                    var variantOffers = find["offers"];
                                    if (variantOffers == null)
                                    {
                                        detailOption.price = ProductHelper.CreatePrice(this.Currency, highPrice, lowPrice);
                                    }
                                    else
                                    {
                                        var variantLowPrice = variantOffers.Value<double?>("lowPrice");
                                        var variantHighPrice = variantOffers.Value<double?>("highPrice");
                                        detailOption.price = ProductHelper.CreatePrice(this.Currency, variantHighPrice, variantLowPrice);
                                    }
                                }
                                else
                                {
                                    detailOption.price = ProductHelper.CreatePrice(this.Currency, highPrice, lowPrice);
                                }
                                detailOption.isSoldOut = false;
                                detailOption.quantity = 9;
                                option.detailOptions.Add(detailOption);
                            }
                        }
                        break;

                    default:
                        throw new Exception("구현이 필요한 상품입니다.");
                }
                return option;
            }
            catch (Exception e)
            {
                throw new Exception("옵션 수집 중 문제가 발생했습니다.", e);
            }
        }
        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var html = await ChatHubService.GetRequest(connectionId, url);
            Product product = new Product();
            try
            {
                JObject data = GetData(html);

                product.site = this.Site;
                product.url = url;
                product.code = GetCode(url);
                product.currency = Currency.EUR.ToString();
                product.title = GetTitle(data);
                product.brand = GetBrand(data);
                product.descs = GetDescs();
                product.tags = GetTags();
                product.price = GetPrice(data);
                product.mainImages = GetMainImages(data);
                product.detailPageHtml = GetDetailPageHtml(html);
                product.option = await GetOption(connectionId, data, html);
                product.regDate = DateTime.Now;
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
