#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.ShoppingNaver.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BrowserScrapingBridge.Server.Sourcing.ShoppingNaver
{
    /// <summary>
    /// ShoppingNaver.Logistics.cs
    /// </summary>
    public class ShoppingNaverLogistics : ShoppingNaver
    {
        public override string Site => "ShoppingNaver-Logistics";
        public override string Name => "네이버N배송";
        #region 사용안함
        //private async Task<string> GetHtml(string uriString)
        //{
        //    using (HttpClient client = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient())
        //    {
        //        client.Timeout = TimeSpan.FromSeconds(10);
        //        DebugConsole.WriteLine($"request -> {uriString}");
        //        HttpResponseMessage httpResponse = await client.GetAsync(uriString);
        //        string responseBody = await httpResponse.Content.ReadAsStringAsync();
        //        string errorTitle = responseBody.SelectNode("//title", HtmlType.InnerText);
        //        if (string.IsNullOrWhiteSpace(errorTitle) == false)
        //        {
        //            bool isContains = errorTitle.Contains("에러 페이지");
        //            if (isContains)
        //            {
        //                throw new Exception("네이버 차단 발생, 잠시 후 다시 시도해주세요.");
        //            }
        //        }
        //        return responseBody;
        //    }
        //}

        //public async void SetLogisticsMenuIdsAsync(string buildId)
        //{
        //    if (this.MainMeunItems != null && this.MainMeunItems.Count > 0)
        //    {
        //        return;
        //    }
        //    string uriString = $"https://shopping.naver.com/_next/data/{buildId}/ns/home/logistics.json?client=BROWSER&deviceType=PC";
        //    string json = await GetHtml(uriString);
        //    JObject jObject = JObject.Parse(json);
        //    JToken? layout = jObject["pageProps"]["initialApolloState"]["NsHomeLayout:6735f042fb502a31c7d4726c"]["layout"];
        //    List<JToken> logisticsCategoryMenuItems = layout["logisticsCategoryMenuItems"]?.ToList();
        //    var menuItems = new List<string>();
        //    foreach (JToken logisticsCategoryMenuItem in logisticsCategoryMenuItems)
        //    {
        //        JToken? __ref = logisticsCategoryMenuItem["__ref"];
        //        string value = __ref.ToString();
        //        string menuItem = value.Split(':').Last();
        //        menuItems.Add(menuItem);
        //    }
        //    this.MainMeunItems = menuItems;
        //}

        //private string GetLogisticsCategoryPageDataUriString(string uriString, int page)
        //{
        //    //string variables = GetVariables(uriString, page);
        //    var fetchPagedLogisticsProductsBody = new FetchPagedLogisticsProductsBody(uriString, page);
        //    string variables = fetchPagedLogisticsProductsBody.ToJson();
        //    variables = HttpUtility.UrlEncode(variables);

        //    var queryBuilder = new List<string>();
        //    queryBuilder.Add($"operationName=FetchPagedLogisticsProducts&");
        //    queryBuilder.Add($"variables={variables}&");
        //    string sha256Hash = "f1a6e6ebb5c0f0ae8cf5810803729556805ff4dcd92b67f43441ee94789d8fc7";
        //    sha256Hash = "5eb390faae78a17aa82a119741f3966c4d7f0b0115dfb0b0aa3741626ad9c960";
        //    queryBuilder.Add($"extensions=%7B%22persistedQuery%22%3A%7B%22version%22%3A1%2C%22sha256Hash%22%3A%22{sha256Hash}%22%7D%7D");
        //    var query = string.Join('&', queryBuilder);

        //    var uriBuilder = new StringBuilder();
        //    uriBuilder.Append("https://shopping.naver.com/responsive/api/shopv/graphql?");
        //    uriBuilder.Append(query);
        //    var requestUriString = uriBuilder.ToString();
        //    return requestUriString;
        //}

        //private List<string> GetLogisticsCategoryProductCodesAsync(string json)
        //{
        //    List<string> result = null;
        //    try
        //    {
        //        JObject data = JObject.Parse(json);
        //        List<JToken> products = data["data"]["pagedLogisticsProducts"]["products"].ToList();
        //        List<string> productIds = products.Select(x => x["productId"].ToString()).ToList();
        //        result = productIds;
        //    }
        //    catch (Exception exception)
        //    {
        //        Console.WriteLine(exception.Message);
        //    }
        //    return result;
        //}
        #endregion

        public string GetLogisticsCategoryProductUriString(string code)
        {
            string uriString = $"https://shopping.naver.com/shopv/vertical-detail/v1/logistics/products/{code}/detail?_nc_={_nc_}";
            return uriString;
        }

        public string GetLogisticsCategoryProductPageUriString(string uriString, int page, string script2763, string script7257)
        {
            //if (string.IsNullOrWhiteSpace(script2763)) script2763 = GetScriptFile(uriString, "2763");
            //if (string.IsNullOrWhiteSpace(script7257)) script7257 = GetScriptFile(uriString, "7257");
            //if (string.IsNullOrWhiteSpace(script2763) || string.IsNullOrWhiteSpace(script2763)) throw new Exception("스크립트를 찾을 수 없습니다");
            //string? groupProductFields = GetGraphQLQuery(script2763, "fragment GroupProductFields on GroupProductResponse", "\"]))),")?.Trim();
            //string? productDeliveryInfo = GetGraphQLQuery(script7257, "fragment ProductDeliveryInfoFields on ProductDeliveryInfo", "\"])))")?.Trim();
            //string? logisticsProductFields = GetGraphQLQuery(script2763, "fragment LogisticsProductFields on LogisticsProduct", "\\n\"])),v")?.Trim();
            //string? fetchGroupProducts = GetGraphQLQuery(script2763, "query FetchGroupProducts", "\\n\"])),v")?.Trim();
            //string? scriptString = script2763.SingleSplit("query FetchPagedLogisticsProducts", "\\n\"])),P)", TextEx.SplitOption.StartWord)?.Trim();
            //string param = scriptString.SingleSplit("(", ")", TextEx.SplitOption.None);
            //List<string> paramValues = param.Split("\\n").ToList();
            //paramValues.RemoveAll(x => string.IsNullOrWhiteSpace(x));
            //for (int i = 0; i < paramValues.Count; i++)
            //{
            //    paramValues[i] = paramValues[i].Trim();
            //}
            //string newParam = string.Join(", ", paramValues);
            //scriptString = scriptString.Replace(param, newParam);
            ////string? fetchPagedLogisticsProducts = GetGraphQLQuery(scriptString)?.Trim();
            //string? fetchPagedLogisticsProducts = GetGraphQLQuery(scriptString);

            //string sha256Hash = GetSha256Hash(groupProductFields, productDeliveryInfo, logisticsProductFields, fetchPagedLogisticsProducts);
            //string sha256Hash = "d6d5d8c116045945e132d1e542328b0aecfcd5e5220bdac376925d660aacf89c";
            string sha256Hash = "256fb8c306c84366c9be9fc39d7e82ced73ea40792ca5e7243e36e3e779cc094";

            var fetchPagedLogisticsProductsBody = new FetchPagedLogisticsProductsBody(uriString, page);
            string variables = fetchPagedLogisticsProductsBody.ToJson();
            variables = HttpUtility.UrlEncode(variables);

            var queryBuilder = new List<string>();
            queryBuilder.Add($"operationName=FetchPagedLogisticsProducts");
            queryBuilder.Add($"variables={variables}");
            queryBuilder.Add($"extensions=%7B%22persistedQuery%22%3A%7B%22version%22%3A1%2C%22sha256Hash%22%3A%22{sha256Hash}%22%7D%7D");
            string query = string.Join('&', queryBuilder);

            var uriBuilder = new StringBuilder();
            uriBuilder.Append("https://shopping.naver.com/responsive/api/shopv/graphql?");
            uriBuilder.Append(query);
            var requestUriString = uriBuilder.ToString();
            return requestUriString;
        }

        private async Task<string> GetVerticalDetailHtmlAsync(string connectionId, string code, JToken contents)
        {
            try
            {
                string productNo = contents["productNo"].ToString();
                string address = $"https://shopping.naver.com/shopv/vertical-detail/v1/products/{code}/contents/{productNo}/PC?_nc_=&usedImageProxy=true";
                string json = await ChatHubService.GetRequest(connectionId, address);
                var jObject = JObject.Parse(json);
                JToken? renderContent = jObject["renderContent"];
                if (renderContent != null)
                {
                    return renderContent.ToString();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exception)
            {
                throw new Exception("상세 페이지를 찾을 수 없습니다.", exception);
            }
        }
        public Product GetLogisticsCategoryProduct(string code, string json, out JObject data)
        {
            try
            {
                data = JObject.Parse(json);
                var product = new Product();
                product.site = this.Site;
                product.title = GetTitie(data);
                List<JToken> viewAttributes = data["viewAttributes"]?.Children().ToList();
                JToken? productInfoProvidedNoticeView = data["productInfoProvidedNoticeView"];
                List<JToken> basic = productInfoProvidedNoticeView["basic"]?.Children().ToList();
                List<JToken> additional = productInfoProvidedNoticeView["additional"]?.Children().ToList();
                List<JToken> detailAttributes = data["detailAttributes"]?.Children().ToList();
                List<JToken> descTokens = new List<JToken>();
                product.descs = GetDesc(viewAttributes, basic, additional, detailAttributes);
                product.code = code;
                product.currency = "KRW";
                product.tags = GetTags(data);
                product.price = GetPrices(data);
                product.url = $"https://shopping.naver.com/logistics/products/{code}";
                product.mainImages = GetMainImages(data);
                product.videoUrl = null; // url 형식이 아님
                product.originalDetailPageUrls = null;
                product.translateDetailPageUrls = null;
                // product.detailPageHtml = GetVerticalDetailHtmlAsync(code, data).redsult;
                product.option = GetOption(data);
                product.groupName = null;
                product.workNumber = null;
                product.memo = null;
                product.regDate = DateTime.Now;
                return product;
            }
            catch (Exception exception)
            {
                exception.Data.Add("code", code);
                throw exception;
            }
        }

        public string GetVerticalDetailPageUriString(string code, JObject data)
        {
            string productNo = data["productNo"].ToString();
            string address = $"https://shopping.naver.com/shopv/vertical-detail/v1/products/{code}/contents/{productNo}/PC?_nc_=&usedImageProxy=true";
            return address;
        }
    }
}
