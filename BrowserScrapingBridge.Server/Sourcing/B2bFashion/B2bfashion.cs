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
using Price = BrowserScrapingBridge.Server.Sourcing.Models.Product.Price;

namespace BrowserScrapingBridge.Server.Sourcing.B2bFashion
{
    /// <summary>
    /// B2bfashion.online (PrestaShop) collector
    /// - List page: parse product cards and extract ALL variants (id_product_attribute + size + stock)
    /// - Queue: push ONLY ONE CollectPageInfo per product (QuickView is called once per product)
    /// - Detail: QuickView JSON (product + quickview_html) + variants(from list, passed via __v query)
    /// </summary>
    public class B2bFashion : ICollect
    {
        public string Site => "B2bFashion";
        public string Name => "B2bFashion";
        public Language Language => Language.EN;
        public Currency Currency => Currency.EUR;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }

        private const string Host = "https://b2bfashion.online";
        private const string LoginUrl = Host + "/login?back=my-account";
        public bool SupportsCaching { get; set; }
        private class ListVariant
        {
            public string AttrId { get; set; }  // id_product_attribute
            public string Size { get; set; }    // "38 IT"
            public int Stock { get; set; }      // 4
        }

        #region ICollect

        public string GetCode(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var (p, a) = ExtractIdsFromUrl(url);
            if (string.IsNullOrWhiteSpace(p)) return null;
            return string.IsNullOrWhiteSpace(a) ? p : $"{p}_{a}";
        }

        public async Task<string> GetLoginUrlAsync(string connectionId)
        {
            string html = await ChatHubService.GetRequest(connectionId, Host + "/my-account");
            if (string.IsNullOrWhiteSpace(html) || IsLoginPage(html)) return LoginUrl;
            return null;
        }

        /// <summary>
        /// ✅ 핵심: "상품당 1개" CollectPageInfo만 생성한다 (QuickView 1회 호출).
        /// - 옵션 전체(사이즈/재고/attrId)는 __v query에 JSON으로 넣어 전달한다.
        /// </summary>
        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uri = new UriBuilderAdvance(url);
            uri.SetQuery("page", page.ToString());
            string pageUrl = uri.ToString();

            string html = await ChatHubService.GetRequest(connectionId, pageUrl, GetListHeaders(pageUrl));

            var result = new List<CollectPageInfo>();
            if (string.IsNullOrWhiteSpace(html)) return result;

            if (IsLoginPage(html))
            {
                // CollectService는 CaptchaException을 "302 Redirect" 흐름으로 처리할 수 있음
                throw new CaptchaException
                {
                    CaptchaInfo = new CaptchaInfo
                    {
                        Message = "B2bFashion 로그인이 필요합니다. 로그인 후 다시 수집을 실행해주세요.",
                        Url = LoginUrl
                    }
                };
            }

            foreach (var card in ExtractProductCardBlocks(html))
            {
                string idProduct = GetAttr(card, "data-id-product");
                if (string.IsNullOrWhiteSpace(idProduct)) continue;

                // 대표 attribute (기본 선택)
                string defaultAttr = GetAttr(card, "data-id-product-attribute");

                // ✅ 카드에서 옵션 전체(사이즈/재고/attrId) 추출
                var variants = ParseVariantsFromCard(card, idProduct);

                // 대표 attr 없으면 첫번째 옵션으로 대체
                if (string.IsNullOrWhiteSpace(defaultAttr) && variants.Count > 0)
                    defaultAttr = variants[0].AttrId;

                if (string.IsNullOrWhiteSpace(defaultAttr))
                    continue;

                // 제목(로그/디버그용)
                string title = ExtractTitleFromCard(card);

                // ✅ variants를 __v query로 전달 (CollectPageInfo 확장필드가 없어서 우회)
                string variantsJson = variants.ToSerializeObject();
                string encoded = Uri.EscapeDataString(variantsJson);

                string qvUrl = BuildQuickViewUrl(idProduct, defaultAttr) + $"&__v={encoded}";

                result.Add(new CollectPageInfo
                {
                    Code = $"{idProduct}_{defaultAttr}",
                    Url = qvUrl,
                    Title = title
                });
            }

