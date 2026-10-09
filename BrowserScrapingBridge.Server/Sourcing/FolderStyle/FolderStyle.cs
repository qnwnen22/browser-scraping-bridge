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

namespace BrowserScrapingBridge.Server.Sourcing.FolderStyle
{
    public class FolderStyle : CollectBase
    {
        public override string Site => nameof(FolderStyle);
        public override string Name => "폴더";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = false;

        private const string HOST = "https://www.folderstyle.com";

        public override string GetCode(string url)
        {
            // /goodsView/0000063195
            var m = Regex.Match(url, @"/goodsView/(\d{10})");
            if (m.Success) return m.Groups[1].Value;
            // fallback: 마지막 10자리 숫자
            var m2 = Regex.Match(url, @"(\d{10})");
            return m2.Success ? m2.Groups[1].Value : url;
        }

        public override string CreateProductUrl(string code)
        {
            return $"{HOST}/shop/goodsView/{code}";
        }

        public override Task<string> GetLoginUrlAsync(string connectionId)
        {
            return Task.FromResult<string>(null);
        }

        #region 카테고리 수집

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            // URL: /shop/goodsList/017000000000000
            var cateMatch = Regex.Match(url, @"/goodsList/(\d+)");
            if (!cateMatch.Success) return null;
            string categoryCode = cateMatch.Groups[1].Value;

            // 1. 카테고리 페이지 GET → 세션쿠키 + CSRF 해시 획득
            var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
            using var client = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            string listPageHtml = await client.GetStringAsync($"{HOST}/shop/goodsList/{categoryCode}");
            string csrfHash = ExtractCsrfHash(listPageHtml);
            if (string.IsNullOrWhiteSpace(csrfHash)) return null;

