#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.ShoppingNaver.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BrowserScrapingBridge.Server.Sourcing.ShoppingNaver
{
    /// <summary>
    /// ShoppingNaver.Directfarm.cs
    /// </summary>
    public class ShoppingNaverDirectfarm : ShoppingNaver
    {
        public override string Site => "ShoppingNaver-Directfarm";
        public override string Name => "네이버산지직송";
        #region 사용 안함
        //private string GetDirectfarmCategoryPageDataUriString(string uriString, int page)
        //{
        //    Dictionary<string, string> getUriQueries = GetUriQueries(uriString);
        //    string menuId;
        //    getUriQueries.TryGetValue("menu", out menuId);
        //    if (string.IsNullOrWhiteSpace(menuId)) { menuId = "10002043"; }

        //    string sort;
        //    getUriQueries.TryGetValue("sort", out sort);
        //    if (string.IsNullOrWhiteSpace(sort)) { sort = "POPULARITY"; }

        //    string delivery;
        //    getUriQueries.TryGetValue("delivery", out delivery);
        //    if (string.IsNullOrWhiteSpace(delivery) == false)
        //    {
        //        delivery = System.Web.HttpUtility.UrlDecode(delivery);
        //        delivery = delivery.Replace("TODAY_DELIVERY", "TODAY_DISPATCH");
        //    }
        //    string certification;
        //    getUriQueries.TryGetValue("certification", out certification);

        //    var queryBuilder = new List<string>();
        //    queryBuilder.Add($"_nc_={_nc_}");
        //    queryBuilder.Add($"subVertical=DIRECT_FARM");
        //    queryBuilder.Add($"page={page}");
        //    queryBuilder.Add($"pageSize=10");
        //    queryBuilder.Add($"sort={sort}");
        //    queryBuilder.Add($"filter=ALL");
        //    queryBuilder.Add($"displayType=CATEGORY_HOME");
        //    queryBuilder.Add($"includeZzim=true");
        //    queryBuilder.Add($"includeViewCount=false");
        //    queryBuilder.Add($"includeStoreCardInfo=false");
        //    queryBuilder.Add($"includeStockQuantity=false");
        //    queryBuilder.Add($"includeBrandInfo=false");
        //    queryBuilder.Add($"includeBrandLogoImage=false");
        //    queryBuilder.Add($"includeRepresentativeReview=false");
        //    queryBuilder.Add($"includeListCardAttribute=false");
        //    queryBuilder.Add($"includeRanking=false");
        //    queryBuilder.Add($"includeRankingByMenus=false");
        //    queryBuilder.Add($"includeStoreCategoryName=false");
        //    queryBuilder.Add($"includeIngredient=false");
        //    queryBuilder.Add($"menuId={menuId}");
        //    queryBuilder.Add($"standardSizeKeys[]=");
        //    queryBuilder.Add($"standardColorKeys[]=");
        //    queryBuilder.Add($"attributeValueIds[]=");
        //    queryBuilder.Add($"attributeValueIdsAll[]=");
        //    if (string.IsNullOrWhiteSpace(certification))
        //    {
        //        queryBuilder.Add($"certifications[]=");
        //    }
        //    else
        //    {
        //        queryBuilder.Add($"certifications[]=ANIMAL_WELFARE%2CNON_PESTICIDE%2CORGANIC_AGRICULTURAL_PROD%2CNON_ANTIBIOTIC%2CHACCP");
        //    }
        //    queryBuilder.Add($"includeStoreInfoWithHighRatingReview=true");
        //    queryBuilder.Add($"guaranteeTypes[]={delivery}");
        //    string query = string.Join('&', queryBuilder);

        //    StringBuilder uriBuilder = new StringBuilder();
        //    uriBuilder.Append("https://shopping.naver.com/v1/products?");
        //    uriBuilder.Append(query);
        //    string requestUriString = uriBuilder.ToString();
        //    return requestUriString;
        //}

        //private List<string> GetDirectfarmCategoryProductCodes(string json)
        //{
        //    List<string> result = null;
        //    try
        //    {
        //        JObject jObject = JObject.Parse(json);
        //        List<JToken> products = jObject["products"].ToList();
        //        List<string> selectIds = products.Select(x => x["_id"].ToString()).ToList();
        //        result = selectIds;
        //    }
        //    catch (Exception exception)
        //    {
        //        Console.WriteLine(exception.Message);
        //    }
        //    return result;
        //} 

        //private string GetDirectfarmHomePageDataUriString(string uriString, int page)
        //{
        //    Dictionary<string, string> getUriQueries = GetUriQueries(uriString);
        //    string productCategoryId;
        //    getUriQueries.TryGetValue("productCategoryId", out productCategoryId);
        //    if (string.IsNullOrWhiteSpace(productCategoryId)) { productCategoryId = "ALL"; }

        //    var result = new List<string>();
        //    // POPULARITY = 추천순
        //    // RECENT = 최신순
        //    // REVIEW_SCORE = 평점순
        //    // REVIEW = 리뷰순
        //    string sort;
        //    getUriQueries.TryGetValue("sort", out sort);
        //    if (string.IsNullOrWhiteSpace(sort)) { sort = "POPULARITY"; } // 기본값 = 추천순

        //    string filter;
        //    getUriQueries.TryGetValue("filter", out filter);

        //    var queryBuilder = new List<string>();
        //    queryBuilder.Add($"_nc_="); // 타임스탬프(의미 없음)
        //    queryBuilder.Add($"page={page}"); // 페이지
        //    queryBuilder.Add($"pageSize=10"); // 페이지 사이즈
        //    queryBuilder.Add($"sort={sort}"); // 정렬 타입
        //    if (string.IsNullOrWhiteSpace(filter) == false) // 필터 (예약구매상품만 보기)
        //    {
        //        queryBuilder.Add($"filter={filter}");
        //    }
        //    queryBuilder.Add($"naverShoppingCategory={productCategoryId}"); // 카테고리 코드
        //    queryBuilder.Add($"menuId="); // 매뉴 코드(의미 없음)
        //    queryBuilder.Add($"subVertical=DIRECT_FARM"); // 산지직송(고정)
        //    queryBuilder.Add($"includeStoreInfoWithHighRatingReview=true"); // 고정 값
        //    string query = string.Join('&', queryBuilder);

        //    StringBuilder uriBuilder = new StringBuilder();
        //    uriBuilder.Append("https://shopping.naver.com/v1/products?");
        //    uriBuilder.Append(query);
        //    string requestUriString = uriBuilder.ToString();
        //    return requestUriString;
        //}

        //private List<string> GetDirectfarmHomePageProductCodes(string json)
        //{
        //    List<string> result = null;
        //    try
        //    {
        //        JObject jObject = JObject.Parse(json);
        //        List<JToken> products = jObject["products"].ToList();
        //        List<string> selectIds = products.Select(x => x["_id"].ToString()).ToList();
        //        result = selectIds;
        //    }
        //    catch (Exception exception)
        //    {
        //        Console.WriteLine(exception.Message);
        //    }
        //    return result;
        //}
        #endregion

        public string GetDirectfarmHomeProductPageUriString(string uriString, int page)
        {
            Dictionary<string, string> getUriQueries = GetUriQueries(uriString);
            string productCategoryId;
            getUriQueries.TryGetValue("productCategoryId", out productCategoryId);
            //if (string.IsNullOrWhiteSpace(productCategoryId)) { productCategoryId = "ALL"; }
            string sort;
            getUriQueries.TryGetValue("sort", out sort);
            if (string.IsNullOrWhiteSpace(sort)) { sort = "POPULARITY"; } // 기본값 = 추천순

            string filter;
            getUriQueries.TryGetValue("filter", out filter);

            var queryBuilder = new List<string>();
            queryBuilder.Add($"_nc_="); // 타임스탬프(의미 없음)
            queryBuilder.Add($"page={page}"); // 페이지
            queryBuilder.Add($"pageSize=10"); // 페이지 사이즈
            queryBuilder.Add($"sort={sort}"); // 정렬 타입
            if (string.IsNullOrWhiteSpace(filter) == false) // 필터 (예약구매상품만 보기)
            {
                queryBuilder.Add($"filter={filter}");
            }
            queryBuilder.Add($"naverShoppingCategory={productCategoryId}"); // 카테고리 코드
            queryBuilder.Add($"menuId="); // 매뉴 코드(의미 없음)
            queryBuilder.Add($"subVertical=DIRECT_FARM"); // 산지직송(고정)
            queryBuilder.Add($"includeStoreInfoWithHighRatingReview=true"); // 고정 값
            string query = string.Join('&', queryBuilder);

            StringBuilder uriBuilder = new StringBuilder();
            uriBuilder.Append("https://shopping.naver.com/v1/products?");
            uriBuilder.Append(query);
            string requestUriString = uriBuilder.ToString();

            return requestUriString;
        }

        public string GetDirectfarmCategoryProductPageUriString(string uriString, int page)
        {
            Dictionary<string, string> getUriQueries = GetUriQueries(uriString);
            string menuId;
            getUriQueries.TryGetValue("menu", out menuId);
            if (string.IsNullOrWhiteSpace(menuId)) { menuId = "10002043"; }

            string sort;
            getUriQueries.TryGetValue("sort", out sort);
            if (string.IsNullOrWhiteSpace(sort)) { sort = "POPULARITY"; }

            string delivery;
            getUriQueries.TryGetValue("delivery", out delivery);
            if (string.IsNullOrWhiteSpace(delivery) == false)
            {
                delivery = System.Web.HttpUtility.UrlDecode(delivery);
                delivery = delivery.Replace("TODAY_DELIVERY", "TODAY_DISPATCH");
            }
            string certification;
            getUriQueries.TryGetValue("certification", out certification);

            var queryBuilder = new List<string>();
            queryBuilder.Add($"_nc_={_nc_}");
            queryBuilder.Add($"subVertical=DIRECT_FARM");
            queryBuilder.Add($"page={page}");
            queryBuilder.Add($"pageSize=10");
            queryBuilder.Add($"sort={sort}");
            queryBuilder.Add($"filter=ALL");
            queryBuilder.Add($"displayType=CATEGORY_HOME");
            queryBuilder.Add($"includeZzim=true");
            queryBuilder.Add($"includeViewCount=false");
            queryBuilder.Add($"includeStoreCardInfo=false");
            queryBuilder.Add($"includeStockQuantity=false");
            queryBuilder.Add($"includeBrandInfo=false");
            queryBuilder.Add($"includeBrandLogoImage=false");
            queryBuilder.Add($"includeRepresentativeReview=false");
            queryBuilder.Add($"includeListCardAttribute=false");
            queryBuilder.Add($"includeRanking=false");
            queryBuilder.Add($"includeRankingByMenus=false");
            queryBuilder.Add($"includeStoreCategoryName=false");
            queryBuilder.Add($"includeIngredient=false");
            queryBuilder.Add($"menuId={menuId}");
            queryBuilder.Add($"standardSizeKeys[]=");
            queryBuilder.Add($"standardColorKeys[]=");
            queryBuilder.Add($"attributeValueIds[]=");
            queryBuilder.Add($"attributeValueIdsAll[]=");
            if (string.IsNullOrWhiteSpace(certification))
            {
                queryBuilder.Add($"certifications[]=");
            }
            else
            {
                queryBuilder.Add($"certifications[]=ANIMAL_WELFARE%2CNON_PESTICIDE%2CORGANIC_AGRICULTURAL_PROD%2CNON_ANTIBIOTIC%2CHACCP");
            }
            queryBuilder.Add($"includeStoreInfoWithHighRatingReview=true");
            queryBuilder.Add($"guaranteeTypes[]={delivery}");
            string query = string.Join('&', queryBuilder);

            StringBuilder uriBuilder = new StringBuilder();
            uriBuilder.Append("https://shopping.naver.com/v1/products?");
            uriBuilder.Append(query);
            string requestUriString = uriBuilder.ToString();
            return requestUriString;
        }

        public string GetDirectfarmSearchProductPageUriString(string uriString, int page, string scriptFile)
        {
            //if (string.IsNullOrWhiteSpace(scriptFile))
            //{
            //    scriptFile = GetScriptFile(uriString);
            //}
            //if (string.IsNullOrWhiteSpace(scriptFile)) throw new Exception("스크립트를 찾을 수 없습니다");
            //string? groupProductFields = GetGraphQLQuery(scriptFile, "fragment GroupProductFields on GroupProductResponse ", "\"]))),")?.Trim();
            //string? logisticsProductFields = GetGraphQLQuery(scriptFile, "fragment LogisticsProductFields on LogisticsProduct ", "\\n\"])),v,f.tM),")?.Trim();
            //string? misterProductFields = GetGraphQLQuery(scriptFile, "fragment MisterProductFields on MisterProduct ", "\"]))").Trim();
            //string? fetchSas = GetGraphQLQuery(scriptFile, "query FetchSas(", "\\n\"])),c.Tc,s.Ne,d.tM),").Trim();
            //string sha256Hash = GetSha256Hash(groupProductFields, logisticsProductFields, misterProductFields, fetchSas);

            // 스크립트 분석할 시간에 피들러 찍어서 수정하는게 더 효율적이고 빠름
            var sha256Hash = "ebb8f901af6d850d4c6404bc2c00f3f65b61af1d13c4fcf9a890f1f1c985d39f";
            var fetchSasBody = new FetchSasBody(uriString, page);
            string variables = fetchSasBody.ToJson();
            variables = WebUtility.UrlEncode(variables);

            var queryBuilder = new List<string>();
            queryBuilder.Add($"operationName=FetchSas");
            queryBuilder.Add($"variables={variables}");
            string extensions = $"{{\"persistedQuery\":{{\"version\":1,\"sha256Hash\":\"{sha256Hash}\"}}}}";
            extensions = WebUtility.UrlEncode(extensions);
            queryBuilder.Add($"extensions={extensions}");
            var query = string.Join('&', queryBuilder);

            var uriBuilder = new StringBuilder();
            uriBuilder.Append("https://shopping.naver.com/responsive/api/shopv/graphql?");
            uriBuilder.Append(query);
            string requestUriString = uriBuilder.ToString();
            return requestUriString;
        }

        public string GetDirectfarmProductDataUriString(string code)
        {
            string uriString = $"https://shopping.naver.com/window-products/directfarm/{code}";
            return uriString;
        }

        private JObject GetDirectfarmData(string html)
        {
            List<string> scripts = html.SelectNodes("//script", HtmlType.InnerText);
            string startText = "window.__PRELOADED_STATE__=";
            string? findScript = scripts.Find(x => x.IndexOf(startText) == 0);
            string json = findScript.Substring(startText.Length);
            JObject jObject = JObject.Parse(json);
            JToken? productDetail = jObject["productDetail"]["A"];
            JToken? contents = productDetail["contents"];
            JObject? data = contents.ToObject<JObject>();
            return data;
        }

        public Product GetDirectfarmProduct(string code, string html, out JObject data)
        {
            try
            {
                string uriString = $"https://shopping.naver.com/window-products/directfarm/{code}";
                List<string> scripts = html.SelectNodes("//script", HtmlType.InnerText);
                string startText = "window.__PRELOADED_STATE__=";
                string? findScript = scripts.Find(x => x.IndexOf(startText) == 0);
                string json = findScript.Substring(startText.Length);
                JObject jObject = JObject.Parse(json);
                JToken? productDetail = jObject["productDetail"]["A"];
                JToken? contents = productDetail["contents"];
                data = contents.ToObject<JObject>();

                string orderDeliveryAttributeType = data.Value<string>("orderDeliveryAttributeType");
                if (string.IsNullOrWhiteSpace(orderDeliveryAttributeType) == false)
                {
                    if (orderDeliveryAttributeType == "PRE_ORDER") throw new Exception("예약 구매 상품으로 수집에서 제외됩니다.");
                }

                var product = new Product();
                product.site = this.Site;
                product.title = GetTitie(contents);
                product.descs = GetDescs(contents);
                product.code = code;
                product.currency = "KRW";
                product.tags = GetTags(contents);
                product.price = GetPrices(contents);
                product.url = uriString;
                product.mainImages = GetMainImages(contents);
                product.option = GetOption(contents);
                product.groupName = null;
                product.regDate = DateTime.Now;
                return product;
            }
            catch (SaleException se)
            {
                throw se;
            }
            catch (Exception exception)
            {
                exception.Data.Add("code", code);
                throw exception;
            }
        }

        public string GetDetailPageDataUriString(string code, JObject data)
        {
            string productNo = data["productNo"].ToString();
            string address = $"https://shopping.naver.com/product-detail/v1/products/{code}/contents/{productNo}/PC?_nc_={this._nc_}&usedImageProxy=false&v=1";
            return address;
        }
    }
}