            return result;
        }

        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            var product = new Product();

            // ✅ 어떤 경우에도 CollectService에서 .First() 호출하다 죽지 않도록 "기본 스키마"를 먼저 채움
            product.site = this.Site;
            product.url = url;
            product.currency = this.Currency.ToString();
            product.collectStatus = ProductCollectStatus.Failed; // 기본은 실패로 두고, 성공 시 BuildProduct에서 Success로 바뀜

            // null 방지용 기본값들
            product.title = new List<Name> { new Name(Language.EN, "") };
            product.price = new List<Price>
            {
                new Price(Currency.EUR, 0, 0, true),
                new Price("KRW", 0, 0)
            };
            product.mainImages = new List<string>();
            product.descs = new List<Desc>();
            product.tags = new List<Tag>();

            try
            {
                // (id_product, 대표 id_product_attribute) 파싱
                var (idProduct, idAttr) = ExtractIdsFromUrl(url);
                if (string.IsNullOrWhiteSpace(idProduct))
                {
                    product.memo = "상품 ID 파싱 실패(QuickView URL 또는 상품 URL 형식 확인 필요)";
                    product.code = ""; // ✅ null 방지
                    return product;
                }

                // ✅ code도 미리 세팅(실패해도 null 아님)
                product.code = string.IsNullOrWhiteSpace(idAttr) ? idProduct : $"{idProduct}_{idAttr}";

                // 리스트에서 실어둔 variants 읽기
                var variants = ParseVariantsFromUrl(url);

                // QuickView URL
                string qvUrl = BuildQuickViewUrl(idProduct, idAttr);

                string json = await ChatHubService.GetRequest(connectionId, qvUrl, GetQuickViewHeaders(url));
                if (string.IsNullOrWhiteSpace(json))
                {
                    product.memo = "QuickView 응답이 비었습니다.";
                    return product;
                }

                // 로그인 페이지가 섞여 들어올 수 있음
                if (IsLoginPage(json))
                {
                    product.memo = "로그인이 필요합니다.";
                    return product;
                }

                JObject jo;
                try { jo = json.ToJObject(); }
                catch
                {
                    product.memo = "QuickView JSON 파싱 실패";
                    return product;
                }

                string quickHtml = jo.Value<string>("quickview_html");
                JToken prod = jo["product"];
                if (prod == null)
                {
                    product.memo = "QuickView JSON에 product 노드가 없습니다.";
                    return product;
                }

                // ✅ 정상 Build (BuildProduct가 title/price/images 등을 제대로 채움)
                product = BuildProduct(prod, quickHtml, idProduct, idAttr, variants);
                product.url = qvUrl;

                // ✅ BuildProduct가 혹시라도 리스트를 null로 만들 경우를 대비한 최후의 안전망
                if (product.title == null || product.title.Count == 0) product.title = new List<Name> { new Name(Language.EN, "") };
                if (product.price == null) product.price = new List<Price> { new Price(Currency.EUR, 0, 0, true), new Price("KRW", 0, 0) };
                if (product.mainImages == null) product.mainImages = new List<string>();

                return product;
            }
            catch (Exception ex)
            {
                // ✅ catch에서도 스키마 유지
                product.memo = ex.Message;

                if (product.title == null || product.title.Count == 0) product.title = new List<Name> { new Name(Language.EN, "") };
                if (product.price == null) product.price = new List<Price> { new Price(Currency.EUR, 0, 0, true), new Price("KRW", 0, 0) };
                if (product.mainImages == null) product.mainImages = new List<string>();
                if (product.code == null) product.code = "";

                return product;
            }
        }


        #endregion

        #region Build Product

        private Product BuildProduct(JToken prod, string quickHtml, string idProduct, string idAttr, List<ListVariant> variantsFromList)
        {
            var p = new Product();
            var embedded = prod["embedded_attributes"];

            p.site = Site;

            string attrFromEmbedded = embedded?.Value<string>("id_product_attribute");
            string attr = !string.IsNullOrWhiteSpace(idAttr) ? idAttr : attrFromEmbedded;
            p.code = string.IsNullOrWhiteSpace(attr) ? idProduct : $"{idProduct}_{attr}";

            p.url = prod.Value<string>("canonical_url")
               ?? prod.Value<string>("url")
               ?? embedded?.Value<string>("link")
               ?? (Host + "/");

            p.currency = Currency.ToString();

            string title = embedded?.Value<string>("name")
                        ?? prod.Value<string>("title")
                        ?? prod.Value<string>("name")
                        ?? "";

            p.title = new List<Name> { new Name(Language.EN, title) };

            string descText = embedded?.Value<string>("description")
                           ?? prod.Value<string>("description")
                           ?? "";
            p.detailPageHtml = WrapAsSimpleHtml(descText);

            p.descs = BuildDescsFromFeatures(embedded);

            double sale = embedded?.Value<double?>("price_amount") ?? 0;
            double regular = embedded?.Value<double?>("regular_price_amount")
                          ?? embedded?.Value<double?>("price_without_reduction")
                          ?? sale;

            p.price = new List<Price>
            {
                new Price(Currency.EUR, regular, sale, true),
                new Price("KRW", 0, 0)
            };

            p.mainImages = ExtractImages(quickHtml);

            // ✅ 옵션 전체 사이즈를 리스트 카드에서 구성
            p.option = BuildOptionsFromListVariants(idProduct, variantsFromList, p.price);

            // fallback: variants를 못 받았으면 quickview의 현재 선택 옵션 1개만이라도 세팅
            if (p.option == null)
            {
                int qty = embedded?.Value<int?>("quantity") ?? 0;
                p.option = BuildSingleOptionFallback(prod, idProduct, attr, qty, p.price);
            }

            p.collectStatus = ProductCollectStatus.Success;
            return p;
        }

        private List<Desc> BuildDescsFromFeatures(JToken embedded)
        {
            var descs = new List<Desc>();
            try
            {
                var features = embedded?["features"]?.ToList();
                if (features == null || features.Count == 0) return descs;

                int idx = 1;
                foreach (var f in features)
                {
                    string name = f.Value<string>("name");
                    string value = f.Value<string>("value");
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value)) continue;
                    descs.Add(new Desc(idx++, Language.EN, name, value));
                }
            }
            catch { }
            return descs;
        }

        /// <summary>
        /// ✅ 카드에서 얻은 variants로 옵션 전체(detailOptions) 구성
        /// - 옵션 가격은 상품 가격과 동일하게 세팅(가격제한 필터 회피)
        /// </summary>
        private Option BuildOptionsFromListVariants(string idProduct, List<ListVariant> variants, List<Price> productPrices)
        {
            if (variants == null || variants.Count == 0) return null;

            var option = new Option
            {
                optionGroups = new List<Option.OptionGroup>(),
                detailOptions = new List<Option.DetailOption>()
            };

            var sizeGroup = new Option.OptionGroup
            {
                optionId = "size",
                groupName = new List<Name> { new Name(Language.EN, "size") },
                groups = new List<Option.OptionGroup.Group>()
            };

            foreach (var v in variants)
            {
                sizeGroup.groups.Add(new Option.OptionGroup.Group
                {
                    groupId = v.AttrId,
                    optionName = new List<Name> { new Name(Language.EN, v.Size) },
                    price = productPrices,
                    quantity = v.Stock,
                    optionImage = null
                });

                option.detailOptions.Add(new Option.DetailOption
                {
                    optionIds = $"{idProduct}_{v.AttrId}",
                    optionGroupName = new List<Name> { new Name(Language.EN, "size") },
                    optionName = new List<Name> { new Name(Language.EN, v.Size) },
                    price = productPrices,
                    quantity = v.Stock,
                    isSoldOut = v.Stock <= 0,
                    optionImage = null
                });
            }

            option.optionGroups.Add(sizeGroup);
            return option;
        }

        /// <summary>
        /// fallback: quickview의 attributes(선택된 1개)로 옵션 1개 생성
        /// </summary>
        private Option BuildSingleOptionFallback(JToken prod, string idProduct, string idAttr, int qty, List<Price> productPrices)
        {
            try
            {
                var attrs = prod["attributes"] as JObject;
                if (attrs == null || !attrs.Properties().Any()) return null;

                var first = attrs.Properties().First().Value;
                string groupName = first.Value<string>("group") ?? "option";
                string valueName = first.Value<string>("name") ?? "";
                string idGroup = first.Value<string>("id_attribute_group") ?? "1";
                string idValue = first.Value<string>("id_attribute") ?? idAttr;

                var option = new Option
                {
                    optionGroups = new List<Option.OptionGroup>(),
                    detailOptions = new List<Option.DetailOption>()
                };

                option.optionGroups.Add(new Option.OptionGroup
                {
                    optionId = idGroup,
                    groupName = new List<Name> { new Name(Language.EN, groupName) },
                    groups = new List<Option.OptionGroup.Group>
                    {
                        new Option.OptionGroup.Group
                        {
                            groupId = idValue,
                            optionName = new List<Name> { new Name(Language.EN, valueName) },
                            price = productPrices,
                            quantity = qty,
                            optionImage = null
                        }
                    }
                });

                option.detailOptions.Add(new Option.DetailOption
                {
                    optionIds = $"{idProduct}_{idAttr}",
                    optionGroupName = new List<Name> { new Name(Language.EN, groupName) },
                    optionName = new List<Name> { new Name(Language.EN, valueName) },
                    price = productPrices,
                    quantity = qty,
                    isSoldOut = qty <= 0,
                    optionImage = null
                });

                return option;
            }
            catch
            {
                return null;
            }
        }

        private string WrapAsSimpleHtml(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            // downstream(번역/이미지 추출/템플릿)을 위해 최소 HTML 래핑
            return $"<div class=\"product-description\">{WebUtility.HtmlEncode(text).Replace("\n", "<br>")}</div>";
        }

        private List<string> ExtractImages(string quickHtml)
        {
            if (string.IsNullOrWhiteSpace(quickHtml)) return new List<string>();

            var hrefMatches = Regex.Matches(
                quickHtml,
                @"href=\""(?<u>https?:\/\/[^\""]+\.(jpg|jpeg|png|webp))\""",
                RegexOptions.IgnoreCase);

            var urls = hrefMatches
                .Select(m => m.Groups["u"].Value)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .ToList();

            if (urls.Count > 0) return urls;

            var dataMatches = Regex.Matches(
                quickHtml,
                @"data-image-large-src=\""(?<u>https?:\/\/[^\""]+)\""",
                RegexOptions.IgnoreCase);

            return dataMatches
                .Select(m => m.Groups["u"].Value)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .ToList();
        }

        #endregion

        #region Parsing helpers

        private string BuildQuickViewUrl(string idProduct, string idAttr)
        {
            if (string.IsNullOrWhiteSpace(idAttr))
                return $"{Host}/index.php?controller=product?more=1&action=quickview&id_product={idProduct}";

            return $"{Host}/index.php?controller=product?more=1&action=quickview&id_product={idProduct}&id_product_attribute={idAttr}";
        }

        private (string idProduct, string idAttr) ExtractIdsFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return (null, null);

            // QuickView URL이면 query에서 파싱 (우리 방식은 여기에 해당)
            string p = GetQuery(url, "id_product");
            string a = GetQuery(url, "id_product_attribute");
            if (!string.IsNullOrWhiteSpace(p)) return (p, a);

            // canonical url fallback
            var m = Regex.Match(url, @"/(?<p>\d+)-(?<a>\d+)-");
            if (m.Success) return (m.Groups["p"].Value, m.Groups["a"].Value);

            var m2 = Regex.Match(url, @"/(?<p>\d+)-");
            if (m2.Success) return (m2.Groups["p"].Value, null);

            return (null, null);
        }

        private List<ListVariant> ParseVariantsFromUrl(string url)
        {
            try
            {
                string v = GetQuery(url, "__v");
                if (string.IsNullOrWhiteSpace(v)) return null;

                v = Uri.UnescapeDataString(v);
                var variants = v.ToDeserializeObject<List<ListVariant>>();
                return variants;
            }
            catch
            {
                return null;
            }
        }

        private List<ListVariant> ParseVariantsFromCard(string cardHtml, string idProduct)
        {
            var list = new List<ListVariant>();

            // rowSingle(사이즈별) 블록 근사 추출
            var rowMatches = Regex.Matches(
                cardHtml,
                @"class=""row\s+rowSingle[\s\S]*?</div>\s*</div>\s*</div>",
                RegexOptions.IgnoreCase);

            foreach (Match rm in rowMatches)
            {
                string row = rm.Value;

                string attrId = Regex.Match(row, @"id_product_attribute=""(?<v>\d+)""",
                    RegexOptions.IgnoreCase).Groups["v"].Value;
                if (string.IsNullOrWhiteSpace(attrId)) continue;

                string maxQty = Regex.Match(row, @"data-max-qty=""(?<v>\d+)""",
                    RegexOptions.IgnoreCase).Groups["v"].Value;

                int stock = 0;
                int.TryParse(maxQty, out stock);

                string size = Regex.Match(row, @"<a[^>]*title=""(?<v>[^""]+)""",
                    RegexOptions.IgnoreCase).Groups["v"].Value?.Trim();

                if (string.IsNullOrWhiteSpace(size))
                {
                    // fallback: "38 IT" 텍스트
                    size = Regex.Match(row, @"data-title=""size""[\s\S]*?>(?<v>\d+\s*IT)<",
                        RegexOptions.IgnoreCase).Groups["v"].Value?.Trim();
                }

                list.Add(new ListVariant
                {
                    AttrId = attrId,
                    Size = string.IsNullOrWhiteSpace(size) ? attrId : size,
                    Stock = stock
                });
            }

            // 중복 제거
            list = list.GroupBy(x => x.AttrId).Select(g => g.First()).ToList();
            return list;
        }

        private string ExtractTitleFromCard(string cardHtml)
        {
            // 하단 product_name 우선 ('SmmZoe' coat)
            var matches = Regex.Matches(cardHtml, @"<div\s+class=""product_name""[^>]*>\s*(?<v>[^<]+)\s*</div>",
                RegexOptions.IgnoreCase);

            // 첫 번째는 브랜드일 수 있으니 마지막 것을 우선 사용
            if (matches.Count > 0)
            {
                string last = matches[matches.Count - 1].Groups["v"].Value?.Trim();
                if (!string.IsNullOrWhiteSpace(last)) return last;
            }

            // fallback: alt
            string alt = Regex.Match(cardHtml, @"alt=""(?<v>[^""]+)""", RegexOptions.IgnoreCase)
                              .Groups["v"].Value?.Trim();
            return alt ?? "";
        }

        private bool IsLoginPage(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return true;
            if (html.Contains("submitLogin", StringComparison.OrdinalIgnoreCase)) return true;
            if (html.Contains("login-form", StringComparison.OrdinalIgnoreCase)) return true;
            if (html.Contains("/login?back=my-account", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private List<string> ExtractProductCardBlocks(string html)
        {
            var blocks = new List<string>();

            // product-miniature 시작점 기준 근사 분리
            var starts = Regex.Matches(
                html,
                "<div\\s+class=\"product-miniature[^>]*data-id-product=\"(?<id>\\d+)\"",
                RegexOptions.IgnoreCase);

            if (starts.Count == 0) return blocks;

            for (int i = 0; i < starts.Count; i++)
            {
                int startIdx = starts[i].Index;
                int endIdx = (i + 1 < starts.Count) ? starts[i + 1].Index : html.Length;
                if (endIdx <= startIdx) continue;

                blocks.Add(html.Substring(startIdx, endIdx - startIdx));
            }

            return blocks;
        }

        private string GetAttr(string htmlBlock, string attrName)
        {
            if (string.IsNullOrWhiteSpace(htmlBlock)) return null;
            var m = Regex.Match(htmlBlock, $"{Regex.Escape(attrName)}=\"(?<v>[^\"]+)\"", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups["v"].Value : null;
        }

        private string GetQuery(string url, string key)
        {
            try
            {
                var uri = new Uri(url);
                var q = uri.Query;
                if (string.IsNullOrWhiteSpace(q)) return null;

                var parts = q.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length != 2) continue;
                    if (kv[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                        return kv[1];
                }
                return null;
            }
            catch { return null; }
        }

        private Dictionary<string, string> GetQuickViewHeaders(string referer)
        {
            return new Dictionary<string, string>
            {
                ["accept"] = "application/json, text/javascript, */*; q=0.01",
                ["accept-language"] = "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7",
                ["user-agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                ["x-requested-with"] = "XMLHttpRequest",
                ["referer"] = string.IsNullOrWhiteSpace(referer) ? Host + "/" : referer,
            };
        }

        private Dictionary<string, string> GetListHeaders(string referer)
        {
            return new Dictionary<string, string>
            {
                ["accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8",
                ["accept-language"] = "ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7",
                ["user-agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                ["referer"] = referer
            };
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

        #endregion
    }
}
