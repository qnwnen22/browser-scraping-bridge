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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Daiso
{
    public class Daiso : ICollect
    {
        public string Site => nameof(Daiso);
        public string Name => "다이소";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }

        private string GetImageUrl(string url)
        {
            var indexOf = url.IndexOf("/file");
            if (indexOf == 0)
            {
                url = "https://cdn.daisomall.co.kr" + url;
            }
            return url.GetUriString();
        }

        public string GetCode(string url)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            string pdNo = uriBuilderAdvance.GetQuery("pdNo");
            return pdNo;
        }

        private T TryGet<T>(List<T> items, int index)
        {
            if (items.Count <= index) return default;
            return items[index];
        }

        private string GetSearchApiUrl(string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            var searchTerm = uriBuilderAdvance.GetQuery("searchTerm");
            var stringBuilder = new StringBuilder();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                List<string> segments = uriBuilderAdvance.Segments;
                for (int i = 0; i < segments.Count; i++)
                {
                    segments[i] = segments[i].Replace("/", "");
                }
                string largeExhCtgrNo = TryGet(segments, 4);
                if (string.IsNullOrWhiteSpace(largeExhCtgrNo))
                {
                    string type = TryGet(segments, 2);
                    switch (type)
                    {
                        case "new":
                            stringBuilder.Append("https://www.daisomall.co.kr/ssn/search/SearchGoods?searchTerm=&");
                            stringBuilder.Append("searchQuery=R27&");
                            stringBuilder.Append($"pageNum={page}&");
                            stringBuilder.Append("brndCd=&");
                            stringBuilder.Append("cntPerPage=20&");
                            stringBuilder.Append("userId=&");
                            stringBuilder.Append("newPdYn=Y&");
                            stringBuilder.Append("massOrPsblYn=&");
                            stringBuilder.Append("fdrmOrPsblYn=&");
                            stringBuilder.Append("quickOrPsblYn=&");
                            stringBuilder.Append("searchSort=new");
                            break;

                        case "rank":
                            string type2 = TryGet(segments, 3);
                            switch (type2)
                            {
                                case "C105":
                                    stringBuilder.Append("https://www.daisomall.co.kr/ssn/search/GoodsBestSale?searchTerm=&");
                                    stringBuilder.Append("period=R&");
                                    stringBuilder.Append($"pageNum={page}&");
                                    stringBuilder.Append("cntPerPage=19&");
                                    stringBuilder.Append("largeExhCtgrNo=&");
                                    stringBuilder.Append("soldOutYn=N");
                                    break;
                                case "pickup":
                                    stringBuilder.Append("https://www.daisomall.co.kr/ssn/search/GoodsBestPkup?searchTerm=&");
                                    stringBuilder.Append($"pageNum={page}&");
                                    stringBuilder.Append("cntPerPage=19&");
                                    stringBuilder.Append("largeExhCtgrNo=");
                                    break;
                                case "boxProduct":
                                    stringBuilder.Append("https://www.daisomall.co.kr/ssn/search/GoodsBestBox?searchTerm=&");
                                    stringBuilder.Append($"pageNum={page}&");
                                    stringBuilder.Append("cntPerPage=20&");
                                    stringBuilder.Append("largeExhCtgrNo=");
                                    break;
                                default:
                                    throw new Exception("추가가 필요한 타입입니다.");
                            }
                            break;
                        case "quickOrder":
                            stringBuilder.Append("https://www.daisomall.co.kr/ssn/search/SearchGoods?searchTerm=&");
                            stringBuilder.Append("searchQuery=R27&");
                            stringBuilder.Append($"pageNum={page}&");
                            stringBuilder.Append("brndCd=&");
                            stringBuilder.Append("cntPerPage=20&");
                            stringBuilder.Append("userId=&");
                            stringBuilder.Append("newPdYn=&");
                            stringBuilder.Append("massOrPsblYn=&");
                            stringBuilder.Append("fdrmOrPsblYn=&");
                            stringBuilder.Append("quickOrPsblYn=Y&");
                            stringBuilder.Append("searchSort=");
                            break;
                        case "bulkOrder":
                            stringBuilder.Append("https://www.daisomall.co.kr/ssn/search/SearchGoods?searchTerm=&");
                            stringBuilder.Append("searchQuery=Q1&");
                            stringBuilder.Append($"pageNum={page}&");
                            stringBuilder.Append("brndCd=&");
                            stringBuilder.Append("cntPerPage=20&");
                            stringBuilder.Append("userId=&");
                            stringBuilder.Append("newPdYn=&");
                            stringBuilder.Append("massOrPsblYn=Y&");
                            stringBuilder.Append("fdrmOrPsblYn=&");
                            stringBuilder.Append("quickOrPsblYn=&");
                            stringBuilder.Append("searchSort=");
                            break;
                        default:
                            throw new Exception("추가가 필요한 타입입니다.");
                    }
                }
                else
                {
                    string exhCtgrNo = segments.Last();
                    stringBuilder.Append($"https://www.daisomall.co.kr/ssn/search/SearchGoods?");
                    stringBuilder.Append($"searchTerm=&");
                    stringBuilder.Append($"searchQuery=Q1&");
                    stringBuilder.Append($"pageNum={page}&");
                    stringBuilder.Append($"brndCd=&");
                    stringBuilder.Append($"cntPerPage=20&");
                    stringBuilder.Append($"userId=&");
                    stringBuilder.Append($"newPdYn=&");
                    stringBuilder.Append($"massOrPsblYn=&");
                    stringBuilder.Append($"fdrmOrPsblYn=&");
                    stringBuilder.Append($"quickOrPsblYn=&");
                    stringBuilder.Append($"searchSort=&");
                    stringBuilder.Append($"exhCtgrNo={exhCtgrNo}&");
                    stringBuilder.Append($"largeExhCtgrNo={largeExhCtgrNo}");
                }
            }
            else
            {
                stringBuilder.Append($"https://www.daisomall.co.kr/ssn/search/SearchGoods?");
                stringBuilder.Append($"searchTerm={searchTerm}&");
                stringBuilder.Append($"searchQuery=Q1&");
                stringBuilder.Append($"pageNum={page}&");
                stringBuilder.Append($"brndCd=&");
                stringBuilder.Append($"cntPerPage=19&");
                stringBuilder.Append($"userId=&");
                stringBuilder.Append($"newPdYn=&");
                stringBuilder.Append($"massOrPsblYn=&");
                stringBuilder.Append($"fdrmOrPsblYn=&");
                stringBuilder.Append($"quickOrPsblYn=&");
                stringBuilder.Append($"searchSort=");
            }
            var requestUrl = stringBuilder.ToString();
            return requestUrl;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionId">HTML 수집을 수행할 브라우저의 SignalR 연결 ID</param>
        /// <param name="url"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        /// <example>https://www.daisomall.co.kr/ds/exhCtgr/C208/CTGR_00002/CTGR_00030/CTGR_00146</example>
        /// <example>https://www.daisomall.co.kr/ds/exhCtgr/C208/CTGR_00002/CTGR_00030</example>
        /// <example>https://www.daisomall.co.kr/ds/dst/SCR_DST_0015?searchTerm=장난감</example>
        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            string requestUrl = GetSearchApiUrl(url, page);
            string Json = await ChatHubService.GetRequest(connectionId, requestUrl);
            JObject data = Json.ToJObject();
            if (data == null || data.HasValues == false) return null;

            var result = new List<CollectPageInfo>();
            List<JToken> resultSets = data["resultSet"]?["result"].ToList();

            List<JToken> resultDocuments = null;
            foreach (var resultSet in resultSets)
            {
                JToken resultDocument = resultSet["resultDocuments"];
                if (resultDocument != null)
                {
                    resultDocuments = resultDocument.ToList();
                    break;
                }
            }
            if (resultDocuments == null || resultDocuments.Count <= 0) return null;
            foreach (JToken resultDocument in resultDocuments)
            {
                string pdNo = resultDocument.Value<string>("pdNo");
                if (string.IsNullOrWhiteSpace(pdNo)) continue;
                string itemUrl = $"https://www.daisomall.co.kr/pd/pdr/SCR_PDR_0001?pdNo={pdNo}";
                string pdNm = resultDocument.Value<string>("pdNm");

                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = pdNm;
                collectPageInfo.Url = itemUrl;
                collectPageInfo.Title = pdNm;
                result.Add(collectPageInfo);
            }
            return result;
        }

        private async Task<string> Post(string url, string body)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36");
            HttpContent httpContent = new StringContent(body, Encoding.UTF8, "application/json");
            using (HttpClient httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient())
            {
                httpClient.SetHeader(headers);
                HttpResponseMessage response = await httpClient.PostAsync(url, httpContent);
                string result = await response.Content.ReadAsStringAsync();
                return result;
            }
        }
        public List<Name> GetTitle(JObject data)
        {
            string? exhPdNm = data["selPdDtlInfo"]?["data"]?.Value<string>("exhPdNm");
            if (string.IsNullOrWhiteSpace(exhPdNm)) throw new Exception("상품명을 찾을 수 없습니다.");
            List<Name> result = new List<Name>() { new Name(Language.KR, exhPdNm) };
            return result;
        }

        public List<Name> GetBrand()
        {
            return null;
        }

        public List<Desc> GetDescs(JObject data)
        {
            List<JToken> datas = data["selPdDtlNtfc"]?["data"]?.ToList();
            if (datas == null || datas.Count <= 0) throw new Exception("상품정보 제공 고시를 찾을 수 없습니다.");
            List<Desc> result = new List<Desc>();
            for (int i = 0; i < datas.Count; i++)
            {
                var item = datas[i];
                string ntfcIemNm = item.Value<string>("ntfcIemNm");
                string ntfcIemCn = item.Value<string>("ntfcIemCn");
                Desc desc = new Desc((i + 1), Language.KR, ntfcIemNm, ntfcIemCn);
                result.Add(desc);
            }
            return result;
        }

        public List<Tag> GetTags()
        {
            return null;
        }

        public List<Price> GetPrice(JObject data)
        {
            double? pdPrc = data["selPdDtlInfo"]?["data"].Value<double>("pdPrc");
            if (pdPrc == null || pdPrc <= 0) throw new Exception("가격 정보를 찾을 수 없습니다.");
            List<Price> result = new List<Price>()
            {
                new Price(Currency.KRW, (double)pdPrc, (double)pdPrc, true),
                // ※다이소는 배송비 포함 안함
                // new Price(Currency.Delivery, 3000d, 3000d, true) // 배송비 3000춴 고정
            };
            return result;
        }

        public List<string> GetMainImages(JObject data)
        {
            List<JToken> ctts = data["selPdDtlInfo"]?["data"]?["ctts"]?.ToList();
            if (ctts == null || ctts.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            List<string> result = new List<string>();
            foreach (JToken ctt in ctts)
            {
                var imgUrl = ctt.Value<string>("imgUrl");
                string getImageUrl = GetImageUrl(imgUrl);
                if (string.IsNullOrWhiteSpace(getImageUrl) == false)
                {
                    result.Add(getImageUrl);
                }
            }
            if (result.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            return result;
        }

        public string GetVideoUrl()
        {
            return null;
        }

        public string GetDetailPageHtml(JObject data)
        {
            string pdDtlDc = data["selPdDtlDesc"]?["data"]?["pdDtlDesc"]?.Value<string>("pdDtlDc");
            if (string.IsNullOrWhiteSpace(pdDtlDc)) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            pdDtlDc = WebUtility.HtmlDecode(pdDtlDc);
            return pdDtlDc;
        }



        public Option GetOption(JObject data)
        {
            List<JToken> onldOpts = data["selPdDtlInfo"]?["data"]?["onldOpt"]?.ToList();
            if (onldOpts == null || onldOpts.Count <= 0) return null;

            var option = new Option();
            option.optionGroups = new List<Option.OptionGroup>();
            var optionGroup = new Option.OptionGroup();
            optionGroup.optionId = "0";
            var groupName = new List<Name> { new Name(Language.KR, "옵션을 선택해주세요.") };
            optionGroup.groupName = groupName;
            optionGroup.groups = new List<Option.OptionGroup.Group>();

            option.detailOptions = new List<Option.DetailOption>();
            foreach (var onldOpt in onldOpts)
            {
                var group = new Option.OptionGroup.Group();
                string onldPdNo = onldOpt.Value<string>("onldPdNo");
                if (string.IsNullOrWhiteSpace(onldPdNo)) throw new Exception("옵션 수집 중 문제가 발생했습니다.");
                string optCn = onldOpt.Value<string>("optCn");
                if (string.IsNullOrWhiteSpace(optCn)) throw new Exception("옵션 수집 중 문제가 발생했습니다.");
                var optionName = new List<Name> { new Name(Language.KR, optCn) };
                string imgUrl = onldOpt.Value<string>("imgUrl");
                imgUrl = GetImageUrl(imgUrl);
                int? stckQy = onldOpt.Value<int?>("stckQy");
                double? pdPrc = onldOpt.Value<double?>("pdPrc");
                if (pdPrc == null || pdPrc <= 0) throw new Exception("옵션 수집 중 문제가 발생했습니다.");

                group.groupId = onldPdNo;
                group.optionName = optionName;
                group.optionImage = imgUrl;
                optionGroup.groups.Add(group);

                var detailOption = new Option.DetailOption();
                detailOption.optionIds = onldPdNo;
                detailOption.optionGroupName = groupName;
                detailOption.optionName = optionName;
                detailOption.price = new List<Price> { new Price(Currency.KRW, (double)pdPrc, (double)pdPrc) };
                if (stckQy == null || stckQy <= 0)
                {
                    detailOption.isSoldOut = true;
                    detailOption.quantity = 0;
                }
                else
                {
                    detailOption.isSoldOut = false;
                    detailOption.quantity = (int)stckQy;
                }
                option.detailOptions.Add(detailOption);
            }
            option.optionGroups.Add(optionGroup);
            return option;
        }
        public Product GetProduct(string code, JObject data)
        {
            var product = new Product();
            try
            {
                product.site = this.Site;
                product.code = code;
                product.url = $"https://www.daisomall.co.kr/pd/pdr/SCR_PDR_0001?pdNo={code}";
                product.currency = Currency.KRW.ToString();

                product.title = GetTitle(data);
                product.brand = GetBrand();
                product.descs = GetDescs(data);
                product.tags = GetTags();
                product.price = GetPrice(data);
                product.mainImages = GetMainImages(data);
                product.videoUrl = GetVideoUrl();
                product.detailPageHtml = GetDetailPageHtml(data);
                product.option = GetOption(data);
                List<Option.DetailOption> soldOutOptions = product.option.detailOptions.FindAll(x => x.isSoldOut == true);
                if (soldOutOptions.Count >= product.option.detailOptions.Count)
                {
                    throw new Exception("재고가 없는 상품입니다.");
                }
                product.regDate = DateTime.Now;
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
            string code = GetCode(url);
            string body = $"{{\"pdNo\":\"{code}\"}}";
            string selPdDtlInfo = await Post("https://www.daisomall.co.kr/api/pd/pdr/pdDtl/selPdDtlInfo", body);
            string selPdDtlDesc = await Post("https://www.daisomall.co.kr/api/pd/pdr/pdDtl/selPdDtlDesc", body);
            string selPdDtlNtfc = await Post("https://www.daisomall.co.kr/api/pd/pdr/pdDtl/selPdDtlNtfc", body);

            var data = new JObject();
            data["selPdDtlInfo"] = selPdDtlInfo.ToJObject();
            data["selPdDtlDesc"] = selPdDtlDesc.ToJObject();
            data["selPdDtlNtfc"] = selPdDtlNtfc.ToJObject();
            return GetProduct(code, data);
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
