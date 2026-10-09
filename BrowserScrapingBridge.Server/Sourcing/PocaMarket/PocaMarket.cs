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

namespace BrowserScrapingBridge.Server.Sourcing.PocaMarket
{
    public class PocaMarket : ICollect
    {
        public string Site => nameof(PocaMarket);
        public string Name => "포카마켓";
        public Language Language => Language.EN;
        public Currency Currency => Currency.USD;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public string GetCode(string url)
        {
            var uri = new UriBuilderAdvance(url);
            return uri.Segments.Last();
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var ub = new UriBuilderAdvance(url);
            ub.Segments = new List<string> { "/", "apis/", "card/", "gb/", "v2/", "search" };
            ub.SetQuery("page", page.ToString());
            var price_step = ub.GetQuery("price_step");
            if (string.IsNullOrWhiteSpace(price_step))
            {
                ub.SetQuery("price_step", "ALL");
            }
            string pageUrl = ub.ToString();

            var json = await ChatHubService.GetRequest(connectionId, pageUrl);
            JObject data = json.ToJObject();
            if (data == null) return null;
            List<JToken> results = data["data"]?["results"]?.ToList();
            if (results == null || results.Count <= 0) return null;
            var collectPageInfos = new List<CollectPageInfo>();
            foreach (JToken result in results)
            {
                var id = result.Value<string>("id");
                if (string.IsNullOrWhiteSpace(id)) continue;
                var group_name_en = result.Value<string>("group_name_en");
                var member_name_en = result.Value<string>("member_name_en");
                var name_en = result.Value<string>("name_en");
                string title = $"{group_name_en} {member_name_en} {name_en}";
                var collectPageInfo = new CollectPageInfo()
                {
                    Code = id,
                    Url = $"https://pocamarket.com/search/detail/{id}",
                    Title = title,
                };
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }

        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }

        public Dictionary<string, int> GetIndexOfs(string text, params string[] values)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            foreach (var value in values)
            {
                var indexOf = text.IndexOf(value);
                if (indexOf < 0) continue;
                result.Add(value, indexOf);
            }
            return result;
        }

        public KeyValuePair<string, int> GetMin(Dictionary<string, int> dictionary)
        {
            KeyValuePair<string, int> result = default;
            foreach (KeyValuePair<string, int> item in dictionary)
            {
                if (result.Key == null)
                {
                    result = item;
                }
                else
                {
                    result = result.Value < item.Value ? result : item;
                }
            }
            return result;
        }
        
        public KeyValuePair<string, int> GetMax(Dictionary<string, int> dictionary)
        {
            KeyValuePair<string, int> result = default;
            foreach (KeyValuePair<string, int> item in dictionary)
            {
                if (result.Key == null)
                {
                    result = item;
                }
                else
                {
                    result = result.Value > item.Value ? result : item;
                }
            }
            return result;
        }

