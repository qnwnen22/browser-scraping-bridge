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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Ikea
{
    public class Ikea : CollectBase
    {
        public override string Site => nameof(Ikea);
        public override string Name => "이케아";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = false;

        private const string SITE_BASE = "https://www.ikea.com/kr/ko";

        public override string GetCode(string url)
        {
            // https://www.ikea.com/kr/ko/p/billy-bookcase-white-00522047/
            Uri uri = new Uri(url);
            string lastSegment = uri.AbsolutePath.TrimEnd('/').Split('/').Last();
            // 상품코드는 슬러그 마지막 '-' 뒤 8자리 숫자
            var m = Regex.Match(lastSegment, @"(\d{8})$");
            if (m.Success) return m.Groups[1].Value;
            return lastSegment;
        }

        public override string CreateProductUrl(string code)
        {
            return $"{SITE_BASE}/p/-{code}/";
        }

        public override Task<string> GetLoginUrlAsync(string connectionId)
        {
            return Task.FromResult<string>(null);
        }

        #region 카테고리 수집

        private static readonly HttpClient _httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient();

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            // IKEA API는 page 파라미터 미지원 → 첫 호출에서만 수집
            if (page > 1) return null;

            // 카테고리 URL에서 ID 추출: cabinets-cupboards-st003 → st003
            string categoryId = ExtractCategoryId(url);
            if (string.IsNullOrWhiteSpace(categoryId)) return null;

            // IKEA 상품 목록 API 직접 호출
            string apiUrl = $"https://sik.search.blue.cdtapps.com/kr/ko/product-list-page?category={categoryId}&size=100";
            string json = await _httpClient.GetStringAsync(apiUrl);
            if (string.IsNullOrWhiteSpace(json)) return null;

            var root = JObject.Parse(json);
            var items = root.SelectToken("productListPage.productWindow") as JArray;
            if (items == null || !items.Any()) return null;

            var result = new List<CollectPageInfo>();
            foreach (JToken item in items)
            {
                string pipUrl = item.Value<string>("pipUrl");
                string name = item.Value<string>("name");
                if (string.IsNullOrWhiteSpace(pipUrl)) continue;

                string code = GetCode(pipUrl);
                result.Add(new CollectPageInfo
                {
                    Code = code,
                    Url = pipUrl,
                    Title = name
                });
            }

            return result.Count > 0 ? result : null;
        }

        private string ExtractCategoryId(string url)
        {
            Uri uri = new Uri(url);
            string lastSegment = uri.AbsolutePath.TrimEnd('/').Split('/').Last();
            // cabinets-cupboards-st003 → st003, syelnab-10382 → 10382
            int lastHyphen = lastSegment.LastIndexOf('-');
            if (lastHyphen >= 0) return lastSegment.Substring(lastHyphen + 1);
            return lastSegment;
        }

        #endregion

        #region 상품 수집

        public override void ThrowIfCaptcha(string url, string html) { }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
            throw new NotImplementedException();
        }

        public override Task<JToken> GetData(string connectionId, string url, string html)
        {
            JObject pageProps = ExtractPageProps(html);
            return Task.FromResult<JToken>(pageProps);
        }

        /// <summary>
        /// text/hydrate 스크립트에서 pageProps 추출
        /// </summary>
        private JObject ExtractPageProps(string html)
        {
            var matches = Regex.Matches(html, @"<script\s+type=""text/hydrate"">([\s\S]*?)</script>");
            foreach (Match match in matches)
            {
                try
                {
                    var obj = JObject.Parse(match.Groups[1].Value);
                    if (obj["pageProps"]?["product"] != null)
                        return obj["pageProps"] as JObject;
                }
                catch { }
            }
            return null;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            var pageProps = data as JObject;
            if (pageProps == null)
                throw new Exception("상품 데이터를 찾을 수 없습니다.");
            var product = pageProps["product"];
            string name = product.Value<string>("name");
            string desc = product.Value<string>("description");
            string measure = product.Value<string>("itemMeasureReferenceText");

            // "BILLY 빌리" + "책장, 화이트" + "80x28x202 cm"
            string fullTitle = name;
            if (!string.IsNullOrWhiteSpace(desc))
                fullTitle += " " + desc;
            if (!string.IsNullOrWhiteSpace(measure))
                fullTitle += " " + measure;

            if (string.IsNullOrWhiteSpace(fullTitle))
                throw new Exception("상품명을 찾을 수 없습니다.");

            return new List<Name> { new Name(Language.KR, fullTitle.Trim()) };
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            return new List<Name> { new Name(Language.KR, "IKEA") };
        }

        public override string GetModel(string html, JToken data) => null;

        public override List<Price> GetPrice(string html, JToken data)
        {
            var pageProps = data as JObject;
            var product = pageProps["product"];
            double price = product.Value<double?>("price") ?? 0;
            if (price <= 0)
                throw new Exception("상품 가격을 찾을 수 없습니다.");

            // pipPriceModuleProps에서 정가(highPrice) 확인
            double originalPrice = price;
            var priceModule = pageProps["pipPriceModuleProps"]?["priceModuleProps"];
            if (priceModule?["previousPriceProps"] != null)
            {
                string prevPriceStr = priceModule["previousPriceProps"]?.Value<string>("integerValue");
                if (!string.IsNullOrWhiteSpace(prevPriceStr))
                {
                    if (double.TryParse(prevPriceStr.Replace(",", ""), out double prevPrice) && prevPrice > price)
                        originalPrice = prevPrice;
                }
            }

            return new List<Price> { new Price(Currency.KRW, originalPrice, price, true) };
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var pageProps = data as JObject;
            var product = pageProps["product"];
            var mediaList = product["mediaList"] as JArray;
            if (mediaList == null) return null;

            var images = new List<string>();
            foreach (JToken media in mediaList)
            {
                if (media.Value<string>("type") != "image") continue;
                string url = media["content"]?.Value<string>("url");
                if (!string.IsNullOrWhiteSpace(url))
                    images.Add(url);
            }

            return images.Count > 0 ? images : null;
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override List<Desc> GetDescs(string html, JToken data)
        {
            var pageProps = data as JObject;
            var product = pageProps["product"];
            var descs = new List<Desc>();
            int order = 1;

            // 시리즈
            string series = product["series"]?.Value<string>("name");
            if (!string.IsNullOrWhiteSpace(series))
                descs.Add(new Desc(order++, Language.KR, "시리즈", series));

            // 치수
            string measure = product.Value<string>("itemMeasureReferenceText");
            if (!string.IsNullOrWhiteSpace(measure))
                descs.Add(new Desc(order++, Language.KR, "치수", measure));

            // 제품번호
            string visibleItemNo = product.Value<string>("visibleItemNo");
            if (!string.IsNullOrWhiteSpace(visibleItemNo))
                descs.Add(new Desc(order++, Language.KR, "제품번호", visibleItemNo));

            // 소재 (productInformationSectionProps)
            var infoSection = pageProps["productInformationSectionProps"];
            var materials = infoSection?["productDetailsProps"]?["accordionObject"]?["materialsAndCare"]?["contentProps"]?["materials"];
            if (materials is JArray matArr && matArr.Count > 0)
            {
                var matList = matArr[0]?["materials"] as JArray;
                if (matList != null)
                {
                    var parts = new List<string>();
                    foreach (JToken mat in matList)
                    {
                        string part = mat.Value<string>("part");
                        string material = mat.Value<string>("material");
                        if (!string.IsNullOrWhiteSpace(material))
                            parts.Add($"{part} {material}".Trim());
                    }
                    if (parts.Count > 0)
                        descs.Add(new Desc(order++, Language.KR, "소재", string.Join(" / ", parts)));
                }
            }

            // 패키지 무게
            var packaging = product["packaging"]?["packages"]?.FirstOrDefault();
            if (packaging != null)
            {
                var measurements = packaging["measurements"]?.FirstOrDefault() as JArray;
                if (measurements != null)
                {
                    var weightItem = measurements.FirstOrDefault(m => m.Value<string>("type") == "weight");
                    if (weightItem != null)
                        descs.Add(new Desc(order++, Language.KR, "무게", weightItem.Value<string>("text")));
                }
            }

            return descs.Count > 0 ? descs : null;
        }

        public override List<Tag> GetTags(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var pageProps = data as JObject;
            var parts = new List<string>();

            // 상품 요약 설명
            var summary = pageProps["productSummaryProps"];
            string summaryDesc = summary?.Value<string>("description");
            if (!string.IsNullOrWhiteSpace(summaryDesc))
                parts.Add($"<p>{summaryDesc}</p>");

            // 참고사항 (goodToKnow)
            var infoSection = pageProps["productInformationSectionProps"];
            var goodToKnow = infoSection?["productDetailsProps"]?["accordionObject"]?["goodToKnow"]?["contentProps"]?["goodToKnow"] as JArray;
            if (goodToKnow != null && goodToKnow.Count > 0)
            {
                parts.Add("<h3>참고</h3><ul>");
                foreach (JToken item in goodToKnow)
                {
                    string text = item.Value<string>("text");
                    if (!string.IsNullOrWhiteSpace(text))
                        parts.Add($"<li>{text}</li>");
                }
                parts.Add("</ul>");
            }

            // 소재 및 관리
            var materialsAndCare = infoSection?["productDetailsProps"]?["accordionObject"]?["materialsAndCare"]?["contentProps"];
            var careInstructions = materialsAndCare?["careInstructions"] as JArray;
            if (careInstructions != null && careInstructions.Count > 0)
            {
                var texts = careInstructions[0]?["texts"] as JArray;
                if (texts != null && texts.Count > 0)
                {
                    parts.Add("<h3>관리 방법</h3><ul>");
                    foreach (JToken t in texts)
                        parts.Add($"<li>{t.ToString()}</li>");
                    parts.Add("</ul>");
                }
            }

            // 치수 정보
            var measureProps = infoSection?["measurementsProps"];
            if (measureProps != null)
            {
                var measureItems = measureProps["measurementGroups"] as JArray;
                if (measureItems != null)
                {
                    parts.Add("<h3>치수</h3><ul>");
                    foreach (JToken group in measureItems)
                    {
                        var measurements = group["measurements"] as JArray;
                        if (measurements == null) continue;
                        foreach (JToken m in measurements)
                        {
                            string label = m.Value<string>("label");
                            string value = m.Value<string>("text");
                            if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                                parts.Add($"<li>{label}: {value}</li>");
                        }
                    }
                    parts.Add("</ul>");
                }
            }

            if (parts.Count == 0) return null;
            string resultHtml = string.Join("\n", parts);
            return HtmlReader.GetDafaultHtml(resultHtml);
        }

        public override Option GetOption(string html, JToken data)
        {
            var prices = GetPrice(html, data);
            return GetDefaultOption(prices);
        }

        public override int? GetTotalQuantity(string html, JToken data) => null;

        public override Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            throw new NotImplementedException();
        }

        private Option GetDefaultOption(List<Price> prices)
        {
            var groupName = new Name(Language.KR, "사이즈");
            var optionName = new Name(Language.KR, "원사이즈");

            var optionGroup = new Option.OptionGroup();
            optionGroup.optionId = "0";
            optionGroup.groupName = new List<Name> { groupName };
            optionGroup.groups = new List<Option.OptionGroup.Group>
            {
                new Option.OptionGroup.Group
                {
                    groupId = "0",
                    optionName = new List<Name> { optionName }
                }
            };

            var detailOption = new Option.DetailOption();
            detailOption.optionIds = "0";
            detailOption.optionGroupName = new List<Name> { groupName };
            detailOption.optionName = new List<Name> { optionName };
            detailOption.price = prices;

            return new Option
            {
                optionGroups = new List<Option.OptionGroup> { optionGroup },
                detailOptions = new List<Option.DetailOption> { detailOption }
            };
        }

        #endregion
    }
}
