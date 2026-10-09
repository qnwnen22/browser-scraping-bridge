#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using BrowserScrapingBridge.Server.Sourcing.Domeggook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Domeggook
{
    public class Domeme : ICollect
    {
        public string Site { get; } = nameof(Domeme);
        public string Name => "도매매";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public string ApiKeyPageUri { get; private set; } = "https://openapi.domeggook.com/main/apikey";
        public string LoginPageUri { get; private set; } = "https://domeggook.com/ssl/member/mem_loginForm.php?back=aHR0cHM6Ly9kb21lbWVkYi5kb21lZ2dvb2suY29tL2luZGV4";
        public string CheckLoginUrl => "https://domeggook.com/ssl/member/mem_loginForm.php";
        private string ApiKey { get; set; }

        public bool SupportsCaching { get; set; }
        #region Common
        /// <summary>
        /// API키 가져오기
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public List<string> GetApiKeys(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;
            List<string> dataApiKeys = html.GetValues("//td[@class='lDataApiKey']", TextEx.XPathOption.InnerText);
            if (dataApiKeys == null || dataApiKeys.Count <= 0) return null;
            return dataApiKeys;
        }
        /// <summary>
        /// 도매매 카테고리 정보 조회
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public List<Category> GetCategories(string apiKey)
        {
            var getCategoryListApi = new GetCategoryListApi(apiKey);
            List<Category> categories = getCategoryListApi.GetCategories();
            return categories;
        }
        #endregion
        public string GetCode(string code)
        {
            if (code.IndexOf("http") == 0)
            {
                try
                {
                    var uri = new Uri(code);
                    code = uri.Segments.Last();
                }
                catch { }
            }
            return code;
        }
        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            string apiKey = BrowserScrapingBridge.Server.Sourcing.Transport.SourcingExecution.Current.GetSetting("domemeApiKey");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return ApiKeyPageUri;
            }
            else
            {
                return null;
            }
        }
        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            UriBuilderAdvance uriBuilderAdvance = new(url);
            uriBuilderAdvance.SetQuery("pagenum", (page - 1).ToString());
            string pageUri = uriBuilderAdvance.ToString();
            string html = await ChatHubService.GetRequest(connectionId, pageUri);

            List<CollectPageInfo> result = new List<CollectPageInfo>();
            List<string> divTags = html.GetValues("//div[contains(@class, 'sub_cont_bane1_SetListGallery')]", TextEx.XPathOption.OuterHtml);
            foreach (string divTag in divTags)
            {
                string itemCode = divTag.GetValue("//label", "for");
                string title = divTag.GetSelectNodeInnerText("//div[contains(@class, 'main_cont_text1')]").Trim();

                CollectPageInfo collectPageInfo = new()
                {
                    Code = itemCode,
                    Url = $"https://domeme.domeggook.com/s/{itemCode}",
                    Title = title
                };

                CollectPageInfo? find = result.Find(x => x.Equals(collectPageInfo));
                if (find == null)
                {
                    result.Add(collectPageInfo);
                }
            }
            return result;
        }
        private List<Name> GetTitle(JObject domeggook)
        {
            string? title = domeggook["basis"]?.Value<string>("title");
            if (string.IsNullOrWhiteSpace(title)) throw new CollectException("상품명을 확인할 수 없습니다.");

            //if (domeMoq != null && domeMoq > 1)
            //{
            //    title = $"{title} ({domeMoq}개)";
            //}

            var result = new List<Name>();
            result.Add(new Name(Language.KR, title));
            return result;
        }
        private List<Desc> GetDescs(JObject domeggook)
        {
            var result = new List<Desc>();

            JToken? jToken = domeggook["detail"]?["infoDuty"]?["item"];
            if (jToken != null)
            {
                List<JToken> items = jToken.ToList();
                for (int i = 0; i < items.Count; i++)
                {
                    string? title = items[i].Value<string>("name");
                    if (string.IsNullOrWhiteSpace(title)) continue;
                    string? value = items[i].Value<string>("desc");
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    var desc = new Desc(i + 1, Language.KR, title, value);
                    result.Add(desc);
                }
            }

            return result;
        }
        private List<Tag> GetTags(JObject domeggook)
        {
            List<Tag> result = new List<Tag>();

            JToken? kw = domeggook["basis"]?["keywords"]?["kw"];
            if (kw != null)
            {
                var tag = new Tag();
                tag.key = "kw";
                tag.values = new List<string>();

                List<JToken> list = kw.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    var value = list[i].ToString();
                    tag.values.Add(value);
                }
            }

            return result;
        }
        private double? ConvertPrice(string priceString)
        {
            double salePrice = 0;

            if (string.IsNullOrWhiteSpace(priceString)) return null;
            if (priceString.Contains("+") || priceString.Contains("|"))
            {
                string[] supplys = priceString.Split('|', StringSplitOptions.RemoveEmptyEntries);

                double tempPrice = -1;
                foreach (string item in supplys)
                {
                    string? salePriceString = item.Split('+', StringSplitOptions.RemoveEmptyEntries)?.Last();
                    bool isTryParse = double.TryParse(salePriceString, out tempPrice);
                    if (isTryParse)
                    {
                        if (salePrice < tempPrice)
                        {
                            salePrice = tempPrice;
                        }
                    }
                }
            }
            else
            {
                bool isTryParse = double.TryParse(priceString, out salePrice);
                if (isTryParse == false) return null;
            }
            return salePrice;
        }
        private List<Price> GetPrice(JObject domeggook)
        {
            List<Price> result = new List<Price>();

            JToken? priceToken = domeggook["price"];
            if (priceToken == null) throw new CollectException("가격 정보를 찾을 수 없습니다");

            string? supply = priceToken.Value<string>("supply");
            if (string.IsNullOrWhiteSpace(supply)) throw new CollectException("도매매 상품이 아닙니다.");
            double? salePrice = ConvertPrice(supply);
            if (salePrice == null || salePrice <= 0) throw new CollectException("가격 정보를 찾을 수 없습니다");

            string? supplyOrg = priceToken.Value<string>("supplyOrg");
            double? originalPrice = ConvertPrice(supplyOrg);
            if (originalPrice == null || originalPrice <= 0)
            {
                originalPrice = salePrice;
            }

            #region 도매꾹 가격 - ※수집 정책 = 도매매 상품이 아닐 경우 예외처리
            //double? dome_salePrice = priceToken.Value<double?>("dome");
            //double? dome_orignalPrice = priceToken.Value<double?>("domeOrg"); 
            #endregion

            var price = new Price(Currency.KRW, (double)originalPrice, (double)salePrice, true);
            result.Add(price);
            return result;
        }
        private List<string> GetMainIages(JObject domeggook)
        {
            JToken? thumb = domeggook["thumb"];
            if (thumb == null) throw new CollectException("메인 이미지를 찾을 수 없습니다.");
            string? original = thumb.Value<string>("original");
            if (string.IsNullOrWhiteSpace(original)) throw new CollectException("메인 이미지를 찾을 수 없습니다.");
            var mainImages = new List<string> { original };
            return mainImages;
        }
        private string GetVideoUrl(JObject domeggook) { return null; }
        private string GetDetailPageHtml(JObject domeggook)
        {
            var stringBuilder = new StringBuilder();

            JToken? desc = domeggook["desc"];
            if (desc == null) throw new CollectException("상품 상세페이지를 찾을 수 없습니다.");

            string? notice = desc.Value<string>("notice");
            if (string.IsNullOrWhiteSpace(notice) == false)
            {
                stringBuilder.AppendLine(notice);
            }

            string? contents = desc["contents"]?.Value<string>("item");
            if (string.IsNullOrWhiteSpace(contents)) throw new CollectException("상품 상세페이지를 찾을 수 없습니다.");
            stringBuilder.AppendLine(contents);

            string detailPageHtml = stringBuilder.ToString();
            return detailPageHtml;
        }
        private List<Option.OptionGroup> GetOptionGroups(JObject optionData)
        {
            var result = new List<Option.OptionGroup>();

            List<JToken> setList = optionData["set"]?.ToList();
            if (setList == null) return null;
            for (int i = 0; i < setList.Count; i++)
            {
                var optionGroup = new Option.OptionGroup();
                JToken set = setList[i];
                string? name = set.Value<string>("name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                optionGroup.optionId = i.ToString();
                optionGroup.groupName = new List<Name> { new Name(Language.KR, name) };
                optionGroup.groups = new List<Option.OptionGroup.Group>();

                List<JToken> opts = set["opts"].ToList();
                for (int j = 0; j < opts.Count; j++)
                {
                    JToken opt = opts[j];
                    var group = new Option.OptionGroup.Group();
                    group.groupId = j.ToString("00");
                    group.optionName = new List<Name> { new Name(Language.KR, opt.ToString()) };
                    optionGroup.groups.Add(group);
                }
                result.Add(optionGroup);
            }
            return result;
        }
        private List<Option.DetailOption> GetDetailOptions(List<Option.OptionGroup> optionGroups, JObject optionData, JToken qtyToken)
        {
            List<Name> groupNames = optionGroups.SelectMany(x => x.groupName).ToList();
            IEnumerable<string> texts = groupNames.Select(x => x.text);
            string optionGroupName = string.Join("ㅡ", texts);

            var result = new List<Option.DetailOption>();

            JToken? data = optionData["data"];

            var keyList = new List<string>();
            JEnumerable<JToken> children = data.Children();
            foreach (JToken child in children)
            {
                string id = child.Path.Split(".").Last();
                keyList.Add(id);
            }

            //int? domeMoq = qtyToken.Value<int?>("domeMoq");
            int? inventory = qtyToken.Value<int?>("inventory");

            for (int i = 0; i < keyList.Count; i++)
            {
                string id = keyList[i];
                JToken? detailOptionData = data[id];

                int qty = detailOptionData.Value<int>("qty");
                if (qty == 0 && inventory != null && inventory > 0)
                {
                    qty = (int)inventory;
                }
                if (qty <= 0) continue;

                string? name = detailOptionData.Value<string>("name");
                if (texts.Count() > 1 && name.Contains("/"))
                {
                    name = name.Replace("/", "ㅡ");
                }
                double domPrice = detailOptionData.Value<double>("domPrice");
                //if (domeMoq != null && domeMoq > 1)
                //{
                //    name += $" x {domeMoq}";
                //    domPrice *= (double)domeMoq;
                //}

                var detailOption = new Option.DetailOption();
                detailOption.optionGroupName = new List<Name> { new Name(Language.KR, optionGroupName) };
                detailOption.optionIds = id;
                detailOption.optionName = new List<Name> { new Name(Language.KR, name) };
                detailOption.quantity = qty;
                detailOption.price = new List<Price>
                {
                    new Price(Currency.KRW, domPrice, domPrice)
                };
                result.Add(detailOption);
            }
            return result;
        }
        private Option GetOption(JObject domeggook)
        {
            try
            {
                JToken? selectOpt = domeggook["selectOpt"];
                if (selectOpt == null) return null;
                string selectOptString = selectOpt.ToString();
                if (string.IsNullOrWhiteSpace(selectOptString)) return null;
                JObject optionData = JObject.Parse(selectOptString);

                var option = new Option();
                option.optionGroups = GetOptionGroups(optionData);
                JToken? qtyToken = domeggook["qty"];
                option.detailOptions = GetDetailOptions(option.optionGroups, optionData, qtyToken);
                if (option.detailOptions.Count <= 0) { throw new CollectException("재고가 없는 상품입니다."); }
                return option;
            }
            catch (Exception exception)
            {
                throw new CollectException("옵션 정보를 가져오는데 실패했습니다", exception);
            }
        }
        public Product GetProduct(string url, string html)
        {
            var product = new Product();
            try
            {
                string code = GetCode(url);

                GetItemViewApi getItemViewApi = new GetItemViewApi(this.ApiKey);
                getItemViewApi.no = code;
                getItemViewApi.allItem = true;
                string requestUri = getItemViewApi.CreateRequestUri();

                BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest request = BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest.CreateHttp(requestUri);
                request.Timeout = 10000;
                var result = request.ReadToEnd();
                JObject data = JObject.Parse(result);
                JObject? domeggook = data.Value<JObject>("domeggook");
                if (domeggook == null)
                {
                    string? dmessage = data["errors"].Value<string>("dmessage");
                    throw new CollectException($"{dmessage}, 상품 코드를 다시 확인해주세요 : {code}");
                }

                product.site = nameof(Domeme);
                product.title = GetTitle(domeggook);
                product.descs = GetDescs(domeggook);
                product.code = code;
                product.currency = Currency.KRW.ToString();
                product.tags = GetTags(domeggook);
                product.url = $"https://domeme.domeggook.com/s/{code}";
                product.mainImages = GetMainIages(domeggook);
                product.videoUrl = GetVideoUrl(domeggook);
                product.detailPageHtml = GetDetailPageHtml(domeggook);
                product.price = GetPrice(domeggook);
                product.option = GetOption(domeggook);

                ProductHelper.SetDetailOptionPriceToProductPrice(product);

                JToken deliSupply = domeggook["deli"]?["supply"];
                if (deliSupply.HasValues)
                {
                    string? fee = domeggook["deli"]?["supply"]?["fee"]?.ToString();
                    if (string.IsNullOrWhiteSpace(fee) == false)
                    {
                        var deliveryFee = (double)ConvertPrice(fee);
                        product.price[0].salePrice += deliveryFee;
                        product.price[0].originalPrice += deliveryFee;

                        if (product.option != null)
                        {
                            for (int i = 0; i < product.option.detailOptions.Count; i++)
                            {
                                product.option.detailOptions[i].price[0].salePrice += deliveryFee;
                                product.option.detailOptions[i].price[0].originalPrice += deliveryFee;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = exception.Message;
            }
            return product;
        }
        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            //string html = await ChatHubService.GetRequest(connectionId, ApiKeyPageUri);

            string apiKey = BrowserScrapingBridge.Server.Sourcing.Transport.SourcingExecution.Current.GetSetting("domemeApiKey");
            this.ApiKey = apiKey ?? throw new ArgumentException("apiSettings.domemeApiKey가 필요합니다.");
            var product = GetProduct(url, null);
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