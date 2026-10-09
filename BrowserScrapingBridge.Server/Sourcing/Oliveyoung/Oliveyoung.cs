#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Oliveyoung
{
    public class Oliveyoung : ICollect
    {
        public string Site => nameof(Oliveyoung);
        public string Name => "올리브영";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public string GetCode(string url)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            string goodsNo = uriBuilderAdvance.GetQuery("goodsNo");
            return goodsNo;
        }

        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }

        private void CheckCaptcha(string url, string html)
        {
            var scripts = html.GetValues("//script", TextEx.XPathOption.InnerText);
            foreach (var item in scripts)
            {
                bool window_cf_chl_opt = item.Contains("window._cf_chl_opt");
                throw new CaptchaException()
                {
                    CaptchaInfo = new CaptchaInfo()
                    {
                        Message = "올리브영 캡차가 발생했습니다.",
                        Url = url
                    }
                };
            }
        }

        private async Task<string> GetHtml(string url)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.Proxy = this.Proxy;
            string result = null;
            using (HttpClient httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler))
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    string html = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(html) == false)
                    {
                        result = html;
                    }
                }
                catch (Exception exception)
                {
                }
            }
            return result;
        }

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("pageIdx", page.ToString());
            uriBuilderAdvance.SetQuery("rowsPerPage", "24");
            string pageUrl = uriBuilderAdvance.ToString();
            //var headers = new Dictionary<string, string>();
            //headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36");
            //var GetCookies = await ChatHubService.GetCookies(connectionId, ".oliveyoung.co.kr");
            string html = await ChatHubService.GetRequest(connectionId, pageUrl);
            //var aaa = await GetHtml(pageUrl);
            //CheckCaptcha(url, aaa);
            if (string.IsNullOrWhiteSpace(html)) return null;
            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            var prd_infos = html.GetValues("//div[@class='prd_info ']", TextEx.XPathOption.OuterHtml);
            foreach (string prd_info in prd_infos)
            {
                string goodsno = prd_info.GetValue("//button[@class='btn_zzim jeem']", "data-ref-goodsno");
                if (string.IsNullOrWhiteSpace(goodsno)) continue;
                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = goodsno;
                collectPageInfo.Url = $"https://www.oliveyoung.co.kr/store/goods/getGoodsDetail.do?goodsNo={goodsno}";
                collectPageInfo.Title = prd_info.GetSelectNodeInnerText("//p[@class='tx_name']")?.Trim();
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }

        public List<Name> GetTitle(string html)
        {
            string itemName = html.GetValue("//meta[@property='eg:itemName']", "content");
            if (string.IsNullOrWhiteSpace(itemName)) throw new Exception("상품명을 찾을 수 없습니다.");
            var titles = new List<Name> { new Name(Language.KR, itemName) };
            return titles;
        }

        public List<Name> GetBrand(string html)
        {
            string brandName = html.GetValue("//meta[@property='eg:brandName']", "content");
            if (string.IsNullOrWhiteSpace(brandName) == false)
            {
                var brand = new List<Name> { new Name(Language.KR, brandName) };
                return brand;
            }
            else
            {
                return null;
            }
        }

        public async Task<List<Desc>> GetDescs(string connectionId, string code, Dictionary<string, string> defaultHeaders)
        {
            string body = $"goodsNo={code}&itemNo=001&pkgGoodsYn=N";
            string html = await ChatHubService.PostRequest(connectionId, "https://www.oliveyoung.co.kr/store/goods/getGoodsArtcAjax.do", defaultHeaders, body);

            var descs = new List<Desc>();
            List<string> detail_info_list = html.GetValues("//dl[@class='detail_info_list']", TextEx.XPathOption.OuterHtml);
            if (detail_info_list != null && detail_info_list.Count > 0)
            {
                for (int i = 0; i < detail_info_list.Count; i++)
                {
                    var detail_info = detail_info_list[i];
                    string dt = detail_info.GetSelectNodeInnerText("//dt")?.Trim();
                    string dd = detail_info.GetSelectNodeInnerText("//dd")?.Trim();
                    if (string.IsNullOrWhiteSpace(dt) || string.IsNullOrWhiteSpace(dd)) continue;
                    var desc = new Desc(i + 1, Language.KR, dt, dd); ;
                    descs.Add(desc);
                }
            }
            return descs;
        }

        public List<Tag> GetTags()
        {
            return null;
        }

        public List<Price> GetPrice(string html)
        {
            string originalPriceValue = html.GetValue("//meta[@property='eg:originalPrice']", "content");
            string salePriceValue = html.GetValue("//meta[@property='eg:salePrice']", "content");
            double? originalPrice = originalPriceValue.ParseToDouble();
            double? salePrice = salePriceValue.ParseToDouble();

            if (originalPrice == null && salePrice == null && originalPrice == 0 && salePrice == 0)
            {
                throw new Exception("상품 가격을 찾을 수 없습니다.");
            }

            if (salePrice == null || salePrice <= 0)
            {
                salePrice = originalPrice;
            }

            if (originalPrice == null || originalPrice < salePrice)
            {
                originalPrice = salePrice;
            }

            var price = new Price(Currency.KRW, (double)originalPrice, (double)salePrice, true);
            List<Price> prices = new List<Price>() { new Price(Currency.KRW, (double)originalPrice, (double)salePrice, true) };
            string dlexAmtPopup = html.GetValue("//div[@id='dlexAmtPopup']", TextEx.XPathOption.OuterHtml);
            string dlexAmt = dlexAmtPopup.GetSelectNodeInnerText("//span[@id='dlexAmt']");
            double? deliveryPrice = dlexAmt.ParseToDouble();
            if (deliveryPrice != null && deliveryPrice > 0)
            {
                price.salePrice += (double)deliveryPrice;
                price.originalPrice += (double)deliveryPrice;
            }
            return prices;
        }

        public List<string> GetMainImages(string html)
        {
            string prd_thumb_list = html.GetValue("//ul[@id='prd_thumb_list']", TextEx.XPathOption.OuterHtml);
            List<string> dataImgs = prd_thumb_list.GetValues("//li/a", "data-img"); ;
            return dataImgs;
        }

        public string GetVideoUrl()
        {
            return null;
        }

        public string GetDetailPageHtml(string html)
        {
            var new_detail_wrap = html.GetValue("//div[@id='new_detail_wrap']", TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(new_detail_wrap)) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            var dafaultHtml = HtmlReader.GetDafaultHtml(new_detail_wrap);
            return dafaultHtml;
        }

        public async Task<Option> GetOption(string connectionId, string code, Dictionary<string, string> defaultHeaders)
        {
            string body = $"goodsNo={code}& quickYn=N&mbrDlvpSeq=&quickGiftYn=&quickAeEvtNo=";
            string optionHtml = await ChatHubService.PostRequest(connectionId, "https://www.oliveyoung.co.kr/store/goods/getOptInfoListAjax.do", defaultHeaders, body);
            if (string.IsNullOrWhiteSpace(optionHtml)) return null;

            var optionGroups = new List<Option.OptionGroup>();
            var optionGroup = new Option.OptionGroup();
            optionGroup.optionId = "0";
            Name groupName = new Name(Language.KR, "상품을 선택해주세요");
            optionGroup.groupName = new List<Name> { groupName };
            optionGroup.groups = new List<Option.OptionGroup.Group>();
            var detailOptions = new List<Option.DetailOption>();

            List<string> lis = optionHtml.GetValues("//li", TextEx.XPathOption.OuterHtml);
            for (int i = 0; i < lis.Count; i++)
            {
                var group = new Option.OptionGroup.Group();
                var detailOption = new Option.DetailOption();
                string id = i.ToString();
                group.groupId = id;
                detailOption.optionIds = id;

                string li = lis[i];
                string src = li.GetValue("//img", "src");
                if (string.IsNullOrWhiteSpace(src) == false)
                {
                    group.optionImage = src;
                }

                string option_value = li.GetSelectNodeInnerText("//span[@class='option_value']").Trim();
                if (string.IsNullOrWhiteSpace(option_value)) continue;
                Name optionName = new Name(Language.KR, option_value);
                group.optionName = new List<Name> { optionName };
                detailOption.optionGroupName = new List<Name> { groupName };
                detailOption.optionName = new List<Name> { optionName };

                string option_price = li.GetSelectNodeInnerText("//span[@class='option_price']//span[@class='tx_num']")?.Trim();
                if (string.IsNullOrWhiteSpace(option_price)) continue;
                double? optionPrice = option_price.ParseToDouble();
                detailOption.price = new List<Price> { new Price(Currency.KRW, (double)optionPrice, (double)optionPrice) };
                optionGroup.groups.Add(group);
                detailOptions.Add(detailOption);

            }
            optionGroups.Add(optionGroup);
            if (optionGroup.groups.Count <= 0 || detailOptions.Count <= 0) return null;
            var option = new Option();
            option.optionGroups = optionGroups;
            option.detailOptions = detailOptions;
            return option;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            var product = new Product();
            try
            {
                string code = GetCode(url);
                CheckCaptcha(url, html);
                var defaultHeaders = new Dictionary<string, string>();
                defaultHeaders.Add("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
                product.site = this.Site;
                product.code = code;
                product.currency = Currency.KRW.ToString();
                product.url = url;
                product.title = GetTitle(html);
                product.brand = GetBrand(html);
                product.descs = await GetDescs(connectionId, code, defaultHeaders);
                product.price = GetPrice(html);
                product.tags = GetTags();
                product.mainImages = GetMainImages(html);
                product.detailPageHtml = GetDetailPageHtml(html);
                product.option = await GetOption(connectionId, code, defaultHeaders);
                product.regDate = DateTime.Now;
            }
            catch (CaptchaException captchaException)
            {
                throw captchaException;
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
