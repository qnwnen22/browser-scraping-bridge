#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Bueromarkt
{
    public class Bottcher : CollectBase
    {
        public override string Site => nameof(Bottcher);
        public override string Name => "뷔르마르크르";
        public override Language Language { get; set; } = Language.DE;
        public override Currency Currency { get; set; } = Currency.EUR;
        public override bool SupportsCaching { get; set; } = true;

        public override async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("page", page);
            string requestUrl = uriBuilderAdvance.ToString();
            string html = await ChatHubService.GetRequest(connectionId, requestUrl);
            List<string> scripts = html.GetValues("//script[@type='application/ld+json']", TextEx.XPathOption.InnerHtml);
            if (scripts == null || scripts.Count == 0) return null;
            var collectPageInfos = new List<CollectPageInfo>();
            foreach (var script in scripts)
            {
                JObject data = script.ToJObject();
                if (data == null || data.HasValues == false) continue;
                List<JToken> itemListElements = data.Value("itemListElement")?.ToList();
                if (itemListElements == null || itemListElements.Count == 0) continue;
                foreach (JToken itemListElement in itemListElements)
                {
                    JToken item = itemListElement.Value("item");
                    string itemUrl = item.Value<string>("url");
                    if (string.IsNullOrWhiteSpace(itemUrl)) continue;
                    string code = GetCode(itemUrl);
                    if (string.IsNullOrWhiteSpace(code)) continue;
                    string name = item.Value<string>("name");
                    var collectPageInfo = new CollectPageInfo()
                    {
                        Code = code,
                        Title = name,
                        Url = itemUrl
                    };
                    collectPageInfos.Add(collectPageInfo);
                }
            }
            return collectPageInfos;
        }

        public override void ThrowIfCaptcha(string url, string html)
        {
            var title = html.GetSelectNodeInnerText("//title");
            if (title == "Pardon Our Interruption") throw new CaptchaException
            {
                CaptchaInfo = new CaptchaInfo
                {
                    Url = url,
                    Message = "보쳐(Bottcher)사이트 쿠키 허용 후 다시 시도해주세요."
                }
            };
        }

        public override string GetCode(string url)
        {
            var uri = new Uri(url);
            string code = null;
            if (uri.Segments[1].Contains("Artikelsuche"))
            {
                code = uri.Segments[1].SingleSplit("Artikelsuche_", ".html");
            }
            else
            {
                var last = uri.Segments.Last();
                string startTxt = ",p-";
                int index = last.LastIndexOf(startTxt);
                code = last.Substring(index + startTxt.Length);
                code = code.Substring(0, code.LastIndexOf(".html"));
            }
            return code;
        }

        public override string CreateProductUrl(string code)
        {
            return null;
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            var pageProductDetail = html.GetValue("//div[@id='page-product-detail']", TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(pageProductDetail)) return null;
            var gaScope = pageProductDetail.GetValue("//div[@id='page-product-detail']", "data-ga-scope");
            var dataVarGroupId = html.GetValue("//div[contains(@class,'product-variations-container')]", "data-var-group-id");
            string requestUrl = $"https://www.bueromarkt-ag.de/product/variations/{dataVarGroupId}?pageType={gaScope}";
            var header = new Dictionary<string, string>();
            header.Add("X-Requested-With", "XMLHttpRequest");
            string optionHtml = await ChatHubService.GetRequest(connectionId, requestUrl, header);
            if (string.IsNullOrWhiteSpace(optionHtml)) return null;
            var data = new JObject();
            data["Option"] = optionHtml;
            return data;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string title = null;

            List<string> scripts = html.GetValues("//script[@type='application/ld+json']", TextEx.XPathOption.InnerHtml);
            foreach (string script in scripts)
            {
                JObject scriptData = script.ToJObject();
                if (scriptData == null) continue;
                string type = scriptData.Value<string>("@type");
                if (string.IsNullOrWhiteSpace(type) || type != "Product") continue;
                string name = scriptData.Value<string>("name");
                if (string.IsNullOrWhiteSpace(name) == false)
                {
                    title = name;
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(title)) throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(this.Language, title);
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            string brand = null;

            List<string> scripts = html.GetValues("//script[@type='application/ld+json']", TextEx.XPathOption.InnerHtml);
            foreach (string script in scripts)
            {
                JObject scriptData = script.ToJObject();
                if (scriptData == null) continue;
                string type = scriptData.Value<string>("@type");
                if (string.IsNullOrWhiteSpace(type) || type != "Product") continue;
                var name = scriptData.Value("brand")?.Value<string>("name");
                if (string.IsNullOrWhiteSpace(name) == false)
                {
                    brand = name;
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(brand)) return null;
            return ProductHelper.CreateName(this.Language, brand);
        }

        public override string GetModel(string html, JToken data)
        {
            return null;
        }

        public override List<Desc> GetDescs(string html, JToken data)
        {
            return null;
        }

        public override List<Tag> GetTags(string html, JToken data)
        {
            return null;
        }

        public override List<Price> GetPrice(string html, JToken data)
        {
            string error = "상품 가격을 확인할 수 없습니다.";
            List<string> scripts = html.GetValues("//script[@type='text/javascript']", TextEx.XPathOption.InnerHtml);
            var gTagScript = scripts.Find(x => x.Trim().IndexOf("gtag") == 0);
            if (string.IsNullOrWhiteSpace(gTagScript)) throw new Exception(error);
            string view_item = gTagScript.SubstringBetween("'view_item',", ");", false);
            var viewItem = view_item.ToJObject();
            if (viewItem == null) throw new Exception(error);
            double? value = viewItem.Value<double?>("value");
            if (value == null || value <= 0) throw new Exception(error);
            return ProductHelper.CreatePrice(this.Currency, value, value, true);
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            List<string> imageUrls = html.GetValues("//div[@class='carousel-item-wrapper']//img", "data-large-img-url");
            for (int i = 0; i < imageUrls.Count; i++)
            {
                var indexOf = imageUrls[i].IndexOf("?");
                if (indexOf > 0)
                {
                    imageUrls[i] = imageUrls[i].Substring(0, indexOf);
                }
            }
            return imageUrls;
        }

        public override string GetVideoUrl(string html, JToken data)
        {
            return null;
        }

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var pdDescriptionContainer = html.GetValue("//div[contains(@class,'pd-description-container')]", TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(pdDescriptionContainer)) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            string dafaultHtml = HtmlReader.GetDafaultHtml(pdDescriptionContainer);
            return dafaultHtml;
        }

        public override Option GetOption(string html, JToken data)
        {
            string optionHtml = data.Value<string>("Option");
            if (string.IsNullOrWhiteSpace(optionHtml)) return null;
            List<string> pdpMains = optionHtml.GetValues("//div[@id='pdp-main']", TextEx.XPathOption.OuterHtml);
            if (pdpMains == null || pdpMains.Count <= 0) return null;

            var option = new Option();

            var optionGroups = new List<Option.OptionGroup>();
            foreach (var pdpMain in pdpMains)
            {
                var optionName = pdpMain.GetSelectNodeInnerText("//div[contains(@class,'product-variations-headline')]");
                if (string.IsNullOrWhiteSpace(optionName)) continue;
                optionName = optionName.Trim();
                var indexOf = optionName.IndexOf(":");
                if (indexOf > 0) optionName = optionName.Substring(0, indexOf);

                Option.OptionGroup optionGroup = optionGroups.Find(x => x.groupName.First().text == optionName);
                if (optionGroup == null)
                {
                    optionGroup = new Option.OptionGroup();
                    optionGroup.groupName = ProductHelper.CreateName(this.Language, optionName);
                    optionGroups.Add(optionGroup);
                    optionGroup.groups = new List<Option.OptionGroup.Group>();
                }

                var dataVarId = pdpMain.GetValue("//li[contains(@class,'selected')]", "data-var-id");
                if (string.IsNullOrWhiteSpace(dataVarId)) continue;
                var selected = pdpMain.GetValue("//li[contains(@class,'selected')]", TextEx.XPathOption.InnerText);
                if (string.IsNullOrWhiteSpace(selected))
                {
                    selected = pdpMain.GetSelectNodeInnerText("//div[contains(@class,'product-variations-headline')]//strong");
                }

                if (string.IsNullOrWhiteSpace(selected)) continue;
                string optionValue = selected.Trim();
                var group = new Option.OptionGroup.Group();
                group.groupId = dataVarId;
                group.optionName = ProductHelper.CreateName(this.Language, optionValue);
                var dataImageZoom = pdpMain.GetValue("//li[contains(@class,'selected')]", "data-image-zoom");
                if (string.IsNullOrWhiteSpace(dataImageZoom) == false)
                {
                    var optionData = dataImageZoom.ToJObject();
                    var image = optionData.Value("images")?.FirstOrDefault()?.ToString();
                    if (string.IsNullOrWhiteSpace(image) == false)
                    {
                        group.optionImage = image;
                    }
                }
                optionGroup.groups.Add(group);
            }

            var detailOptions = ProductHelper.CreateDetailOptions(optionGroups);
            for (int i = 0; i < detailOptions.Count; i++)
            {
                string id = detailOptions[i].optionIds;
                var div = optionHtml.GetValue($"//div[@data-product-id='{id}']", TextEx.XPathOption.OuterHtml);
                List<string> dataPriceNet = div.GetValues("//div[contains(@class,'price-list')]//span[contains(@class,'product_price')]", "data-price-net");
                List<string> dataPriceGross = div.GetValues("//div[contains(@class,'price-list')]//span[contains(@class,'product_price')]", "data-price-gross");
                string salePriceTxt = dataPriceNet.Max();
                string originalPriceTxt = dataPriceGross.Max();
                double? salePrice = salePriceTxt.ParseEurToDouble();
                double? originalPrice = originalPriceTxt.ParseEurToDouble();
                if (salePrice == null || salePrice <= 0 || originalPrice == null || originalPrice <= 0)
                {
                    detailOptions[i].isSoldOut = true;
                }
                else
                {
                    detailOptions[i].price = ProductHelper.CreatePrice(this.Currency, originalPrice, salePrice);
                    detailOptions[i].isSoldOut = false;
                }
            }
            option.optionGroups = optionGroups;
            option.detailOptions = detailOptions;
            return option;
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {

        }

        public override int? GetTotalQuantity(string html, JToken data)
        {
            return null;
        }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            return;
        }
    }
}