        private JObject GetData(string html)
        {
            var lineList = new List<string>();
            List<string> scripts = html.GetValues("//script", TextEx.XPathOption.InnerHtml);
            foreach (string script in scripts)
            {
                string dataString = script.SubstringBetween("self.__next_f.push([", "])", false);
                if (string.IsNullOrWhiteSpace(dataString)) continue;
                dataString = $"[{dataString}]";
                List<JToken> dataArray = dataString.ToJArray().ToList();
                JToken dataItem = dataArray[1];
                string ItemString = dataItem.ToString();
                if (string.IsNullOrWhiteSpace(ItemString)) continue;
                string[] lines = ItemString.Split("\n");
                lineList.AddRange(lines);
            }

            var data = new JObject();
            var temp = new List<string>();
            for (int i = 0; i < lineList.Count; i++)
            {
                string line = lineList[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                KeyValuePair<string, int> max = GetMin(GetIndexOfs(line, "{", "["));
                string key = line.Substring(0, max.Value);
                if (string.IsNullOrWhiteSpace(key)) key = $"data{i}";
                var subText = line.Substring(max.Value);
                JObject toJObject = subText.ToJObject();
                JArray toJArray = subText.ToJArray();
                if (toJObject != null)
                {
                    data[key] = toJObject;
                }
                else if (toJArray != null)
                {
                    data[key] = toJArray;
                }
                else
                {
                    var toJObject2 = line.ToJObject();
                    var toJArray2 = line.ToJArray();
                    if (toJObject2 != null)
                    {
                        data[key] = toJObject2;
                    }
                    else if (toJArray2 != null)
                    {
                        data[key] = toJArray2;
                    }
                    else
                    {
                        temp.Add(line);
                    }
                }
            }
            return data;
        }
        
        public List<Name> GetTitle(JToken photoCard)
        {
            try
            {
                string groupNameEn = photoCard["group"].Value<string>("nameEn");
                string memberNameEn = photoCard["member"].Value<string>("nameEn");
                string nameEn = photoCard.Value<string>("nameEn");
                string title = $"{groupNameEn} {memberNameEn} {nameEn}";
                List<Name> result = new List<Name>();
                result.Add(new Name(this.Language, title));
                return result;
            }
            catch (Exception e)
            {
                throw new Exception("상품명을 확인할 수 없습니다", e);
            }
        }

        public List<Name> GetBrand(JToken photoCard)
        {
            try
            {
                string groupNameEn = photoCard["group"].Value<string>("nameEn");
                string memberNameEn = photoCard["member"].Value<string>("nameEn");
                string title = $"{groupNameEn} {memberNameEn}";
                List<Name> result = new List<Name>();
                result.Add(new Name(this.Language, title));
                return result;
            }
            catch (Exception e)
            {
                throw new Exception("브랜드를 확인할 수 없습니다", e);
            }
        }

        public List<Desc> GetDescs()
        {
            return null;
        }

        public List<Tag> GetTags()
        {
            return null;
        }

        public List<Price> GetPrice(JToken photoCard)
        {
            try
            {
                string price = photoCard.Value<string>("price");
                string discountedPrice = photoCard.Value<string>("discountedPrice");
                double? originalPrice = price.ParseToDouble();
                double? salePrice = discountedPrice.ParseToDouble();
                if (originalPrice == null || originalPrice <= 0) throw new Exception();
                if (salePrice == null || salePrice == 0)
                {
                    salePrice = originalPrice;
                }
                List<Price> result = new List<Price>();
                result.Add(new Price(this.Currency, (double)originalPrice, (double)salePrice, true));
                return result;

            }
            catch (Exception e)
            {
                throw new Exception("가격 정보를 확인할 수 없습니다", e);
            }
        }

        public List<string> GetMainImages(JToken photoCard)
        {
            try
            {
                string image = photoCard.Value<string>("image");
                if (string.IsNullOrWhiteSpace(image)) throw new Exception();
                List<string> result = new List<string>() { image };
                return result;
            }
            catch (Exception e)
            {
                throw new Exception("상품 이미지를 확인할 수 없습니다", e);
            }
        }

        public string GetVideoUrl()
        {
            return null;
        }

        public string GetDetailPageHtml(List<JToken> children)
        {
            return null;
            //var sb = new StringBuilder();
            //foreach (JToken child in children)
            //{
            //    List<JToken> childs = child.ToList();
            //    var tag = childs[1].ToString();
            //    JToken value = childs[3];
            //    sb.AppendLine($"<{tag}>");
            //    string innerHtml = GetDetailPageHtml(childs);
            //    sb.AppendLine(innerHtml);
            //    sb.AppendLine($"</{tag}>");
            //}
            //return sb.ToString();
        }

        public Option GetOption()
        {
            return null;
        }

        public Product GetProduct(string url, string html)
        {
            Product product = new Product();
            try
            {
                JObject data = GetData(html);
                JToken photoCard = data["7:"]?.ToList()?[3]?["state"]?["queries"].ToList()?[0]?["state"]?["data"]?["photoCard"];
                //var children = data["7:"]?.ToList()?[3]?["children"].ToList();
                if (photoCard == null) throw new Exception("데이터 조회 실패");
                product.site = Site;
                product.code = GetCode(url);
                product.url = url;
                product.currency = this.Currency.ToString();
                product.title = GetTitle(photoCard);
                product.brand = GetBrand(photoCard);
                //product.descs = GetDescs();
                //product.tags = GetTags();
                product.price = GetPrice(photoCard);
                product.mainImages = GetMainImages(photoCard);
                //product.detailPageHtml = GetDetailPageHtml(children);
                product.regDate = DateTime.Now;
            }
            catch (Exception e)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = e.Message;
            }
            return product;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            return GetProduct(url, html);
        }

        public async Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            JObject data = GetData(html);
            return new CollectRawData(url, data, html);
        }

        public Product GetProduct(CollectRawData rawData)
        {
            Product product = new Product();
            try
            {
                JToken photoCard = rawData.Data["7:"]?.ToList()?[3]?["state"]?["queries"].ToList()?[0]?["state"]?["data"]?["photoCard"];
                //var children = data["7:"]?.ToList()?[3]?["children"].ToList();
                if (photoCard == null) throw new Exception("데이터 조회 실패");
                product.site = Site;
                product.code = GetCode(rawData.Url);
                product.url = rawData.Url;
                product.currency = this.Currency.ToString();
                product.title = GetTitle(photoCard);
                product.brand = GetBrand(photoCard);
                product.price = GetPrice(photoCard);
                product.mainImages = GetMainImages(photoCard);
                product.regDate = DateTime.Now;
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
