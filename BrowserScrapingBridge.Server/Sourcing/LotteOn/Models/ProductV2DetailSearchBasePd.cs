#nullable disable warnings
using System.Collections.Generic;

namespace BrowserScrapingBridge.Server.Sourcing.LotteOn.Models
{
    public class ProductV2DetailSearchBasePd
    {
        public class Response
        {
            public string returnCode { get; set; }
            public string message { get; set; }
            public string[] subMessages { get; set; }
            public object messageType { get; set; }
            public int dataCount { get; set; }
            public class Data
            {
                public class Basicinfo
                {
                    public string templateType { get; set; }
                    public string scatNo { get; set; }
                    public string stdCatLevel1Id { get; set; }
                    public string stdCatLevel2Id { get; set; }
                    public string stdCatLevel3Id { get; set; }
                    public string stdCatLevel4Id { get; set; }
                    public string mallDvsCd { get; set; }
                    public string mallNo { get; set; }
                    public string pdNo { get; set; }
                    public string pdNm { get; set; }
                    public string pdSlStatCd { get; set; }
                    public string pdSlStatCdNm { get; set; }
                    public string itmNo { get; set; }
                    public string itmNm { get; set; }
                    public string itmSlStatCd { get; set; }
                    public string itmSlStatCdNm { get; set; }
                    public string slTypCd { get; set; }
                    public string slTypCdNm { get; set; }
                    public string pdTypCd { get; set; }
                    public string pdTypCdNm { get; set; }
                    public string spdNo { get; set; }
                    public string spdNm { get; set; }
                    public string spdSlStatCd { get; set; }
                    public string spdSlStatCdNm { get; set; }
                    public string spdSlStatRsnCd { get; set; }
                    public string spdSlStatRsnCdNm { get; set; }
                    public string sitmNo { get; set; }
                    public int sitmWinrRnk { get; set; }
                    public string sitmNm { get; set; }
                    public string sitmSlStatCd { get; set; }
                    public string sitmSlStatCdNm { get; set; }
                    public object sitmSlStatRsnCd { get; set; }
                    public string sitmSlStatRsnCdNm { get; set; }
                    public List<string> sitmNoList { get; set; }
                    public string slStrtDttm { get; set; }
                    public string slEndDttm { get; set; }
                    public string trGrpCd { get; set; }
                    public string trGrpNm { get; set; }
                    public string trNo { get; set; }
                    public string trNm { get; set; }
                    public string lrtrNo { get; set; }
                    public object lrtrNm { get; set; }
                    public object purTrGrpCd { get; set; }
                    public object purTrNo { get; set; }
                    public object purLrtrNo { get; set; }
                    public string strCd { get; set; }
                    public string rtmCtngOprYn { get; set; }
                    public string ctrtTypCd { get; set; }
                    public string ctrtTypCdNm { get; set; }
                    public string brdNo { get; set; }
                    public string brdNm { get; set; }
                    public string brdFlag { get; set; }
                    public string brdImgUrl { get; set; }
                    public string brdSlrYn { get; set; }
                    public List<string> brdFlags { get; set; }
                    public object sizeLktbImgUrl { get; set; }
                    public string pblcStnc { get; set; }
                    public int monthlyPurchaseCnt { get; set; }
                    public string fnlAprvYn { get; set; }
                    public string prstPsbYn { get; set; }
                    public string prstPckPsbYn { get; set; }
                    public string prstMsgPsbYn { get; set; }
                    public string rgnLmtPdYn { get; set; }
                    public string bookCultCstDdctYn { get; set; }
                    public object isbnCd { get; set; }
                    public string brkHmapPkcpPsbYn { get; set; }
                    public string otltPdYn { get; set; }
                    public string toysPdYn { get; set; }
                    public string prmmPdYn { get; set; }
                    public string cshbltyPdYn { get; set; }
                    public string ageLmtCd { get; set; }
                    public string ageLmtCdNm { get; set; }
                    public object gftvShpCd { get; set; }
                    public string gftvShpCdNm { get; set; }
                    public string pdStatCd { get; set; }
                    public string pdStatCdNm { get; set; }
                    public string dmstOvsDvDvsCd { get; set; }
                    public string dmstOvsDvDvsCdNm { get; set; }
                    public string itmByMinPurYn { get; set; }
                    public int minPurQty { get; set; }
                    public string itmByMinPurMtpYn { get; set; }
                    public string itmByMinPurMsg { get; set; }
                    public string itmByMaxPurPsbQtyYn { get; set; }
                    public int maxPurQty { get; set; }
                    public string maxPurLmtTypCd { get; set; }
                    public int maxPurLmtPrd { get; set; }
                    public string maxPurLmtStrtDttm { get; set; }
                    public string maxPurLmtEndDttm { get; set; }
                    public string itmByMaxPurMsg { get; set; }
                    public string saveTypCd { get; set; }
                    public string saveTypCdNm { get; set; }
                    public string shopCnvMsgPsbYn { get; set; }
                    public string prmmInstPdYn { get; set; }
                    public string jntPurPdYn { get; set; }
                    public string etvPdYn { get; set; }
                    public object clrncYn { get; set; }
                    public object spcfStrYn { get; set; }
                    public object fttgDpStupCd { get; set; }
                    public string impDvsCd { get; set; }
                    public string impDvsCdNm { get; set; }
                    public string oplcCdNm { get; set; }
                    public List<string> oplcCdNmList { get; set; }
                    public string mfcrNm { get; set; }
                    public string mdlNo { get; set; }
                    public string entshpNo { get; set; }
                    public string supEntprzNo { get; set; }
                    public object qrSellerNm { get; set; }
                    public string ltmtDvShopYn { get; set; }
                    public string thdyPdYn { get; set; }
                    public object thdyDvProcTypCd { get; set; }
                    public string spicUseYn { get; set; }
                    public string alcoYn { get; set; }
                    public string scatBannerYn { get; set; }
                    public string prmmDv { get; set; }
                    public bool useNative { get; set; }
                    public string stffEusePdYn { get; set; }
                    public object quickCartYn { get; set; }
                    public List<object> pyMnsExcpLst { get; set; }
                    public object fgLst { get; set; }
                    public string headerMallNo { get; set; }
                    public string sizeGdeYn { get; set; }
                    public object sizeGdeTyp { get; set; }
                    public object prstPsbInfo { get; set; }
                    public string slWtYn { get; set; }
                    public object slWtBtnText { get; set; }
                    public string alcPkupYn { get; set; }
                    public string martRsvYn { get; set; }
                    public string appProductYn { get; set; }
                    public object excellentCustomerYn { get; set; }
                    public string excellentCustomerProductYn { get; set; }
                    public class BuyBtnInfo
                    {
                        public string buyBtnType { get; set; }
                        public string text { get; set; }
                        public object highlightText { get; set; }
                        public bool disabled { get; set; }
                    }
                    public BuyBtnInfo buyBtnInfo { get; set; }
                    public string dlcrtYn { get; set; }
                    public string dlcrtPrcDpYn { get; set; }
                    public object dlcrtTimeStr { get; set; }
                    public object dlcrtStoreUrl { get; set; }
                    public bool paidGiftMsgUseYn { get; set; }
                    public object onnuriInfo { get; set; }
                    public object costInfo { get; set; }
                    public string basicSvDttm { get; set; }
                    public object trvTypCd { get; set; }
                    public bool diffBranch { get; set; }
                    public bool fullWeb { get; set; }
                    public bool luxury { get; set; }
                    public bool beautyPremiumBrand { get; set; }
                    public bool notEqualsSitmNoReturn { get; set; }
                }
                public Basicinfo basicInfo { get; set; }
                public class ImgInfo
                {
                    public class ImageList
                    {
                        public string rprtImgYn { get; set; }
                        public string epsrTypCd { get; set; }
                        public string epsrTypCdNm { get; set; }
                        public string epsrTypDtlCd { get; set; }
                        public string origImgFileNm { get; set; }
                        public string imgRteNm { get; set; }
                        public string imgFileNm { get; set; }
                        public string imgCrtYn { get; set; }
                        public int imgSortSeq { get; set; }
                        public string homeShopYn { get; set; }
                        public object bnrImgRteUrl { get; set; }
                    }
                    public List<ImageList> imageList { get; set; }
                    public object emblInfo { get; set; }
                    public object homeShopInfo { get; set; }
                    public object threeDImgInfo { get; set; }
                    public bool vdoFileYn { get; set; }
                }
                public ImgInfo imgInfo { get; set; }
                public class OptionInfo
                {
                    public class OptionList
                    {
                        public string title { get; set; }
                        public string name { get; set; }
                        public string output { get; set; }
                        public string attrValFrmCd { get; set; }
                        public bool imageType { get; set; }
                        public int prioRnk { get; set; }
                        public class Option
                        {
                            public string clrChipImgUrl { get; set; }
                            public int prioRnk { get; set; }
                            public string attrValClorRgb { get; set; }
                            public string label { get; set; }
                            public string value { get; set; }
                            public bool active { get; set; }
                            public bool disabled { get; set; }
                            public string rprtImgUrl { get; set; }
                        }
                        public List<Option> options { get; set; }
                    }
                    public List<OptionList> optionList { get; set; }
                    public List<object> addInputOption { get; set; }
                    public class OptionMappingInfo { }
                    public OptionMappingInfo optionMappingInfo { get; set; }
                    public bool rdb { get; set; }
                    public bool colorLohbs { get; set; }
                    public bool isRDB { get; set; }
                }
                public OptionInfo optionInfo { get; set; }
                public class PriceInfo
                {
                    public int slPrc { get; set; }
                    public float sfcoPdMrgnRt { get; set; }
                    public float sfcoPdLwstMrgnRt { get; set; }
                    public float pcsLwstMrgnRt { get; set; }
                    public object afflMrgnRt { get; set; }
                    public object afflLwstMrgnRt { get; set; }
                    public string stdUtCd { get; set; }
                    public string stdUtNm { get; set; }
                    public int stdCapa { get; set; }
                    public int pdCapa { get; set; }
                    public List<object> discountApplyProductList { get; set; }
                    public int immdDcAplyTotAmt { get; set; }
                    public object immdDcRt { get; set; }
                    public object mbFvrOffrDvsCd { get; set; }
                    public int mbFvrOffrAmt { get; set; }
                    public int adtnDcAplyTotAmt { get; set; }
                    public int adtnDcAmt { get; set; }
                    public object adtnDcDvsCd { get; set; }
                    public object dispName { get; set; }
                    public object fboxBtnEpsrCd { get; set; }
                }
                public PriceInfo priceInfo { get; set; }
                public class DealInfo
                {
                    public object dealNo { get; set; }
                    public object dealTypCd { get; set; }
                    public object dealTypCdNm { get; set; }
                    public object dealDpNm { get; set; }
                    public object dealStrtDttm { get; set; }
                    public object dealEndDttm { get; set; }
                    public object dealAplyStrtTm { get; set; }
                    public object dealAplyEndTm { get; set; }
                    public object currentDateTime { get; set; }
                    public object stkEpsrStdCd { get; set; }
                    public object stkEpsrStdCdNm { get; set; }
                    public object sndStrtDttm { get; set; }
                    public object odQty { get; set; }
                    public object dealByfEndYn { get; set; }
                }
                public DealInfo dealInfo { get; set; }
                public class StckInfo
                {
                    public int stkQty { get; set; }
                    public string stkVerNo { get; set; }
                    public string stkMgtYn { get; set; }
                    public object dealStkQty { get; set; }
                    public object dealLmtQty { get; set; }
                    public object dealOdQty { get; set; }
                    public object qtyOfItemDeal { get; set; }
                    public object qtyOfProductDeal { get; set; }
                    public bool hideStkQty { get; set; }
                }
                public StckInfo stckInfo { get; set; }
                public class BnftInfo
                {
                    public class BnftNotiNudgingInfo
                    {
                        public object bnftStrtDttm { get; set; }
                        public object bnftEndDttm { get; set; }
                        public object bnftNotiText { get; set; }
                    }
                    public BnftNotiNudgingInfo bnftNotiNudgingInfo { get; set; }
                    public class EventItemsInfo
                    {
                        public string appVisibleYn { get; set; }
                        public string moWebVisibleYn { get; set; }
                        public string pcWebVisibleYn { get; set; }
                        public class EventItemsUrlInfo
                        {
                            public string notiMsg { get; set; }
                            public string linkUrl { get; set; }
                        }
                        public EventItemsUrlInfo eventItemsUrlInfo { get; set; }
                    }
                    public EventItemsInfo eventItemsInfo { get; set; }
                    public object fvrCnts { get; set; }
                    public object lpointNotiInfo { get; set; }
                }
                public BnftInfo bnftInfo { get; set; }
                public class DlvInfo
                {
                    public string dvProcTypCd { get; set; }
                    public string dvProcTypCdNm { get; set; }
                    public string dvPdTypCd { get; set; }
                    public string dvPdTypCdNm { get; set; }
                    public string dvMnsCd { get; set; }
                    public string dvMnsCdNm { get; set; }
                    public string sndBgtNday { get; set; }
                    public string crdaySndObyYn { get; set; }
                    public string cartDvsCd { get; set; }
                    public class SndBgtDdJsn
                    {
                        public string nldySndCloseTm { get; set; }
                        public string satSndPsbYn { get; set; }
                        public string satSndCloseTm { get; set; }
                    }
                    public SndBgtDdJsn sndBgtDdJsn { get; set; }
                    public string tmrwArvYn { get; set; }
                    public class SpicJsn
                    {
                        public List<string> spicTypCdLst { get; set; }
                        public object spicTypCdNmLst { get; set; }
                        public object strPicTypJsn { get; set; }
                    }
                    public SpicJsn spicJsn { get; set; }
                    public string dvRgsprGrpCd { get; set; }
                    public string dvRgsprGrpCdNm { get; set; }
                    public string dvCstPolNo { get; set; }
                    public string spicUseYn { get; set; }
                    public string adtnDvCstPolNo { get; set; }
                    public int dvCstStdQty { get; set; }
                    public string dvCstReptTxt { get; set; }
                    public string spicEusePdYn { get; set; }
                    public object spicEusePdTypCd { get; set; }
                    public string spicEusePdTypCdNm { get; set; }
                    public string hpDdDvPsbYn { get; set; }
                    public object hpDdDvPsbPrd { get; set; }
                    public string qckDvUseYn { get; set; }
                    public string qckDvEuseYn { get; set; }
                    public string crdayDvPsbYn { get; set; }
                    public string odCloseTm { get; set; }
                    public string fprdDvPsbYn { get; set; }
                    public string spcfSqncPdYn { get; set; }
                    public string dnDvPdYn { get; set; }
                    public object dnDvEusePdYn { get; set; }
                    public string cmbnDvPsbYn { get; set; }
                    public string dmstOvsDvDvsCd { get; set; }
                    public string dmstOvsDvDvsCdNm { get; set; }
                    public string dvRsvDvsCd { get; set; }
                    public string dvRsvDvsCdNm { get; set; }
                    public object rsvOdStrtDttm { get; set; }
                    public object rsvOdEndDttm { get; set; }
                    public object rsvPdSndStrtDttm { get; set; }
                    public string epctArrDtTxt { get; set; }
                    public int dvCst { get; set; }
                    public string dvRgsprGrpNm { get; set; }
                    public int inrmAdtnDvCst { get; set; }
                    public int jejuAdtnDvCst { get; set; }
                    public string hdcCdNm { get; set; }
                    public class DvCstPolLst
                    {
                        public string dvCstPolNo { get; set; }
                        public int dvCstPolSeq { get; set; }
                        public object dvCstPolSNm { get; set; }
                        public object dvTypCd { get; set; }
                        public string dvProcTypCd { get; set; }
                        public string qckDvYn { get; set; }
                        public string strPicYn { get; set; }
                        public int freeDvStdAmt { get; set; }
                        public int dvCst { get; set; }
                        public int rtrvDvCst { get; set; }
                    }
                    public List<DvCstPolLst> dvCstPolLst { get; set; }
                    public string rplcNo { get; set; }
                    public string rtngPsbYn { get; set; }
                    public string xchgPsbYn { get; set; }
                    public class DvList
                    {
                        public string area { get; set; }
                        public int rank { get; set; }
                        public string type { get; set; }
                        public string typeNm { get; set; }
                        public string hdcCdNm { get; set; }
                        public string dayTxt { get; set; }
                        public object odTxt { get; set; }
                        public object dvCst { get; set; }
                        public object dvCstTxt { get; set; }
                        public class DvCstInfo
                        {
                            public int freeDvStdAmt { get; set; }
                            public int dvCst { get; set; }
                            public string dvCstTxt { get; set; }
                        }
                        public List<DvCstInfo> dvCstInfo { get; set; }
                        public object fprdDvStrtDttm { get; set; }
                        public object fprdDvEndDttm { get; set; }
                        public object addText1 { get; set; }
                        public object addText2 { get; set; }
                        public object addText3 { get; set; }
                        public object addText4 { get; set; }
                        public object addText5 { get; set; }
                        public object tooltip1 { get; set; }
                        public object tooltip2 { get; set; }
                        public object tooltip3 { get; set; }
                        public object tooltip4 { get; set; }
                        public object drtDvOprStTime { get; set; }
                        public object drtDvOprEdTime { get; set; }
                        public object drtDvOprStPopupTime { get; set; }
                        public object drtDvOprEdPopupTime { get; set; }
                        public object defaultDrtDvOprStTime { get; set; }
                        public object defaultDrtDvOprEdTime { get; set; }
                        public object dnDvEndTm { get; set; }
                        public object todayArvTxt { get; set; }
                        public object todayArvHighlight { get; set; }
                        public object dvFlagInfo { get; set; }
                        public object dvTxtStyleInfo { get; set; }
                        public object dvImgUrl { get; set; }
                        public bool holiday { get; set; }
                        public bool todaySnd { get; set; }
                    }
                    public List<DvList> dvList { get; set; }
                    public class Dvlist2
                    {
                        public string area { get; set; }
                        public int rank { get; set; }
                        public string type { get; set; }
                        public string typeNm { get; set; }
                        public string hdcCdNm { get; set; }
                        public string dayTxt { get; set; }
                        public object odTxt { get; set; }
                        public object dvCst { get; set; }
                        public object dvCstTxt { get; set; }
                        public class DvCstInfo
                        {
                            public int freeDvStdAmt { get; set; }
                            public int dvCst { get; set; }
                            public string dvCstTxt { get; set; }
                        }
                        public DvCstInfo[] dvCstInfo { get; set; }
                        public object fprdDvStrtDttm { get; set; }
                        public object fprdDvEndDttm { get; set; }
                        public object addText1 { get; set; }
                        public object addText2 { get; set; }
                        public object addText3 { get; set; }
                        public object addText4 { get; set; }
                        public object addText5 { get; set; }
                        public object tooltip1 { get; set; }
                        public object tooltip2 { get; set; }
                        public object tooltip3 { get; set; }
                        public object tooltip4 { get; set; }
                        public object drtDvOprStTime { get; set; }
                        public object drtDvOprEdTime { get; set; }
                        public object drtDvOprStPopupTime { get; set; }
                        public object drtDvOprEdPopupTime { get; set; }
                        public object defaultDrtDvOprStTime { get; set; }
                        public object defaultDrtDvOprEdTime { get; set; }
                        public object dnDvEndTm { get; set; }
                        public object todayArvTxt { get; set; }
                        public object todayArvHighlight { get; set; }
                        public object dvFlagInfo { get; set; }
                        public class DvTxtStyleInfo
                        {
                            public object flagCode { get; set; }
                            public string flagName { get; set; }
                            public string flagTextColor { get; set; }
                            public bool flagBold { get; set; }
                            public object flagImgUrl { get; set; }
                        }
                        public List<DvTxtStyleInfo> dvTxtStyleInfo { get; set; }
                        public string dvImgUrl { get; set; }
                        public bool holiday { get; set; }
                        public bool todaySnd { get; set; }
                    }
                    public List<Dvlist2> dvList2 { get; set; }
                    public string owhpNo { get; set; }
                    public string chargeInstallCategoryYn { get; set; }
                    public object leadTime { get; set; }
                    public object rsvSlTypCd { get; set; }
                    public class ArvBgtDtInfo
                    {
                        public int day { get; set; }
                        public string dt { get; set; }
                        public int count { get; set; }
                        public int singleRate { get; set; }
                        public int accumulationRate { get; set; }
                        public string dvExpectInfoTxt { get; set; }
                        public bool selected { get; set; }
                    }
                    public List<ArvBgtDtInfo> arvBgtDtInfoList { get; set; }
                    public string arvBgtDtDlvTermTxt { get; set; }
                    public object todayArvTxt { get; set; }
                    public object todayArvHighlight { get; set; }
                    public object todayArvClsTmTxt { get; set; }
                    public object tdyBeforePopupTxt { get; set; }
                    public object tdyAfterPopupTxt { get; set; }
                    public object scatMpngInfo { get; set; }
                    public bool myDefaultDlv { get; set; }
                    public bool viewCmbnDvPsb { get; set; }
                }
                public DlvInfo dlvInfo { get; set; }
                public class SlrInfo
                {
                    public string sellerNm { get; set; }
                    public string sellerImg { get; set; }
                    public string sellerDvsType { get; set; }
                    public string lrtrStoreDpYn { get; set; }
                    public string sellerPhone { get; set; }
                    public string respondRate { get; set; }
                    public string deliverRate { get; set; }
                    public class TrBase
                    {
                        public string trNo { get; set; }
                        public string trNm { get; set; }
                        public object lrtrNo { get; set; }
                        public object lrtrNm { get; set; }
                        public string trDvsCd { get; set; }
                        public string trGrpCd { get; set; }
                        public string trGrpNm { get; set; }
                        public string rprrNm { get; set; }
                        public string stnmZipNo { get; set; }
                        public string stnmZipAddr { get; set; }
                        public string stnmDtlAddr { get; set; }
                        public string bzplcCaddrNo { get; set; }
                        public string bzplcZipAddr { get; set; }
                        public string bzplcDtlAddr { get; set; }
                        public string bzplcStnmZipAddr { get; set; }
                        public string bzplcStnmDtlAddr { get; set; }
                        public string bzplcZipNo { get; set; }
                        public object strPicUseYn { get; set; }
                        public string crssPicUseYn { get; set; }
                        public string rvsPicUseYn { get; set; }
                        public object afflTrCd { get; set; }
                        public object strCd { get; set; }
                        public string telNatnNoCd { get; set; }
                        public string rtmCtngOprYn { get; set; }
                        public string bizpDvsCd { get; set; }
                        public string bizpDvsCdTxt { get; set; }
                        public string bizpRegNo { get; set; }
                        public string mosDeclNo { get; set; }
                        public object pdEpsrEmlAddr { get; set; }
                        public string rpbtrEmlAddr { get; set; }
                        public string onrFrchYn { get; set; }
                    }
                    public TrBase trBase { get; set; }
                    public string trStatCd { get; set; }
                    public string dpShopTmplInfoYn { get; set; }
                    public string strWishYn { get; set; }
                    public object sellerStoreProducts { get; set; }
                    public string storeConnectYn { get; set; }
                    public string storeConnectUrl { get; set; }
                    public string trShopTmplInfoYn { get; set; }
                    public string flagShipStoreYn { get; set; }
                    public string overseasSellerYn { get; set; }
                    public string sellerInquiryAvailableYn { get; set; }
                    public string appLandingUrl { get; set; }
                    public string afflYn { get; set; }
                }
                public SlrInfo slrInfo { get; set; }
                public class EcpnInfo
                {
                    public object csrcPblsYn { get; set; }
                    public object fbilPrc { get; set; }
                    public object ecpnVldPrdDvsCd { get; set; }
                    public object ecpnVldPrdDvsCdNm { get; set; }
                    public object vldPrdStrtDttm { get; set; }
                    public object vldPrdEndDttm { get; set; }
                    public object vldPrdUseStrtTm { get; set; }
                    public object vldPrdUseEndTm { get; set; }
                    public object vldPrdUseWkdy { get; set; }
                    public object vldPrdUseWkdyNm { get; set; }
                    public object usePlcLst { get; set; }
                    public object rtngPsbDvsCd { get; set; }
                    public object rtngPsbDvsCdNm { get; set; }
                    public object rfndPsbYn { get; set; }
                    public object ecpnRfndTypCd { get; set; }
                    public object ecpnRfndTypCdNm { get; set; }
                    public object autoRfndYn { get; set; }
                    public object sndMnsCd { get; set; }
                    public object sndMnsCdNm { get; set; }
                    public object useLmtEpn { get; set; }
                    public object atntMatrEpn { get; set; }
                    public object usePlcEpn { get; set; }
                    public object vldNday { get; set; }
                }
                public EcpnInfo ecpnInfo { get; set; }
                public class ArtlInfo
                {
                    public string pdItmsCd { get; set; }
                    public string pdItmsCdNm { get; set; }
                    public class PdItmsArtlJsn
                    {
                        public string pdArtlCd { get; set; }
                        public string pdArtlCdNm { get; set; }
                        public string pdArtlCnts { get; set; }
                    }
                    public List<PdItmsArtlJsn> pdItmsArtlJsn { get; set; }
                }
                public ArtlInfo artlInfo { get; set; }
                public class SftyInfo
                {
                    public class Sfty
                    {
                        public object sftyAthnNo { get; set; }
                        public string sftyAthnTypCd { get; set; }
                        public string sftyAthnTypCdNm { get; set; }
                        public string sftyAthnOrgnNm { get; set; }
                        public object sftyAthnNoUrl { get; set; }
                        public bool kcmark { get; set; }
                    }
                    public List<Sfty> sftyList { get; set; }
                    public class ImpPrxInfo
                    {
                        public object impPrxCd { get; set; }
                        public object impPrxTxts { get; set; }
                    }
                    public ImpPrxInfo impPrxInfo { get; set; }
                }
                public SftyInfo sftyInfo { get; set; }
                public class AttrInfo
                {
                    public class Attr
                    {
                        public int attrId { get; set; }
                        public string attrDispNm { get; set; }
                        public int attrValId { get; set; }
                        public string attrValDispNm { get; set; }
                        public string attrValTpCd { get; set; }
                        public string attrValTpCdNm { get; set; }
                        public int prioRnk { get; set; }
                        public string attrValFrmCd { get; set; }
                        public string imgUrl { get; set; }
                        public string displayType { get; set; }
                    }
                    public List<Attr> attrList { get; set; }
                }
                public AttrInfo attrInfo { get; set; }
                public class NotiArrInfo
                {
                    public class NotiArr
                    {
                        public string notiTtl { get; set; }
                        public string notiCnts { get; set; }
                        public string notiDvsCd { get; set; }
                        public int notiNo { get; set; }
                    }
                    public List<NotiArr> notiArrList { get; set; }
                }
                public NotiArrInfo notiArrInfo { get; set; }
                public class DescInfo
                {
                    public class EpnJsn
                    {
                        public string pdEpnTypCd { get; set; }
                        public string pdEpnTypCdNm { get; set; }
                        public string pdEpnShpCd { get; set; }
                        public object pdEpnShpCdNm { get; set; }
                        public string dtlFileRteNm { get; set; }
                        public string origDtlFileNm { get; set; }
                        public string dtlFileNm { get; set; }
                        public string fileCrtYn { get; set; }
                    }
                    public List<EpnJsn> epnJsn { get; set; }
                    public object[] fileJsn { get; set; }
                }
                public DescInfo descInfo { get; set; }
                public class PrmmInfo
                {
                    public object qrtbImgRteNm { get; set; }
                    public object qrtbImgFileNm { get; set; }
                    public object actlMeasSzImgRteNm { get; set; }
                    public object actlMeasSzImgFileNm { get; set; }
                    public object actlMeasSzCntsRteNm { get; set; }
                    public object actlMeasSzCntsFileNm { get; set; }
                    public object avnPdDtlEpnRteNm { get; set; }
                    public object avnPdDtlEpnFileNm { get; set; }
                }
                public PrmmInfo prmmInfo { get; set; }
                public class OpngInfo
                {
                    public object mvCmcoCd { get; set; }
                    public object mvCmcoCdNm { get; set; }
                    public object owhPrc { get; set; }
                    public object joinAplUrl { get; set; }
                    public object adtnSptAmt { get; set; }
                    public object sptAmt { get; set; }
                }
                public OpngInfo opngInfo { get; set; }
                public class SlrRcInfo
                {
                    public object[] slrRcList { get; set; }
                }
                public SlrRcInfo slrRcInfo { get; set; }
                public class RntlInfo
                {
                    public object dutyUsePrd { get; set; }
                    public object instCst { get; set; }
                    public object regCst { get; set; }
                    public object cnsmrSlPrc { get; set; }
                    public object mmRntlCst { get; set; }
                    public object vatInclYn { get; set; }
                }
                public RntlInfo rntlInfo { get; set; }
                public class WebviewlinkV2
                {
                    public class DlvNotice
                    {
                        public string url { get; set; }
                        public List<string> _params { get; set; }
                    }
                    public DlvNotice dlvNotice { get; set; }
                    public class DvCstSavingProduct
                    {
                        public string url { get; set; }
                        public List<string> _params { get; set; }
                    }
                    public DvCstSavingProduct dvCstSavingProduct { get; set; }
                    public class FittingReserve
                    {
                        public string url { get; set; }
                    }

