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
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Costco
{
    public class Costco : ICollect
    {
        public readonly string host = "https://www.costco.co.kr";
        public string Site => nameof(Costco);
        public string Name => "코스트코";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public string GetCode(string url)
        {
            var uri = new Uri(url);
            string code = uri.Segments.Last();
            var parentCode = code.Split("-").First();
            return parentCode;
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            UriBuilderAdvance inputUrl = new UriBuilderAdvance(url);

            UriBuilderAdvance uriBuilderAdvance = new UriBuilderAdvance("https://www.costco.co.kr/rest/v2/korea/products/search");
            uriBuilderAdvance.SetQuery("fields", "FULL");
            uriBuilderAdvance.SetQuery("query", "");
            uriBuilderAdvance.SetQuery("pageSize", "48");
            if (page > 0)
            {
                string currentPage = (page - 1).ToString();
                if (currentPage != "0") uriBuilderAdvance.SetQuery("currentPage", currentPage);
            }
            string query = inputUrl.GetQuery("q");
            if (string.IsNullOrWhiteSpace(query) == false)
            {
                #region 부분적 인코딩 문제
                query = query.Replace(":", "%3A");
                query = query.Replace("%2B", "+");
                #endregion
                uriBuilderAdvance.SetQuery("query", query);
            }

            string itm_term = inputUrl.GetQuery("itm_term");
            if (string.IsNullOrWhiteSpace(itm_term) == false)
            {
                uriBuilderAdvance.SetQuery("category", itm_term);
            }
            else
            {
                var findCategory = inputUrl.Segments.Find(x => x == "c/");
                string category = inputUrl.Segments.Last();
                if (category == "search")
                {
                    string text = inputUrl.GetQuery("text");
                    uriBuilderAdvance.SetQuery("query", text);
                }
                else
                {
                    uriBuilderAdvance.SetQuery("category", category);
                }
            }

            uriBuilderAdvance.SetQuery("lang", "ko");
            uriBuilderAdvance.SetQuery("curr", "KRW");
            string pageRequest = uriBuilderAdvance.ToString();

            string html = await ChatHubService.GetRequest(connectionId, pageRequest);
            JObject data = html.ToJObject();
            if (data == null) return null;
            List<JToken> products = data["products"]?.ToList();
            if (products == null || products.Count <= 0) return null;
            var collectPageInfos = new List<CollectPageInfo>();
            foreach (JToken product in products)
            {
                string code = product.Value<string>("code");
                if (string.IsNullOrWhiteSpace(code)) continue;
                string itemUrl = product.Value<string>("url");
                if (string.IsNullOrWhiteSpace(itemUrl)) continue;
                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = code;
                collectPageInfo.Url = $"{host}{itemUrl}";
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }

        public JToken GetData(string html)
        {
            return html.ToJObject();
        }

        public string GetDetailPageHtml(JToken data)
        {
            var description = data.Value<string>("description");
            if (string.IsNullOrWhiteSpace(description)) throw new Exception("상세페에지를 찾을 수 없습니다.");
            string getDafaultHtml = HtmlReader.GetDafaultHtml(description);

            var srcs = getDafaultHtml.GetValues("//img", "src");
            string host = "https://www.costco.co.kr";
            foreach (var src in srcs)
            {
                if (src.IndexOf("https://www.costco.co.kr") != 0)
                {
                    string imageUrl = $"{host}{src}";
                    getDafaultHtml = getDafaultHtml.Replace(src, imageUrl);
                }
            }
            return getDafaultHtml;
        }

        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }

        public string GetModel(JToken data)
        {
            throw new NotImplementedException();
        }


        private async Task<List<JToken>> GetLeafOptionDatas(string connectionId, JToken data)
        {
            List<JToken> result = new List<JToken>();
            JToken variantOptions = data.Value("variantOptions");
            if (variantOptions != null)
            {
                data.Value("");
                List<JToken> list = variantOptions.ToList();
                foreach (var variantOption in list)
                {
                    var code = variantOption.Value<string>("code");
                    var address = $"https://www.costco.co.kr/rest/v2/korea/products/{code}/?fields=FULL&lang=ko&curr=KRW";
                    string html = await ChatHubService.GetRequest(connectionId, address);
                    if (string.IsNullOrWhiteSpace(html)) throw new Exception("옵션 수집 과정에서 문제가 발생했습니다.");
                    JObject innerData = html.ToJObject();
                    var leafOptionDatas = await GetLeafOptionDatas(connectionId, innerData);
                    if (leafOptionDatas == null)
                    {
                        var jObject = new JObject();
                        var optionInfos = new JArray();
                        var optionDatas = new JArray();

                        var baseOptions = innerData.Value("baseOptions").ToList();
                        baseOptions.Reverse();
                        for (int i = 0; i < baseOptions.Count; i++)
                        {
                            List<JToken> options = baseOptions[i].Value("options").ToList();
                            for (int j = 0; j < options.Count; j++)
                            {
                                var optionCode = options[j].Value<string>("code");
                                var variantOptionQualifiers = options[j].Value("variantOptionQualifiers").ToList();
                                for (int k = 0; k < variantOptionQualifiers.Count; k++)
                                {
                                    var name = variantOptionQualifiers[k].Value<string>("name");
                                    var value = variantOptionQualifiers[k].Value<string>("value");
                                    var indexOf = value.IndexOf(",");
                                    if (indexOf > 0)
                                    {
                                        value = value.Substring(indexOf + 1);
                                    }
                                    string text = $"{name}ㅡ{value.Trim()}";

                                    var optionInfo = new JObject();
                                    optionInfo["code"] = optionCode.Split("-").Last();
                                    optionInfo["name"] = name;
                                    optionInfo["value"] = value.Trim();
                                    optionInfos.Add(optionInfo);
                                }
                            }
                        }

                        var lastOption = baseOptions.Last();
                        var lastOptions = lastOption.Value("options").ToList();
                        foreach (var lastOptionItem in lastOptions)
                        {
                            string optionCode = lastOptionItem.Value<string>("code");
                            int priceDataValue = lastOptionItem.Value("priceData").Value<int>("value");
                            int stockLevel = lastOptionItem.Value("stock").Value<int>("stockLevel");
                            string optionPriceDataValue = lastOptionItem.Value("priceData").Value<string>("value");
                            List<JToken> variantOptionQualifiers = lastOptionItem.Value("variantOptionQualifiers").ToList();
                            if (variantOptionQualifiers.Count != 1) throw new Exception("예상치 못한 옵션 오류");
                            JToken variantOptionQualifier = variantOptionQualifiers.First();
                            string variantOptionQualifierValue = variantOptionQualifier.Value<string>("value");
                            var optionData = new JObject();
                            optionData["code"] = optionCode;
                            optionData["price"] = priceDataValue;
                            optionData["stock"] = stockLevel;
                            optionData["value"] = variantOptionQualifierValue;
                            optionDatas.Add(optionData);
                        }

                        jObject["optionInfos"] = optionInfos;
                        jObject["datas"] = optionDatas;
                        result.Add(jObject);
                    }
                    else
                    {
                        result.AddRange(leafOptionDatas);
                    }
                }
            }
            else
            {
                return null;
            }
            return result;
        }

        public async Task<Option> GetOption(string connectionId, JToken data)
        {
            List<JToken> leafOptionDatas = await GetLeafOptionDatas(connectionId, data);
            if (leafOptionDatas == null) return null;

            var option = new Option();
            option.optionGroups = new List<Option.OptionGroup>();

            foreach (var item in leafOptionDatas)
            {
                List<JToken> optionInfos = item.Value("optionInfos").ToList();
                for (int i = 0; i < optionInfos.Count; i++)
                {
                    var optionInfo = optionInfos[i];
                    var code = optionInfo.Value<string>("code");
                    var name = optionInfo.Value<string>("name");
                    var value = optionInfo.Value<string>("value");
                    Option.OptionGroup findOptionGroup = option.optionGroups.Find(x => x.groupName.FirstOrDefault()?.text == name);
                    if (findOptionGroup == null)
                    {
                        findOptionGroup = new Option.OptionGroup();
                        findOptionGroup.optionId = i.ToString();
                        findOptionGroup.groupName = ProductHelper.CreateName(Language.KR, name);
                        option.optionGroups.Add(findOptionGroup);
                    }
                    if (findOptionGroup.groups == null) findOptionGroup.groups = new List<Option.OptionGroup.Group>();

                    var group = findOptionGroup.groups.Find(x => x.optionName.FirstOrDefault().text == value);
                    if (group == null)
                    {
                        group = new Option.OptionGroup.Group();
                        group.groupId = code;
                        group.optionName = ProductHelper.CreateName(Language.KR, value);
                        findOptionGroup.groups.Add(group);
                    }
                }
            }

            string optionGroupName = ProductHelper.GetOptionGroupName(option.optionGroups);
            option.detailOptions = new List<Option.DetailOption>();
            foreach (var leafOptionData in leafOptionDatas)
            {
                List<JToken> datas = leafOptionData.Value("datas").ToList();
                foreach (var item in datas)
                {
                    var detailOption = new Option.DetailOption();

                    #region 기존 옵션 수집 Id 방식에 맞춤
                    string code = item.Value<string>("code");
                    List<string> codeList = code.Split("-").ToList();
                    codeList = codeList.GetRange(1, codeList.Count - 1);
                    detailOption.optionIds = codeList.Join("ㅡ");
                    bool isExists = option.detailOptions.Exists(x => x.optionIds == detailOption.optionIds);
                    if (isExists) continue;
                    #endregion

                    var price = item.Value<double>("price");
                    var stock = item.Value<int>("stock");
                    detailOption.price = ProductHelper.CreatePrice(Currency.KRW, price, price);
                    if (stock > 0)
                    {
                        detailOption.isSoldOut = false;
                        detailOption.quantity = stock;
                    }

                    detailOption.optionGroupName = ProductHelper.CreateName(Language.KR, optionGroupName);

                    var value = item.Value<string>("value");
                    string[] values = value.Split(",");
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = values[i].Trim();
                    }
                    var optionName = string.Join("ㅡ", values);
                    detailOption.optionName = ProductHelper.CreateName(Language.KR, optionName);

                    option.detailOptions.Add(detailOption);
                }
            }
            return option;
        }



        public List<Price> GetPrice(JToken data)
        {
            double? basePrice = data["basePrice"]?.Value<double?>("value");
            double? salePrice = basePrice;
            if (basePrice == null || basePrice <= 0) throw new Exception("상품 가격을 찾을 수 없습니다.");
            List<Price> createPrice = ProductHelper.CreatePrice(Currency.KRW, (double)basePrice, (double)salePrice, true);
            return createPrice;
        }

        public List<Name> GetTitle(JToken data)
        {
            string name = data.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("상품명을 찾을 수 없습니다.");
            List<Name> title = ProductHelper.CreateName(Language.KR, name);
            var englishName = data.Value<string>("englishName");
            if (string.IsNullOrWhiteSpace(englishName) == false)
            {
                title.Add(new Name
                {
                    language = Language.EN.ToString(),
                    text = englishName
                });
            }
            return title;
        }

        public List<Name> GetBrand(JToken data)
        {
            throw new NotImplementedException();
        }

        public List<string> GetMainImages(JToken data)
        {
            List<JToken> images = data["images"]?.ToList();
            if (images == null || images.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다");
            List<string> result = new List<string>();
            foreach (JToken image in images)
            {
                string format = image.Value<string>("format");
                if (format != "product") continue;
                string url = image.Value<string>("url");
                if (string.IsNullOrWhiteSpace(url)) continue;
                result.Add($"{host}{url}");
            }
            if (result.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다");
            return result;
        }

        public List<Desc> GetDescs(JToken data)
        {
            int count = 1;
            List<Desc> result = new List<Desc>();
            List<JToken> classifications = data["classifications"]?.ToList();
            foreach (JToken classification in classifications)
            {
                List<JToken> features = classification["features"]?.ToList();
                foreach (JToken feature in features)
                {
                    string name = feature?.Value<string>("name");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    string value = feature["featureValues"]?.FirstOrDefault()?.Value<string>("value");
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    Desc desc = new Desc(count, Language.KR, name, value);
                    result.Add(desc);
                    count++;
                }
            }
            return result;
        }

        public List<Tag> GetTags(JToken data)
        {
            return null;
        }

        public string GetVideoUrl(JToken data)
        {
            return null;
        }
        private double GetDiscountValue(JToken data, CostcoCollectOption costcoCollectOption)
        {
            bool? UsePromotionSalePrice = costcoCollectOption?.UsePromotionSalePrice;
            if (UsePromotionSalePrice == true)
            {
                double? discountValue = data.Value("couponDiscount")?.Value<double?>("discountValue");
                if (discountValue == null) return 0;
                return (double)discountValue;
            }
            else
            {
                return 0;
            }
        }
        private void ApplyCollectOption(Product product, JToken data, CollectOptionSetting collectOptionSetting)
        {
            if (collectOptionSetting == null) return;
            if (collectOptionSetting.CostcoCollectOption == null) return;
            double discountValue = GetDiscountValue(data, collectOptionSetting.CostcoCollectOption);
            product.price[0].salePrice -= discountValue;
            if (product.option != null)
            {
                for (int i = 0; i < product.option.detailOptions.Count; i++)
                {
                    product.option.detailOptions[i].price[0].salePrice -= discountValue;
                }
            }
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string code = GetCode(url);
            string address = $"https://www.costco.co.kr/rest/v2/korea/products/{code}/?fields=FULL&lang=ko&curr=KRW";
            string html = await ChatHubService.GetRequest(connectionId, address);

            Product product = new Product();
            try
            {
                if (string.IsNullOrWhiteSpace(html)) throw new Exception("상품 정보를 찾을 수 없습니다.");
                JToken data = GetData(html);
                product.site = this.Site;
                product.code = code;
                string path = data.Value<string>("url");
                product.url = $"https://www.costco.co.kr{path}";
                product.currency = Currency.KRW.ToString();
                product.title = GetTitle(data);
                product.descs = GetDescs(data);
                product.tags = GetTags(data);
                product.price = GetPrice(data);
                product.mainImages = GetMainImages(data);
                product.videoUrl = GetVideoUrl(data);
                product.detailPageHtml = GetDetailPageHtml(data);
                product.option = await GetOption(connectionId, data);
                product.regDate = DateTime.Now;

                #region 정확한 품절 처리가 안됌
                //JToken stock = data.Value("stock");
                //if (stock != null)
                //{
                //    var stockLevelStatus = stock.Value<string>("stockLevelStatus");
                //    if (stockLevelStatus == "outOfStock")
                //    {
                //        throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품 입니다.");
                //    }
                //} 
                #endregion
                ApplyCollectOption(product, data, collectOptionSetting);
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
            catch (SaleException se)
            {
                product.collectStatus = ProductCollectStatus.Success;
                product.saleStatus = se.SaleStatus;
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
