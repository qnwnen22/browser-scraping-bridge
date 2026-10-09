#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Common.Exceptions;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Musinsa
{
    public class Musinsa : CollectBase
    {
        public override string Site => nameof(Musinsa);
        public override string Name => "무신사";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = true;

        #region Common
        private async Task<string> GetLoginEntryTokenUrl(string connectionId, string account, string password)
        {

            try
            {
                var loginUrl = "https://www.musinsa.com/auth/login";
                string getResponse = await ChatHubService.GetRequest(connectionId, loginUrl);

                var platformCode = getResponse.SelectNode("//input[@id='platformCode']", "value");
                var clientRedirectUri = getResponse.SelectNode("//input[@id='clientRedirectUri']", "value");
                var clientState = getResponse.SelectNode("//input[@id='clientState']", "value");
                var cipherKey = getResponse.SelectNode("//input[@id='cipherKey']", "value");
                var cipherVersion = getResponse.SelectNode("//input[@id='cipherVersion']", "value");
                var loginToken = getResponse.SelectNode("//input[@id='loginToken']", "value");
                var findPageRedirectUrl = getResponse.SelectNode("//input[@id='findPageRedirectUrl']", "value");
                var encryptMemberId = EncryptAES(account, cipherKey);
                var encryptPassword = EncryptAES(password, cipherKey);
                var isCheckGoogleRecaptcha = getResponse.SelectNode("//input[@id='isCheckGoogleRecaptcha']", "value");

                var cookies = await ChatHubService.GetCookies(connectionId, loginUrl);

                var headers = new Dictionary<string, string>();
                headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
                headers.Add("Accept-Language", "ko");
                headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36");
                headers.Add("Content-Type", "application/x-www-form-urlencoded");

                var posts = new List<string>();
                posts.Add($"platformCode={platformCode.ToWebUrlEncoding()}");
                posts.Add($"clientRedirectUri={clientRedirectUri.ToWebUrlEncoding()}");
                posts.Add($"clientState={clientState.ToWebUrlEncoding()}");
                posts.Add($"cipherKey={cipherKey.ToWebUrlEncoding()}");
                posts.Add($"cipherVersion={cipherVersion.ToWebUrlEncoding()}");
                posts.Add($"loginToken={loginToken.ToWebUrlEncoding()}");
                posts.Add($"findPageRedirectUrl={findPageRedirectUrl.ToWebUrlEncoding()}");
                posts.Add($"encryptMemberId={encryptMemberId.ToWebUrlEncoding()}");
                posts.Add($"encryptPassword={encryptPassword.ToWebUrlEncoding()}");
                posts.Add($"isCheckGoogleRecaptcha={isCheckGoogleRecaptcha.ToWebUrlEncoding()}");
                var post = posts.Join("&");
                var postResponse = await ChatHubService.PostRequest(connectionId, "https://member.one.musinsa.com/login", headers, post);
                return null;
            }
            catch (Exception e)
            {

            }
            return null;
        }


        /// <summary>
        /// 무신사 로그인 시 계정정보 암호화 방법으로 AES 암호화 사용됨
        /// https://static.msscdn.net/static/member/js/security.js
        /// </summary>
        public string EncryptAES(string plainText, string secretKey)
        {
            // Key (UTF-8 그대로 사용)
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);

            // AES 키 길이 맞추기 (16/24/32 바이트)
            Array.Resize(ref keyBytes, 32);

            // IV = 빈 문자열 → 16바이트 0
            byte[] ivBytes = new byte[16];

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        private void CheckProductValidity(Product product, JToken data)
        {
            try
            {
                var isOfflineGoods = data.Value("MSS2")?.Value<bool?>("isOfflineGoods");
                if (isOfflineGoods == true) throw new CollectException(ProductCollectStatus.Exception, "오프라인 상품으로 수집에서 제외 됩니다.");

                var saleTypeToken = data.SelectToken("MSS5.goodsSaleType") ?? data.SelectToken("MSS2.goodsSaleType");
                string saleType = saleTypeToken?.ToString();
                // 마켓 단위 판매 상태 (STOP_SALE / SOLDOUT)
                bool isStoppedOrSoldout =
                        string.Equals(saleType, "SOLDOUT", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(saleType, "STOP_SALE", StringComparison.OrdinalIgnoreCase);

                if (product.option != null)
                {
                    var detailOptions = product.option.detailOptions;

                    // 품절로 강제해야 하는지 여부 플래그
                    bool shouldForceSoldOut = false;

                    // 1. 마켓 자체 상품 판매중지(STOP_SALE, SOLDOUT)
                    if (isStoppedOrSoldout)
                    {
                        shouldForceSoldOut = true;
                    }
                    else if (detailOptions != null && detailOptions.Count > 0)
                    {
                        // 2. 옵션이 전부 quantity == 0 인 경우
                        bool allZeroQuantity = detailOptions.All(x => (x?.quantity ?? 0) == 0);
                        if (allZeroQuantity)
                        {
                            shouldForceSoldOut = true;
                        }
                    }

                    // 품절로 판정된 경우 detailOptions 전부 quantity=0 / isSoldOut=true 로 강제 세팅
                    if (shouldForceSoldOut)
                    {
                        if (detailOptions != null && detailOptions.Count > 0)
                        {
                            for (int i = 0; i < detailOptions.Count; i++)
                            {
                                detailOptions[i].quantity = 0;
                                detailOptions[i].isSoldOut = true;
                            }
                        }
                        throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품 입니다.");
                    }
                }
                else
                {
                    if (isStoppedOrSoldout)
                    {
                        throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품 입니다.");
                    }
                }
            }
            catch (CollectException ce)
            {
                product.collectStatus = ce.CollectStatus;
                product.memo = ce.Message;
            }
            catch (SaleException se)
            {
                product.saleStatus = se.SaleStatus;
                product.memo = se.Message;
            }
            catch (Exception e)
            {
                product.collectStatus = ProductCollectStatus.Error;
                product.memo = e.Message;
            }
        }
        private double CalculateDiscountedPrice(double? currentPrice, JToken data, MusinsaCollectOption option)
        {
            JToken goodsPrice = data["MSS2"]?["goodsPrice"];

            double? calculatePrice = currentPrice;
            if (option == null) return (double)calculatePrice;

            if (option?.UseCouponDiscount == true)
            {
                List<JToken> couponDatas = data["couponDatas"]?["data"]?["list"]?.ToList();
                if (couponDatas != null && couponDatas.Count > 0)
                {
                    List<double?> salePrices = couponDatas.Select(x => x.Value<double?>("salePrice")).ToList();
                    double? max = salePrices.Max();
                    if (max != null)
                    {
                        JToken maxSaleCoupon = couponDatas.Find(x => x.Value<double?>("salePrice") == max);
                        var couponKind = maxSaleCoupon.Value<string>("couponKind");
                        if (couponKind == "%")
                        {
                            string couponValue = maxSaleCoupon.Value<string>("couponValue");
                            double saleValue = double.Parse(couponValue);
                            var discountValue = calculatePrice * (saleValue / 100d);
                            calculatePrice -= discountValue;
                        }
                        else
                        {
                            calculatePrice -= max;
                        }
                    }
                }
            }

            if (option?.UseLevelDiscount == true)
            {
                var isLimitedDc = data["MSS2"].Value<bool>("isLimitedDc");
                var isOutlet = data["MSS2"].Value<bool>("isOutlet");
                var isDrop = data["MSS2"].Value<bool>("isDrop");
                bool isLevelDiscount = isLimitedDc || isOutlet || isDrop; // 등급 할인 가능 체크
                if (isLevelDiscount == false)
                {
                    int? level = data["MSS0"]?["memberInfo"]?.Value<int>("level");
                    if (level != null && level > 0)
                    {
                        double memberDiscountRate = goodsPrice.Value<double>("memberDiscountRate");
                        var discountValue = CulDiscountValue(calculatePrice, memberDiscountRate);
                        calculatePrice -= discountValue;
                    }
                }
            }

            var memberPoint = data["MSS2"]?["point"].Value<double?>("memberPoint");
            if (option?.UsePointDiscount == true)
            {
                var isRestictedUsePoint = data["MSS2"].Value<bool>("isRestictedUsePoint"); // 포인트 사용 제한 여부
                if (isRestictedUsePoint == false)
                {
                    double? pointDiscount = CulDiscountValue(calculatePrice, 7d);
                    if (memberPoint >= 5000)
                    {
                        calculatePrice -= pointDiscount;
                    }
                }
            }

            if (option?.UsePrePointDiscount == true)
            {
                var isPrePoint = data["MSS2"].Value<bool>("isPrePoint"); // 적립금 선할인 여부
                if (isPrePoint)
                {
                    double memberSavePointRate = goodsPrice.Value<double>("memberSavePointRate");
                    double? pointDiscount = CulDiscountValue(calculatePrice, memberSavePointRate);
                    calculatePrice -= pointDiscount;
                }
            }

            if (option?.UseHyundaiCartInstantDiscount == true)
            {
                bool useHyundaiCardInstantDiscount = true; // 현대카드 즉시 할인 사용 여부
                if (useHyundaiCardInstantDiscount)
                {
                    if (calculatePrice >= 31000)
                    {
                        calculatePrice -= 30000d;
                    }
                }
            }

            if (option?.UseHyundaiCartBillDiscount == true)
            {
                bool useHyundaiCardBillDiscount = true; // 현대카드 5% 청구 할인 사용 여부
                if (useHyundaiCardBillDiscount)
                {
                    var billDiscount = CulDiscountValue2(calculatePrice, 5d);
                    calculatePrice -= billDiscount;
                }
            }

            return (double)calculatePrice;
        }
        private double CulDiscountValue(double? input, double discount)
        {
            var persent = (double)discount / 100d;
            double? culValue = input * persent;
            culValue /= 10;
            double discountValue = Math.Truncate((double)culValue);
            discountValue *= 10;
            return discountValue;
        }
        private double CulDiscountValue2(double? input, double discount)
        {
            var persent = (double)discount / 100d;
            double? culValue = input * persent;
            double discountValue = Math.Round((double)culValue, MidpointRounding.AwayFromZero);
            return discountValue;
        }
        #endregion

        #region Back Up
        //public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        //{
        //    string code = GetCode(url);
        //    Product product = new Product();
        //    try
        //    {
        //        JToken data = await GetData(connectionId, url);
        //        product.code = code;
        //        product.site = this.Site;
        //        product.url = $"https://www.musinsa.com/products/{code}";
        //        product.currency = Currency.KRW.ToString();
        //        CollectExtension.CollectSleep(1000); // 무신사 API 호출 시 1초 대기

        //        if (collectOptionSetting?.MusinsaCollectOption?.IsBoutiqueProduct == true)
        //        {
        //            List<JToken> storeCodes = data["MSS2"]?["storeCodes"]?.ToList();
        //            if (storeCodes != null && storeCodes.Count > 0)
        //            {
        //                foreach (JToken storeCode in storeCodes)
        //                {
        //                    if (storeCode.ToString() == "boutique") throw new Exception("부티크 상품 입니다.");
        //                }
        //            }
        //        }

        //        product.title = GetTitle(data, collectOptionSetting);
        //        product.brand = GetBrand(data);
        //        product.descs = GetDescs(data);
        //        product.mainImages = GetMainImages(data);
        //        product.detailPageHtml = GetDetailPageHtml(data);
        //        product.price = GetPrice(data);
        //        product.option = GetOption(data);
        //        //product.option = GetOption2(data);

        //        ApplyCollectOptionSettingToPrice(product, data, collectOptionSetting);
        //        CheckProductValidity(product, data);
        //    }
        //    catch (CollectException ce)
        //    {
        //        product.collectStatus = ce.CollectStatus;
        //        product.memo = ce.Message;
        //    }
        //    catch (SaleException se)
        //    {
        //        product.saleStatus = se.SaleStatus;
        //        product.memo = se.Message;
        //    }
        //    catch (SocketException socketException)
        //    {
        //        product.collectStatus = ProductCollectStatus.Failed;
        //        product.memo = "소켓 연결을 확인해주세요.";
        //    }
        //    catch (Exception e)
        //    {
        //        product.collectStatus = ProductCollectStatus.Failed;
        //        product.memo = e.Message;
        //    }
        //    return product;
        //} 

        /// <summary>
        /// 옵션 수집 ver 2.0
        /// 옵션 중 추가 상품이 붙어있는 케이스가 발견됨, ex) https://www.musinsa.com/products/4128817
        /// 추가 상품 옵션도 포함되도록 변경
        /// </summary>
        //[Obsolete("사용 시 재고관리 쪽에서 문제 생김, 추가 상품이 옵션에 포함될 경우 재고관리가 어떻게 처리되야할지 확인 후 사용")]
        //private Option GetOption2(JToken data)
        //{
        //    JToken optionData = data["optionData"];
        //    if (optionData == null) return null;

        //    var optionValueNos = new List<string>();
        //    var optionItems = optionData.Value("data", "optionItems").ToList();
        //    foreach (var optionItem in optionItems)
        //    {
        //        var getOptionValueNos = optionItem.Value("optionValueNos").ToList();
        //        if (getOptionValueNos != null)
        //        {
        //            foreach (var getOptionValueNo in getOptionValueNos)
        //            {
        //                optionValueNos.Add(getOptionValueNo.ToString());
        //            }
        //        }
        //    }

        //    optionValueNos = optionValueNos.Distinct().ToList();

        //    var optionGroups = new List<Option.OptionGroup>();

        //    var basics = optionData.Value("data", "basic").ToList();
        //    foreach (JToken basic in basics)
        //    {
        //        var getOptionGroup = GetOptionGroup(basic, optionValueNos);
        //        optionGroups.Add(getOptionGroup);
        //    }

        //    var extraToken = optionData.Value("data", "extra");
        //    if (extraToken != null)
        //    {
        //        List<JToken> extras = extraToken.ToList();
        //        foreach (JToken extra in extras)
        //        {
        //            var getOptionGroup = GetOptionGroup(extra);
        //            optionGroups.Add(getOptionGroup);
        //        }
        //    }

        //    List<Option.DetailOption> detailOptions = ProductHelper.CreateDetailOptions(optionGroups);
        //    foreach (var optionGroup in optionGroups)
        //    {
        //        foreach (var group in optionGroup.groups)
        //        {
        //            var findAll = detailOptions.FindAll(x => x.id.Contains(group.id));
        //            if (findAll == null) continue;

        //            var salePrice = group.price?.First().salePrice;
        //            var originalPrice = group.price?.First().originalPrice;

        //            foreach (var find in findAll)
        //            {
        //                if (find.price == null)
        //                {
        //                    find.price = ProductHelper.CreatePrice(this.Currency, originalPrice, salePrice);
        //                }
        //                else
        //                {
        //                    if (originalPrice != null) find.price.First().originalPrice += (double)originalPrice;
        //                    if (salePrice != null) find.price.First().salePrice += (double)salePrice;
        //                }
        //            }
        //        }
        //    }

        //    var option = new Option();
        //    option.optionGroups = optionGroups;
        //    option.detailOptions = detailOptions;
        //    return option;
        //}

        //private Option.OptionGroup GetOptionGroup(JToken item, List<string> optionValueNos = null)
        //{
        //    Option.OptionGroup optionGroup = new Option.OptionGroup();
        //    var no = item.Value<string>("no");
        //    optionGroup.id = no;
        //    var name = item.Value<string>("name");
        //    optionGroup.groupName = ProductHelper.CreateName(this.Language, name);

        //    optionGroup.groups = new List<Option.OptionGroup.Group>();
        //    List<JToken> optionValues = item.Value("optionValues").ToList();
        //    foreach (var optionValue in optionValues)
        //    {
        //        Option.OptionGroup.Group group = new Option.OptionGroup.Group();
        //        string optNo = optionValue.Value<string>("no");
        //        if (optionValueNos != null)
        //        {
        //            if (optionValueNos.Contains(optNo) == false) continue;
        //        }
        //        group.id = optNo;
        //        string optName = optionValue.Value<string>("name");
        //        group.optionName = ProductHelper.CreateName(this.Language, optName);
        //        long? optPrice = optionValue.Value<long?>("price");
        //        if (optPrice != null) group.price = ProductHelper.CreatePrice(this.Currency, optPrice, optPrice);
        //        int? optQuantity = optionValue.Value<int?>("quantity");
        //        if (optQuantity != null) group.quantity = (int)optQuantity;
        //        optionGroup.groups.Add(group);
        //    }
        //    return optionGroup;
        //}

        //private void ApplyCollectOptionSettingToPrice(Product product, JToken data, CollectOptionSetting collectOptionSetting)
        //{
        //    if (product.option != null)
        //    {
        //        var salePrice = product.price.First().salePrice;
        //        var originalPrice = product.price.First().originalPrice;
        //        for (int i = 0; i < product.option.detailOptions.Count; i++)
        //        {
        //            var optionSalePrice = product.option.detailOptions[i].price.First().salePrice + salePrice;
        //            product.option.detailOptions[i].price.First().salePrice = CalculateDiscountedPrice(optionSalePrice, data, collectOptionSetting.MusinsaCollectOption);
        //            product.option.detailOptions[i].price.First().originalPrice += originalPrice;
        //        }
        //        product.price.First().salePrice = CalculateDiscountedPrice(salePrice, data, collectOptionSetting.MusinsaCollectOption);
        //    }
        //}
        #endregion

        public override async Task<string> GetLoginUrlAsync(string connectionId)
        {
            string loginUrl = "https://www.musinsa.com/auth/login?referer=https%3A%2F%2Fwww.musinsa.com%2Fmain%2Fmusinsa%2Frecommend%3Fgf%3DA";
            try
            {
                string loginStatus = await ChatHubService.GetRequest(connectionId, "https://my.musinsa.com/api/member/v1/login-status");
                if (string.IsNullOrWhiteSpace(loginStatus)) throw new Exception();
                JObject data = loginStatus.ToJObject();
                if (data == null) throw new Exception();
                string nickName = data["data"]?["memberInfo"]?.Value<string>("nickName");
                if (string.IsNullOrWhiteSpace(nickName)) throw new Exception();
                loginUrl = null;
            }
            catch (Exception exception)
            {

            }
            return loginUrl;
        }

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            var apiUriBuilder = new UriBuilderAdvance("https://api.musinsa.com/api2/dp/v1/plp/goods");
            apiUriBuilder.queryDictionary = uriBuilderAdvance.queryDictionary;
            apiUriBuilder.queryDictionary.Remove("keywordType");

            string sortCode = apiUriBuilder.GetQuery("sortCode");
            if (string.IsNullOrWhiteSpace(sortCode)) sortCode = "POPULAR";

            if (uriBuilderAdvance.Segments[1] == "category/")
            {
                // Segments.Last() -> Segments[2] : https://www.musinsa.com/category/106013/goods?separatorId=3&gf=F&brand=discoverykids
                // string category = uriBuilderAdvance.Segments.Last();
                string category = uriBuilderAdvance.Segments[2];
                if (category.Contains("/")) category = category.Replace("/", "");
                apiUriBuilder.SetQuery("category", category);
                apiUriBuilder.SetQuery("caller", "CATEGORY");
            }
            else if (uriBuilderAdvance.Segments[1] == "brand/")
            {
                string categoryCode = apiUriBuilder.GetQuery("categoryCode");
                if (string.IsNullOrWhiteSpace(categoryCode) == false)
                {
                    apiUriBuilder.DeleteQuery("categoryCode");
                    apiUriBuilder.SetQuery("category", categoryCode);
                }
                string brandName = uriBuilderAdvance.Segments[2].Replace("/", "");
                apiUriBuilder.SetQuery("brand", brandName);
                apiUriBuilder.SetQuery("caller", "FLAGSHIP");
            }
            else
            {
                apiUriBuilder.SetQuery("caller", "SEARCH");
            }
            apiUriBuilder.SetQuery("sortCode", sortCode);
            apiUriBuilder.SetQuery("page", page.ToString());
            apiUriBuilder.SetQuery("size", "60");

            string apiRequestUrl = apiUriBuilder.ToString();

            string json = await ChatHubService.GetRequest(connectionId, apiRequestUrl);
            if (string.IsNullOrEmpty(json)) return null;
            var data = json.ToJObject();
            if (data == null) return null;
            List<JToken> itemlist = data["data"]?["list"]?.ToList();
            if (itemlist == null || itemlist.Count == 0) return null;

            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            foreach (JToken item in itemlist)
            {
                string goodsNo = item.Value<string>("goodsNo");
                if (string.IsNullOrEmpty(goodsNo)) continue;
                string goodsLinkUrl = item.Value<string>("goodsLinkUrl");
                if (string.IsNullOrWhiteSpace(goodsLinkUrl)) goodsLinkUrl = $"https://www.musinsa.com/products/{goodsNo}";
                string goodsName = item.Value<string>("goodsName");
                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = goodsNo;
                collectPageInfo.Url = goodsLinkUrl;
                collectPageInfo.Title = goodsName;
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }

        public override void ThrowIfCaptcha(string url, string html) { }

        public override string GetCode(string url)
        {
            Uri uri = new Uri(url);
            return uri.Segments.Last();
        }

        public override string CreateProductUrl(string code)
        {
            return null;
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            try
            {
                var pdpData = html.GetValue("//script[@id='pdp-data']", TextEx.XPathOption.InnerHtml);
                if (string.IsNullOrWhiteSpace(pdpData)) throw new Exception();
                var scripts = pdpData.MultiSplit("= {", "};");
                if (scripts == null || scripts.Count <= 0)
                {
                    var __next_data__ = html.GetValue("//script[@id='__NEXT_DATA__']", TextEx.XPathOption.InnerHtml);
                    if (string.IsNullOrWhiteSpace(__next_data__) == false)
                    {
                        JObject nextData = __next_data__.ToJObject();
                        var message = nextData.Value("props", "pageProps", "meta", "meta").Value<string>("message");
                        if (message == "유효하지 않은 상품 입니다.")
                        {
                            throw new SaleException(ProductSaleStatus.Stopped, message);
                        }
                    }
                    throw new Exception();
                }

                var data = new JObject();
                for (int i = 0; i < scripts.Count; i++)
                {
                    string script = scripts[i];
                    var json = $"{{{script}}}";
                    JObject itemData = json.ToJObject();
                    if (itemData != null)
                    {
                        data[$"MSS{i}"] = itemData;
                    }
                }
                if (data.HasValues == false) throw new Exception();

                string code = GetCode(url);

                string descJson = await ChatHubService.GetRequest(connectionId, $"https://goods-detail.musinsa.com/api2/goods/{code}/essential");
                if (string.IsNullOrWhiteSpace(descJson)) throw new SocketException();
                JObject descData = descJson.ToJObject();
                if (descData != null && descData.HasValues)
                {
                    data["descData"] = descData;
                }

                //var optionApiUrlBuilder = new UriBuilderAdvance($"https://goods-detail.musinsa.com/api2/goods/{code}/v2/options");
                var optionApiUrlBuilder = new UriBuilderAdvance($"https://goods-detail.musinsa.com/api2/goods/{code}/options");
                string goodsSaleType = data["MSS2"]?["goodsSaleType"]?.ToString();
                if (string.IsNullOrWhiteSpace(goodsSaleType) == false) optionApiUrlBuilder.SetQuery("goodsSaleType", goodsSaleType);
                string optKindCd = data["MSS2"]?["optKindCd"]?.ToString();
                if (string.IsNullOrWhiteSpace(optKindCd) == false) optionApiUrlBuilder.SetQuery("optKindCd", optKindCd);
                string optionRequestUrl = optionApiUrlBuilder.ToString();
                string optionJson = await ChatHubService.GetRequest(connectionId, optionRequestUrl);
                if (string.IsNullOrWhiteSpace(optionJson)) throw new SocketException();
                JObject optionData = optionJson.ToJObject();
                if (optionData != null && optionData.HasValues)
                {
                    data["optionData"] = optionData;

                    List<JToken> optionValues = optionData.Value("data", "basic").First().Value("optionValues").ToList();
                    IEnumerable<int> optionValueNos = optionValues.Select(x => x.Value<int>("no"));
                    var postBody = new JObject();
                    postBody["optionValueNos"] = new JArray(optionValueNos);
                    var post = postBody.ToString();
                    var header = new Dictionary<string, string>();
                    header.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
                    header.Add("Content-Type", "application/json");
                    string request = $"https://goods-detail.musinsa.com/api2/goods/{code}/options/prioritized-inventories";
                    string prioritizedInventories = await ChatHubService.PostRequest(connectionId, request, header, post);
                    JObject optionStockData = prioritizedInventories.ToJObject();
                    if (optionStockData != null)
                    {
                        data["optionStockData"] = optionStockData;
                    }
                }

                var brand = data["MSS2"].Value<string>("brand");
                var comId = data["MSS2"].Value<string>("comId");
                var salePrice = data["MSS2"]["goodsPrice"].Value<string>("salePrice");

                UriBuilderAdvance uriBuilderAdvance1 = new UriBuilderAdvance("https://api.musinsa.com/api2/coupon/coupons/getUsableCouponsByGoodsNo");
                uriBuilderAdvance1.SetQuery("goodsNo", code);
                uriBuilderAdvance1.SetQuery("brand", brand);
                uriBuilderAdvance1.SetQuery("comId", comId);
                uriBuilderAdvance1.SetQuery("salePrice", salePrice);
                var usableCouponsUrl = uriBuilderAdvance1.ToString();
                string getUsableCouponsByGoodsNo = await ChatHubService.GetRequest(connectionId, usableCouponsUrl);
                if (string.IsNullOrWhiteSpace(getUsableCouponsByGoodsNo)) throw new SocketException();
                JObject couponData = getUsableCouponsByGoodsNo.ToJObject();
                if (couponData != null || couponData.HasValues == false)
                {
                    data["couponDatas"] = couponData;
                }

                return data;
            }
            catch (SaleException saleException)
            {
                throw saleException;
            }
            catch (SocketException socketException)
            {
                throw socketException;
            }
            catch (Exception exception)
            {
                throw new Exception("상품 정보를 확인할 수 없습니다.", exception);
            }
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string goodsNm = data["MSS2"].Value<string>("goodsNm");
            if (string.IsNullOrWhiteSpace(goodsNm)) throw new CollectException(ProductCollectStatus.Failed, "상품명을 찾을 수 없습니다.");
            return new List<Name> { new Name(Language.KR, goodsNm) };
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            string brand = data["MSS2"].Value<string>("brand");
            if (string.IsNullOrWhiteSpace(brand)) return null;
            return new List<Name> { new Name(Language.KR, brand) };
        }

        public override string GetModel(string html, JToken data) { return null; }

        public override List<Desc> GetDescs(string html, JToken data)
        {
            JToken descData = data["descData"];
            if (descData == null) return null;
            var essentials = descData["data"]?["essentials"]?.ToList();
            if (essentials == null || essentials.Count <= 0) return null;
            int count = 1;
            List<Desc> descs = new List<Desc>();
            foreach (JToken essential in essentials)
            {
                string name = essential.Value<string>("name");
                string value = essential.Value<string>("value");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value)) continue;
                Desc desc = new Desc(count, Language.KR, name, value);
                descs.Add(desc);
                count++;
            }
            return descs;
        }

        public override List<Tag> GetTags(string html, JToken data) { return null; }

        public override List<Price> GetPrice(string html, JToken data)
        {
            JToken goodsPrice = data["MSS2"]?["goodsPrice"];
            if (goodsPrice == null || goodsPrice.HasValues == false) throw new Exception("상품 가격을 찾을 수 없습니다.");
            double? salePrice = goodsPrice.Value<double?>("salePrice");
            double? normalPrice = goodsPrice.Value<double?>("normalPrice");
            var result = new List<Price>() { new Price(Currency.KRW, (double)normalPrice, (double)salePrice, true) };
            return result;
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            List<string> mainImages = new List<string>();

            var thumbnailImageUrl = data["MSS2"].Value<string>("thumbnailImageUrl");
            if (string.IsNullOrWhiteSpace(thumbnailImageUrl) == false)
            {
                mainImages.Add(thumbnailImageUrl);
            }

            List<JToken> goodsImages = data["MSS2"]?["goodsImages"]?.ToList();
            if (goodsImages != null && goodsImages.Count > 0)
            {
                foreach (JToken goodsImage in goodsImages)
                {
                    var imageUrl = goodsImage.Value<string>("imageUrl");
                    mainImages.Add(imageUrl);
                }
            }
            for (int i = 0; i < mainImages.Count; i++)
            {
                mainImages[i] = $"https://image.msscdn.net/thumbnails{mainImages[i]}";
            }
            if (mainImages.Count <= 0) throw new CollectException(ProductCollectStatus.Failed, "상품 이미지를 찾을 수 없습니다.");
            return mainImages;
        }

        public override string GetVideoUrl(string html, JToken data) { return null; }

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var sb = new StringBuilder();
            string goodsContents = data["MSS2"]?.Value<string>("goodsContents");
            sb.AppendLine(goodsContents);
            string mdOpinion = data["MSS2"]?.Value<string>("mdOpinion");
            if (string.IsNullOrWhiteSpace(mdOpinion) == false)
            {
                mdOpinion = $"<div>{mdOpinion}</div>";
                sb.AppendLine(mdOpinion);
            }
            string detailPage = sb.ToString();
            if (string.IsNullOrWhiteSpace(detailPage)) return null;
            return HtmlReader.GetDafaultHtml(detailPage);
        }

        public override Option GetOption(string html, JToken data)
        {
            JToken optionData = data["optionData"];
            if (optionData == null) return null;
            Option option = new Option();

            var extra = optionData.Value("data", "extra");
            if (extra != null) throw new CollectException(ProductCollectStatus.Exception, "옵션 중 추가구성이 있는 경우 재고 파악에 문제가 있어, 수집에서 예외됩니다.");

            var basics = optionData["data"]?["basic"]?.ToList();
            if (basics == null || basics.Count <= 0) return null;
            option.optionGroups = new List<Option.OptionGroup>();

            int count = 1;
            foreach (JToken basic in basics)
            {
                var optionGroup = new Option.OptionGroup();
                var no = basic.Value<string>("no");
                if (string.IsNullOrWhiteSpace(no)) continue;
                var name = basic.Value<string>("name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"옵션 선택{count}";
                    count++;
                }
                var optionValues = basic["optionValues"]?.ToList();
                if (optionValues == null || optionValues.Count <= 0) continue;

                optionGroup.optionId = no;
                optionGroup.groupName = new List<Name> { new Name(Language.KR, name) };
                optionGroup.groups = new List<Option.OptionGroup.Group>();
                foreach (JToken optionValue in optionValues)
                {
                    string optionNo = optionValue.Value<string>("no");
                    string optionName = optionValue.Value<string>("name");
                    string imageUrl = optionValue.Value<string>("imageUrl");

                    var group = new Option.OptionGroup.Group();
                    group.groupId = optionNo;
                    group.optionName = new List<Name> { new Name(Language.KR, optionName) };
                    if (string.IsNullOrWhiteSpace(imageUrl) == false) { group.optionImage = imageUrl; }
                    optionGroup.groups.Add(group);
                }
                option.optionGroups.Add(optionGroup);
            }

            if (option.optionGroups.Count <= 0) return null;

            List<JToken> optionItems = optionData["data"]?["optionItems"]?.ToList();
            if (optionItems == null || optionItems.Count <= 0) return null;
            option.detailOptions = new List<Option.DetailOption>();

            List<Option.OptionGroup.Group> selectGroups = option.optionGroups.SelectMany(x => x.groups).ToList();
            var dic = new Dictionary<string, bool>();
            foreach (var selectGroup in selectGroups)
            {
                dic.Add(selectGroup.groupId, false);
            }

            foreach (JToken optionItem in optionItems)
            {
                var nos = new List<string>();
                var optionNames = new List<string>();
                var names = new List<string>();
                List<JToken> optionValues = optionItem["optionValues"].ToList();

                foreach (JToken optionValue in optionValues)
                {
                    var no = optionValue.Value<string>("no");
                    if (string.IsNullOrWhiteSpace(no) == false) nos.Add(no);

                    foreach (Option.OptionGroup optionGroup in option.optionGroups)
                    {
                        bool isBreak = false;
                        foreach (Option.OptionGroup.Group group in optionGroup.groups)
                        {
                            if (group.groupId == no)
                            {
                                optionNames.Add(optionGroup.groupName.First().text);
                                isBreak = true;
                                dic[no] = true;
                                break;
                            }
                        }
                        if (isBreak) break;
                    }
                    var name = optionValue.Value<string>("name");
                    if (string.IsNullOrWhiteSpace(name) == false) names.Add(name);
                }

                bool? isSoldOut = optionItem.Value<bool?>("isSoldOut");
                int remainQuantity = optionItem.Value<int>("remainQuantity");
                var price = optionItem.Value<double>("price");
                var detailOption = new Option.DetailOption();
                detailOption.optionIds = string.Join("ㅡ", nos);
                detailOption.optionGroupName = new List<Name> { new Name(Language.KR, string.Join("ㅡ", optionNames)) };
                detailOption.optionName = new List<Name> { new Name(Language.KR, string.Join("ㅡ", names)) };
                detailOption.price = ProductHelper.CreatePrice(Currency.KRW, price, price);

                if (isSoldOut == null)
                {
                    int no = optionItem.Value<int>("no");
                    List<JToken> optionStockDatas = data.Value("optionStockData", "data")?.ToList();
                    JToken findData = optionStockDatas?.Find(x => x.Value<int>("productVariantId") == no);
                    if (findData != null)
                    {
                        bool? outOfStock = findData.Value<bool?>("outOfStock");
                        if (outOfStock == false)
                        {
                            detailOption.quantity = 9;
                        }
                    }
                }
                else if (isSoldOut == false)
                {
                    if (remainQuantity == 0)
                    {
                        detailOption.quantity = 9;
                    }
                    else
                    {
                        detailOption.quantity = remainQuantity;
                    }
                }
                detailOption.isSoldOut = detailOption.quantity == 0;
                option.detailOptions.Add(detailOption);
            }

            // 불필요한 옵션 제거
            for (int i = 0; i < option.optionGroups.Count; i++)
            {
                Option.OptionGroup optionGroup = option.optionGroups[i];
                var deleteGroups = new List<Option.OptionGroup.Group>();
                for (int j = 0; j < optionGroup.groups.Count; j++)
                {
                    if (dic[optionGroup.groups[j].groupId] == false)
                    {
                        Option.OptionGroup.Group group = optionGroup.groups[j];
                        deleteGroups.Add(group);
                    }
                }
                foreach (Option.OptionGroup.Group group in deleteGroups)
                {
                    optionGroup.groups.Remove(group);
                }
            }
            return option;
        }

        public override int? GetTotalQuantity(string html, JToken data)
        {
            return null;
        }

        public override async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting setting = null)
        {
            CollectRawData rawData = await GetCollectRawDataAsync(connectionId, url);
            Product product = GetProduct(rawData);
            await ApplyCollectOptionSetting(connectionId, rawData, product, setting);
            return product;
        }

        public override Product GetProduct(CollectRawData rawData)
        {
            var product = base.GetProduct(rawData);
            if (product.option != null)
            {
                var salePrice = product.price.First().salePrice;
                var originalPrice = product.price.First().originalPrice;

                foreach (var detailOption in product.option.detailOptions)
                {
                    var optionPrice = detailOption.price.First();
                    optionPrice.salePrice += salePrice;
                    optionPrice.originalPrice += originalPrice;
                }
            }
            CheckProductValidity(product, rawData.Data);
            return product;
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
            var isOfflineGoods = data.Value("MSS2")?.Value<bool?>("isOfflineGoods");
            if (isOfflineGoods == true) throw new CollectException(ProductCollectStatus.Exception, "오프라인 상품으로 수집에서 제외 됩니다.");

            var saleTypeToken = data.SelectToken("MSS5.goodsSaleType") ?? data.SelectToken("MSS2.goodsSaleType");
            string saleType = saleTypeToken?.ToString();
            // 마켓 단위 판매 상태 (STOP_SALE / SOLDOUT)
            bool isStoppedOrSoldout =
                    string.Equals(saleType, "SOLDOUT", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(saleType, "STOP_SALE", StringComparison.OrdinalIgnoreCase);

            if (product.option != null)
            {
                var detailOptions = product.option.detailOptions;

                // 품절로 강제해야 하는지 여부 플래그
                bool shouldForceSoldOut = false;

                // 1. 마켓 자체 상품 판매중지(STOP_SALE, SOLDOUT)
                if (isStoppedOrSoldout)
                {
                    shouldForceSoldOut = true;
                }
                else if (detailOptions != null && detailOptions.Count > 0)
                {
                    // 2. 옵션이 전부 quantity == 0 인 경우
                    bool allZeroQuantity = detailOptions.All(x => (x?.quantity ?? 0) == 0);
                    if (allZeroQuantity)
                    {
                        shouldForceSoldOut = true;
                    }
                }

                // 품절로 판정된 경우 detailOptions 전부 quantity=0 / isSoldOut=true 로 강제 세팅
                if (shouldForceSoldOut)
                {
                    if (detailOptions != null && detailOptions.Count > 0)
                    {
                        for (int i = 0; i < detailOptions.Count; i++)
                        {
                            detailOptions[i].quantity = 0;
                            detailOptions[i].isSoldOut = true;
                        }
                    }
                    throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품 입니다.");
                }
            }
            else
            {
                if (isStoppedOrSoldout)
                {
                    throw new SaleException(ProductSaleStatus.SoldOut, "품절 상품 입니다.");
                }
            }
        }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            try
            {
                JToken data = rawData.Data;
                if (setting?.MusinsaCollectOption?.IsBoutiqueProduct == true)
                {
                    List<JToken> storeCodes = data["MSS2"]?["storeCodes"]?.ToList();
                    if (storeCodes != null && storeCodes.Count > 0)
                    {
                        foreach (JToken storeCode in storeCodes)
                        {
                            if (storeCode.ToString() == "boutique") throw new CollectException(ProductCollectStatus.Exception, "부티크 상품 입니다.");
                        }
                    }
                }

                if (setting.MusinsaCollectOption?.HasTitleShippingText == true)
                {
                    string headDesc = data["MSS2"].Value<string>("headDesc");
                    if (string.IsNullOrWhiteSpace(headDesc) == false)
                    {
                        bool isContains = headDesc.Contains("예약배송");
                        if (isContains) throw new CollectException(ProductCollectStatus.Exception, $"예약 배송 상품입니다, {headDesc}");
                    }

                    string deliveryDueType = data.Value("MSS5").Value<string>("deliveryDueType");
                    if (deliveryDueType.ToUpper() == "RESERVATION")
                    {
                        throw new CollectException(ProductCollectStatus.Exception, $"예약 배송 상품입니다, {headDesc}");
                    }

                }

                var salePrice = product.price.First().salePrice;
                product.price.First().salePrice = CalculateDiscountedPrice(salePrice, data, setting.MusinsaCollectOption);

                if (product.option != null)
                {
                    for (int i = 0; i < product.option.detailOptions.Count; i++)
                    {
                        var optionSalePrice = product.option.detailOptions[i].price.First().salePrice;
                        product.option.detailOptions[i].price.First().salePrice = CalculateDiscountedPrice(optionSalePrice, data, setting.MusinsaCollectOption);
                    }
                }
            }
            catch (CollectException ce)
            {
                product.collectStatus = ce.CollectStatus;
                product.memo = ce.Message;
            }
            catch (SaleException se)
            {
                product.saleStatus = se.SaleStatus;
                product.memo = se.Message;
            }
            catch (Exception e)
            {
                product.collectStatus = ProductCollectStatus.Failed;
                product.memo = e.Message;
            }
        }
    }
}
