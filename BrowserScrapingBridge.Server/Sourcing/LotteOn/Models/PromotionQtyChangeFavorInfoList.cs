#nullable disable warnings
using System.Collections.Generic;

namespace BrowserScrapingBridge.Server.Sourcing.LotteOn.Models
{
    public class PromotionQtyChangeFavorInfoList
    {
        public string url { get; set; } = "https://pbf.lotteon.com/product/v1/detail/promotion/promotionQtyChangeFavorInfoList";
        public class PostBody
        {
            public string spdNo { get; set; }
            public string sitmNo { get; set; }
            public string trGrpCd { get; set; }
            public string trNo { get; set; }
            public string lrtrNo { get; set; }
            public string strCd { get; set; }
            public string ctrtTypCd { get; set; }
            public int slPrc { get; set; }
            public int slQty { get; set; }
            public string scatNo { get; set; }
            public string brdNo { get; set; }
            public int sfcoPdMrgnRt { get; set; }
            public int sfcoPdLwstMrgnRt { get; set; }
            public object afflPdMrgnRt { get; set; }
            public object afflPdLwstMrgnRt { get; set; }
            public int pcsLwstMrgnRt { get; set; }
            public string infwMdiaCd { get; set; }
            public string chCsfCd { get; set; }
            public string chTypCd { get; set; }
            public string chNo { get; set; }
            public string chDtlNo { get; set; }
            public string aplyStdDttm { get; set; }
            public string cartDvsCd { get; set; }
            public string thdyPdYn { get; set; }
            public int dvCst { get; set; }
            public string fprdDvPdYn { get; set; }
            public List<object> discountApplyProductList { get; set; }
            public int maxPurQty { get; set; }
            public string stkMgtYn { get; set; }
            public string screenType { get; set; }
            public string dmstOvsDvDvsCd { get; set; }
            public string dvPdTypCd { get; set; }
            public int dvCstStdQty { get; set; }
            public string aplyBestPrcChk { get; set; }
            public string cpnBoxVersion { get; set; }
        }
        public class Response
        {

        }
        public PostBody postBody { get; set; }
        public Response response { get; set; }
    }
}
