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
using System.Threading;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Etsy
{
    public class Etsy : ICollect
    {
        public string Site => nameof(Etsy);

        public string Name => "에트시";

        public Language Language => Language.EN;

        public Currency Currency => Currency.USD;

        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public string GetCode(string url)
        {
            var uri = new Uri(url);
            var segments = uri.Segments.ToList();
            int index = segments.IndexOf("listing/");
            if (index <= 0) return null;
            List<string> range = segments.GetRange(index + 1, 2);
            string code = string.Join("", range);
            if (string.IsNullOrWhiteSpace(code)) return null;
            int lastIndex = code.LastIndexOf("/");
            if (code.Length == lastIndex + 1)
            {
                code = code.Substring(0, code.Length - 1);
            }
            return code;
        }

        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
            var html = await ChatHubService.GetRequest(connectionId, "https://www.etsy.com/");

            return "https://www.etsy.com/?ref=lgo";
        }

        private async Task<JObject> GetApiListingDatas(string connectionId, string html)
        {
            string csrf_nonce = html.GetValue("//meta[@name='csrf_nonce']", "content");
            List<string> scripts = html.GetValues("//script", TextEx.XPathOption.InnerHtml);
            string windowEtsy = scripts.Find(x => x.Contains("window.Etsy=window.Etsy"));
            string context = windowEtsy.SingleSplit("Etsy.Context.data=assign(", ");");
            context = context.SingleSplit("Etsy.Context.data ? Etsy.Context.data : {}, ");
            JObject contextData = context.ToJObject();
            JObject listingCard = new JObject();
            JToken listing_ids = contextData.Value("lazy_loaded_listing_ids");
            listingCard["listing_ids"] = listing_ids;
            JToken ad_ids = contextData.Value("lazy_loaded_ad_ids");
            listingCard["ad_ids"] = ad_ids;
            JToken ads_logging_keys = contextData.Value("lazy_loaded_ad_logging_keys");
            listingCard["ads_logging_keys"] = ads_logging_keys;
            JToken organic_logging_keys = contextData.Value("lazy_loaded_organic_logging_keys");
            listingCard["organic_logging_keys"] = organic_logging_keys;
            JToken search_request_params = contextData.Value("search_request_params");
            listingCard["search_request_params"] = search_request_params;
            bool is_mobile = contextData.Value<bool>("is_mobile");
            listingCard["is_mobile"] = is_mobile;
            JToken organic_listings_count = contextData.Value<int>("organic_listings_count");
            listingCard["organic_listings_count"] = organic_listings_count;
            JToken user_favorite_shop_ids = contextData.Value("user_favorite_shop_ids");
            listingCard["user_favorite_shop_ids"] = user_favorite_shop_ids;
            JToken customizable_ads_listing_ids = new JArray();
            listingCard["customizable_ads_listing_ids"] = customizable_ads_listing_ids;
            JToken customizable_organic_listing_ids = new JArray();
            listingCard["customizable_organic_listing_ids"] = customizable_organic_listing_ids;
            JToken customizable_ads_logging_keys = new JArray();
            listingCard["customizable_ads_logging_keys"] = customizable_ads_logging_keys;
            JToken ads_organic_positions = contextData.Value("lazy_loaded_ads_organic_positions");
            listingCard["ads_organic_positions"] = ads_organic_positions;

            JObject request = new JObject();
            request["log_performance_metrics"] = true;
            JObject specs = new JObject();
            var listingCards = new JArray();
            listingCards.Add("Search2_ApiSpecs_LazyListingCards");
            listingCards.Add(listingCard);
            specs["listingCards"] = listingCards;
            request["specs"] = specs;
            request["view_data_event_name"] = "search_lazy_loaded_cards_specview_rendered";
            request["runtime_analysis"] = false;
            string apiRequestUrl = "https://www.etsy.com/api/v3/ajax/bespoke/member/neu/specs/listingCards";
            var header = new Dictionary<string, string>();
            header.Add("Content-Type", "application/json");
            header.Add("x-csrf-token", csrf_nonce);
            header.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
            string post = await ChatHubService.PostRequest(connectionId, apiRequestUrl, header, request.ToString());
            JObject apiData = post.ToJObject();
            return apiData;
        }

        private CollectPageInfo GetCollectPageInfo(string html)
        {
            string listing_url = html.GetValue("//input[@name='listing_url']", "value");
            if (string.IsNullOrWhiteSpace(listing_url)) return null;
            string code = GetCode(listing_url);
            if (string.IsNullOrWhiteSpace(code)) return null;
            string listing_title = html.GetValue("//input[@name='listing_title']", "value");
            CollectPageInfo collectPageInfo = new CollectPageInfo();
            collectPageInfo.Url = listing_url;
            collectPageInfo.Code = code;
            collectPageInfo.Title = listing_title;
            return collectPageInfo;
        }

        private void CheckVisit(string url, string html)
        {
            var cmsg = html.GetValue("//p[@id='cmsg']", TextEx.XPathOption.InnerText);
            if (cmsg == "Please enable JS and disable any ad blocker")
            {
                throw new CaptchaException
                {
                    CaptchaInfo = new CaptchaInfo
                    {
                        Message = "사이트 방문 후 다시 시도해주세요.",
                        Url = url
                    }
                };
            }
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("page", page);
            var requestUrl = uriBuilderAdvance.ToString();

            string html = await ChatHubService.GetRequest(connectionId, requestUrl);
            CheckVisit(url, html);
            var collectPageInfos = new List<CollectPageInfo>();

            var query = uriBuilderAdvance.GetQuery("q");
            if (string.IsNullOrWhiteSpace(query))
            {
                var scripts = html.GetValues("//script[@type='application/ld+json']", TextEx.XPathOption.InnerHtml);
                foreach (string script in scripts)
                {
                    JObject pageData = script.ToJObject();
                    if (pageData == null) continue;
                    var @type = pageData.Value<string>("@type");
                    if (@type == "ItemList")
                    {
                        var itemListElements = pageData.Value("itemListElement")?.ToList();
                        if (itemListElements != null || itemListElements.Count <= 0)
                        {
                            foreach (var itemListElement in itemListElements)
                            {
                                var itemUrl = itemListElement.Value("item")?.Value<string>("url");
                                if (string.IsNullOrWhiteSpace(itemUrl)) continue;
                                var code = GetCode(itemUrl);
                                if (string.IsNullOrWhiteSpace(code)) continue;
                                var name = itemListElement.Value("item")?.Value<string>("name");
                                var collectPageInfo = new CollectPageInfo();
                                collectPageInfo.Url = itemUrl;
                                collectPageInfo.Code = code;
                                collectPageInfo.Title = name;
                                collectPageInfos.Add(collectPageInfo);
                            }
                        }
                    }
                }
            }
            else
            {
                List<string> forms = html.GetValues("//form[@action='/cart/listing.php']", TextEx.XPathOption.OuterHtml);
                if (forms != null)
                {
                    foreach (string form in forms)
                    {
                        CollectPageInfo collectPageInfo = GetCollectPageInfo(form);
                        if (collectPageInfo == null) continue;
                        collectPageInfos.Add(collectPageInfo);
                    }
                }

                JObject apiData = await GetApiListingDatas(connectionId, html);
                string listingCardsTag = apiData.Value("output")?.Value<string>("listingCards");
                if (string.IsNullOrWhiteSpace(listingCardsTag) == false)
                {
                    List<string> lis = listingCardsTag.GetValues("/li", TextEx.XPathOption.InnerHtml);
                    if (lis != null)
                    {
                        foreach (string li in lis)
                        {
                            CollectPageInfo collectPageInfo = GetCollectPageInfo(li);
                            if (collectPageInfo == null) continue;
                            collectPageInfos.Add(collectPageInfo);
                        }
                    }
                }
            }
            if (collectPageInfos.Count <= 0) return null;
            return collectPageInfos;
        }

        public JToken GetData(string html)
        {
            var scripts = html.GetValues("//script[@type='application/ld+json']", TextEx.XPathOption.InnerHtml);
            JObject productData = null;
            foreach (var script in scripts)
            {
                JObject scriptsData = script.ToJObject();
                if (scriptsData == null) continue;
                string @type = scriptsData.Value<string>("@type");
                if (@type == "Product")
                {
                    productData = scriptsData;
                    break;
                }
            }
            if (productData == null) throw new Exception("상품 정보를 확인할 수 없습니다.");
            return productData;
        }

        public List<Name> GetTitle(JToken data)
        {
            string name = data.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("상품명을 확인할 수 없습니다.");
            return ProductHelper.CreateName(this.Language, name);
        }

        public List<Name> GetBrand(JToken data)
        {
            string name = data.Value("brand")?.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) return null;
            return ProductHelper.CreateName(this.Language, name);
        }

        public string GetModel(JToken data)
        {
            var sku = data.Value<string>("sku");
            return sku;
        }

        public List<Price> GetPrice(string html)
        {
            var priceHtml = html.GetValue("//div[@data-appears-component-name='price']", TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(priceHtml)) throw new Exception("상품 가격을 찾을 수 없습니다.");
            var pTags = priceHtml.GetValues("//p", TextEx.XPathOption.InnerText);
            if (pTags == null || pTags.Count <= 0) throw new Exception("상품 가격을 찾을 수 없습니다.");
            var priceList = new List<double?>();
            foreach (string pTag in pTags)
            {
                var price = pTag.ParseToDouble();
                priceList.Add(price);
            }
            if (priceList.Count <= 0) throw new Exception("상품 가격을 찾을 수 없습니다.");
            return ProductHelper.CreatePrice(this.Currency, priceList.Max(), priceList.Min(), true);
        }

        public List<string> GetMainImages(JToken data)
        {
            var images = data.Value("image")?.ToList();
            if (images == null || images.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");

            var result = new List<string>();
            foreach (JToken image in images)
            {
                string contentURL = image.Value<string>("contentURL");
                if (string.IsNullOrWhiteSpace(contentURL)) continue;
                result.Add(contentURL);
            }
            if (result.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            return result;
        }

        public string GetDetailPageHtml(JToken data)
        {
            var description = data.Value<string>("description");
            var html = description.Replace("\n", "<br>");
            return html;
        }
        public Option GetOption(string html)
        {
            var variations = html.GetValue("//div[@data-appears-component-name='variations']", TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(variations)) return null;
            var variationDivs = variations.GetValues("//div[@data-selector='listing-page-variations']//div[@data-selector='listing-page-variation']", TextEx.XPathOption.OuterHtml);
            if (variationDivs == null || variationDivs.Count <= 0) return null;

            var optionGroups = new List<Option.OptionGroup>();

            var dic = new Dictionary<string, double?>();

            for (int i = 0; i < variationDivs.Count; i++)
            {
                var div = variationDivs[i];
                Option.OptionGroup optionGroup = new Option.OptionGroup();
                optionGroup.optionId = i.ToString();

                string optionName = div.GetValue("//label//span", TextEx.XPathOption.InnerText)?.Trim();
                if (string.IsNullOrWhiteSpace(optionName)) continue;
                optionGroup.groupName = ProductHelper.CreateName(this.Language, optionName);

                var select = div.GetValue("//select[contains(@id,'variation-selector-')]", TextEx.XPathOption.OuterHtml);
                var groups = new List<Option.OptionGroup.Group>();
                List<string> options = select.GetValues("//option", TextEx.XPathOption.OuterHtml);
                foreach (string option in options)
                {
                    string value = option.GetValue("//option", "value");
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    string name = option.GetValue("//option", TextEx.XPathOption.InnerText)?.Trim();
                    var optionPriceTxt = name.SubstringBetween("(USD", ")");
                    if (string.IsNullOrWhiteSpace(optionPriceTxt) == false)
                    {
                        var optionPrice = optionPriceTxt.ParseToDouble();
                        dic.Add(value, optionPrice);
                        name = name.Replace(optionPriceTxt, "").Trim();
                    }
                    var group = new Option.OptionGroup.Group();
                    group.groupId = value;
                    group.optionName = ProductHelper.CreateName(this.Language, name);
                    groups.Add(group);
                }
                optionGroup.groups = groups;
                optionGroups.Add(optionGroup);
            }

            Option productOption = new Option();
            productOption.optionGroups = optionGroups;
            productOption.detailOptions = ProductHelper.CreateDetailOptions(optionGroups);
            for (int i = 0; i < productOption.detailOptions.Count; i++)
            {
                var ids = productOption.detailOptions[i].optionIds.Split("ㅡ");
                foreach (var id in ids)
                {
                    double? price = null;
                    if (dic.TryGetValue(id, out price))
                    {
                        if (productOption.detailOptions[i].price == null)
                        {
                            productOption.detailOptions[i].price = ProductHelper.CreatePrice(this.Currency, price, price);
                        }
                        else
                        {
                            productOption.detailOptions[i].price[0].salePrice += (double)price;
                            productOption.detailOptions[i].price[0].originalPrice += (double)price;
                        }
                    }
                }
            }
            return productOption;
        }

        private async Task SetDetailOptionPrice(string connectionId, string productCode, Option.DetailOption detailOption, int loopCount = 1)
        {
            if (loopCount > 5)
            {
                detailOption.isSoldOut = true;
                return;
            }
            var sb = new StringBuilder();
            sb.Append($"https://www.etsy.com/api/v3/ajax/bespoke/member/listings/{productCode}/offerings/find-by-variations?");
            sb.Append("channel=1&");
            string[] codes = detailOption.optionIds.Split("ㅡ");
            foreach (var code in codes)
            {
                sb.Append($"listing_variation_ids%5B%5D={code}&");
            }
            sb.Append("selected_quantity=1&");
            sb.Append("is_users_own_listing=false&");
            sb.Append("is_mobile_experience=false&");
            sb.Append("is_quantity_submodule_enabled=false&");
            sb.Append("is_quantity_inventory_submodule_enabled=true&");
            sb.Append("is_eligible_for_klarna_osm=false&");
            sb.Append("is_eligible_for_variations_update=true&");
            sb.Append("coupons_in_buy_box_is_enabled=false&");
            sb.Append("can_listing_have_coupon_applied=false&");
            sb.Append("is_multiple_questions_enabled_buyer=false&");
            sb.Append("lp_toffers_v2_true_sale_enabled=false&");
            sb.Append("lp_toffers_v2_gamed_sale_enabled=false&");
            sb.Append("is_rare_deal=false&");
            sb.Append("rare_deal_msg_id=0&");
            sb.Append("gamed_sales_v3_enabled=false");
            var ajaxOptionRequestUrl = sb.ToString();

            var header = new Dictionary<string, string>();
            header.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
            header.Add("x-etsy-protection", "1");
            var optionPriceHtml = await ChatHubService.GetRequest(connectionId, ajaxOptionRequestUrl, header);
            var data = optionPriceHtml.ToJObject();
            if (data == null)
            {
                Thread.Sleep(1000);
                await SetDetailOptionPrice(connectionId, productCode, detailOption, loopCount + 1);
            }
            else
            {
                var priceHtml = data.Value<string>("price");
                var pTags = priceHtml.GetValues("//p", TextEx.XPathOption.InnerText);

                var priceList = new List<double?>();
                foreach (string pTag in pTags)
                {
                    var price = pTag.ParseToDouble();
                    priceList.Add(price);
                }
                if (priceList == null || priceList.Count <= 0)
                {
                    detailOption.isSoldOut = true;
                }
                else
                {
                    detailOption.price = ProductHelper.CreatePrice(this.Currency, priceList.Max(), priceList.Min());
                    detailOption.quantity = 100;
                }
            }
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var html = await ChatHubService.GetRequest(connectionId, url);
            CheckVisit(url, html);
            var product = new Product();
            try
            {
                var productData = GetData(html);
                product.site = this.Site;
                product.code = GetCode(url);
                product.url = url;
                product.currency = this.Currency.ToString();
                product.title = GetTitle(productData);
                product.brand = GetBrand(productData);
                product.model = GetModel(productData);
                product.price = GetPrice(html);
                product.mainImages = GetMainImages(productData);
                product.detailPageHtml = GetDetailPageHtml(productData);
                product.option = GetOption(html);
                if (product.option != null)
                {
                    List<Price> copyPrice = product.price.DeepCopyObject();
                    var _salePrice = copyPrice.First().salePrice;
                    var _originalPrice = copyPrice.First().originalPrice;
                    double gap = _salePrice / _originalPrice * 100d;
                    gap = Math.Truncate(gap) / 100d;
                    for (int i = 0; i < product.option.detailOptions.Count; i++)
                    {
                        if (product.option.detailOptions[i].price == null)
                        {
                            product.option.detailOptions[i].price = copyPrice;
                        }
                        else
                        {
                            double salePrice = product.option.detailOptions[i].price.First().salePrice;
                            product.option.detailOptions[i].price.First().originalPrice = salePrice / gap;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = e.Message;
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
