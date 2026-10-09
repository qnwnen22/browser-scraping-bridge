#nullable disable warnings
using System.Collections.Generic;

namespace BrowserScrapingBridge.Server.Sourcing.LotteOn.Models
{
    public class QtyChangeFavorInfoList
    {
        public string url { get; set; } = "https://pbf.lotteon.com/product/v2/extlmsa/promotion/qtyChangeFavorInfoList";
        public Dictionary<string, string> headers { get; set; }
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
            public List<object> pyMnsExcpLst { get; set; }
            public string cpnBoxVersion { get; set; }
        }
        public PostBody postBody { get; set; }

        public class Response
        {
            public string returnCode { get; set; }
            public string message { get; set; }
            public object subMessages { get; set; }
            public object messageType { get; set; }
            public class Data
            {
                public object mbNo { get; set; }
                public string spdNo { get; set; }
                public string sitmNo { get; set; }
                public int slPrc { get; set; }
                public int slQty { get; set; }
                public class DiscountApplyProduct
                {
                    public string prNo { get; set; }
                    public string prNm { get; set; }
                    public int prRegNo { get; set; }
                    public string prFvrNo { get; set; }
                    public string prTypCd { get; set; }
                    public string prTypNm { get; set; }
                    public string prKndCd { get; set; }
                    public string prKndNm { get; set; }
                    public string dcTnnoCd { get; set; }
                    public string dcTypCd { get; set; }
                    public int dcAmt { get; set; }
                    public string cpnNo { get; set; }
                    public string cpnIssNo { get; set; }
                    public int cartSn { get; set; }
                    public string sptDcPgmAgrYn { get; set; }
                    public int sfcoShrAmt { get; set; }
                    public int entpShrAmt { get; set; }
                    public int dcPgmCmsn { get; set; }
                    public object fvrNm { get; set; }
                    public float dcRt { get; set; }
                    public object mrgnPrsvRt { get; set; }
                    public object cphsPrNo { get; set; }
                    public object cphsPrSfcoShrVal { get; set; }
                    public object cphsPrEntpShrVal { get; set; }
                    public object dcSectMaxVal { get; set; }
                    public object chlPcsTypCd { get; set; }
                    public class PyMnsDtl
                    {
                        public string prNo { get; set; }
                        public string pyMnsCd { get; set; }
                        public string pyMnsDtlTypCd { get; set; }
                        public string pyMnsDtlVal { get; set; }
                        public int regNo { get; set; }
                    }
                    public PyMnsDtl pyMnsDtl { get; set; }
                }
                public List<DiscountApplyProduct> discountApplyProductList { get; set; }
                public class OneQtyDiscountApplyProduct
                {
                    public object prNo { get; set; }
                    public object prNm { get; set; }
                    public int prRegNo { get; set; }
                    public object prFvrNo { get; set; }
                    public object prTypCd { get; set; }
                    public object prTypNm { get; set; }
                    public string prKndCd { get; set; }
                    public string prKndNm { get; set; }
                    public string dcTnnoCd { get; set; }
                    public object dcTypCd { get; set; }
                    public int dcAmt { get; set; }
                    public object cpnNo { get; set; }
                    public object cpnIssNo { get; set; }
                    public int cartSn { get; set; }
                    public object sptDcPgmAgrYn { get; set; }
                    public object sfcoShrAmt { get; set; }
                    public object entpShrAmt { get; set; }
                    public object dcPgmCmsn { get; set; }
                    public string fvrNm { get; set; }
                    public object dcRt { get; set; }
                    public object mrgnPrsvRt { get; set; }
                    public object cphsPrNo { get; set; }
                    public object cphsPrSfcoShrVal { get; set; }
                    public object cphsPrEntpShrVal { get; set; }
                    public object dcSectMaxVal { get; set; }
                    public object chlPcsTypCd { get; set; }
                    public object pyMnsDtl { get; set; }
                }
                public List<OneQtyDiscountApplyProduct> oneQtyDiscountApplyProductList { get; set; }
                public int immdDcAplyTotAmt { get; set; }
                public int immdDcAplyUtAmt { get; set; }
                public int immdDcRt { get; set; }
                public string mbFvrOffrDvsCd { get; set; }
                public int mbFvrOffrAmt { get; set; }
                public int mbFvrOffrUtAmt { get; set; }
                public string adtnDcDvsCd { get; set; }
                public int adtnDcAplyTotAmt { get; set; }
                public int adtnDcAplyUtAmt { get; set; }
                public int adtnDcAmt { get; set; }
                public int adtnDcUtAmt { get; set; }
                public string orderDcAplyDispNm { get; set; }
                public int orderDcAplyTotAmt { get; set; }
                public int orderDcAplyUtAmt { get; set; }
                public string aplyPsbCpnYn { get; set; }
                public string dnldPsbCpnYn { get; set; }
                public object qtyPrKndCd { get; set; }
                public object qtyDcAplyTotAmt { get; set; }
                public object dispName { get; set; }
                public string spdWitrPrAplyYn { get; set; }
                public string fboxBtnEpsrCd { get; set; }
                public string crdProdDcYn { get; set; }
                public string crdProdDcTxt { get; set; }
                public string pyCardCd { get; set; }
                public string pyCardNm { get; set; }
                public string crdProdDcEndDt { get; set; }
                public string fboxBtnEpsrNm { get; set; }
                public string fboxBtnEpsrStnc { get; set; }
                public object stffDvsNm { get; set; }
                public object fprdDvDcRt { get; set; }
                public object fprdDvDcAmt { get; set; }
                public object fprdDvDcSum { get; set; }
                public int orginCmDcAmt { get; set; }
                public object fboxCpList { get; set; }
                public object mainCpnGrpCdList { get; set; }
                public object mainCpnGrpNmList { get; set; }
                public object dispList { get; set; }
                public object orderLayerDispList { get; set; }
                public object mamBuyProductType { get; set; }
                public object mamBuyTagName { get; set; }
                public object mamBuyCmplYn { get; set; }
                public object mamBuyAplyStrtDttm { get; set; }
                public object mamBuyAplyEndDttm { get; set; }
                public string mamBuyAplyDateYn { get; set; }
                public object mamBuyAplyDateMsg { get; set; }
                public object mamBuyInfmStnc { get; set; }
                public object nudgeCouponDescription { get; set; }
            }
            public Data data { get; set; }
        }
        public Response response { get; set; }
        public QtyChangeFavorInfoList()
        {
            headers = new Dictionary<string, string>()
            {
                { "Content-Type", "application/json; charset=UTF-8" }
            };
        }
    }
}
