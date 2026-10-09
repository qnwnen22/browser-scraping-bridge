#nullable disable warnings
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using BrowserScrapingBridge.Server.Sourcing.LotteOn.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.LotteOn
{
    public class LotteOn : CollectBase
    {
        public override string Site => "LotteOnCol";
        public override string Name => "롯데온";
        public override Language Language { get; set; } = Language.KR;
        public override Currency Currency { get; set; } = Currency.KRW;
        public override bool SupportsCaching { get; set; } = true;

        private async Task LoginConnecting(string connectionId, CancellationToken cancellationToken)
        {
            DebugConsole.WriteLine($"[{connectionId}] {this.Name} 로그인 연결 유지 시작");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                while (cancellationToken.IsCancellationRequested == false)
                {
                    string html = await ChatHubService.GetRequest(connectionId, "https://www.lotteon.com/p/mylotte/v1/api/getIndexInfo");

                    JObject userInfo = html.ToJObject();

                    if (userInfo != null)
                    {
                        var memberName = userInfo.Value("data", "memberInfo", "data")?.Value<string>("memberName");

                        if (string.IsNullOrWhiteSpace(memberName))
                        {
                            DebugConsole.WriteLine($"세션 끊김 Response : {html}");
                            break;
                        }

                        DebugConsole.WriteLine($"{Name} 세션 유지 중  / {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    }

                    await Task.Delay(60000, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                DebugConsole.WriteLine("작업이 취소되었습니다.");
            }
            catch (Exception e)
            {
                DebugConsole.WriteLine(e.Message);
                DebugConsole.WriteLine(e.StackTrace);
            }
            finally
            {
                stopwatch.Stop();
                DebugConsole.WriteLine($"세션 유지 시간 : {stopwatch.Elapsed}");
            }
        }

        private async Task Stop(CancellationTokenSource cancellationToken)
        {
            Thread.Sleep(10000);
            cancellationToken.Cancel();
        }

        public override async Task<string> GetLoginUrlAsync(string connectionId)
        {
            var loginUrl = "https://www.lotteon.com/p/member/login/common?";
            string html = await ChatHubService.GetRequest(connectionId, "https://www.lotteon.com/p/mylotte/v1/api/getIndexInfo");
            JObject userInfo = html.ToJObject();
            if (userInfo != null)
            {
                var memberName = userInfo.Value("data", "memberInfo", "data")?.Value<string>("memberName");
                if (string.IsNullOrWhiteSpace(memberName) == false)
                {
                    loginUrl = null;
                    //var cts = new CancellationTokenSource();
                    //LoginConnecting(connectionId, cts.Token);
                    //Stop(cts);

                }
            }
            return loginUrl;
        }

        public override async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            var pageHtml = await ChatHubService.GetRequest(connectionId, url);
            string script = pageHtml.GetValue("//script[@type='application/javascript']");
            var json = script.SubstringBetween("create('.srchResultWrap',", ");", false);
            var data = json.ToJObject();
            JToken fetch = data["panelContentProps"]?["fetch"];
            if (fetch == null || fetch.HasValues == false) return null;
            string queryParams = fetch.Value<string>("queryParams");
            if (string.IsNullOrWhiteSpace(queryParams)) return null;
            var queryParamList = queryParams.Split("&").ToList();
            UriBuilderAdvance uriBuilderAdvance = new UriBuilderAdvance(url);
            uriBuilderAdvance.SetQuery("u2", $"{(page - 1) * 60}");
            uriBuilderAdvance.SetQuery("u3", "60");
            uriBuilderAdvance.SetQuery("u16", "ranking.desc");

            #region 브랜드 필터 (브랜드에 따라 값이 다름)
            var brand = uriBuilderAdvance.GetQuery("brand");
            if (string.IsNullOrWhiteSpace(brand) == false)
            {
                uriBuilderAdvance.DeleteQuery("brand");
                string[] brands = brand.Split(",");
                uriBuilderAdvance.SetQuery("u5", string.Join("+", brands));
                uriBuilderAdvance.SetQuery("u37", "true");
                uriBuilderAdvance.SetQuery("u39", "0");
            }
            #endregion

            #region 판매처 필터, 롯데 백화점: le, 롯데 마트: lm, 롯데온 쇼핑: li, 입점 판매자: sr
            var cpCode = uriBuilderAdvance.GetQuery("cpCode");
            if (string.IsNullOrWhiteSpace(cpCode) == false)
            {
                uriBuilderAdvance.DeleteQuery("cpCode");
                string[] cpCodes = cpCode.Split(",");
                uriBuilderAdvance.SetQuery("u6", string.Join("+", cpCodes));
                uriBuilderAdvance.SetQuery("u37", "true");
                uriBuilderAdvance.SetQuery("u39", "0");
            }
            #endregion

            #region 배송 필터, 무료배송: null, 오늘ON다: ta, 내일ON다: tma
            var deliveryType = uriBuilderAdvance.GetQuery("deliveryType");
            if (string.IsNullOrWhiteSpace(deliveryType) == false)
            {
                uriBuilderAdvance.DeleteQuery("deliveryType");
                string[] deliveryTypes = deliveryType.Split(",");
                uriBuilderAdvance.SetQuery("u7", string.Join("+", deliveryTypes));
                uriBuilderAdvance.SetQuery("u37", "true");
                uriBuilderAdvance.SetQuery("u39", "0");
            }
            #endregion

            foreach (var queryParam in queryParamList)
            {
                string[] split = queryParam.Split("=");
                if (split.Length != 2) continue;
                uriBuilderAdvance.SetQuery(split[0], split[1]);
            }

            string apiPageUrl = uriBuilderAdvance.ToString();
            var pageJson = await ChatHubService.GetRequest(connectionId, apiPageUrl);
            if (string.IsNullOrWhiteSpace(pageJson)) return null;
            var pageData = pageJson.ToJObject();
            if (pageData == null) return null;
            List<JToken> itemList = pageData["itemList"]?.ToList();
            if (itemList == null || itemList.Count <= 0) return null;
            var collectPageInfos = new List<CollectPageInfo>();
            foreach (JToken item in itemList)
            {
                var productType = item.Value<string>("productType");
                // 롯데온 API에서 응답받는 필드의 이름이 변경됨 26.03.06
                // 방어코드 추가
                if (string.IsNullOrWhiteSpace(productType)) productType = item.Value<string>("pdType");
                if (productType.ToUpper() == "BUNDLE") continue;

                // 롯데온 API에서 응답받는 필드의 이름이 변경됨 26.03.06
                // 방어코드 추가
                var productId = item.Value<string>("productId");
                if (string.IsNullOrWhiteSpace(productId)) productId = item.Value<string>("pdId");

                if (string.IsNullOrWhiteSpace(productId)) continue;
                string productUrl = $"https://www.lotteon.com/m/product/{productId}";

                // 롯데온 API에서 응답받는 필드의 이름이 변경됨 26.03.06
                // 방어코드 추가
                var productName = item.Value<string>("productName");
                if (string.IsNullOrWhiteSpace(productName)) productName = item.Value<string>("pdName");

                CollectPageInfo collectPageInfo = new CollectPageInfo();
                collectPageInfo.Code = productId;
                collectPageInfo.Url = productUrl;
                collectPageInfo.Title = productName;
                collectPageInfos.Add(collectPageInfo);
            }
            return collectPageInfos;
        }

        public override void ThrowIfCaptcha(string url, string html)
        {
        }

        public override string GetCode(string url)
        {
            var uri = new Uri(url);
            return uri.Segments.Last();
        }

        public override string CreateProductUrl(string code)
        {
            return $"https://www.lotteon.com/m/product/{code}";
        }

        public override async Task<JToken> GetData(string connectionId, string url, string html)
        {
            string code = GetCode(url);

            string productJson = await ChatHubService.GetRequest(connectionId, $"https://pbf.lotteon.com/product/v2/detail/search/base/pd/{code}?isNotContainOptMapping=true");
            JObject productData = productJson.ToJObject();
            JToken data = productData.GetValue("data");

            string detailPageHtml = await GetDetailPageHtmlAsync(connectionId, data);
            data["DetailHtml"] = detailPageHtml;

            JToken promotion = await GetPromotionData(connectionId, data);
            data["Price"] = promotion;

            JToken optionData = await GetOptionData(connectionId, html, data);
            data["Option"] = optionData;



            return data;
        }
        public async Task<string> GetDetailPageHtmlAsync(string connectionId, JToken data)
        {
            var epnJsn = data["descInfo"]?["epnJsn"]?.ToList();
            if (epnJsn == null || epnJsn.Count <= 0) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            JToken epn = epnJsn.FirstOrDefault();
            if (epn == null || epn.HasValues == false) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            string dtlFileRteNm = epn.Value<string>("dtlFileRteNm");
            if (string.IsNullOrWhiteSpace(dtlFileRteNm)) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            string dtlFileNm = epn.Value<string>("dtlFileNm");
            if (string.IsNullOrWhiteSpace(dtlFileNm)) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            string model = string.Join("", "itemdetail", dtlFileRteNm, dtlFileNm);
            var uriBuilderAdvance = new UriBuilderAdvance("https://llis-goodsdetail.lotteon.com/goodsdetail");
            uriBuilderAdvance.SetQuery("model", model);
            string detailPageUrl = uriBuilderAdvance.ToString();
            string detailHtml = await ChatHubService.GetRequest(connectionId, detailPageUrl);
            if (string.IsNullOrWhiteSpace(detailHtml)) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            string body = detailHtml.GetValue("//body");
            if (string.IsNullOrWhiteSpace(body)) throw new Exception("상세 페이지를 찾을 수 없습니다.");
            string dafaultHtml = HtmlReader.GetDafaultHtml(body);
            return dafaultHtml;
        }
        private async Task<JToken> GetPromotionData(string connectionId, JToken data)
        {
            try
            {
                var postModel = new PromotionQtyChangeFavorInfoList.PostBody();
                postModel.spdNo = data["basicInfo"].Value<string>("spdNo");
                postModel.sitmNo = data["basicInfo"].Value<string>("sitmNo");
                postModel.trGrpCd = data["basicInfo"].Value<string>("trGrpCd");
                postModel.trNo = data["basicInfo"].Value<string>("trNo");
                postModel.lrtrNo = data["basicInfo"].Value<string>("lrtrNo");
                if (string.IsNullOrWhiteSpace(postModel.lrtrNo)) postModel.lrtrNo = "";
                postModel.strCd = data["basicInfo"].Value<string>("strCd");
                if (string.IsNullOrWhiteSpace(postModel.strCd)) postModel.strCd = "";
                postModel.ctrtTypCd = data["basicInfo"].Value<string>("ctrtTypCd");
                postModel.slPrc = data["priceInfo"].Value<int>("slPrc");
                postModel.slQty = 1;
                postModel.scatNo = data["basicInfo"]?.Value<string>("scatNo");
                postModel.brdNo = data["basicInfo"]?.Value<string>("brdNo");
                postModel.sfcoPdMrgnRt = data["priceInfo"].Value<int>("sfcoPdMrgnRt");
                postModel.sfcoPdLwstMrgnRt = data["priceInfo"].Value<int>("sfcoPdLwstMrgnRt");
                postModel.afflPdMrgnRt = null;
                postModel.afflPdLwstMrgnRt = null;
                postModel.pcsLwstMrgnRt = data["priceInfo"].Value<int>("pcsLwstMrgnRt");
                postModel.infwMdiaCd = "PC";
                postModel.chCsfCd = "DI";
                postModel.chTypCd = "DI02";
                postModel.chNo = "100195";
                postModel.chDtlNo = "1000617";
                postModel.aplyStdDttm = DateTime.Now.ToString("yyyyMMddHHmmss");
                postModel.cartDvsCd = data["dlvInfo"].Value<string>("cartDvsCd");
                postModel.thdyPdYn = data["basicInfo"].Value<string>("thdyPdYn");
                postModel.dvCst = data["dlvInfo"].Value<int>("dvCst");
                postModel.fprdDvPdYn = "N";
                postModel.discountApplyProductList = new List<object>();
                postModel.maxPurQty = data["basicInfo"].Value<int>("maxPurQty");
                postModel.stkMgtYn = data["stckInfo"].Value<string>("stkMgtYn");
                postModel.screenType = "PRODUCT";
                postModel.dmstOvsDvDvsCd = data["dlvInfo"].Value<string>("dmstOvsDvDvsCd");
                postModel.dvPdTypCd = data["dlvInfo"].Value<string>("dvPdTypCd");
                postModel.dvCstStdQty = data["dlvInfo"].Value<int>("dvCstStdQty");
                postModel.aplyBestPrcChk = "Y";
                postModel.cpnBoxVersion = "V2";
                string postString = postModel.ToSerializeObject();
                string url = "https://pbf.lotteon.com/product/v1/detail/promotion/promotionQtyChangeFavorInfoList";
                var header = new Dictionary<string, string>();
                header["Content-Type"] = "application/json;charset=UTF-8";
                string promotionJson = await ChatHubService.PostRequest(connectionId, url, header, postString);
                var promotionData = promotionJson.ToJObject();
                if (promotionData == null) return null;
                JToken result = promotionData["data"];
                if (result == null || result.HasValues == false) return null;
                return result;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public async Task<JToken> GetOptionData(string connectionId, string html, JToken data)
        {
            string pdId = html.GetValue("//meta[@property='ant:pdId']", "content");

            JToken basicInfo = data["basicInfo"];

            string pdNo = basicInfo.Value<string>("pdNo");
            string sitmNo = basicInfo.Value<string>("sitmNo");

            string trNo = basicInfo.Value<string>("trNo");
            string trGrpCd = basicInfo.Value<string>("trGrpCd");
            string lrtrNo = basicInfo.Value<string>("lrtrNo");

            var uriBuilderAdvance = new UriBuilderAdvance($"https://pbf.lotteon.com/product/v2/detail/option/mapping/{pdNo}/{sitmNo}");
            uriBuilderAdvance.SetQuery("trNo", trNo);
            uriBuilderAdvance.SetQuery("trGrpCd", trGrpCd);
            uriBuilderAdvance.SetQuery("lrtrNo", lrtrNo);
            uriBuilderAdvance.SetQuery("pdNo", pdNo);
            string optionRequestUrl = uriBuilderAdvance.ToString();

            var optionJson = await ChatHubService.GetRequest(connectionId, optionRequestUrl);
            JObject optionData = optionJson.ToJObject();
            if (optionData == null) return null;
            JToken optionInfo = optionData["data"]?["optionInfo"];
            if (optionInfo == null || optionInfo.HasValues == false) return null;
            return optionInfo;
        }
        private async Task<JToken> GetExtlmsaPromotionFavorBoxBenefits(string connectionId, JToken data)
        {
            var extlmsaPromotionFavorBoxBenefits = new ExtlmsaPromotionFavorBoxBenefits();
            var postBody = new ExtlmsaPromotionFavorBoxBenefits.PostBody();
            postBody.spdNo = data["basicInfo"]?["spdNo"].ToString();
            postBody.sitmNo = data["basicInfo"]?["itmNo"].ToString();
            postBody.trGrpCd = data["basicInfo"]?["trGrpCd"].ToString();
            postBody.trNo = data["basicInfo"]?["trNo"].ToString();
            postBody.lrtrNo = data["basicInfo"]?["lrtrNo"].ToString();
            postBody.strCd = "";
            postBody.ctrtTypCd = data["basicInfo"]?["ctrtTypCd"].ToString();
            postBody.slPrc = (int)data["priceInfo"]?.Value<int?>("slPrc");
            postBody.slQty = 1;
            postBody.scatNo = data["basicInfo"]?["scatNo"].ToString();
            postBody.brdNo = data["basicInfo"]?["brdNo"].ToString();
            postBody.sfcoPdMrgnRt = (int)data["priceInfo"]?.Value<int?>("sfcoPdMrgnRt");
            postBody.sfcoPdLwstMrgnRt = (int)data["priceInfo"]?.Value<int?>("sfcoPdLwstMrgnRt");
            postBody.afflPdMrgnRt = null;
            postBody.afflPdLwstMrgnRt = null;
            postBody.pcsLwstMrgnRt = 0;
            postBody.infwMdiaCd = "PC";
            postBody.chCsfCd = "DI";
            postBody.chTypCd = "DI02";
            postBody.chNo = "100195";
            postBody.chDtlNo = "1000617";
            postBody.aplyStdDttm = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            postBody.cartDvsCd = data["dlvInfo"]?["cartDvsCd"].ToString();
            postBody.thdyPdYn = data["basicInfo"]?["thdyPdYn"].ToString();
            postBody.dvCst = (int)data["dlvInfo"].Value<int?>("dvCst");
            postBody.fprdDvPdYn = "N";
            postBody.discountApplyProductList = null;
            var post = postBody.ToSerializeObject();
            var responseString = await ChatHubService.PostRequest(connectionId, extlmsaPromotionFavorBoxBenefits.url, extlmsaPromotionFavorBoxBenefits.headers, post);
            return responseString.ToJObject();
        }
        private async Task<string> GetQtyChangeFavorInfoList(string connectionId, JToken data)
        {
            var qtyChangeFavorInfoListPost = new QtyChangeFavorInfoList.PostBody();
            qtyChangeFavorInfoListPost.spdNo = data["basicInfo"].Value<string>("pdNo");
            qtyChangeFavorInfoListPost.sitmNo = data["basicInfo"].Value<string>("itmNo");
            qtyChangeFavorInfoListPost.trGrpCd = data["basicInfo"].Value<string>("trGrpCd");
            qtyChangeFavorInfoListPost.trNo = data["basicInfo"].Value<string>("trNo");
            qtyChangeFavorInfoListPost.lrtrNo = "";
            qtyChangeFavorInfoListPost.strCd = "";
            qtyChangeFavorInfoListPost.ctrtTypCd = data["basicInfo"].Value<string>("ctrtTypCd");
            qtyChangeFavorInfoListPost.slPrc = data["priceInfo"].Value<int>("slPrc");
            qtyChangeFavorInfoListPost.slQty = data["basicInfo"].Value<int>("minPurQty");
            qtyChangeFavorInfoListPost.scatNo = data["basicInfo"].Value<string>("scatNo");
            qtyChangeFavorInfoListPost.brdNo = data["basicInfo"].Value<string>("brdNo");
            qtyChangeFavorInfoListPost.sfcoPdMrgnRt = (int)data["priceInfo"].Value<double>("sfcoPdMrgnRt");
            qtyChangeFavorInfoListPost.sfcoPdLwstMrgnRt = (int)data["priceInfo"].Value<double>("sfcoPdLwstMrgnRt");
            qtyChangeFavorInfoListPost.pcsLwstMrgnRt = (int)data["priceInfo"].Value<double>("pcsLwstMrgnRt");
            qtyChangeFavorInfoListPost.infwMdiaCd = "PC"; // 고정값
            qtyChangeFavorInfoListPost.chCsfCd = "DI"; // 고정값
            qtyChangeFavorInfoListPost.chTypCd = "DI02"; // 고정값
            qtyChangeFavorInfoListPost.chNo = "100195"; // 고정값
            qtyChangeFavorInfoListPost.chDtlNo = "1000617"; // 고정값
            qtyChangeFavorInfoListPost.aplyStdDttm = DateTime.UtcNow.ToString("yyyyMMddHHmmss").ToString();
            qtyChangeFavorInfoListPost.cartDvsCd = data["dlvInfo"].Value<string>("cartDvsCd");
            qtyChangeFavorInfoListPost.thdyPdYn = data["basicInfo"].Value<string>("thdyPdYn");
            qtyChangeFavorInfoListPost.dvCst = data["dlvInfo"].Value<int>("dvCst");
            qtyChangeFavorInfoListPost.fprdDvPdYn = data["dlvInfo"].Value<string>("dnDvPdYn");
            qtyChangeFavorInfoListPost.discountApplyProductList = new List<object>();
            qtyChangeFavorInfoListPost.maxPurQty = data["basicInfo"].Value<int>("maxPurQty");
            qtyChangeFavorInfoListPost.stkMgtYn = data["stckInfo"].Value<string>("stkMgtYn");
            qtyChangeFavorInfoListPost.screenType = "PRODUCT"; // 고정값
            qtyChangeFavorInfoListPost.dmstOvsDvDvsCd = data["basicInfo"].Value<string>("dmstOvsDvDvsCd");
            qtyChangeFavorInfoListPost.dvPdTypCd = data["dlvInfo"].Value<string>("dvPdTypCd");
            qtyChangeFavorInfoListPost.dvCstStdQty = data["dlvInfo"].Value<int>("dvCstStdQty");
            qtyChangeFavorInfoListPost.aplyBestPrcChk = "Y";  // 고정값
            qtyChangeFavorInfoListPost.pyMnsExcpLst = new List<object>();
            qtyChangeFavorInfoListPost.cpnBoxVersion = "V2"; // 고정값
            string post = qtyChangeFavorInfoListPost.ToSerializeObject();
            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json; charset=UTF-8");
            var qtyChangeFavorInfoList = await ChatHubService.PostRequest(connectionId, "https://pbf.lotteon.com/product/v2/extlmsa/promotion/qtyChangeFavorInfoList", headers, post);
            return qtyChangeFavorInfoList;
        }

        public override List<Name> GetTitle(string html, JToken data)
        {
            string pdNm = data["basicInfo"]?.Value<string>("pdNm");
            if (string.IsNullOrWhiteSpace(pdNm)) throw new Exception("상품명을 찾을 수 없습니다.");
            return new List<Name> { new Name(Language.KR, pdNm) };
        }

        public override List<Name> GetBrand(string html, JToken data)
        {
            string brdNm = data["basicInfo"]?.Value<string>("brdNm");
            if (string.IsNullOrWhiteSpace(brdNm)) return null;
            return new List<Name> { new Name(Language.KR, brdNm) };
        }

        public override string GetModel(string html, JToken data)
        {
            var mdlNo = data.Value("basicInfo")?.Value<string>("mdlNo");
            if (string.IsNullOrWhiteSpace(mdlNo)) return null;
            return mdlNo;
        }

        public override List<Desc> GetDescs(string html, JToken data)
        {
            List<Desc> descs = new List<Desc>();
            int count = 1;

            var attrList = data["attrInfo"]?["attrList"]?.ToList();
            if (attrList != null && attrList.Count > 0)
            {
                foreach (JToken attr in attrList)
                {
                    string attrDispNm = attr.Value<string>("attrDispNm");
                    if (string.IsNullOrWhiteSpace(attrDispNm)) continue;
                    string attrValDispNm = attr.Value<string>("attrValDispNm");
                    if (string.IsNullOrWhiteSpace(attrValDispNm)) continue;
                    Desc desc = new Desc(count, Language.KR, attrDispNm, attrValDispNm);
                    descs.Add(desc);
                    count++;
                }
            }

            var pdItmsArtlJsn = data["artlInfo"]?["pdItmsArtlJsn"]?.ToList();
            if (pdItmsArtlJsn != null && pdItmsArtlJsn.Count > 0)
            {
                foreach (var pdArtl in pdItmsArtlJsn)
                {
                    string pdArtlCdNm = pdArtl.Value<string>("pdArtlCdNm");
                    if (string.IsNullOrWhiteSpace(pdArtlCdNm)) continue;
                    string pdArtlCnts = pdArtl.Value<string>("pdArtlCnts");
                    if (string.IsNullOrWhiteSpace(pdArtlCnts)) continue;
                    Desc desc = new Desc(count, Language.KR, pdArtlCdNm, pdArtlCnts);
                    descs.Add(desc);
                    count++;
                }
            }
            if (descs.Count <= 0) return null;
            return descs;
        }

        public override List<Tag> GetTags(string html, JToken data)
        {
            return null;
        }

        public override List<Price> GetPrice(string html, JToken data)
        {
            List<Price> result = null;
            var promotionData = data.Value("Price");
            if (promotionData != null)
            {
                double? slPrc = promotionData.Value<double?>("slPrc");
                double? immdDcAplyTotAmt = promotionData?.Value<double?>("immdDcAplyTotAmt");
                if (slPrc != null && slPrc > 0)
                {
                    if (immdDcAplyTotAmt == null || immdDcAplyTotAmt <= 0) immdDcAplyTotAmt = slPrc;
                    result = new List<Price> { new Price(this.Currency, (double)slPrc, (double)immdDcAplyTotAmt, true) };
                }
            }

            if (result == null)
            {
                double? slPrc = data["priceInfo"].Value<double?>("slPrc");
                if (slPrc == null || slPrc <= 0) throw new Exception("기본 가격을 찾을 수 없습니다.");
                double originalPrice = (double)slPrc;
                result = new List<Price> { new Price(this.Currency, originalPrice, originalPrice, true) };
            }
            return result;
        }

        public override List<string> GetMainImages(string html, JToken data)
        {
            List<JToken> imageList = data["imgInfo"]?["imageList"]?.ToList();
            if (imageList == null || imageList.Count <= 0) throw new Exception("상품 이미지를 찾을 수 없습니다.");

            var mainImages = new List<string>();
            string host = "https://contents.lotteon.com/itemimage";
            foreach (JToken imageItem in imageList)
            {
                var imgRteNm = imageItem.Value<string>("imgRteNm");
                if (string.IsNullOrWhiteSpace(imgRteNm)) continue;
                var imgFileNm = imageItem.Value<string>("imgFileNm");
                if (string.IsNullOrWhiteSpace(imgFileNm)) continue;
                string imageUrl = $"{host}{imgRteNm}{imgFileNm}";
                mainImages.Add(imageUrl);
            }
            if (mainImages.Count == 0)
            {
                List<string> origImgFileNms = imageList.Select(x => x.Value<string>("origImgFileNm")).ToList();
                for (int i = 0; i < origImgFileNms.Count; i++)
                {
                    if (origImgFileNms[i].IndexOf("data:image") == 0) throw new Exception("비정상적인 상품 이미지가 포함되어 있습니다.");
                    int queryIndex = origImgFileNms[i].IndexOf("?");
                    if (queryIndex < 0) continue;
                    origImgFileNms[i] = origImgFileNms[i].Substring(0, queryIndex);
                }
                return origImgFileNms;
            }
            else
            {
                return mainImages;
            }
        }

        public override string GetVideoUrl(string html, JToken data)
        {
            return null;
        }

        public override string GetDetailPageHtml(string html, JToken data)
        {
            var detailHtml = data.Value<string>("DetailHtml");

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(detailHtml);

            HtmlNodeCollection imgs = htmlDocument.DocumentNode.SelectNodes("//img");
            if (imgs != null)
            {

                foreach (HtmlNode img in imgs)
                {
                    foreach (var attr in img.Attributes.ToList())
                    {
                        if (attr.Name != "src")
                        {
                            img.Attributes.Remove(attr);
                        }
                    }
                }
            }
            return htmlDocument.DocumentNode.OuterHtml;
        }

        public override Option GetOption(string html, JToken data)
        {
            try
            {
                string pdId = html.GetValue("//meta[@property='ant:pdId']", "content");

                JToken basicInfo = data["basicInfo"];

                string pdNo = basicInfo.Value<string>("pdNo");
                string sitmNo = basicInfo.Value<string>("sitmNo");

                string trNo = basicInfo.Value<string>("trNo");
                string trGrpCd = basicInfo.Value<string>("trGrpCd");
                string lrtrNo = basicInfo.Value<string>("lrtrNo");

                var uriBuilderAdvance = new UriBuilderAdvance($"https://pbf.lotteon.com/product/v2/detail/option/mapping/{pdNo}/{sitmNo}");
                uriBuilderAdvance.SetQuery("trNo", trNo);
                uriBuilderAdvance.SetQuery("trGrpCd", trGrpCd);
                uriBuilderAdvance.SetQuery("lrtrNo", lrtrNo);
                uriBuilderAdvance.SetQuery("pdNo", pdNo);
                string optionRequestUrl = uriBuilderAdvance.ToString();

                JToken optionInfo = data.Value("Option");
                if (optionInfo == null || optionInfo.HasValues == false) return null;

                Option option = new Option();

                List<JToken> optionList = optionInfo["optionList"]?.ToList();
                if (optionList == null || optionList.Count <= 0) return null;
                option.optionGroups = new List<Option.OptionGroup>();
                foreach (JToken optionListItem in optionList)
                {
                    var attrValFrmCd = optionListItem.Value<string>("attrValFrmCd");
                    var title = optionListItem.Value<string>("title");
                    var name = optionListItem.Value<string>("name");

                    var optionGroup = new Option.OptionGroup();
                    optionGroup.optionId = attrValFrmCd;
                    optionGroup.groupName = new List<Name> { new Name(Language.KR, title) };

                    optionGroup.groups = new List<Option.OptionGroup.Group>();
                    List<JToken> options = optionListItem["options"].ToList();
                    foreach (JToken optionsItem in options)
                    {
                        var group = new Option.OptionGroup.Group();
                        group.groupId = optionsItem.Value<string>("value");
                        var label = optionsItem.Value<string>("label");
                        group.optionName = new List<Name> { new Name(Language.KR, label) };
                        optionGroup.groups.Add(group);
                    }
                    option.optionGroups.Add(optionGroup);
                }

                option.detailOptions = new List<Option.DetailOption>();
                JToken optionMappingInfo = optionInfo["optionMappingInfo"];
                JEnumerable<JToken> children = optionMappingInfo.Children();

                string optionGroupName = ProductHelper.GetOptionGroupName(option.optionGroups);
                var optionGroups = option.optionGroups.SelectMany(x => x.groups).ToList();
                List<List<Option.OptionGroup.Group>> selectOptionGroups = option.optionGroups.Select(x => x.groups).ToList();
                IEnumerable<IEnumerable<Option.OptionGroup.Group>> cartesianProduct = GetCartesianProduct(selectOptionGroups);

                var createCartesianOptions = new Dictionary<string, string>();
                List<IEnumerable<Option.OptionGroup.Group>> cartesianOptions = cartesianProduct.ToList();
                foreach (IEnumerable<Option.OptionGroup.Group> cartesianOption in cartesianOptions)
                {
                    IEnumerable<string> ids = cartesianOption.Select(x => x.groupId);
                    IEnumerable<string> optionNames = cartesianOption.Select(x => x.optionName.First().text);
                    var createId = string.Join("ㅡ", ids);
                    var createOptionName = string.Join("ㅡ", optionNames);
                    createCartesianOptions.Add(createId, createOptionName);
                }

                foreach (KeyValuePair<string, string> cartesianOption in createCartesianOptions)
                {
                    string key = cartesianOption.Key.Replace("ㅡ", "_");
                    JToken detailOptionData = optionMappingInfo[key];
                    if (detailOptionData == null || detailOptionData.HasValues == false) continue;

                    List<double> priceList = new List<double>();
                    string[] priceKeys = new string[]
                    {
                        "displayPrc",
                        "frstFvrPrc",
                        "scndFvrPrc",
                        //"onerFvrPrc",
                        //"fthFprdDvDcPrc"
                    };

                    foreach (var priceKey in priceKeys)
                    {
                        double? getPrice = detailOptionData.Value<double?>(priceKey);
                        if (getPrice != null && getPrice > 0)
                        {
                            priceList.Add((double)getPrice);
                        }
                    }
                    if (priceList.Count <= 0) throw new Exception("옵션 가격을 확인할 수 없습니다.");
                    double originalPrice = priceList.Max();
                    double salePrice = priceList.Min();

                    int? stkQty = detailOptionData.Value<int?>("stkQty");

                    Option.DetailOption detailOption = new Option.DetailOption();
                    detailOption.optionIds = cartesianOption.Key;
                    detailOption.optionGroupName = new List<Name> { new Name(Language.KR, optionGroupName) };
                    detailOption.optionName = new List<Name> { new Name(Language.KR, cartesianOption.Value) };
                    detailOption.price = new List<Price> { new Price(Currency.KRW, originalPrice, salePrice) };
                    if (stkQty == null || stkQty <= 0)
                    {
                        detailOption.isSoldOut = true;
                        detailOption.quantity = 0;
                    }
                    else
                    {
                        detailOption.isSoldOut = false;
                        detailOption.quantity = (int)stkQty;
                    }
                    option.detailOptions.Add(detailOption);
                }
                return option;
            }
            catch (Exception exception)
            {
                throw new Exception($"옵션 수집 중 문제가 발생했습니다.\n{exception.Message}", exception);
            }
        }
        /// <summary>
        /// 모든 옵션에 곱집합(카테시안 곱) 가져오기
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequences"></param>
        /// <returns></returns>
        private IEnumerable<IEnumerable<T>> GetCartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> result = new[] { Enumerable.Empty<T>() };

            foreach (var sequence in sequences)
            {
                result = result.SelectMany(acc => sequence, (acc, item) => acc.Append(item));
            }

            return result;
        }

        public override void ThrowIfOutOfStock(string html, JToken data, Product product)
        {
            string spd = data.SelectToken("basicInfo.spdSlStatCd")?.ToString();
            if (string.Equals(spd, "SOUT", StringComparison.OrdinalIgnoreCase))
            {
                throw new SaleException(ProductSaleStatus.SoldOut, "품절된 상품입니다.");
            }
        }

        public override int? GetTotalQuantity(string html, JToken data)
        {
            return null;
        }

        public override async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            if (setting?.LotteOnCollectOption == null) return;

            var benefits = await GetExtlmsaPromotionFavorBoxBenefits(connectionId, rawData.Data);
            List<JToken> discountGroups = benefits?["data"]?["discountGroups"]?.ToList();

            var salePrice = product.price.First().salePrice;
            if (setting?.LotteOnCollectOption?.UseProductCoupon == true)
            {
                salePrice = CalculrateCouponPrice(salePrice, discountGroups, "즉시할인/상품쿠폰");
                if (product.option != null)
                {
                    for (int i = 0; i < product.option.detailOptions.Count; i++)
                    {
                        product.option.detailOptions[i].price[0].salePrice = CalculrateCouponPrice(product.option.detailOptions[i].price[0].salePrice, discountGroups, "즉시할인/상품쿠폰");
                    }
                }
            }

            if (setting?.LotteOnCollectOption?.UseProductCoupon == true)
            {
                salePrice = CalculrateCouponPrice(salePrice, discountGroups, "상품쿠폰");
                if (product.option != null)
                {
                    for (int i = 0; i < product.option.detailOptions.Count; i++)
                    {
                        product.option.detailOptions[i].price[0].salePrice = CalculrateCouponPrice(product.option.detailOptions[i].price[0].salePrice, discountGroups, "상품쿠폰");
                    }
                }
            }

            if (setting?.LotteOnCollectOption?.UseOverlapCoupon == true)
            {
                salePrice = CalculrateCouponPrice(salePrice, discountGroups, "중복쿠폰");
                if (product.option != null)
                {
                    for (int i = 0; i < product.option.detailOptions.Count; i++)
                    {
                        product.option.detailOptions[i].price[0].salePrice = CalculrateCouponPrice(product.option.detailOptions[i].price[0].salePrice, discountGroups, "중복쿠폰");
                    }
                }
            }

            if (setting?.LotteOnCollectOption?.UseStoreCoupong == true)
            {
                salePrice = CalculrateCouponPrice(salePrice, discountGroups, "스토어쿠폰");
                if (product.option != null)
                {
                    for (int i = 0; i < product.option.detailOptions.Count; i++)
                    {
                        product.option.detailOptions[i].price[0].salePrice = CalculrateCouponPrice(product.option.detailOptions[i].price[0].salePrice, discountGroups, "스토어쿠폰");
                    }
                }
            }

            if (setting?.LotteOnCollectOption?.UseCardOrCartCoupon == true)
            {
                salePrice = CalculrateCouponPrice(salePrice, discountGroups, "카드즉시할인/장바구니쿠폰");
                if (product.option != null)
                {
                    for (int i = 0; i < product.option.detailOptions.Count; i++)
                    {
                        product.option.detailOptions[i].price[0].salePrice = CalculrateCouponPrice(product.option.detailOptions[i].price[0].salePrice, discountGroups, "카드즉시할인/장바구니쿠폰");
                    }
                }
            }
            product.price[0].salePrice = salePrice;
        }
        private double CalculrateCouponPrice(double price, List<JToken> discountGroups, string title)
        {
            double result = price;
            JToken discountGroup = discountGroups.Find(x => x.Value<string>("title") == title);
            if (discountGroup != null)
            {
                List<JToken> discountApplyPromotionList = discountGroup["discountApplyPromotionList"]?.ToList();
                if (discountApplyPromotionList != null && discountApplyPromotionList.Count > 0)
                {
                    JToken bestPromotion = discountApplyPromotionList.Find(x => x.Value<string>("bestPrAplyYn") == "Y");
                    if (bestPromotion != null)
                    {
                        bool? isAppOnly = bestPromotion.Value("mainFlag")?.Value<bool?>("isAppOnly");
                        if (isAppOnly != true)
                        {
                            if (bestPromotion.Value<string>("groupId") == "IMMD_AND_PRODUCT_COUPON")
                            {
                                JToken defaultPromotion = discountApplyPromotionList.Find(x => x.Value<string>("dispName").Contains("롯데ON 즉시할인"));
                                if (defaultPromotion != null) result += defaultPromotion.Value<double>("dcAmt");
                            }

                            var prAplyYn = bestPromotion.Value<string>("prAplyYn");
                            if (prAplyYn == "Y")
                            {
                                double? maxDiscount = null;
                                List<JToken> dispDtls = bestPromotion["dispDtls"]?.ToList();
                                if (dispDtls != null && dispDtls.Count > 0)
                                {
                                    foreach (var dispDtl in dispDtls)
                                    {
                                        string text = dispDtl.ToString().SingleSplit("최대", "원");
                                        if (string.IsNullOrWhiteSpace(text) == false)
                                        {
                                            maxDiscount = text.ParseToDouble();
                                            break;
                                        }
                                    }
                                }

                                string dcTypCd = bestPromotion.Value<string>("dcTypCd");
                                switch (dcTypCd)
                                {
                                    case "FL":
                                        int dcAmt = bestPromotion.Value<int>("dcAmt");
                                        result -= dcAmt;
                                        break;

                                    default:
                                        double dcRt = bestPromotion.Value<double>("dcRt");
                                        double cal = Math.Truncate((result * (dcRt) / 100d) / 10d) * 10d;
                                        if (maxDiscount != null && cal > maxDiscount)
                                        {
                                            cal = (double)maxDiscount;
                                        }
                                        result -= cal;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
