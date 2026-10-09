#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrowserScrapingBridge.Server.Sourcing.Rakuten
{
    public class RakutenBrandavenue : Rakuten
    {
        private List<Desc> GetDescs(string html)
        {
            var descs = new List<Desc>();

            List<string> trTags = html.GetValues("//table[@class='item-detail-description-table']//tr", TextEx.XPathOption.OuterHtml);
            if (trTags != null && trTags.Count > 0)
            {
                descs = new List<Desc>();
                for (int i = 0; i < trTags.Count; i++)
                {
                    string th = trTags[i].GetValue("//th", TextEx.XPathOption.InnerText);
                    string td = trTags[i].GetValue("//td", TextEx.XPathOption.InnerText);
                    Desc desc = new Desc(i + 1, Language.JP.ToString(), th, td);
                    descs.Add(desc);
                }
            }

            return descs;
        }
        private List<Option.OptionGroup> GetOptionGroups(string html)
        {
            List<Option.OptionGroup> optionGroups = new List<Option.OptionGroup>();
            string optionTag = html.GetValue("//div[@class='item-sku-actions']", TextEx.XPathOption.OuterHtml);
            string colorTag = optionTag.GetValue("//div[@class='item-sku-actions-color']");
            if (string.IsNullOrWhiteSpace(colorTag) == false)
            {
                string colorOptionName = colorTag.GetSelectNodeInnerText("//p[@class='item-sku-actions-color-text']");
                colorOptionName = colorOptionName.Replace("：", string.Empty).Trim();

                var optionGroup_color = new Option.OptionGroup();
                optionGroup_color.optionId = colorOptionName;
                optionGroup_color.groupName = new List<Name> { new Name(Language.JP, colorOptionName) };

                List<string> lis = colorTag.GetValues("//ul[@class='item-sku-actions-color-list']//li", TextEx.XPathOption.OuterHtml);
                optionGroup_color.groups = new List<Option.OptionGroup.Group>();
                for (int i = 0; i < lis.Count; i++)
                {
                    string optionName_color = lis[i].GetValue("//li", "data-color");
                    string src = lis[i].GetValue("//img", "src");
                    if (src.IndexOf("https:") < 0)
                    {
                        src = "https:" + src;
                    }
                    UriBuilder uriBuilder = new UriBuilder(src);
                    uriBuilder.Query = null;
                    string optionImage = uriBuilder.Uri.ToString();

                    List<Name> optionName = new List<Name> { new Name(Language.JP, optionName_color) };
                    var group = new Option.OptionGroup.Group();
                    group.groupId = optionName_color;
                    group.optionName = optionName;
                    group.optionImage = optionImage;
                    optionGroup_color.groups.Add(group);
                }
                optionGroups.Add(optionGroup_color);
            }

            List<string> captions = optionTag.GetValues("//table//caption", TextEx.XPathOption.InnerText);
            captions = captions.Distinct().ToList();
            if (captions.Count != 1) throw new Exception("test");
            string optionName_size = captions.First();
            var optionGroup_size = new Option.OptionGroup();
            optionGroup_size.optionId = optionName_size;
            optionGroup_size.groupName = new List<Name> { new Name(Language.JP, optionName_size) };

            List<string> tables = optionTag.GetValues("//table", TextEx.XPathOption.OuterHtml);
            if (tables != null && tables.Count > 0)
            {
                optionGroup_size.groups = new List<Option.OptionGroup.Group>();
                var sizeOptionList = new List<string>();
                for (int i = 0; i < tables.Count; i++)
                {
                    List<string> trs = tables[i].GetValues("//tbody//tr", TextEx.XPathOption.OuterHtml);
                    for (int j = 0; j < trs.Count; j++)
                    {
                        string tr = trs[j];
                        string size = tr.GetValue("//p[@class='item-sku-actions-info-size']", TextEx.XPathOption.InnerText);
                        if (sizeOptionList.Exists(x => x == size) == false)
                        {
                            sizeOptionList.Add(size);
                        }
                    }
                }
                for (int i = 0; i < sizeOptionList.Count; i++)
                {
                    var group = new Option.OptionGroup.Group();
                    group.groupId = sizeOptionList[i];
                    group.optionName = new List<Name> { new Name(Language.JP, sizeOptionList[i]) };
                    optionGroup_size.groups.Add(group);
                }
                optionGroups.Add(optionGroup_size);
            }

            return optionGroups;
        }
        private List<Option.DetailOption> GetDetailOptions(string html, JToken offers)
        {
            List<JToken> offerList = offers.ToArray().ToList();
            string item_sku = html.GetValue("//div[@class='item-sku-actions']", TextEx.XPathOption.OuterHtml);
            string colorOptionName = item_sku.GetSelectNodeInnerText("//p[@class='item-sku-actions-color-text']");
            colorOptionName = colorOptionName.Replace("：", string.Empty).Trim();

            List<Option.DetailOption> detailOptions = new List<Option.DetailOption>();
            var optionTables = html.GetValues("//div[@class='item-sku-actions']//table", TextEx.XPathOption.OuterHtml);
            for (int i = 0; i < optionTables.Count; i++)
            {
                string table = optionTables[i];
                string data_color = table.GetValue("//table", "data-color");
                string caption = table.GetValue("//caption[@class='item-sku-actions-info-caption']", TextEx.XPathOption.InnerText);

                string tbody = table.GetValue("//tbody", TextEx.XPathOption.OuterHtml);
                List<string> trs = tbody.GetValues("//tr", TextEx.XPathOption.OuterHtml);
                for (int j = 0; j < trs.Count; j++)
                {
                    string tr = trs[j];
                    string data_variant_id = tr.GetValue("//tr", "data-variant_id");
                    string size = tr.GetValue("//p[@class='item-sku-actions-info-size']", TextEx.XPathOption.InnerText);
                    string id = tr.GetValue("//button", "id");
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    #region 불필요
                    //string shop_bid = tr.GetValue("//form//input[@name='shop_bid']", "value");
                    //string item_id = tr.GetValue("//form//input[@name='item_id']", "value");
                    //string inventory_flag = tr.GetValue("//form//input[@name='inventory_flag']", "value");
                    //string __event = tr.GetValue("//form//input[@name='__event']", "value");
                    //string units = tr.GetValue("//form//input[@name='units']", "value");
                    //string variant_id = tr.GetValue("//form//input[@name='variant_id']", "value"); 
                    #endregion

                    var detailOption = new Option.DetailOption();
                    string optionGroupName = string.Join("ㅡ", colorOptionName, caption);
                    detailOption.optionGroupName = new List<Name> { new Name(Language.JP, optionGroupName) };
                    string optionName = string.Join("ㅡ", data_color, size);
                    detailOption.optionIds = optionName;
                    detailOption.optionName = new List<Name> { new Name(Language.JP, optionName) };

                    List<string> skuValues = id.Split('_').ToList();
                    skuValues = skuValues.GetRange(0, skuValues.Count - 1);
                    string sku = string.Join("", skuValues);
                    JToken? offer = offerList.Find(x => x.Value<string>("sku") == sku);
                    if (offer == null) continue;
                    double price = offer.ToObject<JObject>().Value<double>("price");
                    detailOption.price = new List<Price> { new Price(Currency.JPY, price, price) };
                    detailOption.isSoldOut = false;
                    detailOption.quantity = 100;
                    detailOptions.Add(detailOption);
                }
            }
            return detailOptions;
        }
        private Option GetOption(string html, JToken offers)
        {
            var option = new Option();

            List<Option.OptionGroup> optionGroups = GetOptionGroups(html);
            if (optionGroups == null) return null;
            option.optionGroups = optionGroups;

            List<Option.DetailOption> detailOption = GetDetailOptions(html, offers);
            if (detailOption == null || detailOption.Count <= 0) return null;
            option.detailOptions = detailOption;

            return option;
        }
        private string CreateDetailHtml(List<string> images, string html)
        {
            string description = html.GetValue("//p[@class='item-detail-description-text']", TextEx.XPathOption.OuterHtml);

            var htmlBuilder = new StringBuilder();
            htmlBuilder.AppendLine("<div>");
            foreach (string image in images)
            {
                htmlBuilder.AppendLine($"<img src='{image}' style='max-width: 600px'>");
                htmlBuilder.AppendLine("<br>");
            }
            htmlBuilder.AppendLine(description);
            htmlBuilder.AppendLine("</div>");
            string detailHtml = htmlBuilder.ToString();
            return detailHtml;
        }
        public Product GetBrandavenueProduct(Uri uri, string html)
        {
            var product = new Product();
            try
            {
                JObject productData = null;
                List<string> scripts = html.GetValues("//script[@type='application/ld+json']", TextEx.XPathOption.InnerHtml);
                foreach (string script in scripts)
                {
                    try
                    {
                        JObject data = JObject.Parse(script);
                        string? @type = data.Value<string>("@type");
                        if (type == "Product")
                        {
                            productData = data;
                            break;
                        }
                    }
                    catch (Exception e) { e.Show(); }
                }
                if (productData == null) throw new Exception("상품 정보를 찾을 수 없습니다.");

                product.site = "Rakuten";
                string? name = productData.Value<string>("name");
                product.title = base.GetTitle(name);
                product.descs = GetDescs(html);
                string code = GetCode(uri.AbsoluteUri);
                product.code = code;
                product.currency = Currency.JPY.ToString();
                product.tags = new List<Tag>();
                product.price = GetPrice(html);
                JToken? offers = productData["offers"];
                if (offers == null)
                {
                    throw new Exception("라쿠텐 해당 상품의 가격 정보를 확인할 수 없습니다.");
                }
                else
                {
                    product.option = GetOption(html, offers);
                    if (product.option == null) throw new Exception("라쿠텐 해당 상품은 재고가 없는 상품입니다.");
                    List<Price> optionPrices = product.option.detailOptions.SelectMany(x => x.price).ToList();
                    if (product.price == null)
                    {
                        optionPrices = optionPrices.FindAll(x => x.currency == Currency.JPY.ToString());
                        Price minPrice = optionPrices.OrderBy(x => x.salePrice).First();
                        product.price = new List<Price> { minPrice };
                    }
                }

                product.url = uri.AbsoluteUri;
                product.mainImages = new List<string>();

                var detailHtmlImages = new List<string>();
                List<string> imgTags = html.GetValues("//div[@class='item-images-thumbnail-container']//ul//li//img", TextEx.XPathOption.OuterHtml);
                if (imgTags == null || imgTags.Count <= 0) throw new Exception("라쿠텐 해당 상품의 메인 이미지를 찾을 수 없습니다.");
                foreach (string imgTag in imgTags)
                {
                    string src = imgTag.GetValue("//img", "src");
                    string alt = imgTag.GetValue("//img", "alt");
                    try
                    {
                        var imageUri = new Uri(src);
                        if (imageUri.IsFile)
                        {
                            src = "https:" + src;
                        }
                        UriBuilder uriBuilder = new UriBuilder(src);
                        uriBuilder.Query = null;
                        if (string.IsNullOrWhiteSpace(alt) == false) // alt가 빈값일 경우 상세페이지 이미지로 간주
                        {
                            product.mainImages.Add(uriBuilder.Uri.AbsoluteUri);
                        }
                        else
                        {
                            detailHtmlImages.Add(uriBuilder.Uri.AbsoluteUri);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                product.videoUrl = null;
                product.detailPageHtml = CreateDetailHtml(detailHtmlImages, html);
            }
            catch (Exception e)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = e.Message;
            }
            return product;
        }

        private List<Price> GetPrice(string html)
        {
            string ratPrice = html.GetValue("//input[@id='ratPrice']", "value");
            if (string.IsNullOrWhiteSpace(ratPrice)) return null;
            double? price = ratPrice.ParseToDouble();
            if (price == null || price <= 0) return null;
            List<Price> result = new List<Price>() { new Price(Currency.JPY, (double)price, (double)price, true) };
            return result;
        }
    }
}
