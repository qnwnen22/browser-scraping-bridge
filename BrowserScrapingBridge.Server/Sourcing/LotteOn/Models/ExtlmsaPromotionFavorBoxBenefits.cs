#nullable disable warnings
using System.Collections.Generic;

namespace BrowserScrapingBridge.Server.Sourcing.LotteOn.Models
{
    public class ExtlmsaPromotionFavorBoxBenefits
    {
        public string url { get; set; } = "https://pbf.lotteon.com/product/v2/extlmsa/promotion/favorBox/benefits";
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
            public class DiscountApplyProductList
            {
                public string prNm { get; set; }
                public string prKndNm { get; set; }
                public string cpnIssNo { get; set; }
                public string cpnNo { get; set; }
                public class Pymnsdtl
                {
                    public string prNo { get; set; }
                    public string pyMnsCd { get; set; }
                    public string pyMnsDtlTypCd { get; set; }
                    public string pyMnsDtlVal { get; set; }
                    public int regNo { get; set; }
                }
                public Pymnsdtl pyMnsDtl { get; set; }
                public string prNo { get; set; }
                public int prRegNo { get; set; }
                public string prTypCd { get; set; }
                public string prKndCd { get; set; }
            }
            public List<DiscountApplyProductList> discountApplyProductList { get; set; }
            public int maxPurQty { get; set; }
            public string stkMgtYn { get; set; }
            public string screenType { get; set; }
            public string dmstOvsDvDvsCd { get; set; }
            public string dvPdTypCd { get; set; }
            public int dvCstStdQty { get; set; }
            public string aplyBestPrcChk { get; set; }
            public List<object> pyMnsExcpLst { get; set; } = new List<object>();
            public string mallNo { get; set; }
        }
        public PostBody postBody { get; set; }
        public class Response
        {
            public string returnCode { get; set; }
            public string message { get; set; }
            public object subMessages { get; set; }
            public object messageType { get; set; }
            public object dataCount { get; set; }
            public class Data
            {
                public string spdNo { get; set; }
                public string sitmNo { get; set; }
                public int totSlPrc { get; set; }
                public int slPrc { get; set; }
                public int slQty { get; set; }
                public int totAmt { get; set; }
                public int totDcAmt { get; set; }
                public class UserInfo
                {
                    public bool isLpntMb { get; set; }
                    public bool isClub { get; set; }
                }
                public UserInfo userInfo { get; set; }
                public string aplyBestPrcChk { get; set; }
                public string aplyBestPrcChkTitle { get; set; }
                public string fboxBtnEpsrCd { get; set; }
                public string fboxBtnEpsrNm { get; set; }
                public class DiscountGroup
                {
                    public string title { get; set; }
                    public string desc { get; set; }
                    public class DiscountApplyPromotionList
                    {
                        public string groupId { get; set; }
                        public string check { get; set; }
                        public string prNo { get; set; }
                        public int prRegNo { get; set; }
                        public string prNm { get; set; }
                        public string prTypCd { get; set; }
                        public string prTypNm { get; set; }
                        public string prKndCd { get; set; }
                        public string prKndNm { get; set; }
                        public string dcTnnoCd { get; set; }
                        public string dcTypCd { get; set; }
                        public int dcAmt { get; set; }
                        public float dcRt { get; set; }
                        public string prAplyYn { get; set; }
                        public int sfcoShrAmt { get; set; }
                        public int entpShrAmt { get; set; }
                        public string sptDcPgmAgrYn { get; set; }
                        public string dispTitle { get; set; }
                        public string dispName { get; set; }
                        public object dispDesces { get; set; }
                        public string[] dispDtls { get; set; }
                        public object dispCardName { get; set; }
                        public int dcPgmCmsn { get; set; }
                        public class CouponInfo
                        {
                            public string cpnNo { get; set; }
                            public string cpnIssNo { get; set; }
                            public string otherPdAplyYn { get; set; }
                            public object otherAplyPdNm { get; set; }
                            public string downloadYn { get; set; }
                            public string issYn { get; set; }
                            public int issCnt { get; set; }
                            public int cartSn { get; set; }
                        }
                        public CouponInfo couponInfo { get; set; }
                        public class MainFlag
                        {
                            public bool isAppOnly { get; set; }
                            public bool isFirstBuy { get; set; }
                            public bool isClub { get; set; }
                            public bool isLpntMb { get; set; }
                            public bool isStrJJim { get; set; }
                            public bool isPrst { get; set; }
                            public bool isRcvAgr { get; set; }
                        }
                        public MainFlag mainFlag { get; set; }
                        public List<string> mainFlagTextArr { get; set; }
                        public object storeInfo { get; set; }
                        public string bestPrAplyYn { get; set; }
                        public class PyMnsDtl
                        {
                            public string prNo { get; set; }
                            public string pyMnsCd { get; set; }
                            public string pyMnsDtlTypCd { get; set; }
                            public string pyMnsDtlVal { get; set; }
                            public int regNo { get; set; }
                        }
                        public PyMnsDtl pyMnsDtl { get; set; }
                        public bool isCrdPrd { get; set; }
                    }
                    public List<DiscountApplyPromotionList> discountApplyPromotionList { get; set; }
                }
                public List<DiscountGroup> discountGroups { get; set; }
            }
            public Data data { get; set; }
        }
        public Response response { get; set; }
        public ExtlmsaPromotionFavorBoxBenefits()
        {
            headers = new Dictionary<string, string>()
            {
                { "Content-Type", "application/json; charset=UTF-8" }
            };
        }
    }
}