                    public FittingReserve fittingReserve { get; set; }
                    public class OwnersDiscountPriceInfo
                    {
                        public string url { get; set; }
                    }
                    public OwnersDiscountPriceInfo ownersDiscountPriceInfo { get; set; }
                    public class SaleStoreInfo
                    {
                        public string url { get; set; }
                        public List<string> _params { get; set; }
                    }
                    public SaleStoreInfo saleStoreInfo { get; set; }
                    public class ProductDeliveryPlace
                    {
                        public string url { get; set; }
                        public List<string> _params { get; set; }
                    }
                    public ProductDeliveryPlace productDeliveryPlace { get; set; }
                    public class PromotionReceive
                    {
                        public string url { get; set; }
                    }
                    public PromotionReceive promotionReceive { get; set; }
                    public class PromotionAdditionFavor
                    {
                        public string url { get; set; }
                    }
                    public PromotionAdditionFavor promotionAdditionFavor { get; set; }
                    public class UnitCal
                    {
                        public string url { get; set; }
                    }
                    public UnitCal unitCal { get; set; }
                    public class EventProductPutbox
                    {
                        public string url { get; set; }
                    }
                    public EventProductPutbox eventProductPutbox { get; set; }
                    public class ProductStatusImg
                    {
                        public string url { get; set; }
                        public List<string> _params { get; set; }
                    }
                    public ProductStatusImg productStatusImg { get; set; }
                    public class AuthCheck
                    {
                        public string url { get; set; }
                    }
                    public AuthCheck authCheck { get; set; }
                    public class ColorLohbs
                    {
                        public string url { get; set; }
                        public List<string> _params { get; set; }
                    }
                    public ColorLohbs colorLohbs { get; set; }
                    public class ReviewList
                    {
                        public string url { get; set; }
                    }
                    public ReviewList reviewList { get; set; }
                    public class ReviewBList
                    {
                        public string url { get; set; }
                    }
                    public ReviewBList reviewBList { get; set; }
                    public class CardInstallment
                    {
                        public string url { get; set; }
                    }
                    public CardInstallment cardInstallment { get; set; }
                    public class RecmCartProducts
                    {
                        public string url { get; set; }
                        public List<string> _params { get; set; }
                    }
                    public RecmCartProducts recmCartProducts { get; set; }
                    public class AlcoPickupStore
                    {
                        public string url { get; set; }
                    }
                    public AlcoPickupStore alcoPickupStore { get; set; }
                    public class QnaRegistration
                    {
                        public string url { get; set; }
                    }
                    public QnaRegistration qnaRegistration { get; set; }
                    public class StoreRecommendProduct
                    {
                        public string url { get; set; }
                    }