            // 2. POST로 상품 목록 API 호출
            var postContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("ForbizCsrfTestName", csrfHash),
                new KeyValuePair<string, string>("page", page.ToString()),
                new KeyValuePair<string, string>("max", "40"),
                new KeyValuePair<string, string>("filterCid", categoryCode),
                new KeyValuePair<string, string>("orderBy", "regDate")
            });
            var response = await client.PostAsync($"{HOST}/controller/product/getGoodsList", postContent);
            string json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return null;

            var root = JObject.Parse(json);
            var list = root["data"]?["list"] as JArray;
            if (list == null || !list.Any()) return null;

            var result = new List<CollectPageInfo>();
            foreach (JToken item in list)
            {
                string id = item.Value<string>("id");
                string pname = item.Value<string>("pname");
                if (string.IsNullOrWhiteSpace(id)) continue;

                result.Add(new CollectPageInfo
                {
                    Code = id,
                    Url = $"{HOST}/shop/goodsView/{id}",
                    Title = pname
                });
            }

            return result.Count > 0 ? result : null;
        }

        private string ExtractCsrfHash(string html)
        {
            // forbizCsrf = { name:"ForbizCsrfTestName", hash:"abc123..." }
            var m = Regex.Match(html, @"forbizCsrf\s*=\s*\{[^}]*hash\s*:\s*""([^""]+)""");
            return m.Success ? m.Groups[1].Value : null;
        }

        #endregion

        #region 상품 수집

        public override void ThrowIfCaptcha(string url, string html) { }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            var data = new JObject();

            // hidden input 추출
            data["pid"] = ExtractHiddenInput(html, "devPid");
            data["productName"] = ExtractHiddenInput(html, "devProductName");
            data["listPrice"] = ExtractHiddenInput(html, "devListPrice");
            data["price"] = ExtractHiddenInput(html, "devPrice");
            data["brandName"] = ExtractHiddenInput(html, "devBrandNane"); // 사이트 오타 그대로

            // OG 메타태그 보조
            if (string.IsNullOrWhiteSpace(data.Value<string>("price")))
                data["price"] = ExtractOgMeta(html, "og:dcprice");
            if (string.IsNullOrWhiteSpace(data.Value<string>("productName")))
                data["productName"] = ExtractOgMeta(html, "og:title");

            // 옵션 API 호출
            string code = data.Value<string>("pid") ?? GetCode(url);
            try
            {
                using var client = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                string optionResponse = await client.GetStringAsync($"{HOST}/controller/product/loadOptionDatas/{code}");
                JObject optionData = ExtractOptionJson(optionResponse);
                if (optionData != null)
                    data["optionData"] = optionData;
            }
            catch { }

            // 이미지 URL 추출
            var images = ExtractImageUrls(html, code);
            if (images.Count > 0)
                data["images"] = JArray.FromObject(images);

            return data;
        }

        private string ExtractHiddenInput(string html, string id)
        {
            var m = Regex.Match(html, $@"<input[^>]+id\s*=\s*""{id}""[^>]+value\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;
            // value가 id보다 앞에 올 수도 있음
            m = Regex.Match(html, $@"<input[^>]+value\s*=\s*""([^""]*)""[^>]+id\s*=\s*""{id}""", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        private string ExtractOgMeta(string html, string property)
        {
            var m = Regex.Match(html, $@"<meta\s+property\s*=\s*""{property}""\s+content\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;
            m = Regex.Match(html, $@"<meta\s+content\s*=\s*""([^""]*)""\s+property\s*=\s*""{property}""", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        private JObject ExtractOptionJson(string response)
        {
            // var devOptionData = {...};
            var m = Regex.Match(response, @"var\s+devOptionData\s*=\s*(\{[\s\S]*?\})\s*;");
            if (!m.Success) return null;
            try { return JObject.Parse(m.Groups[1].Value); }
            catch { return null; }
        }

        private List<string> ExtractImageUrls(string html, string code)
        {
            var images = new List<string>();

            // OG 이미지
            string ogImage = ExtractOgMeta(html, "og:image");
            if (!string.IsNullOrWhiteSpace(ogImage))
                images.Add(ogImage);

            // 추가 이미지: m_{code}_add.gif 패턴
            var addMatches = Regex.Matches(html, @"(https?://image\.folderstyle\.com/[^""'\s]+?\.(?:gif|jpg|png))", RegexOptions.IgnoreCase);
            foreach (Match match in addMatches)
            {
                string imgUrl = match.Groups[1].Value;
                // 중복 제거, 소형(ms_) 제외
                if (!images.Contains(imgUrl) && !imgUrl.Contains("/ms_") && !imgUrl.Contains("/c_"))
                    images.Add(imgUrl);
            }

            return images;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string name = data.Value<string>("productName");
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("상품명을 찾을 수 없습니다.");
            return new List<Name> { new Name(Language.KR, name.Trim()) };
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            string brand = data.Value<string>("brandName");
            if (string.IsNullOrWhiteSpace(brand)) return null;
            return new List<Name> { new Name(Language.KR, brand.Trim()) };
        }

        public override string GetModel(string html, JToken data) => null;

        public override List<Price> GetPrice(string html, JToken data)
        {
            string priceStr = data.Value<string>("price");
            string listPriceStr = data.Value<string>("listPrice");

            double salePrice = ParsePrice(priceStr);
            double originalPrice = ParsePrice(listPriceStr);

            if (salePrice <= 0 && originalPrice <= 0)
                throw new Exception("상품 가격을 찾을 수 없습니다.");

            if (salePrice <= 0) salePrice = originalPrice;
            if (originalPrice <= 0) originalPrice = salePrice;

            return new List<Price> { new Price(Currency.KRW, originalPrice, salePrice, true) };
        }

        private double ParsePrice(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            string cleaned = Regex.Replace(value, @"[^\d.]", "");
            return double.TryParse(cleaned, out double result) ? result : 0;
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var imagesToken = data["images"] as JArray;
            if (imagesToken == null || !imagesToken.Any()) return null;
            return imagesToken.Select(t => t.ToString()).ToList();
        }

        public override string GetVideoUrl(string html, JToken data) => null;

        public override List<Desc> GetDescs(string html, JToken data)
        {
            var descs = new List<Desc>();
            int order = 1;

            string brand = data.Value<string>("brandName");
            if (!string.IsNullOrWhiteSpace(brand))
                descs.Add(new Desc(order++, Language.KR, "브랜드", brand));

            return descs.Count > 0 ? descs : null;
        }

        public override List<Tag> GetTags(string html, JToken data) => null;

        public override string GetDetailPageHtml(string html, JToken data)
        {
            // 상세 이미지 영역 추출: detail_cont 클래스
            var m = Regex.Match(html, @"<div[^>]+class\s*=\s*""[^""]*detail_cont[^""]*""[^>]*>([\s\S]*?)</div>\s*(?=<div[^>]+class\s*=\s*""[^""]*(?:info_tbl|tab_cont))", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                string detailHtml = m.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(detailHtml))
                    return HtmlReader.GetDafaultHtml(detailHtml);
            }

            // fallback: 이랜드 CDN 이미지들로 상세 구성
            var detailImages = Regex.Matches(html, @"(https?://imgfass\.eland\.co\.kr/[^""'\s]+\.(?:jpg|png|gif))", RegexOptions.IgnoreCase);
            if (detailImages.Count > 0)
            {
                var parts = new List<string>();
                foreach (Match img in detailImages)
                    parts.Add($"<img src=\"{img.Groups[1].Value}\" />");
                return HtmlReader.GetDafaultHtml(string.Join("\n", parts));
            }

            return null;
        }

        public override Option GetOption(string html, JToken data)
        {
            var optionData = data["optionData"] as JObject;
            if (optionData == null)
                return GetDefaultOption(GetPrice(html, data));

            var options = optionData["options"] as JArray;
            if (options == null || !options.Any())
                return GetDefaultOption(GetPrice(html, data));

            // 옵션 그룹명 추출
            string optionGroupName = "사이즈";
            var viewOptions = optionData["viewOptions"] as JArray;
            if (viewOptions != null && viewOptions.Any())
            {
                string name = viewOptions[0]?.Value<string>("option_name");
                if (!string.IsNullOrWhiteSpace(name)) optionGroupName = name;
            }

            var groupName = new Name(Language.KR, optionGroupName);
            var groups = new List<Option.OptionGroup.Group>();
            var detailOptions = new List<Option.DetailOption>();

            foreach (JToken opt in options)
            {
                string optDiv = opt.Value<string>("option_div");
                string state = opt.Value<string>("state");
                int stock = opt.Value<int?>("option_stock") ?? 0;
                bool isSoldOut = state != "sale" || stock <= 0;

                string dcPriceStr = opt.Value<string>("option_dcprice");
                double optPrice = ParsePrice(dcPriceStr);
                if (optPrice <= 0) optPrice = ParsePrice(opt.Value<string>("option_listprice"));

                string listPriceStr = opt.Value<string>("option_listprice");
                double optListPrice = ParsePrice(listPriceStr);
                if (optListPrice <= 0) optListPrice = optPrice;

                var optionName = new Name(Language.KR, optDiv);

                groups.Add(new Option.OptionGroup.Group
                {
                    groupId = optDiv,
                    optionName = new List<Name> { optionName }
                });

                var detailOption = new Option.DetailOption();
                detailOption.optionIds = optDiv;
                detailOption.optionGroupName = new List<Name> { groupName };
                detailOption.optionName = new List<Name> { optionName };
                detailOption.price = new List<Price> { new Price(Currency.KRW, optListPrice, optPrice, true) };
                detailOption.isSoldOut = isSoldOut;
                detailOption.quantity = isSoldOut ? 0 : stock;
                detailOptions.Add(detailOption);
            }

            if (groups.Count == 0)
                return GetDefaultOption(GetPrice(html, data));

            var optionGroup = new Option.OptionGroup();
            optionGroup.optionId = "1";
            optionGroup.groupName = new List<Name> { groupName };
            optionGroup.groups = groups;

            return new Option
            {
                optionGroups = new List<Option.OptionGroup> { optionGroup },
                detailOptions = detailOptions
            };
        }

        public override int? GetTotalQuantity(string html, JToken data) => null;

        public override void ThrowIfOutOfStock(string html, JToken data, Product product) { }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            await Task.CompletedTask;
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
                new Option.OptionGroup.Group { groupId = "0", optionName = new List<Name> { optionName } }
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
