#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserScrapingBridge.Server.Sourcing.Rakuten
{
    public class RakutenBook : Rakuten
    {
        #region 라쿠텐 Book
        private List<Name> GetBookTitle(string html)
        {
            //string productInfo = html.GetValue("//div[@id='productInfo']", TextEx.XPathOption.OuterHtml);
            //string title = productInfo.GetSelectNodeInnerText("//div[@id='productTitle']//h1[@itemprop='name']");
            //title = title.Trim();

            string title = html.GetValue("//form[@class='ratForm']//input[@id='ratItemName']", "value");
            if (string.IsNullOrWhiteSpace(title)) throw new Exception("상품명을 찾을 수 없습니다.");
            title = title.Trim();
            List<Name> getTitle = base.GetTitle(title);
            return getTitle;
        }
        private List<Desc> GetBookDescs(string html)
        {
            List<Desc> descs = null;
            string descUlTag = html.GetValue("//div[@id='productDetailedDescription']//ul", TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(descUlTag) == false)
            {
                descs = new List<Desc>();

                List<string> lis = descUlTag.GetValues("//li", TextEx.XPathOption.OuterHtml);
                for (int i = 0; i < lis.Count; i++)
                {
                    string li = lis[i];
                    if (string.IsNullOrWhiteSpace(li)) continue;
                    string key = li.GetValue("//span[@class='category']", TextEx.XPathOption.InnerText);
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    string value = li.GetValue("//span[@class='categoryValue']", TextEx.XPathOption.InnerText);
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    key = key.Trim();
                    value = value.Trim();
                    var desc = new Desc(i + 1, Language.JP, key, value);
                    descs.Add(desc);
                }
            }
            return descs;
        }
        private string GetBookCode(Uri uri)
        {
            string code = uri.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
            if (string.IsNullOrWhiteSpace(code)) throw new Exception("상품 코드 수집에 문제가 생겼습니다.");
            return code;
        }

        private List<Price> GetBookPrice(string html)
        {
            string ratItemPrice = html.GetValue("//form[@class='ratForm']//input[@id='ratItemPrice']", "value");
            if (string.IsNullOrWhiteSpace(ratItemPrice)) throw new Exception("상품 가격을 찾을 수 없습니다.");
            double price = Convert.ToDouble(ratItemPrice);
            if (price <= 0) throw new Exception("상품 가격을 찾을 수 없습니다.");
            List<Price> prices = new List<Price>();
            prices.Add(new Price(Currency.JPY, price, price));
            prices.Add(new Price(Currency.KRW, 0, 0));
            return prices;
        }

        private static string GetUrl(Uri uri)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(uri);
            uriBuilderAdvance.ClearQuery();
            string url = uriBuilderAdvance.ToString();
            return url;
        }

        private List<string> GetImageSlider(string html)
        {
            var imageSlider = html.GetValue("//ul[@id='imageSlider']");
            if (string.IsNullOrWhiteSpace(imageSlider)) return null;
            List<string> imageUriStrings = imageSlider.GetValues("//li", "data-thumb");

            for (int i = 0; i < imageUriStrings.Count; i++)
            {
                string imageUri = imageUriStrings[i];
                try
                {
                    var uri = new Uri(imageUri);
                    if (uri.IsFile)
                    {
                        imageUri = "https:" + imageUri;
                    }
                    int queryIndex = imageUri.IndexOf("?");
                    if (queryIndex < 0) continue;
                    imageUri = imageUri.Substring(0, queryIndex);
                    imageUriStrings[i] = imageUri;
                }
                catch { continue; }
            }
            return imageUriStrings;
        }

        private List<string> GetOneImage(string html)
        {
            string src = html.GetValue("//div[@id='oneImageWrap']//img", "src");
            try
            {
                var uri = new Uri(src);
                if (uri.IsFile)
                {
                    src = $"https:{src}";
                }
                int queryIndex = src.IndexOf("?");
                if (queryIndex > 0)
                {
                    src = src.Substring(0, queryIndex);
                }
                return new List<string> { src };
            }
            catch (Exception e)
            {
                return null;
            }
        }


        private List<string> GetBookImages(string html)
        {
            var getImageMethods = new List<Func<string, List<string>>>();
            getImageMethods.Add(GetImageSlider);
            getImageMethods.Add(GetOneImage);

            List<string> result = null;
            foreach (Func<string, List<string>> getImageMethod in getImageMethods)
            {
                List<string> images = getImageMethod.Invoke(html);
                if (images != null && images.Count > 0)
                {
                    result = images;
                    break;
                }
            }
            if (result == null) { throw new Exception("메인 이미지를 찾을 수 없습니다."); }
            return result;
        }
        private string GetBookDetailPageHtml(string html)
        {
            string editArea2 = html.GetValue("//div[@id='editArea2']", TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(editArea2)) return null;
            return editArea2;
        }
        private Option CreateBookOption(string html, string code, List<Price> price)
        {
            Option option = new Option();

            option.optionGroups = new List<Option.OptionGroup>();
            Option.OptionGroup optionGroup = new Option.OptionGroup();
            optionGroup.optionId = "0";
            optionGroup.groupName = new List<Name>();
            optionGroup.groupName.Add(new Name("JP", "OPT"));
            optionGroup.groupName.Add(new Name("KR", "옵션"));
            option.optionGroups.Add(optionGroup);

            optionGroup.groups = new List<Option.OptionGroup.Group>();
            Option.OptionGroup.Group group = new Option.OptionGroup.Group();
            group.groupId = "0";
            group.optionName = new List<Name>();
            group.optionName.Add(new Name("JP", "OPT"));
            group.optionName.Add(new Name("KR", "옵션1"));
            optionGroup.groups.Add(group);

            option.detailOptions = new List<Option.DetailOption>();
            option.detailOptions.Add(new Option.DetailOption()
            {
                optionIds = $"0ㅡ0ㅡ{code}",
                isSoldOut = false,
                quantity = 999,
                optionGroupName = new List<Name>()
                {
                    new Name(Language.JP, "OPT"),
                    new Name(Language.KR, "옵션")
                },
                optionName = new List<Name>()
                {
                    new Name(Language.JP, "OPT"),
                    new Name(Language.KR, "옵션1")
                },
                price = price
            });
            return option;
        }
        #endregion

        public Product GetBookProduct(Uri uri, string html)
        {
            var product = new Product();
            try
            {
                product.site = "Rakuten";
                product.title = GetBookTitle(html);
                product.descs = GetBookDescs(html);
                product.code = GetBookCode(uri);
                product.currency = Currency.JPY.ToString();
                product.tags = new List<Tag>();
                product.price = GetBookPrice(html);
                product.url = GetUrl(uri);
                product.mainImages = GetBookImages(html);
                product.videoUrl = null;
                product.detailPageHtml = GetBookDetailPageHtml(html);
                product.option = CreateBookOption(html, product.code, product.price);
            }
            catch (Exception exception)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = $"상품 수집 실패 : {exception.Message}";
            }
            return product;
        }

        
    }
}
