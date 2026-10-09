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

namespace BrowserScrapingBridge.Server.Sourcing.Halfclub
{
    public class Halfclub : ICollect
    {
        public string Site => nameof(Halfclub);
        public string Name => "하프클럽";
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }

        private const string API_BASE = "https://hapix.halfclub.com";
        private const string CDN_BASE = "https://cdn2.halfclub.com/";
        private const string SITE_BASE = "https://www.halfclub.com";

        private Dictionary<string, string> GetApiHeaders()
        {
            return new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Origin", SITE_BASE },
                { "Referer", SITE_BASE + "/" }
            };
        }

        public string GetCode(string url)
        {
            // https://www.halfclub.com/product/405297248
            Uri uri = new Uri(url);
            string[] segments = uri.AbsolutePath.TrimEnd('/').Split('/');
            return segments.Last();
        }

        public Task<string> GetLoginUrlAsync(string connectionId)
        {
            return Task.FromResult<string>(null);
        }

        private string BuildImageUrl(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return null;
            if (relativePath.StartsWith("http")) return relativePath;
            return CDN_BASE + relativePath;
        }

        #region 카테고리 수집

        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            // 카테고리: https://www.halfclub.com/categories/32618
            // 검색: https://www.halfclub.com/search/더블플래그?gndCd=02&dpCtgrNo1=111&dpCtgrNo2=111001&dpCtgrNo3=111001002
            Uri uri = new Uri(url);
            string path = uri.AbsolutePath.TrimEnd('/');

            int pageSize = 40;
            int offset = (page - 1) * pageSize;
            string apiUrl;

            if (path.StartsWith("/search/"))
            {
                // /search/{keyword} → keyword 추출 + 쿼리파라미터 전달
                string keyword = Uri.UnescapeDataString(path.Split('/').Last());
                apiUrl = $"{API_BASE}/searches/prdList/?limit={offset},{pageSize}&sortSeq=12&siteCd=1&device=pc&keyword={Uri.EscapeDataString(keyword)}";
                if (!string.IsNullOrEmpty(uri.Query))
                    apiUrl += "&" + uri.Query.TrimStart('?');
            }
            else
            {
                // /categories/{catNo}
                string catNo = path.Split('/').Last();
                apiUrl = $"{API_BASE}/searches/prdList/?limit={offset},{pageSize}&sortSeq=12&siteCd=1&device=pc&dpCtgrNo2={catNo}";
            }

            var headers = GetApiHeaders();
            string json = await ChatHubService.GetRequest(connectionId, apiUrl, headers);
            if (string.IsNullOrWhiteSpace(json)) return null;

            JObject data = json.ToJObject();
            var hits = data?["data"]?["result"]?["hits"]?["hits"];
            if (hits == null || !hits.Any()) return null;

            var result = new List<CollectPageInfo>();
            foreach (JToken hit in hits)
            {
                var source = hit["_source"];
                if (source == null) continue;

                string prdNo = source["prdNo"]?.ToString();
                string prdNm = source.Value<string>("prdNm");
                if (string.IsNullOrWhiteSpace(prdNo)) continue;

                result.Add(new CollectPageInfo
                {
                    Code = prdNo,
                    Url = $"{SITE_BASE}/product/{prdNo}",
                    Title = prdNm
                });

                CollectExtension.CollectSleep();
            }

            return result;
        }

        #endregion

        #region 상품 수집

        private async Task<JToken> GetProductSource(string connectionId, string code)
        {
            string apiUrl = $"{API_BASE}/searches/prdList/?limit=0,1&sortSeq=12&siteCd=1&device=pc&keyword={code}";
            var headers = GetApiHeaders();
            string json = await ChatHubService.GetRequest(connectionId, apiUrl, headers);
            if (string.IsNullOrWhiteSpace(json)) return null;

            JObject data = json.ToJObject();
            var hits = data?["data"]?["result"]?["hits"]?["hits"];
            if (hits == null || !hits.Any()) return null;

            return hits.First()?["_source"];
        }

        private List<Name> GetTitle(JToken source)
        {
            string prdNm = source.Value<string>("prdNm");
            if (string.IsNullOrWhiteSpace(prdNm)) throw new Exception("상품명을 찾을 수 없습니다.");
            return new List<Name> { new Name(Language.KR, prdNm) };
        }

        private List<Name> GetBrand(JToken source)
        {
            string brandNm = source.Value<string>("brandNm");
            if (string.IsNullOrWhiteSpace(brandNm)) return null;
            return new List<Name> { new Name(Language.KR, brandNm) };
        }

        private List<Price> GetPrice(JToken source)
        {
            double? normPrc = source.Value<double?>("normPrc");
            double? dcPrcPc = source.Value<double?>("dcPrcPc");
            double? selPrc = source.Value<double?>("selPrc");

            double? salePrice = dcPrcPc ?? selPrc ?? normPrc;

            if (salePrice == null || salePrice <= 0)
                throw new Exception("상품 가격을 찾을 수 없습니다.");

            return new List<Price> { new Price(Currency.KRW, (double)salePrice, (double)salePrice, true) };
        }

        private List<string> GetMainImages(JToken source)
        {
            var images = new List<string>();

            string prdImg = source.Value<string>("prdImg");
            string imgUrl = BuildImageUrl(prdImg);
            if (!string.IsNullOrWhiteSpace(imgUrl)) images.Add(imgUrl);

            string prdAddImg1 = source.Value<string>("prdAddImg1");
            string addImgUrl = BuildImageUrl(prdAddImg1);
            if (!string.IsNullOrWhiteSpace(addImgUrl)) images.Add(addImgUrl);

            return images.Count > 0 ? images : null;
        }

        /// <summary>
        /// "M (66, 95)" → "M (95)" : 괄호 안 첫 번째 숫자(한국 호수) 제거
        /// </summary>
        private string CleanSizeName(string size)
        {
            if (string.IsNullOrWhiteSpace(size)) return size;
            var match = Regex.Match(size, @"^(.+?)\s*\(\d+,\s*(\d+)\)$");
            if (match.Success)
                return $"{match.Groups[1].Value} ({match.Groups[2].Value})";
            return size;
        }

        private List<Desc> GetDescs(JToken source)
        {
            var descs = new List<Desc>();
            int order = 1;

            string dpCtgrNm1 = source.Value<string>("dpCtgrNm1");
            string dpCtgrNm2 = source.Value<string>("dpCtgrNm2");
            string dpCtgrNm3 = source.Value<string>("dpCtgrNm3");
            string category = string.Join(" > ", new[] { dpCtgrNm1, dpCtgrNm2, dpCtgrNm3 }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (!string.IsNullOrWhiteSpace(category))
                descs.Add(new Desc(order++, Language.KR, "카테고리", category));

            string brandNm = source.Value<string>("brandNm");
            if (!string.IsNullOrWhiteSpace(brandNm))
                descs.Add(new Desc(order++, Language.KR, "브랜드", brandNm));

            string selAcntNm = source.Value<string>("selAcntNm");
            if (!string.IsNullOrWhiteSpace(selAcntNm))
                descs.Add(new Desc(order++, Language.KR, "판매자", selAcntNm));

            string dlvFreeYn = source.Value<string>("dlvFreeYn");
            if (dlvFreeYn == "Y")
                descs.Add(new Desc(order++, Language.KR, "배송", "무료배송"));

            return descs.Count > 0 ? descs : null;
        }

        #endregion

        #region __NUXT_DATA__ 파싱

        /// <summary>
        /// HTML에서 __NUXT_DATA__ 스크립트 태그 내용을 추출하여 JArray로 파싱
        /// Nuxt 3 devalue 형식: 값들이 flat array에 저장되고, 객체의 속성값은 인덱스 참조
        /// </summary>
        private JArray ExtractNuxtData(string html)
        {
            // Nuxt 3 형식: <script id="__NUXT_DATA__" data-ssr="true">[...]</script>
            int nuxtIdx = html.IndexOf("__NUXT_DATA__");
            if (nuxtIdx < 0) return null;

            // __NUXT_DATA__ 이후 첫 번째 '>' 찾기 (script 태그 닫는 부분)
            int tagClose = html.IndexOf('>', nuxtIdx);
            if (tagClose < 0) return null;

            // '</script>' 찾기
            int scriptEnd = html.IndexOf("</script>", tagClose);
            if (scriptEnd < 0) return null;

            string jsonStr = html.Substring(tagClose + 1, scriptEnd - tagClose - 1).Trim();
            if (string.IsNullOrWhiteSpace(jsonStr)) return null;

            try { return JArray.Parse(jsonStr); }
            catch { return null; }
        }

        /// <summary>
        /// devalue 인덱스 참조를 따라가서 실제 값(JToken)을 반환
        /// </summary>
        private JToken NuxtRef(JArray data, JToken refToken)
        {
            if (refToken == null || refToken.Type != JTokenType.Integer) return null;
            int idx = refToken.Value<int>();
            if (idx < 0 || idx >= data.Count) return null;
            return data[idx];
        }

        /// <summary>
        /// devalue 인덱스 참조를 따라가서 문자열 값을 반환
        /// </summary>
        private string NuxtString(JArray data, JToken refToken)
        {
            var val = NuxtRef(data, refToken);
            if (val == null || val.Type == JTokenType.Null) return null;
            return val.ToString();
        }

        /// <summary>
        /// devalue 인덱스 참조를 따라가서 숫자 값을 반환 (null이면 0)
        /// </summary>
        private double NuxtDouble(JArray data, JToken refToken)
        {
            var val = NuxtRef(data, refToken);
            if (val == null || val.Type == JTokenType.Null) return 0;
            try { return val.Value<double>(); }
            catch { return 0; }
        }

        /// <summary>
        /// __NUXT_DATA__ 배열에서 상품 데이터 객체(prdNo + prdNm 키를 가진 원시 JObject) 찾기
        /// </summary>
        private JObject FindNuxtProductObject(JArray nuxtData, string code)
        {
            for (int i = 0; i < nuxtData.Count; i++)
            {
                if (nuxtData[i] is JObject obj && obj.ContainsKey("prdNo") && obj.ContainsKey("prdNm"))
                {
                    string prdNo = NuxtString(nuxtData, obj["prdNo"]);
                    if (prdNo == code) return obj;
                }
            }
            return null;
        }

        /// <summary>
        /// __NUXT_DATA__의 productImage에서 전체 이미지 URL 추출 (basic + add1~add9)
        /// </summary>
        private List<string> GetMainImagesFromNuxt(JArray nuxtData, JObject productObj)
        {
            if (!productObj.ContainsKey("productImage")) return null;

            var imgObj = NuxtRef(nuxtData, productObj["productImage"]) as JObject;
            if (imgObj == null) return null;

            string basePath = NuxtString(nuxtData, imgObj["basicExtUrl"]) ?? "";
            string baseUrl = CDN_BASE + basePath;
            var images = new List<string>();

            string basicNm = NuxtString(nuxtData, imgObj["basicExtNm"]);
            if (!string.IsNullOrWhiteSpace(basicNm))
                images.Add(baseUrl + basicNm);

            for (int i = 1; i <= 9; i++)
            {
                string key = $"add{i}ExtNm";
                if (imgObj.ContainsKey(key))
                {
                    string addNm = NuxtString(nuxtData, imgObj[key]);
                    if (!string.IsNullOrWhiteSpace(addNm))
                        images.Add(baseUrl + addNm);
                }
            }

            return images.Count > 0 ? images : null;
        }

        /// <summary>
        /// __NUXT_DATA__의 prdDescContClob 필드에서 상세페이지 HTML 추출
        /// </summary>
        private string GetDetailPageHtmlFromNuxt(JArray nuxtData, JObject productObj)
        {
            // productObj → productDesc(sub-object) → prdDescContClob(HTML 문자열)
            if (!productObj.ContainsKey("productDesc")) return null;
            var descObj = NuxtRef(nuxtData, productObj["productDesc"]) as JObject;
            if (descObj == null || !descObj.ContainsKey("prdDescContClob")) return null;
            string html = NuxtString(nuxtData, descObj["prdDescContClob"]);
            if (string.IsNullOrWhiteSpace(html)) return null;
            return HtmlReader.GetDafaultHtml(html);
        }

        /// <summary>
        /// __NUXT_DATA__의 optionItem 구조에서 전체 옵션 파싱
        /// 구조: optionItem → [그룹1(색상), 그룹2(사이즈)] 각 그룹의 optValueList에 56개 조합
        /// 동일 stockNo로 그룹 간 매칭하여 "색상 / 사이즈" 조합 생성, stock.addPrc로 추가가격 적용
        /// </summary>
        private Option GetOptionFromNuxt(JArray nuxtData, JObject productObj, double basePrice)
        {
            if (!productObj.ContainsKey("optionItem")) return null;

            var optionItemArr = NuxtRef(nuxtData, productObj["optionItem"]) as JArray;
            if (optionItemArr == null || optionItemArr.Count == 0) return null;

            // 각 옵션 그룹(색상, 사이즈 등)별로 stockNo → 옵션명 매핑 수집
            var groupDimensions = new List<Dictionary<string, string>>();
            var groupNames = new List<string>();
            var addPrcMap = new Dictionary<string, double>();
            var stockQuantityMap = new Dictionary<string, int>(); // stockNo → 재고 수량 매핑
            var stockNoOrder = new List<string>();

            foreach (JToken groupRef in optionItemArr)
            {
                if (groupRef.Type != JTokenType.Integer) continue;
                var groupObj = nuxtData[groupRef.Value<int>()] as JObject;
                if (groupObj == null) continue;

                string optItemNm = NuxtString(nuxtData, groupObj["optItemNm"]);
                groupNames.Add(optItemNm ?? "옵션");

                var nameMap = new Dictionary<string, string>();

                if (!groupObj.ContainsKey("optValueList"))
                {
                    groupDimensions.Add(nameMap);
                    continue;
                }

                var optValueArr = NuxtRef(nuxtData, groupObj["optValueList"]) as JArray;
                if (optValueArr == null)
                {
                    groupDimensions.Add(nameMap);
                    continue;
                }

                foreach (JToken valRef in optValueArr)
                {
                    if (valRef.Type != JTokenType.Integer) continue;
                    var valObj = nuxtData[valRef.Value<int>()] as JObject;
                    if (valObj == null) continue;

                    string optValueNm = NuxtString(nuxtData, valObj["optValueNm"]);
                    string stockNo = NuxtRef(nuxtData, valObj["stockNo"])?.ToString();

                    if (string.IsNullOrWhiteSpace(stockNo) || string.IsNullOrWhiteSpace(optValueNm)) continue;

                    nameMap[stockNo] = CleanSizeName(optValueNm);

                    // 첫 번째 그룹에서 stockNo 순서, addPrc, 재고 수량 수집 (중복 방지)
                    if (!addPrcMap.ContainsKey(stockNo))
                    {
                        stockNoOrder.Add(stockNo);

                        double addPrc = 0;
                        int stockQuantity = 0;
                        if (valObj.ContainsKey("stock"))
                        {
                            var stockObj = NuxtRef(nuxtData, valObj["stock"]) as JObject;
                            if (stockObj != null)
                            {
                                if (stockObj.ContainsKey("addPrc"))
                                {
                                    addPrc = NuxtDouble(nuxtData, stockObj["addPrc"]);
                                }
                                // stockObj에서 realStckQty 필드를 찾아 재고 수량 가져오기
                                if (stockObj.ContainsKey("realStckQty"))
                                {
                                    var realStckQtyToken = stockObj["realStckQty"];
                                    // realStckQty가 이미 실제 값인지 인덱스 참조인지 확인
                                    if (realStckQtyToken.Type == JTokenType.Integer)
                                    {
                                        // 직접 값인 경우 (이미 해석된 객체)
                                        stockQuantity = realStckQtyToken.Value<int>();
                                    }
                                    else
                                    {
                                        // 인덱스 참조인 경우
                                        stockQuantity = (int)NuxtDouble(nuxtData, realStckQtyToken);
                                    }
                                }
                            }
                        }
                        addPrcMap[stockNo] = addPrc;
                        stockQuantityMap[stockNo] = stockQuantity;
                    }
                }

                groupDimensions.Add(nameMap);
            }

            if (groupDimensions.Count == 0 || stockNoOrder.Count == 0) return null;

            // 옵션 그룹명 조합
            string groupNameStr = string.Join(" / ", groupNames);
            Name groupName = new Name(Language.KR, groupNameStr);

            var optionGroups = new List<Option.OptionGroup>();
            var optionGroup = new Option.OptionGroup();
            optionGroup.optionId = "0";
            optionGroup.groupName = new List<Name> { groupName };
            optionGroup.groups = new List<Option.OptionGroup.Group>();
            var detailOptions = new List<Option.DetailOption>();

            int idx = 0;
            foreach (string stockNo in stockNoOrder)
            {
                // stockNo를 기준으로 각 그룹의 옵션명 조합
                var nameParts = new List<string>();
                foreach (var dim in groupDimensions)
                {
                    if (dim.TryGetValue(stockNo, out string name))
                        nameParts.Add(name);
                }

                string combinedName = string.Join(" / ", nameParts);
                if (string.IsNullOrWhiteSpace(combinedName)) continue;

                double addPrc = addPrcMap.TryGetValue(stockNo, out double ap) ? ap : 0;
                double optionPrice = basePrice + addPrc;

                string id = idx.ToString();
                var group = new Option.OptionGroup.Group();
                group.groupId = id;
                group.optionName = new List<Name> { new Name(Language.KR, combinedName) };
                optionGroup.groups.Add(group);

                var detailOption = new Option.DetailOption();
                detailOption.optionIds = id;
                detailOption.optionGroupName = new List<Name> { groupName };
                detailOption.optionName = new List<Name> { new Name(Language.KR, combinedName) };
                if (optionPrice > 0)
                {
                    detailOption.price = new List<Price> { new Price(Currency.KRW, optionPrice, optionPrice) };
                }
                // 재고 수량 설정
                if (stockQuantityMap.TryGetValue(stockNo, out int quantity))
                {
                    detailOption.quantity = quantity;
                }
                detailOptions.Add(detailOption);
                idx++;
            }

            optionGroups.Add(optionGroup);
            if (optionGroup.groups.Count <= 0) return null;

            var option = new Option();
            option.optionGroups = optionGroups;
            option.detailOptions = detailOptions;
            return option;
        }

        #endregion

        #region 상품 수집 메인

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var product = new Product();
            try
            {
                string code = GetCode(url);
                product.site = this.Site;
                product.code = code;
                product.currency = Currency.KRW.ToString();
                product.url = url;

                // 1. 검색 API에서 기본 정보 조회 (제목, 브랜드, 가격, 이미지, 설명)
                JToken source = await GetProductSource(connectionId, code);
                if (source == null) throw new Exception("검색 API에서 상품 정보를 찾을 수 없습니다.");

                product.title = GetTitle(source);
                product.brand = GetBrand(source);
                product.price = GetPrice(source);
                product.descs = GetDescs(source);
                product.tags = null;

                // 2. 상품 페이지 HTML에서 __NUXT_DATA__ 파싱 (이미지 + 옵션 + 상세페이지)
                CollectExtension.CollectSleep();
                string html = await ChatHubService.GetRequest(connectionId, url);
                if (!string.IsNullOrWhiteSpace(html))
                {
                    JArray nuxtData = ExtractNuxtData(html);
                    if (nuxtData != null)
                    {
                        JObject productObj = FindNuxtProductObject(nuxtData, code);
                        if (productObj != null)
                        {
                            // 이미지: __NUXT_DATA__ productImage에서 전체 이미지 추출
                            product.mainImages = GetMainImagesFromNuxt(nuxtData, productObj);

                            // 옵션: __NUXT_DATA__에서 전체 색상/사이즈 + addPrc 추출
                            double salePrice = source.Value<double?>("dcPrcPc")
                                ?? source.Value<double?>("selPrc") ?? 0;
                            product.option = GetOptionFromNuxt(nuxtData, productObj, salePrice);

                            // 상세페이지: __NUXT_DATA__의 prdDescContClob 필드
                            product.detailPageHtml = GetDetailPageHtmlFromNuxt(nuxtData, productObj);
                        }
                    }
                }

                // __NUXT_DATA__ 파싱 실패 시 검색 API 이미지 폴백
                if (product.mainImages == null)
                {
                    product.mainImages = GetMainImages(source);
                }

                // 배송비 합산: dlvFreeYn이 "N"이면 dlvAmt를 가격에 합산
                string dlvFreeYn = source.Value<string>("dlvFreeYn");
                if (dlvFreeYn != "Y")
                {
                    double dlvAmt = source.Value<double?>("dlvAmt") ?? 0;
                    if (dlvAmt > 0)
                    {
                        product.price[0].salePrice += dlvAmt;
                        product.price[0].originalPrice += dlvAmt;

                        if (product.option != null)
                        {
                            for (int i = 0; i < product.option.detailOptions.Count; i++)
                            {
                                product.option.detailOptions[i].price[0].salePrice += dlvAmt;
                                product.option.detailOptions[i].price[0].originalPrice += dlvAmt;
                            }
                        }
                    }
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

        public Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            throw new NotImplementedException();
        }

        public Product GetProduct(CollectRawData rawData)
        {
            throw new NotImplementedException();
        }

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