                    public StoreRecommendProduct storeRecommendProduct { get; set; }
                    public class MarketingAgreement
                    {
                        public string url { get; set; }
                    }
                    public MarketingAgreement marketingAgreement { get; set; }
                }
                public WebviewlinkV2 webviewlinkV2 { get; set; }
                public class GftInfo
                {
                    public object prKndCd { get; set; }
                    public object aplyStrtDttm { get; set; }
                    public object aplyEndDttm { get; set; }
                    public object prntRngCd { get; set; }
                    public object fcfsMaxNopNbr { get; set; }
                    public object chcGiftList { get; set; }
                    public object estlGiftList { get; set; }
                    public object seqGiftList { get; set; }
                }
                public GftInfo gftInfo { get; set; }
                public class AdtnPdInfo
                {
                    public object adtnPdList { get; set; }
                }
                public AdtnPdInfo adtnPdInfo { get; set; }
                public class LinkUrlInfo
                {
                    public string lpointJoinUrl { get; set; }
                    public string addCartRecommendProducts { get; set; }
                    public string addCartRecommendProductsV2 { get; set; }
                }
                public LinkUrlInfo linkUrlInfo { get; set; }
                public class NudgingInfo
                {
                    public class OtherPeoplesReaction
                    {
                        public bool show { get; set; }
                        public int order { get; set; }
                        public string message { get; set; }
                        public string text { get; set; }
                        public List<string> highlightText { get; set; }
                        public string iconUrl { get; set; }
                    }
                    public List<OtherPeoplesReaction> otherPeoplesReaction { get; set; }
                }
                public NudgingInfo nudgingInfo { get; set; }
                public class DispCategoryInfo
                {
                    public string mallDvsCd { get; set; }
                    public string dispCatId { get; set; }
                    public string dispCatNm { get; set; }
                    public string dispCatId0 { get; set; }
                    public string dispCatNm0 { get; set; }
                    public string dispCatId1 { get; set; }
                    public string dispCatNm1 { get; set; }
                    public string dispCatId2 { get; set; }
                    public string dispCatNm2 { get; set; }
                    public object headerVertiCd { get; set; }
                    public object pddHeaderCustomType { get; set; }
                    public class SearchHeaderInfo
                    {
                        public string searchKeyword { get; set; }
                        public string searchLandingPath { get; set; }
                        public bool showSearchHeader { get; set; }
                    }
                    public SearchHeaderInfo searchHeaderInfo { get; set; }
                    public object clubBenefit { get; set; }
                    public object themeCatInfo { get; set; }
                    public List<string> dcatList { get; set; }
                    public class ServiceBannerInfo
                    {
                        public string isAppVisible { get; set; }
                        public string isMoWebVisible { get; set; }
                        public class MoInfo
                        {
                            public string imgUrl { get; set; }
                            public string linkUrl { get; set; }
                            public object imgAltCnts { get; set; }
                        }
                        public MoInfo moInfo { get; set; }
                        public object pcInfo { get; set; }
                    }
                    public ServiceBannerInfo serviceBannerInfo { get; set; }
                    public object pcsBannerInfo { get; set; }
                }
                public DispCategoryInfo dispCategoryInfo { get; set; }
                public class ModuleInfo
                {
                    public class Module
                    {
                        public string name { get; set; }
                        public int order { get; set; }
                        public object toggle { get; set; }
                        public List<string> classes { get; set; }
                    }
                    public List<Module> modules { get; set; }
                    public string fileKey { get; set; }
                }
                public ModuleInfo moduleInfo { get; set; }
                public class AppInfo
                {
                    public object tracerInfo { get; set; }
                    public object dispBrandName { get; set; }
                }
                public AppInfo appInfo { get; set; }
            }
            public Data data { get; set; }
        }
        public Response response { get; set; }
    }
}
