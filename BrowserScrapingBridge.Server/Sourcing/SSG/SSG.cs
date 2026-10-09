#nullable disable warnings
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.SSG
{
    public class SSG : CollectBase
    {
        public override string Site => "SSGCol";
        public override string Name => "신세계";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = true;

        public override async Task<string> GetLoginUrlAsync(string connectionId)
        {
            return null;
        }

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("page", page.ToString());
            var pageUrl = uriBuilderAdvance.ToString();

            var html = await ChatHubService.GetRequest(connectionId, pageUrl);
            var nextData = html.GetValue("//script[@id='__NEXT_DATA__']", TextEx.XPathOption.InnerHtml);

            var collectPageInfos = new List<CollectPageInfo>();

            JObject data = nextData.ToJObject();
            if (data == null)
            {
                List<string> itemCodes = html.GetValues("//div[@id='area_itemlist']//li[@data-unittype='item']", "data-adtgtid");
                if (itemCodes == null || itemCodes.Count <= 0) return null;
                foreach (string itemCode in itemCodes)
                {
                    if (string.IsNullOrWhiteSpace(itemCode)) continue;
                    string itemUrl = $"https://www.ssg.com/item/itemView.ssg?itemId={itemCode}";
                    CollectPageInfo collectPageInfo = new CollectPageInfo();
                    collectPageInfo.Code = itemCode;
                    collectPageInfo.Url = itemUrl;
                    collectPageInfos.Add(collectPageInfo);
                }
            }
            else
            {
                var queries = data["props"]?["pageProps"]?["dehydratedState"]?["queries"]?.ToList();
                if (queries == null || queries.Count <= 0) return null;
                for (int i = 0; i < queries.Count; i++)
                {
                    try
                    {
                        JToken query = queries[i];
                        var areaList = query["state"]?["data"]?["areaList"];
                        if (areaList == null || areaList.HasValues == false) continue;
                        var areas = areaList.ToList();
                        for (int j = 0; j < areas.Count; j++)
                        {
                            if (j >= 4) continue;
                            JToken area = areas[j];
                            var dataList = area["dataList"];
                            if (dataList == null || dataList.HasValues == false) continue;
                            var unitType = area.Value<string>("unitType");
                            if (string.IsNullOrWhiteSpace(unitType) || unitType != "ITEM_UNIT_LIST") continue;
                            List<JToken> datas = dataList.ToList();
                            for (int k = 0; k < datas.Count; k++)
                            {
                                var dataItem = datas[k];
                                string itemId = dataItem.Value<string>("itemId");
                                if (string.IsNullOrWhiteSpace(itemId)) continue;
                                string itemUrl = $"https://www.ssg.com/item/itemView.ssg?itemId={itemId}";
                                string itemName = dataItem.Value<string>("itemName");

                                CollectPageInfo collectPageInfo = new CollectPageInfo();
                                collectPageInfo.Code = itemId;
                                collectPageInfo.Url = itemUrl;
                                collectPageInfo.Title = itemName;
                                collectPageInfos.Add(collectPageInfo);
                            }
                        }
                    }
                    catch { } // 데이터에 object와 List가 섞여있기 때문에 예외로 continue 처리
                }
            }
            return collectPageInfos;
        }

        public override async Task<string> GetHtmlAsync(string connectionId, string url)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            CollectExtension.CollectSleep(1000);
            var recapchaTag = html.GetValue("//div[@class='recapcha']", TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(recapchaTag) == false)
            {
                string getHtml = GetHtml(url);
                if (string.IsNullOrWhiteSpace(getHtml) == false)
                {
                    html = getHtml;
                }
            }
            return html;
        }
        private string GetHtml(string url, bool useProxy = false)
        {
            try
            {
                BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest request = BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest.CreateHttp(url);
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
                request.Headers["Accept-Language"] = "ko,en-US;q=0.9,en;q=0.8,ko-KR;q=0.7";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";
                if (useProxy) request.Proxy = this.Proxy;
                string html = request.ReadToEnd();
                return html;
            }
            catch (Exception exception)
            {
                return null;
            }
        }

        public override void ThrowIfCaptcha(string url, string html)
        {
        }

        public override string GetCode(string url)
        {
            var uriBuilderAdvance = new UriBuilderAdvance(url);
            string itemId = uriBuilderAdvance.GetQuery("itemId");
            return itemId;
        }

        public override string CreateProductUrl(string code)
        {
            return $"https://www.ssg.com/item/itemView.ssg?itemId={code}";
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            string title = html.SelectNode("//title", HtmlType.InnerText)?.Trim();
            if (title == "flagMsg")
            {
                string alertScript = html.SelectNode("//script", HtmlType.InnerHtml);
                string alertMsg = alertScript.SingleSplit("alert('", "');");
                if (alertMsg == "판매가 종료된 상품입니다.")
                {
                    throw new SaleException(ProductSaleStatus.Stopped, alertMsg);
                }
            }

            string errorMsg = "상품 정보를 조회할 수 없습니다.";

            var script = html.GetValue("//script[@type='text/javaScript']", TextEx.XPathOption.InnerHtml);
            var resultItemObj = script.SingleSplit("var resultItemObj = {", "};");
            if (string.IsNullOrWhiteSpace(resultItemObj))
            {
                errorMsg = "연속적인 접근으로 상품 페이지를 확인할 수 없습니다.\n해당 상품은 잠시 후 다시 시도해주세요.";
                throw new Exception(errorMsg);
            }
            var data = new JObject();
            try
            {
                var lines = resultItemObj.Split("\n");
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    List<string> keyValues = line.Split(":")?.ToList();
                    if (keyValues.Count < 2) continue;
                    var key = keyValues[0].Trim();
                    string value = null;
                    switch (keyValues.Count)
                    {
                        case 2:
                            value = keyValues[1];
                            break;
                        default:
                            value = keyValues.GetRange(1, keyValues.Count - 1).Join(":");
                            break;
                    }
                    var stringValue = value.SingleSplit("'", "'");
                    if (stringValue != null)
                    {
                        data[key] = stringValue;
                    }
                    else
                    {
                        var lastIndexOf = value.LastIndexOf(",");
                        if (lastIndexOf != -1)
                        {
                            value = value.Substring(0, lastIndexOf);
                        }
                        char startChar = value.First();
                        switch (startChar)
                        {
                            case '{':
                                data[key] = JToken.Parse(value);
                                break;
                            case '[':
                                data[key] = JArray.Parse(value);
                                break;
                            default:
                                data[key] = value;
                                break;
                        }
                    }
                }
                var itemNm = data.Value<string>("itemNm");
                var uitemObjs = script.MultiSplit("uitemObj = {", "};");
                JArray jArray = new JArray();
                for (int i = 0; i < uitemObjs.Count; i++)
                {
                    var replaceScript = uitemObjs[i].Replace(itemNm, "itemNm"); // 이상한 상품명 때문에 파싱이 재대로 안됨
                    JObject uItemData = GetUItemData(replaceScript);
                    if (uItemData == null) continue;
                    jArray.Add(uItemData);
                }
                if (jArray.Count > 0)
                {
                    data["uItems"] = jArray;
                }
                string iframe = html.GetValue("//div[@class='cdtl_capture_img']//iframe", "src");
                if (iframe.IndexOf("https://") < 0)
                {
                    iframe = $"https://www.ssg.com" + iframe;
                }
                var detailHtml = await RequestDetailHtmlAsync(connectionId, iframe);
                data["detailHtml"] = detailHtml;
                return data;
            }
            catch (Exception exception)
            {
                throw new Exception(errorMsg, exception);
            }
        }
        private JObject GetUItemData(string html)
        {
            var data = new JObject();
            while (true)
            {
                int IndexOf = html.IndexOf(":");
                string key = html.Substring(0, IndexOf).Trim();
                html = html.Substring(key.Length + 1);
                var nextIndexOf = html.IndexOf(":");
                if (nextIndexOf >= 0)
                {
                    var valueTemp = html.Substring(0, nextIndexOf);
                    var lastIndexOf = valueTemp.LastIndexOf(",");
                    var value = valueTemp.Substring(0, lastIndexOf);
                    string startChar = value.First().ToString();
                    switch (startChar)
                    {
                        case "'":
                        case "\"":
                            var stringValue = value.SubstringBetween(startChar, startChar, false);
                            data[key] = stringValue;
                            break;
                        default:
                            data[key] = value;
                            break;
                    }
                    int removeIndexOf = html.IndexOf(value);
                    html = html.Substring(removeIndexOf + value.Length);
                    if (html.Trim().First() == ',')
                    {
                        html = html.Substring(html.IndexOf(",") + 1).Trim();
                    }
                }
                else
                {
                    var startChar = html.First().ToString();
                    switch (startChar)
                    {
                        case "'":
                        case "\"":
                            var stringValue = html.SubstringBetween(startChar, startChar, false);
                            data[key] = stringValue;
                            break;
                        default:
                            data[key] = html.Trim();
                            break;
                    }
                    break;
                }
            }
            return data;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string itemNm = data.Value<string>("itemNm");
            if (string.IsNullOrWhiteSpace(itemNm)) throw new Exception("상품명을 찾을 수 없습니다.");
            return new List<Name> { new Name(Language.KR, itemNm) };
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            string brandNm = data.Value<string>("brandNm");
            if (string.IsNullOrWhiteSpace(brandNm)) return null;
            return new List<Name> { new Name(Language.KR, brandNm) };
        }

        public override string GetModel(string html, JToken data)
        {
            return null;
        }

        public override List<Desc> GetDescs(string html, JToken data)
        {
            var tables = html.GetValues("//div[@class='cdtl_cont_info']//table", TextEx.XPathOption.OuterHtml);
            if (tables == null || tables.Count <= 0) return null;

            var descs = new List<Desc>();
            int count = 1;
            foreach (var table in tables)
            {
                var trs = table.GetValues("//tr", TextEx.XPathOption.OuterHtml);
                foreach (var tr in trs)
                {
                    var th = tr.GetValue("//th", TextEx.XPathOption.InnerText);
                    if (string.IsNullOrWhiteSpace(th)) continue;
                    var td = tr.GetValue("//td", TextEx.XPathOption.InnerText);
                    if (string.IsNullOrWhiteSpace(td)) continue;
                    Desc desc = new Desc(count, Language.KR, th.Trim(), td.Trim());
                    descs.Add(desc);
                    count++;
                }
            }
            return descs;
        }

        public override List<Tag> GetTags(string html, JToken data)
        {
            return null;
        }

        public override List<Price> GetPrice(string html, JToken data)
        {
            double? sellprc = data.Value<double?>("sellprc");
            if (sellprc == null || sellprc <= 0) throw new Exception("기본 가격을 찾을 수 없습니다.");
            double? bestAmt = data.Value<double?>("bestAmt");
            if (bestAmt == null || bestAmt == 0)
            {
                bestAmt = sellprc;
            }
            return new List<Price> { new Price(Currency.KRW, (double)sellprc, (double)bestAmt, true) };
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            var mainImages = html.GetValues("//div[@class='cdtl_pager_sec']//img", "src");
            if (mainImages == null || mainImages.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");
            for (int i = 0; i < mainImages.Count; i++)
            {
                var image = mainImages[i];
                if (string.IsNullOrWhiteSpace(image)) continue;

                image = image.Trim();
                if (image.StartsWith("//"))
                    image = "https:" + image;

                var extension = Path.GetExtension(image);
                int indexOf = image.IndexOf("_500");
                if (indexOf >= 0) // _90같은 항목들이 continue 때문에 추가되지않아서 그대로 들어가도록 수정
                {
                    image = image.Substring(0, indexOf) + extension;
                }
                mainImages[i] = image;
            }
            return mainImages;
        }

        public override string GetVideoUrl(string html, JToken data)
        {
            return null;
        }

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var htmlBuilder = new StringBuilder();

            //string iframe = html.GetValue("//div[@class='cdtl_capture_img']//iframe", "src");
            //if (iframe.IndexOf("https://") < 0)
            //{
            //    iframe = $"https://www.ssg.com" + iframe;
            //}
            //for (int i = 0; i < 12; i++)
            //{
            //    var iframeHtml = RequestDetailHtml(iframe);
            //    if (string.IsNullOrWhiteSpace(iframeHtml))
            //    {
            //        DebugConsole.WriteLine($"SSG 상세페이지 조회 {i + 1}번째 시도 실패");
            //        Thread.Sleep(5000);
            //    }
            //    else
            //    {
            //        htmlBuilder.AppendLine("<div>");
            //        htmlBuilder.AppendLine(iframeHtml);
            //        htmlBuilder.AppendLine("</div>");
            //        break;
            //    }
            //}

            var iframeHtml = data.Value<string>("detailHtml");
            htmlBuilder.AppendLine("<div>");
            htmlBuilder.AppendLine(iframeHtml);
            htmlBuilder.AppendLine("</div>");

            string cdtl_detail_img = html.GetValue("//div[@class='cdtl_detail_img']", TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(cdtl_detail_img) == false)
            {
                cdtl_detail_img = HtmlReader.GetDafaultHtml(cdtl_detail_img);
                htmlBuilder.AppendLine(cdtl_detail_img);
            }

            string detailHtml = htmlBuilder.ToString();
            if (string.IsNullOrWhiteSpace(detailHtml)) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            return detailHtml;
        }
        private async Task<string> RequestDetailHtmlAsync(string connectionId, string iframeUrl)
        {
            try
            {
                UriBuilderAdvance uriBuilderAdvance = new UriBuilderAdvance(iframeUrl);
                uriBuilderAdvance.SetQuery("ts", "/m2x/mixed/main/image/optimize");
                string iframeHtml = await ChatHubService.GetRequest(connectionId, uriBuilderAdvance.ToString());

                var getDetailHtmls = new List<Func<string, string>>();
                getDetailHtmls.Add(GetDetailHtml1);
                getDetailHtmls.Add(GetDetailHtml2);

                string detailHtml = null;
                foreach (Func<string, string> getDetailHtml in getDetailHtmls)
                {
                    detailHtml = getDetailHtml.Invoke(iframeHtml);
                    if (string.IsNullOrWhiteSpace(detailHtml) == false)
                    {
                        detailHtml = HtmlReader.GetDafaultHtml(detailHtml);
                        break;
                    }
                }
                return detailHtml;
            }
            catch (Exception exception)
            {
                return null;
            }
        }

        private string RequestDetailHtml(string iframeUrl)
        {
            try
            {
                UriBuilderAdvance uriBuilderAdvance = new UriBuilderAdvance(iframeUrl);
                uriBuilderAdvance.SetQuery("ts", "/m2x/mixed/main/image/optimize");
                string iframeHtml = GetHtml(uriBuilderAdvance.ToString(), false);

                var getDetailHtmls = new List<Func<string, string>>();
                getDetailHtmls.Add(GetDetailHtml1);
                getDetailHtmls.Add(GetDetailHtml2);

                string detailHtml = null;
                foreach (Func<string, string> getDetailHtml in getDetailHtmls)
                {
                    detailHtml = getDetailHtml.Invoke(iframeHtml);
                    if (string.IsNullOrWhiteSpace(detailHtml) == false)
                    {
                        detailHtml = HtmlReader.GetDafaultHtml(detailHtml);
                        break;
                    }
                }
                return detailHtml;
            }
            catch (Exception exception)
            {
                return null;
            }
        }
        private string GetDetailHtml1(string html)
        {
            return html.GetValue("//div[@id='descContents']");
        }
        private string GetDetailHtml2(string html)
        {
            var wrap_ifr = html.GetValue("//div[@id='wrap_ifr']");
            if (string.IsNullOrWhiteSpace(wrap_ifr)) return null;
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(wrap_ifr);
            HtmlNodeCollection warnings = htmlDocument.DocumentNode.SelectNodes("//div[contains(@class,'warning')]");
            foreach (var item in warnings)
            {
                item.Remove();
            }
            wrap_ifr = htmlDocument.DocumentNode.OuterHtml;
            return wrap_ifr;
        }

        public override Option GetOption(string html, JToken data)
        {
            List<JToken> uItems = data["uItems"]?.ToList();
            if (uItems == null || uItems.Count <= 0) return null;
            Option option = new Option();
            option.detailOptions = new List<Option.DetailOption>();
            foreach (JToken uItem in uItems)
            {
                var uitemOptnNms = new List<string>();
                var uitemOptnTypeNms = new List<string>();
                for (int i = 0; i < 5; i++)
                {
                    string uitemOptnNmKey = $"uitemOptnNm{(i + 1)}";
                    var uitemOptnNm = uItem.Value<string>(uitemOptnNmKey);
                    if (string.IsNullOrWhiteSpace(uitemOptnNm)) break;

                    string uitemOptnTypeNmkey = $"uitemOptnTypeNm{(i + 1)}";
                    var uitemOptnTypeNm = uItem.Value<string>(uitemOptnTypeNmkey);
                    if (string.IsNullOrWhiteSpace(uitemOptnTypeNm)) break;
                    uitemOptnNms.Add(uitemOptnNm);
                    uitemOptnTypeNms.Add(uitemOptnTypeNm);

                }
                if (uitemOptnNms.Count <= 0) continue;
                if (uitemOptnNms.Count != uitemOptnTypeNms.Count) throw new Exception("옵션 수집 중 문제가 발생했습니다.\n관리자에게 문의해주세요.");
                var optionName = string.Join("ㅡ", uitemOptnNms);
                var optionGroupName = string.Join("ㅡ", uitemOptnTypeNms);
                var detailOption = new Option.DetailOption();
                detailOption.optionIds = optionName;
                detailOption.optionGroupName = new List<Name> { new Name(Language.KR, optionGroupName) };
                detailOption.optionName = new List<Name> { new Name(Language.KR, optionName) };

                double? bestAmt = uItem.Value<double?>("bestAmt");
                if (bestAmt == null || bestAmt <= 0) throw new Exception("옵션 수집 중 문제가 발생했습니다.\n관리자에게 문의해주세요.");

                double? originalPrice = null;
                string sellprc = uItem.Value<string>("sellprc");
                if (string.IsNullOrWhiteSpace(sellprc) == false)
                {
                    sellprc = sellprc.SingleSplit("parseInt('", ")");
                    if (sellprc.Contains(","))
                    {
                        sellprc = sellprc.Split(",").First();
                    }
                    originalPrice = sellprc.ParseToDouble();
                    if (originalPrice == null || originalPrice <= 0)
                    {
                        originalPrice = bestAmt;
                    }
                }

                detailOption.price = new List<Price> { new Price(Currency.KRW, (double)originalPrice, (double)bestAmt) };
                int? usablInvQty = uItem.Value<int?>("usablInvQty");
                if (usablInvQty == null || usablInvQty <= 0)
                {
                    detailOption.isSoldOut = true;
                    detailOption.quantity = 0;
                }
                else
                {
                    detailOption.isSoldOut = false;
                    detailOption.quantity = (int)usablInvQty;
                }
                option.detailOptions.Add(detailOption);
            }

            if (option.detailOptions.Count <= 0) return null;

            option.optionGroups = new List<Option.OptionGroup>();
            foreach (Option.DetailOption detailOption in option.detailOptions)
            {
                var groupNames = detailOption.optionGroupName.First().text.Split("ㅡ");
                var optionNames = detailOption.optionName.First().text.Split("ㅡ");

                for (int i = 0; i < groupNames.Length; i++)
                {
                    var groupName = groupNames[i];

                    // 옵션 명이 동일한 이상한 상품 때문에 예외처리 https://trello.com/c/qcIaJfux
                    int count = groupNames.Count(x => x == groupName);
                    if (count > 1) groupName = $"{i + 1}.{groupName}";

                    List<Name> selectGroupNames = option.optionGroups.SelectMany(x => x.groupName).ToList();

                    Option.OptionGroup currentOptionGroup = null;

                    if (selectGroupNames == null || selectGroupNames.Count <= 0)
                    {
                        var optionGroup = new Option.OptionGroup
                        {
                            groupName = new List<Name> { new Name(Language.KR, groupName) },
                            groups = new List<Option.OptionGroup.Group>()
                        };
                        option.optionGroups.Add(optionGroup);
                        currentOptionGroup = optionGroup;
                    }
                    else
                    {
                        Option.OptionGroup find = option.optionGroups.Find(x => x.groupName.First().text == groupName);
                        if (find == null)
                        {
                            var optionGroup = new Option.OptionGroup
                            {
                                groupName = new List<Name> { new Name(Language.KR, groupName) },
                                groups = new List<Option.OptionGroup.Group>()
                            };
                            option.optionGroups.Add(optionGroup);
                            currentOptionGroup = optionGroup;
                        }
                        else
                        {
                            currentOptionGroup = find;
                        }
                    }
                    currentOptionGroup.optionId = groupName;
                    string optionName = optionNames[i];
                    if (currentOptionGroup.groups.Count <= 0)
                    {
                        currentOptionGroup.groups.Add(new Option.OptionGroup.Group
                        {
                            groupId = optionName,
                            optionName = new List<Name> { new Name(Language.KR, optionName) }
                        });
                    }
                    else
                    {
                        var find = currentOptionGroup.groups.Find(x => x.optionName.First().text == optionName);
                        if (find == null)
                        {
                            currentOptionGroup.groups.Add(new Option.OptionGroup.Group
                            {
                                groupId = optionName,
                                optionName = new List<Name> { new Name(Language.KR, optionName) }
                            });
                        }
                    }
                }
            }
            return option;
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
            var isSoldOut =
                    string.Equals(data.Value<string>("soldOut"), "Y", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(data.Value<string>("soldOutPass"), "Y", StringComparison.OrdinalIgnoreCase);

            if (isSoldOut) throw new SaleException(ProductSaleStatus.SoldOut, "품절된 상품입니다.");
        }

        public override int? GetTotalQuantity(string html, JToken data)
        {
            return null;
        }

        public override async Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            string html = await ChatHubService.GetRequest(connectionId, url);
            CollectExtension.CollectSleep(1000);
            var recapchaTag = html.GetValue("//div[@class='recapcha']", TextEx.XPathOption.OuterHtml);
            if (string.IsNullOrWhiteSpace(recapchaTag) == false)
            {
                string getHtml = GetHtml(url);
                if (string.IsNullOrWhiteSpace(getHtml) == false)
                {
                    html = getHtml;
                }
            }
            var data = await GetData(connectionId, url, html);
            return new CollectRawData(url, data, html);
        }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            if (setting?.SsgCollectOption?.UseCardBenefitPrice == true)
            {
                var ssg_price = rawData.Html.GetValue("//span[@class='mndtl_price']//em", TextEx.XPathOption.InnerText);
                if (string.IsNullOrWhiteSpace(ssg_price) == false)
                {
                    double? cardBeefitPrice = ssg_price.ParseToDouble();
                    double salePrice = product.price.First().salePrice;
                    double? discountRate = cardBeefitPrice * 100 / salePrice;
                    discountRate = Math.Truncate((double)discountRate);

                    product.price.First().salePrice = (double)cardBeefitPrice;
                    if (product.option != null)
                    {
                        for (int i = 0; i < product.option.detailOptions.Count; i++)
                        {
                            double? discountOptionSalePrice = (product.option.detailOptions[i].price[0].salePrice * discountRate / 100);
                            discountOptionSalePrice = Math.Ceiling((double)discountOptionSalePrice);
                            product.option.detailOptions[i].price[0].salePrice = (double)discountOptionSalePrice;
                        }
                    }
                }
            }
        }
    }
}
