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
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Breuninger
{
    public class Breuninger : CollectBase
    {
        public override string Site => nameof(Breuninger);
        public override string Name => "브로이닝어";
        public override Language Language { get; set; } = Language.EN;
        public override Currency Currency { get; set; } = Currency.USD;
        public override bool SupportsCaching { get; set; } = true;

        public override async Task<string> GetLoginUrlAsync(string connectionId) { return null; }

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            UriBuilderAdvance uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("page", page.ToString());
            string pageUrl = uriBuilderAdvance.ToString();

            string html = await ChatHubService.GetRequest(connectionId, pageUrl);

            var getCollectPageInfos = new List<Func<string, List<CollectPageInfo>>>();
            //getCollectPageInfos.Add(GetCollectPageInfos1);
            getCollectPageInfos.Add(GetCollectPageInfos2);

            List<CollectPageInfo> result = null;
            foreach (var getCollectPageInfo in getCollectPageInfos)
            {
                result = getCollectPageInfo.Invoke(html);
                if (result != null && result.Count > 0) break;
            }
            return result;
        }
        [Obsolete("이전 로직, 브로이닝어 HTML이 변경되어 기능이 안되지만 기록용으로 남김")]
        private List<CollectPageInfo> GetCollectPageInfos1(string html)
        {
            var articles = html.GetValues("//article", TextEx.XPathOption.OuterHtml);

            List<CollectPageInfo> result = new List<CollectPageInfo>();
            string host = "https://www.breuninger.com";
            foreach (string article in articles)
            {
                string aTag = article.GetValue("//a", TextEx.XPathOption.OuterHtml);

                string code = aTag.GetValue("//a", "data-syan");
                if (string.IsNullOrWhiteSpace(code)) continue;

                string href = aTag.GetValue("//a", "href");
                if (string.IsNullOrWhiteSpace(href)) continue;
                string collectUrl = $"{host}{href}";
                var ub = new UriBuilderAdvance(collectUrl);
                ub.ClearQuery();
                collectUrl = ub.ToString();

                string title = aTag.GetSelectNodeInnerText("//span[@class='suchen-produkt__name__text']");

                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Url = collectUrl;
                collectPageInfo.Title = title;
                collectPageInfo.Code = code;
                result.Add(collectPageInfo);
            }
            return result;
        }
        private List<CollectPageInfo> GetCollectPageInfos2(string html)
        {
            var ul = html.GetValue("//ul[@class='suchen-produktliste__content']", TextEx.XPathOption.OuterHtml);
            List<string> suchenProdukts = ul.GetValues("//li//suchen-produkt", TextEx.XPathOption.OuterHtml);

            List<CollectPageInfo> result = new List<CollectPageInfo>();
            string host = "https://www.breuninger.com";
            foreach (string suchenProdukt in suchenProdukts)
            {
                string aTag = suchenProdukt.GetValue("//a", TextEx.XPathOption.OuterHtml);

                string code = aTag.GetValue("//a", "data-syan");
                if (string.IsNullOrWhiteSpace(code)) continue;

                string href = aTag.GetValue("//a", "href");
                if (string.IsNullOrWhiteSpace(href)) continue;
                string collectUrl = $"{host}{href}";
                var ub = new UriBuilderAdvance(collectUrl);
                ub.ClearQuery();
                collectUrl = ub.ToString();

                string title = aTag.GetSelectNodeInnerText("//span[@class='suchen-produkt__name__text']");

                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Url = collectUrl;
                collectPageInfo.Title = title;
                collectPageInfo.Code = code;
                result.Add(collectPageInfo);
            }
            return result;
        }

        public override void ThrowIfCaptcha(string url, string html)
        {

        }

        public override string GetCode(string url)
        {
            Uri uri = new Uri(url);
            if (uri.Segments.Length != 8) return null;
            string last = uri.Segments.Last();
            if (last != "p/") return null;
            string code = uri.Segments[6];
            code = code.Substring(0, code.Length - 1);
            return code;
        }

        public override string CreateProductUrl(string code)
        {
            return null;
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            var datas = new Dictionary<string, JToken>();

            var applicationJsons = html.GetValues("//script[@type='application/json']", TextEx.XPathOption.OuterHtml);
            for (int i = 0; i < applicationJsons.Count; i++)
            {
                try
                {
                    string id = applicationJsons[i].GetValue("//script", "id");
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        id = $"data{i}";
                    }
                    string json = applicationJsons[i].GetValue("//script", TextEx.XPathOption.InnerText);
                    json = WebUtility.HtmlDecode(json);
                    JToken jToken = JToken.Parse(json);
                    datas.Add(id, jToken);
                }
                catch { }
            }
            JObject result = datas.ToSerializeObject().ToJObject();
            if (result == null) throw new Exception("상품 정보를 확인할 수 없습니다.");

            string? waehrung = result["entsEventTrackingData"]?.Value<string>("waehrung");
            if (string.IsNullOrWhiteSpace(waehrung)) { waehrung = "EUR"; }
            this.Currency = waehrung.ConvertEnum<Currency>();

            return result;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string? name = data["brtn-product-data"]?.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("상품명을 확인할 수 없습니다.");
            List<Name> titles = new List<Name>();
            titles.Add(new Name(this.Language, name));
            return titles;
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            string? brandName = data["brtn-product-data"].Value<string>("brandName");
            if (string.IsNullOrWhiteSpace(brandName)) throw new Exception("브랜드명을 확인할 수 없습니다.");
            List<Name> brands = new List<Name>();
            brands.Add(new Name(this.Language, brandName));
            return brands;
        }

        public override string GetModel(string html, JToken data)
        {
            string webcode = data["entsProductData"]?["webcode"]?.ToString();
            if (string.IsNullOrWhiteSpace(webcode)) throw new Exception("모델명을 찾을 수 없습니다.");
            return webcode;
        }

        public override List<Desc> GetDescs(string html, JToken data) { return null; }

        public override List<Tag> GetTags(string html, JToken data) { return null; }

        public override List<Price> GetPrice(string html, JToken data)
        {
            try
            {
                List<Price> result = null;
                var scripts = html.GetValues("//script", TextEx.XPathOption.OuterHtml);
                for (int i = 0; i < scripts.Count; i++)
                {
                    string script = scripts[i];
                    int dataLayerIndex = script.IndexOf("dataLayer.push(");
                    if (dataLayerIndex >= 0)
                    {
                        var dataLayer = script.SubstringBetween("dataLayer.push(", ");", false);
                        JObject priceData = dataLayer.ToJObject();
                        if (priceData == null) continue;
                        List<JToken> products = priceData["products"]?.ToList();
                        if (products == null || products.Count <= 0) continue;
                        double originalPrice = products.First().Value<double>("price_original");
                        originalPrice /= 100d;
                        double salePrice = products.First().Value<double>("price");
                        salePrice /= 100d;
                        if (result == null) result = new List<Price>();
                        var price = new Price(this.Currency, originalPrice, salePrice, true);
                        result.Add(price);
                        break;
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var images = new List<string>();
            try
            {
                List<JToken>? artikels = data["entsProductData"]?["artikel"]?.Children().ToList();

                string[] properties = new string[]
                {
                    "standardUrl",
                    "standardRetinaUrl",
                    "standardRedesignUrl",
                    "standardRedesignRetinaUrl",
                    "thumbnailUrl",
                    "thumbnailRetinaUrl",
                    "fallbackThumbnailUrl",
                    "fallbackStandardUrl",
                    "fallbackStandardRedesignUrl",
                    "fallbackZoomUrl",
                    "zoomUrl",
                    "zoomRetinaUrl",
                };

                for (int i = 0; i < artikels.Count; i++)
                {
                    JToken artikel = artikels[i].First();
                    List<JToken>? ansichtens = artikel["ansichten"]?.ToList();
                    foreach (JToken? ansichten in ansichtens)
                    {
                        string? imageUrl = null;
                        for (int j = properties.Length - 1; j >= 0; j--)
                        {
                            imageUrl = ansichten.Value<string>(properties[j]);
                            if (string.IsNullOrWhiteSpace(imageUrl) == false) break;
                        }

                        if (images.Exists(x => x == imageUrl))
                        {
                            continue;
                        }
                        images.Add(imageUrl);
                    }
                }
            }
            catch (Exception e)
            {
                DebugConsole.WriteLine(e.Message);
                DebugConsole.WriteLine(e.StackTrace);
            }
            if (images.Count <= 0) throw new CollectException("메인 이미지를 찾을 수 없습니다.");
            return images;
        }

        public override string GetVideoUrl(string html, JToken data)
        {
            return null;
        }

        public override string GetDetailPageHtml(string html, JToken data)
        {
            try
            {
                var blueprint = data["entsProductDetailData"]?["blueprint"];
                string? details = blueprint?["beschreibung"]?.Value<string>("details");
                string? headline = blueprint?["materialAndCareInstructions"]?.Value<string>("headline");
                string? material = blueprint?["materialAndCareInstructions"]?.Value<string>("material");

                StringBuilder htmlBuilder = new StringBuilder();
                htmlBuilder.AppendLine("<h2>Product details</h2>");
                htmlBuilder.AppendLine("<div>");
                htmlBuilder.AppendLine("<p><strong>Description</strong></p>");
                htmlBuilder.AppendLine(details);
                htmlBuilder.AppendLine("</div>");

                string product_detail_accordion_template = html.GetValue("//script[@id='product-detail-accordion-template']", TextEx.XPathOption.InnerHtml);
                string dimension = product_detail_accordion_template.GetValue("//details//brtn-product-details-accordion", "dimension");
                dimension = WebUtility.HtmlDecode(dimension);
                htmlBuilder.AppendLine("<div>");
                htmlBuilder.AppendLine($"<p>FIT & MEASUREMENTS</p>");
                htmlBuilder.AppendLine(dimension);
                htmlBuilder.AppendLine("</div>");

                htmlBuilder.AppendLine("<div>");
                htmlBuilder.AppendLine($"<p><strong>{headline}</strong></p>");
                htmlBuilder.AppendLine(material);
                htmlBuilder.AppendLine("</div>");

                var detailHtml = htmlBuilder.ToString();
                return detailHtml;
            }
            catch (Exception exception)
            {
                throw new Exception("상세 페이지 수집에 실패했습니다.", exception);
            }
        }

        public override Option GetOption(string html, JToken data)
        {
            try
            {
                JToken? artikel = data["entsProductData"]?["artikel"];
                List<JToken> children = artikel.Children().ToList();

                JToken? productData = data["brtn-product-data"];
                List<JToken> optionDatas = productData["colors"].ToList();
                string colorString = "Color";
                string sizeString = "Size";

                List<Option.OptionGroup> optionGroups = new List<Option.OptionGroup>();
                var optionGroupColor = new Option.OptionGroup();
                optionGroupColor.optionId = colorString;
                optionGroupColor.groupName = new List<Name>() { new Name(Language.EN, colorString) };
                optionGroupColor.groups = new List<Option.OptionGroup.Group>();
                var optionGroupSize = new Option.OptionGroup();
                optionGroupSize.optionId = sizeString;
                optionGroupSize.groupName = new List<Name>() { new Name(Language.EN, sizeString) };
                optionGroupSize.groups = new List<Option.OptionGroup.Group>();
                optionGroups.Add(optionGroupColor);
                optionGroups.Add(optionGroupSize);

                List<Option.DetailOption> detailOptions = new List<Option.DetailOption>();
                for (int i = 0; i < children.Count; i++)
                {
                    Option.DetailOption detailOption = new Option.DetailOption();

                    JToken child = children[i].First();
                    JToken? colors = child["colors"]?["aktiv"];
                    string? artikelNrColor = colors.Value<string>("artikelNr");
                    JToken? groessen = child["groessen"]?["aktiv"];
                    string? artikelNrSize = groessen.Value<string>("artikelNr");
                    if (string.IsNullOrWhiteSpace(artikelNrColor) || string.IsNullOrWhiteSpace(artikelNrSize)) continue;

                    string? colorName = colors.Value<string>("name");
                    string? sizeName = groessen.Value<string>("name");
                    if (string.IsNullOrWhiteSpace(colorName) || string.IsNullOrWhiteSpace(colorName))
                    {
                        throw new Exception("옵션명을 확인할 수 없습니다.");
                    }

                    string? rotpreis = child["preis"]?.Value<string>("rotpreis"); // salePrice1
                    string? schemaPreis = child["preis"]?.Value<string>("schemaPreis"); // salePrice2

                    string? schemaWaehrung = child["preis"]?.Value<string>("schemaWaehrung"); // currency

                    string? schwarzpreis = child["preis"]?.Value<string>("schwarzpreis"); // originalPrice1
                    string? niedrigsterGesamtpreis = child["preis"]?.Value<string>("niedrigsterGesamtpreis"); //originalPrice2

                    double salePrice = 0;
                    double originalPrice = 0;

                    double? minPrice = GetRegexMinPrice(rotpreis, schemaPreis);
                    double? maxPrice = GetRegexMaxPrice(schwarzpreis, niedrigsterGesamtpreis);
                    if (minPrice == null && maxPrice == null)
                    {
                        throw new Exception("옵션 가격을 확인할 수 없습니다.");
                    }
                    else
                    {
                        salePrice = minPrice == null ? 0 : (double)minPrice;
                        originalPrice = maxPrice == null ? 0 : (double)maxPrice;
                    }

                    if (salePrice <= 0 && originalPrice <= 0)
                    {
                        throw new Exception("옵션 가격을 확인할 수 없습니다.");
                    }

                    else if (salePrice > originalPrice)
                    {
                        originalPrice = salePrice;
                    }
                    else if (salePrice <= 0)
                    {
                        salePrice = originalPrice;
                    }

                    JToken? findColorOption = optionDatas.Find(x => x["colorId"].ToString() == artikelNrColor);
                    List<JToken> sizes = findColorOption["sizes"].ToList();
                    JToken? findSizeOption = sizes.Find(x => x["articleId"].ToString() == artikelNrSize);
                    int stock = findSizeOption.Value<int>("stock");
                    Price price = new Price(this.Currency, originalPrice, salePrice);
                    detailOption.optionIds = $"{artikelNrColor}ㅡ{artikelNrSize}";
                    detailOption.optionGroupName = new List<Name> { new Name(Language.EN, $"{colorString}ㅡ{sizeString}") };
                    detailOption.optionName = new List<Name> { new Name(Language.EN, $"{colorName}ㅡ{sizeName}") };
                    detailOption.price = new List<Price> { price };
                    detailOption.quantity = stock;
                    detailOption.isSoldOut = stock <= 0;
                    detailOptions.Add(detailOption);

                    if (optionGroupColor.groups.Find(x => x.groupId == artikelNrColor) == null)
                    {
                        var gruop = new Option.OptionGroup.Group();
                        gruop.groupId = artikelNrColor;
                        gruop.optionName = new List<Name> { new Name(Language.EN, colorName) };
                        gruop.optionImage = colors.Value<string>("colorThumbnailUrl");
                        optionGroupColor.groups.Add(gruop);
                    }

                    if (optionGroupSize.groups.Find(x => x.groupId == artikelNrSize) == null)
                    {
                        var gruop = new Option.OptionGroup.Group();
                        gruop.groupId = artikelNrSize;
                        gruop.optionName = new List<Name> { new Name(Language.EN, sizeName) };
                        optionGroupSize.groups.Add(gruop);
                    }
                }
                var option = new Option();
                option.optionGroups = optionGroups;
                option.detailOptions = detailOptions;
                return option;
            }
            catch (Exception exception)
            {
                throw new Exception("옵션 수집 과정에서 문제가 발생했습니다.", exception);
            }
        }
        private static double? GetRegexMinPrice(params string[] values)
        {
            var prices = new List<double>();
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                double? price = value.ParseEurToDouble();
                if (price != null)
                {
                    prices.Add((double)price);
                }
            }
            if (prices.Count <= 0) return null;
            double max = prices.Min();
            return max;
        }
        private static double? GetRegexMaxPrice(params string[] values)
        {
            var prices = new List<double>();
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                double? price = value.ParseEurToDouble();
                if (price != null)
                {
                    prices.Add((double)price);
                }
            }
            if (prices.Count <= 0) return null;
            double max = prices.Max();
            return max;
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
