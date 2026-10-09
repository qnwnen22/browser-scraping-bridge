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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace BrowserScrapingBridge.Server.Sourcing.Hmall
{
    public class Hmall : ICollect
    {
        public string Site => nameof(Hmall);
        public string Name => "H몰";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        private const string HOST = "https://www.hmall.com";
        private const string IMAGE_HOST = "https://image.hmall.com";

        public Task<string> GetLoginUrlAsync(string connectionId)
        {
            return Task.FromResult<string>(null);
        }

        // =========================
        // Code 추출
        // =========================

        public string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            // https://www.hmall.com/md/pda/itemPtc?slitmCd=2215371292
            var uri = new Uri(url);
            var qs = HttpUtility.ParseQueryString(uri.Query);
            string code = qs["slitmCd"];
            if (!string.IsNullOrWhiteSpace(code)) return code;

            // URL 경로에 숫자 코드가 있는 경우 폴백
            var m = Regex.Match(url, @"(\d{8,})");
            return m.Success ? m.Groups[1].Value : null;
        }

        // =========================
        // 이미지 URL 생성
        // =========================

        /// <summary>
        /// 상품 코드로 이미지 URL 생성
        /// 코드 "2215371292" → /static/2/1/37/15/2215371292_0.jpg
        /// 경로 규칙: /static/{code[7]}/{code[6]}/{code[4..5]}/{code[2..3]}/{code}_{idx}.jpg
        /// </summary>
        private string BuildImageUrl(string code, int idx = 0)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length < 8) return null;
            string dir1 = code[7].ToString();
            string dir2 = code[6].ToString();
            string dir3 = code.Substring(4, 2);
            string dir4 = code.Substring(2, 2);
            return $"{IMAGE_HOST}/static/{dir1}/{dir2}/{dir3}/{dir4}/{code}_{idx}.jpg";
        }

        // =========================
        // __NEXT_DATA__ 파싱
        // =========================

        private static JObject ExtractNextData(string html)
        {
            try
            {
                var m = Regex.Match(html,
                    @"<script\s+id=""__NEXT_DATA__""\s+type=""application/json"">(.+?)</script>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (!m.Success) return null;
                return JObject.Parse(m.Groups[1].Value);
            }
            catch { return null; }
        }

        // =========================
        // 카테고리 수집
        // =========================

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(
            string connectionId, string url, int page)
        {
            JToken items;

            if (page <= 1)
            {
                // 페이지 1: 원본 URL → __NEXT_DATA__ 파싱
                string html = await ChatHubService.GetRequest(connectionId, url);
                if (string.IsNullOrWhiteSpace(html)) return null;

                var nextData = ExtractNextData(html);
                if (nextData == null) return null;

                items = nextData.SelectToken("props.pageProps.res.searchResult.hmallItemSearchResultList");
            }
            else
            {
                // 페이지 2+: 검색 API 호출 (H몰은 AJAX 페이징)
                string apiUrl = await BuildSearchApiUrl(connectionId, url, page);
                if (string.IsNullOrWhiteSpace(apiUrl)) return null;

                string json = await ChatHubService.GetRequest(connectionId, apiUrl);
                if (string.IsNullOrWhiteSpace(json)) return null;

                try
                {
                    var apiResult = JObject.Parse(json);
                    items = apiResult.SelectToken("respData.searchResult.hmallItemSearchResultList");
                }
                catch { return null; }
            }

            if (items == null || !items.Any()) return null;

            var result = new List<CollectPageInfo>();
            foreach (var item in items)
            {
                string code = item["slitmCd"]?.ToString();
                if (string.IsNullOrWhiteSpace(code)) continue;

                string title = item["slitmNm"]?.ToString() ?? "";
                string productUrl = $"{HOST}/md/pda/itemPtc?slitmCd={code}";

                result.Add(new CollectPageInfo
                {
                    Code = code,
                    Url = productUrl,
                    Title = title
                });
            }

            return result.Count > 0 ? result : null;
        }

        // =========================
        // 상품 수집
        // =========================

        public async Task<Product> GetProductAsync(
            string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var product = new Product();
            try
            {
                string code = GetCode(url);
                product.site = this.Site;
                product.code = code;
                product.currency = Currency.KRW.ToString();
                product.url = url;

                var hmallOption = collectOptionSetting?.HmallCollectOption;
                string html = await ChatHubService.GetRequest(connectionId, url);
                if (string.IsNullOrWhiteSpace(html))
                    throw new Exception("H몰 상품 페이지 응답이 없습니다.");

                var nextData = ExtractNextData(html);
                if (nextData == null)
                    throw new Exception("__NEXT_DATA__를 찾을 수 없습니다.");

                // props.pageProps.respData.itemPtc
                var data = nextData.SelectToken("props.pageProps.respData.itemPtc")
                    ?? nextData.SelectToken("props.pageProps.respData");
                if (data == null)
                    throw new Exception("상품 데이터를 찾을 수 없습니다.");

                // 품절 체크
                bool soldout = data["soldout"]?.Value<bool>() ?? false;
                if (soldout)
                    throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품입니다.");

                // 상품명
                product.title = GetTitle(data);

                // 가격 (쿠폰할인/카드즉시할인 옵션 반영)
                double cardDiscountPrice = 0;
                if (hmallOption?.UseCardDiscount == true)
                {
                    double bbprc = data["bbprc"]?.Value<double>() ?? data["sellPrc"]?.Value<double>() ?? 0;
                    cardDiscountPrice = await GetCardDiscountPrice(connectionId, code, bbprc);
                }
                product.price = GetPrice(data, hmallOption, cardDiscountPrice);

                // 이미지
                product.mainImages = GetMainImages(data, code);

                // 옵션
                product.option = await GetOption(data, product.price, connectionId, code);

                // 상세 페이지 HTML
                product.detailPageHtml = GetDetailPageHtml(nextData);

                product.regDate = DateTime.Now;
                product.collectStatus = ProductCollectStatus.Success;
            }
            catch (SaleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = ex.Message;
            }

            return product;
        }

        // =========================
        // 상품 파싱 헬퍼
        // =========================

        private List<Name> GetTitle(JToken data)
        {
            string name = data["slitmNm"]?.ToString();
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("상품명을 찾을 수 없습니다.");
            return ProductHelper.CreateName(Language.KR, name);
        }

        private List<Price> GetPrice(JToken data, HmallCollectOption hmallOption = null, double cardDiscountPrice = 0)
        {
            // sellPrc: 판매가(정가), bbprc: 할인가(쿠폰 적용가)
            double sellPrc = data["sellPrc"]?.Value<double>() ?? 0;
            double bbprc = data["bbprc"]?.Value<double>() ?? 0;

            double originalPrice = sellPrc > 0 ? sellPrc : bbprc;
            double salePrice;

            if (hmallOption?.UseCardDiscount == true && cardDiscountPrice > 0)
            {
                // 카드즉시할인: HTML의 dc-price에서 추출한 가격 사용
                salePrice = cardDiscountPrice;
            }
            else if (hmallOption?.UseCouponDiscount == true && bbprc > 0)
            {
                // 쿠폰할인: bbprc 사용
                salePrice = bbprc;
            }
            else
            {
                // 체크 없음: sellPrc 사용
                salePrice = sellPrc > 0 ? sellPrc : bbprc;
            }

            if (originalPrice <= 0 && salePrice <= 0)
                throw new Exception("상품 가격을 찾을 수 없습니다.");

            if (originalPrice < salePrice)
                originalPrice = salePrice;

            return ProductHelper.CreatePrice(Currency, salePrice, originalPrice, true);
        }

        private List<string> GetMainImages(JToken data, string code)
        {
            var images = new List<string>();

            // 메인 이미지
            string mainImg = BuildImageUrl(code, 0);
            if (!string.IsNullOrWhiteSpace(mainImg))
                images.Add(mainImg);

            // 추가 이미지 (_1 ~ _9)
            for (int i = 1; i <= 9; i++)
            {
                string addImg = BuildImageUrl(code, i);
                if (!string.IsNullOrWhiteSpace(addImg))
                    images.Add(addImg);
            }

            return images.Count > 0 ? images : null;
        }

        private async Task<Option> GetOption(JToken data, List<Price> defaultPrice, string connectionId, string code)
        {
            var stockList = data["stockList"] as JArray;
            if (stockList == null || stockList.Count == 0)
                return CreateDefaultOption(defaultPrice);

            // 옵션이 없는 단일상품 (uitmTotNm == "없음" 또는 optCnt == 0)
            int optCnt = data["optCnt"]?.Value<int>() ?? 0;
            if (optCnt == 0 && stockList.Count == 1)
                return CreateDefaultOption(defaultPrice);

            // 조합형 옵션 (2단 이상): item-stockcount API로 전체 조합 수집
            bool isCombined = data["uitmCombYn"]?.ToString() == "Y";
            if (isCombined)
                return await GetCombinedOption(stockList, defaultPrice, connectionId, code);

            // 단일 옵션 (1단)
            return GetSingleOption(stockList, defaultPrice);
        }

        /// <summary>
        /// 1단 옵션 구성 (__NEXT_DATA__ stockList 사용)
        /// </summary>
        private Option GetSingleOption(JArray stockList, List<Price> defaultPrice)
        {
            var groups = new List<Option.OptionGroup.Group>();
            var details = new List<Option.DetailOption>();
            int idx = 1;

            foreach (var stock in stockList)
            {
                int stockCount = stock["stockCount"]?.Value<int>() ?? 0;
                string sellGbcd = stock["sellGbcd"]?.ToString() ?? "00";
                bool isSoldOut = sellGbcd == "11";

                string optionName = stock["uitmTotNm"]?.ToString()
                    ?? stock["uitmAttrNm"]?.ToString()
                    ?? $"옵션{idx}";

                if (optionName == "없음") optionName = "단일상품";

                double optSellPrc = stock["sellPrc"]?.Value<double>() ?? 0;
                List<Price> optPrice = optSellPrc > 0
                    ? ProductHelper.CreatePrice(Currency, optSellPrc, optSellPrc, true)
                    : defaultPrice;

                string id = idx.ToString();
                groups.Add(new Option.OptionGroup.Group
                {
                    groupId = id,
                    optionName = ProductHelper.CreateName(Language.KR, optionName),
                    quantity = isSoldOut ? 0 : (stockCount > 0 ? stockCount : 9),
                    price = optPrice
                });

                details.Add(new Option.DetailOption
                {
                    optionIds = id,
                    isSoldOut = isSoldOut,
                    optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                    optionName = ProductHelper.CreateName(Language.KR, optionName),
                    quantity = isSoldOut ? 0 : (stockCount > 0 ? stockCount : 9),
                    price = optPrice
                });

                idx++;
            }

            if (details.Count == 0)
                return CreateDefaultOption(defaultPrice);

            return new Option
            {
                optionGroups = new List<Option.OptionGroup>
                {
                    new Option.OptionGroup
                    {
                        optionId = "1",
                        groupName = ProductHelper.CreateName(Language.KR, "옵션"),
                        groups = groups
                    }
                },
                detailOptions = details
            };
        }

        /// <summary>
        /// 2단 조합형 옵션 구성 (item-stockcount API 호출)
        /// 1단(컬러) × 2단(사이즈) 전체 SKU 조합 수집
        /// </summary>
        private async Task<Option> GetCombinedOption(JArray stockList, List<Price> defaultPrice, string connectionId, string code)
        {
            // 1단 옵션별로 item-stockcount API 호출하여 2단 조합 수집
            var allStocks = new List<JToken>();

            for (int i = 0; i < stockList.Count; i++)
            {
                int uitmSeq = i + 1;

                string apiUrl = $"{HOST}/api/hf/dp/v1/item-ptc/item-stockcount?slitmCd={code}" +
                    $"&setItemYn=N&uitmCombYn=Y&uitmAttrTypeSeq=2&selectBoxIdx=1&uitmSeq={uitmSeq}" +
                    $"&rishpNotfExpsYn=Y&befUitmSeq1=0&befUitmSeq2=0&befUitmSeq3=0&setSlitmCd={code}&setSlitmYn=N";

                string json = await ChatHubService.GetRequest(connectionId, apiUrl);
                if (string.IsNullOrWhiteSpace(json)) continue;

                try
                {
                    var result = JObject.Parse(json);
                    var apiStockList = result.SelectToken("respData.stockList") as JArray;
                    if (apiStockList != null)
                        allStocks.AddRange(apiStockList);
                }
                catch { continue; }
            }

            if (allStocks.Count == 0)
                return CreateDefaultOption(defaultPrice);

            // 각 레벨의 고유 값 추출
            var group1Values = allStocks
                .Select(s => s["uitm1AttrNm"]?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s) && s != "없음")
                .Distinct().ToList();
            var group2Values = allStocks
                .Select(s => s["uitm2AttrNm"]?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s) && s != "없음")
                .Distinct().ToList();

            // OptionGroup 구성
            var optionGroups = new List<Option.OptionGroup>
            {
                new Option.OptionGroup
                {
                    optionId = "1",
                    groupName = ProductHelper.CreateName(Language.KR, "옵션1"),
                    groups = group1Values.Select(v => new Option.OptionGroup.Group
                    {
                        groupId = v,
                        optionName = ProductHelper.CreateName(Language.KR, v)
                    }).ToList()
                },
                new Option.OptionGroup
                {
                    optionId = "2",
                    groupName = ProductHelper.CreateName(Language.KR, "옵션2"),
                    groups = group2Values.Select(v => new Option.OptionGroup.Group
                    {
                        groupId = v,
                        optionName = ProductHelper.CreateName(Language.KR, v)
                    }).ToList()
                }
            };

            string optionGroupName = ProductHelper.GetOptionGroupName(optionGroups);

            // DetailOption 구성 (전체 SKU 조합)
            var detailOptions = new List<Option.DetailOption>();
            foreach (var s in allStocks)
            {
                string name1 = s["uitm1AttrNm"]?.ToString() ?? "";
                string name2 = s["uitm2AttrNm"]?.ToString() ?? "";
                string combinedName = $"{name1}ㅡ{name2}";

                int stockCount = s["stockCount"]?.Value<int>() ?? 0;
                string sellGbcd = s["sellGbcd"]?.ToString() ?? "00";
                bool isSoldOut = sellGbcd == "11" || stockCount <= 0;

                double optSellPrc = s["sellPrc"]?.Value<double>() ?? 0;
                var optPrice = optSellPrc > 0
                    ? ProductHelper.CreatePrice(Currency, optSellPrc, optSellPrc, true)
                    : defaultPrice;

                detailOptions.Add(new Option.DetailOption
                {
                    optionIds = combinedName,
                    optionGroupName = ProductHelper.CreateName(Language.KR, optionGroupName),
                    optionName = ProductHelper.CreateName(Language.KR, combinedName),
                    isSoldOut = isSoldOut,
                    quantity = isSoldOut ? 0 : (stockCount > 0 ? stockCount : 9),
                    price = optPrice
                });
            }

            return new Option
            {
                optionGroups = optionGroups,
                detailOptions = detailOptions
            };
        }

        private Option CreateDefaultOption(List<Price> prices)
        {
            return new Option
            {
                optionGroups = new List<Option.OptionGroup>
                {
                    new Option.OptionGroup
                    {
                        optionId = "1",
                        groupName = ProductHelper.CreateName(Language.KR, "옵션"),
                        groups = new List<Option.OptionGroup.Group>
                        {
                            new Option.OptionGroup.Group
                            {
                                groupId = "1",
                                optionName = ProductHelper.CreateName(Language.KR, "단일상품"),
                                quantity = 9,
                                price = prices
                            }
                        }
                    }
                },
                detailOptions = new List<Option.DetailOption>
                {
                    new Option.DetailOption
                    {
                        optionIds = "1",
                        isSoldOut = false,
                        optionGroupName = ProductHelper.CreateName(Language.KR, "옵션"),
                        optionName = ProductHelper.CreateName(Language.KR, "단일상품"),
                        quantity = 9,
                        price = prices
                    }
                }
            };
        }

        private string GetDetailPageHtml(JObject nextData)
        {
            // __NEXT_DATA__ 내 상세 HTML 필드 탐색
            var detailHtml = nextData.SelectToken("props.pageProps.respData.itemDtlDesc")
                ?? nextData.SelectToken("props.pageProps.respData.dtlDescHtml")
                ?? nextData.SelectToken("props.pageProps.respData.itemPtc.dtlDescHtml");
            return detailHtml?.ToString() ?? "";
        }

        /// <summary>
        /// 카드즉시할인 가격 조회 (item-prmo-lst API)
        /// crdImdtDcPrmoList[0].imdtCardDcAmt = 카드즉시할인 적용 가격
        /// </summary>
        private async Task<double> GetCardDiscountPrice(string connectionId, string code, double bbprc)
        {
            string apiUrl = $"{HOST}/api/hf/dp/v1/item-ptc/item-prmo-lst?slitmCd={code}&intgItemGbcd=null&optFormItemYn=N&bbprc={bbprc}&referCode=800";
            string json = await ChatHubService.GetRequest(connectionId, apiUrl);
            if (string.IsNullOrWhiteSpace(json)) return 0;

            try
            {
                var result = JObject.Parse(json);
                var prmoList = result.SelectToken("respData.data.crdImdtDcPrmoList") as JArray;
                if (prmoList == null || prmoList.Count == 0) return 0;

                return prmoList[0]["imdtCardDcAmt"]?.Value<double>() ?? 0;
            }
            catch { return 0; }
        }

        // =========================
        // 페이징 API
        // =========================

        private const string SEARCH_API = "/md/api/cache";

        /// <summary>
        /// H몰 검색 API URL 생성 (페이지 2+ 용)
        /// 검색 URL: searchTerm을 URL에서 직접 추출
        /// 카테고리 URL: __NEXT_DATA__에서 sectInfFilter 추출
        /// </summary>
        private async Task<string> BuildSearchApiUrl(string connectionId, string url, int page)
        {
            var uri = new Uri(url);
            var qs = HttpUtility.ParseQueryString(uri.Query);

            // 검색 URL인 경우: searchTerm 직접 추출 (원본 페이지 재요청 불필요)
            string searchTerm = qs["searchTerm"];
            if (!string.IsNullOrWhiteSpace(searchTerm))
                return BuildApiUrl(page, searchTerm: searchTerm);

            // 카테고리 URL인 경우: 원본 페이지에서 sectInfFilter 추출
            string html = await ChatHubService.GetRequest(connectionId, url);
            if (string.IsNullOrWhiteSpace(html)) return null;

            var nextData = ExtractNextData(html);
            string sectInfFilter = nextData?.SelectToken("props.pageProps.res.searchResult.sectInfFilter")?.ToString();
            if (!string.IsNullOrWhiteSpace(sectInfFilter))
                return BuildApiUrl(page, sectInfFilter: sectInfFilter);

            return null;
        }

        private string BuildApiUrl(int page, string searchTerm = null, string sectInfFilter = null)
        {
            var apiParams = HttpUtility.ParseQueryString(string.Empty);
            apiParams["url"] = "/api/hf/dp/v1/search/search";
            apiParams["page"] = page.ToString();
            apiParams["listSize"] = "30";
            apiParams["scrollPaging"] = "Y";
            apiParams["ajaxChk"] = "Y";
            apiParams["deviceInfo"] = "pc";

            if (!string.IsNullOrWhiteSpace(searchTerm))
                apiParams["searchTerm"] = searchTerm;
            if (!string.IsNullOrWhiteSpace(sectInfFilter))
                apiParams["sectInfFilter"] = sectInfFilter;

            return $"{HOST}{SEARCH_API}?{apiParams}";
        }

        public Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            throw new NotImplementedException();
        }

        public Product GetProduct(CollectRawData rawData)
        {
            throw new NotImplementedException();
        }

        public void ApplyCollectOptionSetting(CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            throw new NotImplementedException();
        }

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            throw new NotImplementedException();
        }
    }
}
