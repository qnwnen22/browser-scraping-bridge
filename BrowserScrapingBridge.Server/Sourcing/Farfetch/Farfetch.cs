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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Farfetch
{
    public class Farfetch : ICollect
    {
        public string Site { get; } = nameof(Farfetch);
        public string Name { get; } = "파페치";
        public Language Language => Language.EN;
        public Currency Currency { get; private set; } = Currency.EUR;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        #region Common
        public void CheckBlocking(string html)
        {
            string h1 = html.GetValue("//h1", TextEx.XPathOption.InnerText);
            var titles = new string[]
            {
                "429 Too Many Requests",
                "Access Denied"
            }.ToList();

            if (titles.Exists(x => x == h1))
            {
                string msg = html.GetSelectNodeInnerText("//body").Trim();
                throw new CaptchaException(msg);
            }
        }
        private string GetScript(string html)
        {
            List<string> scripts = html.GetValues("//script", TextEx.XPathOption.InnerHtml);
            if (scripts == null || scripts.Count <= 0) throw new CollectException();
            string? script = scripts.Find(x => x.IndexOf("window.__HYDRATION_STATE__") >= 0);
            if (string.IsNullOrWhiteSpace(script)) return null;
            script = script.SubstringBetween("{", "}");
            script = Regex.Unescape(script);
            return script;
        }
        private List<JToken> GetJTokens(JToken jToken, string containsKey)
        {
            return jToken.Children().ToList().FindAll(x => x.Path.Contains(containsKey));
        }
        public async Task<string> GetHtmlAsync(string url)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Connection", "keep-alive");
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            headers.Add("Accept-Language", "ko,en-US;q=0.9,en;q=0.8,ko-KR;q=0.7");

            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                Proxy = Proxy
            };

            using (var httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler))
            {
                httpClient.SetHeader(headers);

                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url);
                string readAsString = await httpResponseMessage.Content.ReadAsStringAsync();
                return readAsString;
            }
        }
        #endregion
        public string GetCode(string url)
        {
            string splitLast = url.Split("-").Last();
            string code = Path.GetFileNameWithoutExtension(splitLast);
            return code;
        }
        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }
        private CollectPageInfo GetCollectPageInfo(JToken jToken)
        {
            string? path = jToken["resourceIdentifier"]?["path"]?.ToString();
            if (string.IsNullOrWhiteSpace(path)) { return null; }
            var code = GetCode(path);

            string? shortDescription = jToken.Value<string>("shortDescription");

            return new CollectPageInfo
            {
                Url = $"https://www.farfetch.com/nl{path}",
                Title = shortDescription,
                Code = code
            };
        }
        public List<CollectPageInfo> GetCollectPageInfos(string html)
        {
            List<CollectPageInfo> collectPageInfos = new List<CollectPageInfo>();
            try
            {
                CheckBlocking(html);

                string script = GetScript(html);

                JObject jObject = script.ToJObject();
                if (jObject == null) throw new CollectException();

                JToken? apolloInitialState = jObject["apolloInitialState"];

                List<JToken> productCatalogItems = apolloInitialState.Children().ToList().FindAll(x => x.Path.Contains("ProductCatalogItem:")).ToList();
                if (productCatalogItems == null || productCatalogItems.Count <= 0)
                {
                    JToken? root_query = apolloInitialState["ROOT_QUERY"];
                    if (root_query == null) throw new CollectException();

                    JToken? productCatalog = root_query.Children().FirstOrDefault(x => x.Path.Contains("productCatalog:"));
                    if (productCatalog == null) throw new CollectException();

                    JToken? edges = productCatalog.First()["edges"];
                    if (edges == null) throw new CollectException();

                    foreach (JToken edge in edges)
                    {
                        JToken? node = edge["node"];
                        CollectPageInfo getCollectPageInfo = GetCollectPageInfo(node);
                        if (getCollectPageInfo != null)
                        {
                            collectPageInfos.Add(getCollectPageInfo);
                        }
                    }
                }
                else
                {
                    foreach (JToken productCatalogItem in productCatalogItems)
                    {
                        CollectPageInfo getCollectPageInfo = GetCollectPageInfo(productCatalogItem.First());
                        if (getCollectPageInfo != null)
                        {
                            collectPageInfos.Add(getCollectPageInfo);
                        }
                    }
                }
                return collectPageInfos;
            }
            catch (Exception exception)
            {
                throw new CollectException("페이지 정보를 조회할 수 없습니다. URL을 확인해주세요.", exception);
            }
        }
        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("page", page.ToString());
            string pageUrl = uriBuilderAdvance.ToString();
            string html = await GetHtmlAsync(pageUrl);
            List<CollectPageInfo> collectPageInfos = GetCollectPageInfos(html);
            return collectPageInfos;
        }
        public List<Name> GetTitle(JToken productGroup)
        {
            try
            {
                string? name = productGroup.Value<string>("name");
                if (string.IsNullOrWhiteSpace(name)) throw new CollectException("상품명을 찾을 수 없습니다.");
                List<Name> title = new List<Name> { new Name(this.Language, name) };
                return title;
            }
            catch (Exception exception)
            {
                throw new CollectException("상품명 수집 중 문제가 발생했습니다.");
            }
        }
        public List<Name> GetBrand(JToken productGroup)
        {
            string? brand = productGroup["brand"]?.Value<string>("name");
            brand = WebUtility.HtmlDecode(brand);

            if (string.IsNullOrWhiteSpace(brand)) throw new CollectException("브랜드명을 찾을 수 없습니다.");
            return new List<Name> { new Name(this.Language, brand) };
        }

        private string GetModel(JObject productGroup)
        {
            var productGroupID = productGroup.Value<string>("productGroupID");
            return productGroupID;
        }

        public List<Desc> GetDescs(JToken data)
        {
            return new List<Desc>();
        }
        public List<Tag> GetTags(JToken data)
        {
            return new List<Tag>();
        }
        public List<Price> GetPrice(JToken data)
        {
            return null;
        }
        public List<string> GetMainImages(JToken productGroup)
        {
            try
            {
                var mainImages = new List<string>();

                List<JToken> images = productGroup["image"].ToList();
                foreach (var image in images)
                {
                    string? contentUrl = image.Value<string>("contentUrl");
                    mainImages.Add(contentUrl);
                }
                return mainImages;
            }
            catch (Exception exception)
            {
                throw new CollectException("상품 이미지를 찾을 수 없습니다.");
            }
        }
        public string GetVideoUrl(JToken data) { return null; }
        public List<string> GetOriginalDetailPageUrls(JToken data) { return null; }
        public List<string> GetTranslateDetailPageUrls(JToken data) { return null; }
        public async Task<string> GetDetailPageJson(string referer, string productId, string merchantId, WebProxy proxy = null)
        {
            #region Request Row
            // POST https://www.farfetch.com/nl/experience-gateway HTTP/1.1
            // Host: www.farfetch.com
            // Connection: keep-alive
            // Content-Length: 4553
            // accept-language: en-GB
            // accept: */*
            // content-type: application/json
            // Origin: https://www.farfetch.com
            // Referer: https://www.farfetch.com/nl/shopping/women/mm6-maison-margiela-twist-jeans-item-28925192.aspx?storeid=12425
            // Accept-Encoding: gzip, deflate, br, zstd            // 
            // {
            // 	"operationName": "SizeAndFitData",
            // 	"variables": {
            // 		"productId": "28925192",
            // 		"merchantId": "12425"
            // 	},
            // 	"query": "query SizeAndFitData($productId: ID!, $merchantId: ID) {\n  user {\n    id\n    preference {\n      unitSystem\n      __typename\n    }\n    __typename\n  }\n  product(id: $productId, merchantId: $merchantId) {\n    ... on Product {\n      id\n      scale {\n        id\n        description\n        abbreviation\n        isOneSize\n        __typename\n      }\n      variations {\n        edges {\n          node {\n            ... on Variation {\n              id\n              price {\n                value {\n                  raw\n                  formatted\n                  __typename\n                }\n                __typename\n              }\n              images {\n                order\n                size1000 {\n                  url\n                  alt\n                  __typename\n                }\n                modelVariationSize\n                modelMeasurement {\n                  height {\n                    type\n                    name\n                    imperial {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    metric {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    __typename\n                  }\n                  bodyMeasurements {\n                    type\n                    name\n                    imperial {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    metric {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    __typename\n                  }\n                  __typename\n                }\n                __typename\n              }\n              fitting {\n                type\n                description\n                __typename\n              }\n              measurements {\n                type\n                name\n                imperial {\n                  raw\n                  formatted\n                  __typename\n                }\n                metric {\n                  raw\n                  formatted\n                  __typename\n                }\n                __typename\n              }\n              variationProperties {\n                ... on ScaledSizeVariationProperty {\n                  order\n                  values {\n                    id\n                    order\n                    description\n                    scale {\n                      id\n                      description\n                      __typename\n                    }\n                    __typename\n                  }\n                  __typename\n                }\n                __typename\n              }\n              __typename\n            }\n            ... on VariationUnavailable {\n              product {\n                id\n                __typename\n              }\n              images {\n                order\n                size1000 {\n                  url\n                  alt\n                  __typename\n                }\n                modelVariationSize\n                modelMeasurement {\n                  height {\n                    type\n                    name\n                    imperial {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    metric {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    __typename\n                  }\n                  bodyMeasurements {\n                    type\n                    name\n                    imperial {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    metric {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    __typename\n                  }\n                  __typename\n                }\n                __typename\n              }\n              __typename\n            }\n            __typename\n          }\n          __typename\n        }\n        __typename\n      }\n      __typename\n    }\n    __typename\n  }\n}\n"
            // } 
            #endregion

            // 현재 브라우저 프로필의 쿠키를 사용합니다.
            CookieContainer cookieContainer = new CookieContainer();

            ExperienceGateway experienceGateway = new ExperienceGateway("28925192", "12425");
            var postString = experienceGateway.ToPostString();
            StringContent stringContent = new StringContent(postString, Encoding.UTF8, "application/json");
            var headers = new Dictionary<string, string>();
            headers.Add("Connection", "keep-alive");
            headers.Add("accept-language", "en-GB");
            headers.Add("accept", "*/*");
            headers.Add("Origin", "https://www.farfetch.com");
            headers.Add("Referer", referer);
            headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");

            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                CookieContainer = cookieContainer,
                Proxy = proxy
            };

            using (HttpClient httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler))
            {
                httpClient.SetHeader(headers);
                HttpResponseMessage httpResponseMessage = await httpClient.PostAsync("https://www.farfetch.com/nl/experience-gateway", stringContent);
                string json = await httpResponseMessage.Content.ReadAsStringAsync();
                return json;
            }
        }
        private void SetDic(JToken data, Dictionary<string, string> dictionary)
        {
            if (data == null) return;
            string? name = data.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name)) return;

            var valueList = new List<string>();

            string? imperial = data["imperial"]?["formatted"]?.ToString();
            if (string.IsNullOrWhiteSpace(imperial) == false)
            {
                valueList.Add(imperial);
            }

            string? metric = data["metric"]?["formatted"]?.ToString();
            if (string.IsNullOrWhiteSpace(metric) == false)
            {
                valueList.Add(metric);
            }

            if (valueList.Count == 0) return;
            string value = string.Join(" / ", valueList);
            if (string.IsNullOrWhiteSpace(value)) return;

            bool isTry = dictionary.TryAdd(name, value);
            if (isTry == false)
            {
                string getValue = dictionary[name];
                if (getValue != value)
                {
                    dictionary[name] += $", {value}";
                }
            }
        }
        /// <summary>
        /// 완성된 HTML 가져올 수 없기 때문에 직접 구현
        /// </summary>
        /// <param name="apolloInitialState"></param>
        /// <returns></returns>
        public string GetDetailPageHtml(JToken apolloInitialState)
        {
            try
            {
                var htmlBuilder = new StringBuilder();
                JToken? variation = GetJTokens(apolloInitialState, "Variation:")?.First()?.First();
                if (variation == null) return null;

                htmlBuilder.AppendLine("<p>THE DETAILS</p>");

                JToken? highlight = variation["description"]?["highlight"];
                if (highlight != null)
                {
                    JToken? richTextContent = highlight["richTextContent"];
                    if (richTextContent != null && richTextContent.HasValues)
                    {
                        JToken? richTextContentFirst = richTextContent?.ToList()?.First();
                        List<JToken>? blocks = richTextContentFirst["blocks"]?.ToList();
                        if (blocks != null && blocks.Count > 0)
                        {
                            var lis = new List<string>();
                            foreach (JToken block in blocks)
                            {
                                List<JToken>? items = block["items"]?.ToList();
                                if (items == null) continue;
                                foreach (JToken item in items)
                                {
                                    string? value = item["value"]?.ToString();
                                    if (string.IsNullOrWhiteSpace(value)) continue;
                                    lis.Add(value);
                                }
                            }

                            if (lis.Count > 0)
                            {
                                htmlBuilder.AppendLine("<div>");
                                htmlBuilder.AppendLine("<h4>Highlights</h4>");
                                htmlBuilder.AppendLine("<ul>");
                                foreach (string li in lis)
                                {
                                    htmlBuilder.AppendLine($"<li>{li}</li>");
                                }
                                htmlBuilder.AppendLine("</ul>");
                                htmlBuilder.AppendLine("</div>");
                            }
                        }
                    }
                }

                JToken? composition = variation["composition"];
                if (composition != null)
                {
                    List<JToken>? parts = composition["parts"]?.ToList();
                    if (parts != null && parts.Count > 0)
                    {
                        var dictionary = new Dictionary<string, string>();
                        foreach (JToken part in parts)
                        {
                            string? name = part["name"]?.ToString();
                            if (string.IsNullOrWhiteSpace(name)) continue;
                            JToken? material = part["materials"]?.ToList()?.First();
                            if (material == null) continue;
                            string? materialName = material["name"]?.ToString();
                            if (string.IsNullOrWhiteSpace(materialName)) continue;
                            string? percentage = material["percentage"]?["formatted"].ToString();
                            if (string.IsNullOrWhiteSpace(percentage)) continue;

                            dictionary.Add(name, $"{materialName} {percentage}");
                        }
                        if (dictionary.Count > 0)
                        {
                            htmlBuilder.AppendLine("<div>");
                            htmlBuilder.AppendLine("<h4>Composition</h4>");
                            foreach (KeyValuePair<string, string> item in dictionary)
                            {
                                string innerText = $"{item.Key}: {item.Value}";
                                htmlBuilder.AppendLine($"<p>{innerText}</p>");
                            }
                            htmlBuilder.AppendLine("</div>");
                        }
                    }
                }

                JToken? images = variation["images"];
                if (images != null)
                {
                    var dictionary = new Dictionary<string, string>();
                    JEnumerable<JToken> imagesChildren = images.Children();
                    foreach (JToken imagesChild in imagesChildren)
                    {
                        JToken? modelMeasurement = imagesChild["modelMeasurement"];
                        if (modelMeasurement != null && modelMeasurement.HasValues)
                        {
                            JToken? height = modelMeasurement["height"];
                            if (height == null) continue;
                            SetDic(height, dictionary);

                            List<JToken>? bodyMeasurements = modelMeasurement["bodyMeasurements"]?.ToList();
                            if (bodyMeasurements != null && bodyMeasurements.Count > 0)
                            {
                                foreach (JToken bodyMeasurement in bodyMeasurements)
                                {
                                    SetDic(bodyMeasurement, dictionary);
                                }
                            }
                        }
                    }

                    if (dictionary.Count > 0)
                    {
                        htmlBuilder.AppendLine("<div>");
                        htmlBuilder.AppendLine("<h4>Model Measurements</h4>");
                        htmlBuilder.AppendLine("<table>");
                        htmlBuilder.AppendLine("<tbody>");
                        foreach (KeyValuePair<string, string> item in dictionary)
                        {
                            htmlBuilder.AppendLine($"<tr>");
                            htmlBuilder.AppendLine($"<td>{item.Key}</td>");
                            htmlBuilder.AppendLine($"<td>{item.Value}</td>");
                            htmlBuilder.AppendLine($"</tr>");
                        }
                        htmlBuilder.AppendLine("</tbody>");
                        htmlBuilder.AppendLine("</table>");
                        htmlBuilder.AppendLine("</div>");
                    }
                }
                return htmlBuilder.ToString();
            }
            catch (Exception exception)
            {
                DebugConsole.WriteLine(exception?.Message);
                DebugConsole.WriteLine(exception?.StackTrace);
                return null;
            }
        }
        public Option GetOption(JToken data, string isoCode)
        {
            try
            {
                Option option = new Option();
                List<JToken> scaledSizeVariationPropertyValue = GetJTokens(data, "ScaledSizeVariationPropertyValue:");
                if (scaledSizeVariationPropertyValue == null && scaledSizeVariationPropertyValue.Count <= 0) return null;

                option.optionGroups = new List<Option.OptionGroup>();

                var optionGroup = new Option.OptionGroup();
                optionGroup.optionId = "0";
                optionGroup.groupName = new List<Name> { new Name(this.Language, "Size") };
                optionGroup.groups = new List<Option.OptionGroup.Group>();
                for (int i = 0; i < scaledSizeVariationPropertyValue.Count; i++)
                {
                    JToken valueToken = scaledSizeVariationPropertyValue[i].First();
                    if (valueToken == null) continue;

                    string? description = valueToken.Value<string>("description");
                    if (string.IsNullOrWhiteSpace(description)) continue;

                    var group = new Option.OptionGroup.Group();
                    group.groupId = i.ToString();
                    group.optionName = new List<Name>() { new Name(this.Language, description) };
                    optionGroup.groups.Add(group);
                }
                option.optionGroups.Add(optionGroup);


                option.detailOptions = new List<Option.DetailOption>();
                for (int i = 0; i < scaledSizeVariationPropertyValue.Count; i++)
                {
                    Option.DetailOption detailOption = new Option.DetailOption();
                    detailOption.optionIds = i.ToString();
                    foreach (var groupName in optionGroup.groupName)
                    {
                        if (detailOption.optionGroupName == null)
                        {
                            detailOption.optionGroupName = new List<Name>();
                        }
                        detailOption.optionGroupName.Add(new Name(groupName.language, groupName.text));
                    }

                    JToken valueToken = scaledSizeVariationPropertyValue[i].First();
                    string? description = valueToken.Value<string>("description");
                    if (string.IsNullOrWhiteSpace(description)) continue;
                    detailOption.optionName = new List<Name> { new Name(Language.EN, description) };

                    JToken? availableIn = valueToken["availableIn"]?.First();
                    string? __ref = availableIn?.Value<string>("__ref");
                    if (string.IsNullOrWhiteSpace(__ref)) continue;
                    JToken? variation = data[__ref];
                    if (variation == null) continue;

                    double? originalPrice = variation["productPrice"]?["full"]?["value"]?.Value<double>("raw");
                    if (originalPrice == null || originalPrice <= 0) continue;

                    double salePrice = (double)originalPrice;
                    JToken? salePriceRaw = variation["productPrice"]?["final"]?["value"]?["raw"];
                    if (salePriceRaw != null)
                    {
                        salePrice = salePriceRaw.ToObject<double>();
                    }

                    var price = new Price(isoCode, (double)originalPrice, (double)salePrice)
                    {
                        isoCode = isoCode
                    };
                    detailOption.price = new List<Price> { price };

                    JToken? quantity = variation["quantity"];
                    if (quantity != null)
                    {
                        detailOption.quantity = quantity.ToObject<int>();
                    }
                    detailOption.isSoldOut = detailOption.quantity <= 0;

                    JToken? image = variation["images"]?.First();
                    List<JToken> imageSizes = GetJTokens(image, "size");
                    if (imageSizes != null && imageSizes.Count > 0)
                    {
                        JToken imageSize = imageSizes.Last();
                        if (imageSize != null)
                        {
                            string? imageUrl = imageSize.First()?.Value<string>("url");
                            detailOption.optionImage = imageUrl;
                        }
                    }
                    option.detailOptions.Add(detailOption);
                }
                if (option.detailOptions.Count <= 0) throw new CollectException("재고가 없는 상품입니다.");

                return option;
            }
            catch (CollectException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new CollectException("옵션 수집 과정에서 문제가 발생했습니다.", exception);
            }
        }
        public Product GetProduct(string url, string html)
        {
            var product = new Product();
            CheckBlocking(html);
            try
            {
                product.site = "Farfetch";
                product.url = url;
                string script = GetScript(html);
                JObject data = script.ToJObject();
                if (data == null) throw new CollectException("상품 정보를 확인할 수 없습니다.");
                JToken? apolloInitialState = data["apolloInitialState"];

                JObject productGroup = null;
                List<string> scripts = html.GetValues("//main[@id='content']//script[@type='application/ld+json']", TextEx.XPathOption.InnerHtml);
                for (int i = 0; i < scripts.Count; i++)
                {
                    JObject jObject = scripts[i].ToJObject();
                    if (jObject == null) continue;
                    string? @type = jObject.Value<string>("@type");
                    if (string.IsNullOrWhiteSpace(@type)) continue;
                    if (@type == "ProductGroup")
                    {
                        productGroup = jObject;
                        break;
                    }
                }
                JToken currency = GetJTokens(apolloInitialState, "Currency:").First().First();
                string? isoCode = currency["isoCode"]?.ToString();
                this.Currency = isoCode.ConvertEnum<Currency>();
                product.currency = isoCode;
                if (isoCode != "EUR") throw new Exception($"{isoCode} 상품 입니다.");
                product.title = GetTitle(productGroup);
                product.brand = GetBrand(productGroup);
                product.model = GetModel(productGroup);
                product.descs = GetDescs(null);
                product.code = GetCode(url);
                product.tags = GetTags(null);
                product.price = GetPrice(null);
                product.mainImages = GetMainImages(productGroup);
                product.videoUrl = GetVideoUrl(null);
                product.originalDetailPageUrls = GetOriginalDetailPageUrls(null);
                product.translateDetailPageUrls = GetTranslateDetailPageUrls(null);
                product.detailPageHtml = GetDetailPageHtml(apolloInitialState);
                product.option = GetOption(apolloInitialState, isoCode);
                if (product.option != null)
                {
                    product.price = ProductHelper.GetMinPriceInDetailOption(product.option.detailOptions, isoCode);
                }
                else if (product.option.detailOptions == null || product.option.detailOptions.Count <= 0)
                {
                    throw new CollectException("재고가 없는 상품입니다.");
                }
                if (product.price == null) throw new CollectException("상품 가격을 확인할 수 없습니다");
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
            string html = await GetHtmlAsync(url);
            //string html = await ChatHubService.GetRequest(connectionId, url);
            return GetProduct(url, html);
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