#nullable disable warnings
using System.Collections.Generic;

namespace BrowserScrapingBridge.Server.Sourcing.AliExpress_KR.Models
{
    public class H5NewDetailItemModel
    {

        public string api { get; set; }
        public Data data { get; set; }
        public string[] ret { get; set; }
        public string traceId { get; set; }
        public string v { get; set; }

        public class Data
        {
            public Result result { get; set; }
        }

        public class Result
        {
            public POPUP POPUP { get; set; }
            public HEADER_IMAGE_PC HEADER_IMAGE_PC { get; set; }
            public PC_RATING PC_RATING { get; set; }
            public QUANTITY_PC QUANTITY_PC { get; set; }
            public MIDDLE_BANNER MIDDLE_BANNER { get; set; }
            public PRICE_EXTEND PRICE_EXTEND { get; set; }
            public SHIPPING SHIPPING { get; set; }
            public DESC DESC { get; set; }
            public TRAFFIC_IDENTIFY TRAFFIC_IDENTIFY { get; set; }
            public AFTER_SALE_SERVICE_BLOCK AFTER_SALE_SERVICE_BLOCK { get; set; }
            public PRODUCT_PROP_PC PRODUCT_PROP_PC { get; set; }
            public SUSTAINABILITY_SERVICE_BLOCK SUSTAINABILITY_SERVICE_BLOCK { get; set; }
            public GLOBAL_DATA GLOBAL_DATA { get; set; }
            public PRICE PRICE { get; set; }
            public SELLING_POINT_PC SELLING_POINT_PC { get; set; }
            public WISHLIST WISHLIST { get; set; }
            public SHOP_CARD_PC SHOP_CARD_PC { get; set; }
            public RECOMMEND_PC RECOMMEND_PC { get; set; }
            public BOTTOM_BAR_PC BOTTOM_BAR_PC { get; set; }
            public MIND MIND { get; set; }
            public PRODUCT_TITLE PRODUCT_TITLE { get; set; }
            public PERSONAL_INFORMATION_SECURITY PERSONAL_INFORMATION_SECURITY { get; set; }
            public COUPON_BLOCK_PC COUPON_BLOCK_PC { get; set; }
            public SKU SKU { get; set; }
            public DELIVERY_GUARANTEE_SERVICE DELIVERY_GUARANTEE_SERVICE { get; set; }
        }

        public class POPUP
        {
            public Popupmap popupMap { get; set; }
            public object[] sortedPopup { get; set; }
        }

        public class Popupmap
        {
        }

        public class HEADER_IMAGE_PC
        {
            public string[] currentSkuImages { get; set; }
            public string[] imagePathList { get; set; }
            public string[] imgList { get; set; }
            public List<Mainimage> mainImages { get; set; }
            public Productvideo productVideo { get; set; }
            public bool showColorTab { get; set; }
            public bool showLongImage { get; set; }
            public Skuimagesmap skuImagesMap { get; set; }
            public string[] summImagePathList { get; set; }
        }

        public class Productvideo
        {
            public string posterUrl { get; set; }
            public long videoId { get; set; }
            public Videoplayinfo videoPlayInfo { get; set; }
        }

        public class Videoplayinfo
        {
            public string androidPhoneUrl { get; set; }
            public string iphoneUrl { get; set; }
            public string webUrl { get; set; }
        }

        public class Skuimagesmap
        {
            public string[] _12000037380104820 { get; set; }
        }

        public class Mainimage
        {
            public string imageKey { get; set; }
            public string imageUrl { get; set; }
        }

        public class PC_RATING
        {
            public string otherText { get; set; }
            public string rating { get; set; }
            public string separator { get; set; }
            public int totalValidNum { get; set; }
        }

        public class QUANTITY_PC
        {
            public Allskuquantityview allSkuQuantityView { get; set; }
            public Currentskuquantityview currentSkuQuantityView { get; set; }
            public string title { get; set; }
            public int totalAvailableInventory { get; set; }
        }

        public class Allskuquantityview
        {
            public _12000037380104820 _12000037380104820 { get; set; }
        }

        public class _12000037380104820
        {
            public int currentCount { get; set; }
            public int maxBuyCount { get; set; }
            public string maxBuyCountStr { get; set; }
        }

        public class Currentskuquantityview
        {
            public int currentCount { get; set; }
            public int maxBuyCount { get; set; }
            public string maxBuyCountStr { get; set; }
        }

        public class MIDDLE_BANNER
        {
            public Atmospherecodemap atmosphereCodeMap { get; set; }
        }

        public class Atmospherecodemap
        {
        }

        public class PRICE_EXTEND
        {
            public Priceextendview[] priceExtendViews { get; set; }
            public string seperator { get; set; }
        }

        public class Priceextendview
        {
            public string content { get; set; }
        }

        public class SHIPPING
        {
            public string bgColor { get; set; }
            public string deliveryExt { get; set; }
            public List<Deliverylayoutinfo> deliveryLayoutInfo { get; set; }
            public string icon { get; set; }
            public Originallayoutresultlist[] originalLayoutResultList { get; set; }
            public string selectedDeliveryOptionCode { get; set; }
            public string styleCode { get; set; }
            public Systemfeature systemFeature { get; set; }
            public string title { get; set; }
        }

        public class Systemfeature
        {
            public bool hideShipFrom { get; set; }
        }

        public class Deliverylayoutinfo
        {
            public Additionlayout[] additionLayout { get; set; }
            public Bizdata bizData { get; set; }
            public Contentlayout[][] contentLayout { get; set; }
            public string[] deliveryOptionPanelDisplayList { get; set; }
            public string[] detailFirstScreenDisplayList { get; set; }
            public Dxtemplateinfo dxTemplateInfo { get; set; }
            public int finalPatternId { get; set; }
            public string[] skuSwitchDisplayList { get; set; }
            public Titlelayout[][] titleLayout { get; set; }
        }

        public class Bizdata
        {
            public int patternId { get; set; }
            public string logisticsComposeThreshold { get; set; }
            public double discount { get; set; }
            public string tracking { get; set; }
            public string workDayOneFourBeforeCutTime { get; set; }
            public string delayCompensationTime { get; set; }
            public string displayTomorrow { get; set; }
            public string itemScene { get; set; }
            public string shipToCode { get; set; }
            public string thresholdOverZero { get; set; }
            public string delayCompensationOriginalCurrency { get; set; }
            public string delayCompensationOriginalAmount { get; set; }
            public string shipFrom { get; set; }
            public int deliveryDayMax { get; set; }
            public string displayEtaMinDate { get; set; }
            public int guaranteedDeliveryTime { get; set; }
            public string etaVersionDisplayType { get; set; }
            public string freightCommitDay { get; set; }
            public string etaTraceId { get; set; }
            public string delayCompensationAmountDisplay { get; set; }
            public int etaVersionStrategyId { get; set; }
            public string workDayOneFiveBeforeCutTime { get; set; }
            public string delayCompensationType { get; set; }
            public long itemId { get; set; }
            public string solutionBusinessType { get; set; }
            public int etaVersionSceneId { get; set; }
            public float displayAmount { get; set; }
            public string shippingFee { get; set; }
            public string displayEtaMaxDate { get; set; }
            public int deliveryDayMin { get; set; }
            public string delayCompensationMode { get; set; }
            public string warehouseType { get; set; }
            public int solutionId { get; set; }
            public string deliveryOptionCode { get; set; }
            public int[] leadTimeSort { get; set; }
            public string deliveryProviderName { get; set; }
            public string useTimeType { get; set; }
            public string subWarehouseType { get; set; }
            public string utParams { get; set; }
            public string warehouseCode { get; set; }
            public string existEtaMinTime { get; set; }
            public string provider { get; set; }
            public string itemShipFromType { get; set; }
            public string identityType { get; set; }
            public string currency { get; set; }
            public string company { get; set; }
            public string deliveryDate { get; set; }
            public string formattedAmount { get; set; }
            public string passiveDelayCompensationDelayDisplay { get; set; }
            public string choiceFreeShipping { get; set; }
            public int quantity { get; set; }
            public string leadTimeType { get; set; }
            public string existEtaMaxTime { get; set; }
            public int solutionBusinessXDays { get; set; }
            public string guaranteedDeliveryTimeProviderName { get; set; }
            public string showXDayDeliveryTips { get; set; }
            public string shipFromCode { get; set; }
            public string displayCurrency { get; set; }
            public string deliveryProviderCode { get; set; }
            public string shipTo { get; set; }
        }

        public class Dxtemplateinfo
        {
        }

        public class Additionlayout
        {
            public string blockGroup { get; set; }
            public string blockType { get; set; }
            public string componentId { get; set; }
            public string content { get; set; }
            public string displayCondition { get; set; }
            public int distance { get; set; }
            public string medusaKey { get; set; }
            public int order { get; set; }
            public string type { get; set; }
            public bool useInDeliveryOptionPanel { get; set; }
            public bool useInDetailFirstScreen { get; set; }
            public bool useInSkuSwitch { get; set; }
        }

        public class Contentlayout
        {
            public string blockGroup { get; set; }
            public string blockType { get; set; }
            public string componentId { get; set; }
            public string content { get; set; }
            public string displayCondition { get; set; }
            public int distance { get; set; }
            public string medusaKey { get; set; }
            public int order { get; set; }
            public string type { get; set; }
            public bool useInDeliveryOptionPanel { get; set; }
            public bool useInDetailFirstScreen { get; set; }
            public bool useInSkuSwitch { get; set; }
        }

        public class Titlelayout
        {
            public string blockGroup { get; set; }
            public string blockType { get; set; }
            public string componentId { get; set; }
            public string content { get; set; }
            public string displayCondition { get; set; }
            public int distance { get; set; }
            public string medusaKey { get; set; }
            public int order { get; set; }
            public string type { get; set; }
            public bool useInDeliveryOptionPanel { get; set; }
            public bool useInDetailFirstScreen { get; set; }
            public bool useInSkuSwitch { get; set; }
        }

        public class Originallayoutresultlist
        {
            public Additionlayout1[] additionLayout { get; set; }
            public Bizdata1 bizData { get; set; }
            public Contentlayout1[][] contentLayout { get; set; }
            public string[] deliveryOptionPanelDisplayList { get; set; }
            public string[] detailFirstScreenDisplayList { get; set; }
            public Dxtemplateinfo1 dxTemplateInfo { get; set; }
            public int finalPatternId { get; set; }
            public string[] skuSwitchDisplayList { get; set; }
            public Titlelayout1[][] titleLayout { get; set; }
        }

        public class Bizdata1
        {
            public int patternId { get; set; }
            public string logisticsComposeThreshold { get; set; }
            public double discount { get; set; }
            public string tracking { get; set; }
            public string workDayOneFourBeforeCutTime { get; set; }
            public string delayCompensationTime { get; set; }
            public string displayTomorrow { get; set; }
            public string itemScene { get; set; }
            public string shipToCode { get; set; }
            public string thresholdOverZero { get; set; }
            public string delayCompensationOriginalCurrency { get; set; }
            public string delayCompensationOriginalAmount { get; set; }
            public string shipFrom { get; set; }
            public int deliveryDayMax { get; set; }
            public string displayEtaMinDate { get; set; }
            public int guaranteedDeliveryTime { get; set; }
            public string etaVersionDisplayType { get; set; }
            public string freightCommitDay { get; set; }
            public string etaTraceId { get; set; }
            public string delayCompensationAmountDisplay { get; set; }
            public int etaVersionStrategyId { get; set; }
            public string workDayOneFiveBeforeCutTime { get; set; }
            public string delayCompensationType { get; set; }
            public long itemId { get; set; }
            public string solutionBusinessType { get; set; }
            public int etaVersionSceneId { get; set; }
            public float displayAmount { get; set; }
            public string shippingFee { get; set; }
            public string displayEtaMaxDate { get; set; }
            public int deliveryDayMin { get; set; }
            public string delayCompensationMode { get; set; }
            public string warehouseType { get; set; }
            public int solutionId { get; set; }
            public string deliveryOptionCode { get; set; }
            public int[] leadTimeSort { get; set; }
            public string deliveryProviderName { get; set; }
            public string useTimeType { get; set; }
            public string subWarehouseType { get; set; }
            public string utParams { get; set; }
            public string warehouseCode { get; set; }
            public string existEtaMinTime { get; set; }
            public string provider { get; set; }
            public string itemShipFromType { get; set; }
            public string identityType { get; set; }
            public string currency { get; set; }
            public string company { get; set; }
            public string deliveryDate { get; set; }
            public string formattedAmount { get; set; }
            public string passiveDelayCompensationDelayDisplay { get; set; }
            public string choiceFreeShipping { get; set; }
            public int quantity { get; set; }
            public string leadTimeType { get; set; }
            public string existEtaMaxTime { get; set; }
            public int solutionBusinessXDays { get; set; }
            public string guaranteedDeliveryTimeProviderName { get; set; }
            public string showXDayDeliveryTips { get; set; }
            public string shipFromCode { get; set; }
            public string displayCurrency { get; set; }
            public string deliveryProviderCode { get; set; }
            public string shipTo { get; set; }
        }

        public class Dxtemplateinfo1
        {
        }

        public class Additionlayout1
        {
            public string blockGroup { get; set; }
            public string blockType { get; set; }
            public string componentId { get; set; }
            public string content { get; set; }
            public string displayCondition { get; set; }
            public int distance { get; set; }
            public string medusaKey { get; set; }
            public int order { get; set; }
            public string type { get; set; }
            public bool useInDeliveryOptionPanel { get; set; }
            public bool useInDetailFirstScreen { get; set; }
            public bool useInSkuSwitch { get; set; }
        }

        public class Contentlayout1
        {
            public string blockGroup { get; set; }
            public string blockType { get; set; }
            public string componentId { get; set; }
            public string content { get; set; }
            public string displayCondition { get; set; }
            public int distance { get; set; }
            public string medusaKey { get; set; }
            public int order { get; set; }
            public string type { get; set; }
            public bool useInDeliveryOptionPanel { get; set; }
            public bool useInDetailFirstScreen { get; set; }
            public bool useInSkuSwitch { get; set; }
        }

        public class Titlelayout1
        {
            public string blockGroup { get; set; }
            public string blockType { get; set; }
            public string componentId { get; set; }
            public string content { get; set; }
            public string displayCondition { get; set; }
            public int distance { get; set; }
            public string medusaKey { get; set; }
            public int order { get; set; }
            public string type { get; set; }
            public bool useInDeliveryOptionPanel { get; set; }
            public bool useInDetailFirstScreen { get; set; }
            public bool useInSkuSwitch { get; set; }
        }

        public class DESC
        {
            public bool isLocalSeller { get; set; }
            public string nativeDescUrl { get; set; }
            public string pcDescUrl { get; set; }
            public string sellerLoginId { get; set; }
            public Sellingpointinfo sellingPointInfo { get; set; }
            public int storeId { get; set; }
            public string title { get; set; }
        }

        public class Sellingpointinfo
        {
            public int limitCnt { get; set; }
            public string title { get; set; }
            public string viewLessText { get; set; }
            public string viewMoreText { get; set; }
        }

        public class TRAFFIC_IDENTIFY
        {
            public bool targetTraffic { get; set; }
        }

        public class AFTER_SALE_SERVICE_BLOCK
        {
            public string icon { get; set; }
            public string popupTitle { get; set; }
            public int promiseId { get; set; }
            public string title { get; set; }
            public bool titleBold { get; set; }
            public string titleColor { get; set; }
        }

        public class PRODUCT_PROP_PC
        {
            public Outerprop[] outerProps { get; set; }
            public object[] propGroups { get; set; }
            public bool sexy { get; set; }
            public bool showOnAbove { get; set; }
            public bool showPropGroups { get; set; }
            public bool showPropOuter { get; set; }
            public List<Showedprop> showedProps { get; set; }
            public Showedpropsmap showedPropsMap { get; set; }
        }

        public class Showedpropsmap
        {
            public _10 _10 { get; set; }
            public _219 _219 { get; set; }
            public _266081643 _266081643 { get; set; }
        }

        public class _10
        {
            public string attrName { get; set; }
            public string attrValue { get; set; }
        }

        public class _219
        {
            public string attrName { get; set; }
            public string attrValue { get; set; }
        }

        public class _266081643
        {
            public string attrName { get; set; }
            public string attrValue { get; set; }
        }

        public class Outerprop
        {
            public string attrName { get; set; }
            public string attrValue { get; set; }
        }

        public class Showedprop
        {
            public string attrName { get; set; }
            public string attrValue { get; set; }
        }

        public class SUSTAINABILITY_SERVICE_BLOCK
        {
            public string icon { get; set; }
            public string popupTitle { get; set; }
            public int promiseId { get; set; }
            public string title { get; set; }
            public bool titleBold { get; set; }
            public string titleColor { get; set; }
        }

        public class GLOBAL_DATA
        {
            public Globaldata globalData { get; set; }
        }

        public class Globaldata
        {
            public object aff_platform { get; set; }
            public string subject { get; set; }
            public bool hideCartSideBar { get; set; }
            public object dp { get; set; }
            public string localStr { get; set; }
            public string regionCountryName { get; set; }
            public Producttaginfo productTagInfo { get; set; }
            public int httpStatus { get; set; }
            public int itemStatus { get; set; }
            public object sk { get; set; }
            public string choiceIcon { get; set; }
            public bool eticketStructContent { get; set; }
            public string siteType { get; set; }
            public string category2 { get; set; }
            public string traceId { get; set; }
            public Recommendinfo recommendInfo { get; set; }
            public string category3 { get; set; }
            public string image { get; set; }
            public string aeOrderFrom { get; set; }
            public string topCategoryId { get; set; }
            public string regionProvinceName { get; set; }
            public string productId { get; set; }
            public object af { get; set; }
            public bool stealthSelection { get; set; }
            public string categoryPath { get; set; }
            public string category1 { get; set; }
            public bool superLinkProduct { get; set; }
            public object cn { get; set; }
            public I18n i18n { get; set; }
            public Productinfo productInfo { get; set; }
            public string securityToken { get; set; }
            public object relationProductId { get; set; }
            public string regionCityName { get; set; }
            public object cv { get; set; }
            public bool crawler { get; set; }
            public Breadcrumb breadcrumb { get; set; }
            public Offlineinfo offlineInfo { get; set; }
            public Categorytaginfo categoryTagInfo { get; set; }
            public object aff_trace_key { get; set; }
            public string complaintUrl { get; set; }
            public string currencyCode { get; set; }
            public string categoryId { get; set; }
            public Buriedloginfo buriedLogInfo { get; set; }
        }

        public class Producttaginfo
        {
            public bool mergeChoice { get; set; }
            public Otherapptagmap otherAppTagMap { get; set; }
            public Otherpctagmap otherPcTagMap { get; set; }
        }

        public class Otherapptagmap
        {
            public string eticketStructContent { get; set; }
        }

        public class Otherpctagmap
        {
            public bool eticketStructContent { get; set; }
        }

        public class Recommendinfo
        {
            public Recommendinfo1 recommendInfo { get; set; }
            public int sellerRecommendCount { get; set; }
            public bool needRecommendByTag { get; set; }
            public int recommendCount { get; set; }
            public bool storeRecommendHoriztal { get; set; }
            public int recommendForYouCount { get; set; }
            public string newPlatformRecommendParams { get; set; }
            public string newSellerRecommendParams { get; set; }
        }

        public class Recommendinfo1
        {
            public int categoryId { get; set; }
            public int productType { get; set; }
            public long sellerAdminSeq { get; set; }
            public int status { get; set; }
            public int storeId { get; set; }
            public bool supportRecommend { get; set; }
            public bool supportSellerRecommend { get; set; }
        }

        public class I18n
        {
            public Skumodule SKUModule { get; set; }
            public Priceguarantee priceGuarantee { get; set; }
            public Actionmodule ActionModule { get; set; }
            public Storemodule StoreModule { get; set; }
            public Recommendmodule RecommendModule { get; set; }
            public Storeinfo StoreInfo { get; set; }
            public Itemdetailresp ItemDetailResp { get; set; }
            public Bpfreereturn30 bpFreeReturn30 { get; set; }
            public Bplocaldomesticreturn bpLocalDomesticReturn { get; set; }
            public Pricemodule PriceModule { get; set; }
            public Shippingmodule ShippingModule { get; set; }
            public Buyerprotectionmodule BuyerProtectionModule { get; set; }
            public Bpspainfreereturn bpSpainFreeReturn { get; set; }
            public Common Common { get; set; }
            public Buyerprotection buyerProtection { get; set; }
            public Businessmodule BusinessModule { get; set; }
            public Quantitymodule QuantityModule { get; set; }
            public Otherservicemodule OtherServiceModule { get; set; }
            public Crosslinkmodule CrossLinkModule { get; set; }
            public Bplocalreturn15 bpLocalReturn15 { get; set; }
            public Titlemodule TitleModule { get; set; }
            public Middlebannermodule MiddleBannerModule { get; set; }
            public Couponmodule CouponModule { get; set; }
            public Bpdomesticreturn7 bpDomesticReturn7 { get; set; }
        }

        public class Skumodule
        {
            public string SIZING_INFO { get; set; }
            public string BUST_PROMPT { get; set; }
            public string GLASSES_DIALOG_TITLE { get; set; }
            public string NV_ADD { get; set; }
            public string SPH { get; set; }
            public string PUPILLARY_PROMPT { get; set; }
            public string SIZE_HOVER_TITLE { get; set; }
            public string FLOOR_CONTENT { get; set; }
            public string CUSTOM_SIZE_CONTENT { get; set; }
            public string NV_ADD_PROMPT { get; set; }
            public string PLEASE_ENTER { get; set; }
            public string WAIST_TITLE { get; set; }
            public string WAIST_CONTENT { get; set; }
            public string CYL { get; set; }
            public string SERVICE { get; set; }
            public string BUST_CONTENT { get; set; }
            public string SIZE_INFO { get; set; }
            public string SIZE_INFO_DESC { get; set; }
            public string ITEM_CONDITION_TIP { get; set; }
            public string BTN_CANCEL { get; set; }
            public string HOW_TO_MEASURE { get; set; }
            public string SIZE_INFO_TIP { get; set; }
            public string FLOOR_PROMPT { get; set; }
            public string FLOOR_TITLE { get; set; }
            public string SIZE_INFO_COMPARE_TIP { get; set; }
            public string UNIT_PROMPT { get; set; }
            public string SELECT { get; set; }
            public string HIPS_TITLE { get; set; }
            public string HEIGHT_PROMPT { get; set; }
            public string WAIST_PROMPT { get; set; }
            public string BTN_SAVE { get; set; }
            public string TITLE_OPTIONAL { get; set; }
            public string SIZE_DIALOG_TITLE { get; set; }
            public string GLASSES_TIP { get; set; }
            public string SIZE_CHART { get; set; }
            public string HIPS_PROMPT { get; set; }
            public string SPH_PROMPT { get; set; }
            public string HIPS_CONTENT { get; set; }
            public string BUST_TITLE { get; set; }
            public string AXIS { get; set; }
        }

        public class Priceguarantee
        {
            public string brief { get; set; }
            public string name { get; set; }
            public string desc { get; set; }
        }

        public class Actionmodule
        {
            public string VIEW_DETAIL { get; set; }
            public string WISH_MAX_NOTICE { get; set; }
            public string BUYER_ABLE { get; set; }
            public string WISHLIST_SAVE_BUTTON { get; set; }
            public string WISH_MOVE_TO_ANOTHER_LIST_TIPS { get; set; }
            public string ADD_X_MORE { get; set; }
            public string COMING_SOON { get; set; }
            public string SC_ADD_SUCC { get; set; }
            public string WISHLIST_PUBLIC_NOTICE { get; set; }
            public string WISH_DETAULT_LIST { get; set; }
            public string WISH_CREATE_LIST { get; set; }
            public string WL_ERROR { get; set; }
            public string WISH_NAME_ALREADY_USE { get; set; }
            public string WISH_REVISELIST { get; set; }
            public string ADD_TO_MY_PICKS { get; set; }
            public string SC_ADD_FAILED { get; set; }
            public string SC_HAVE { get; set; }
            public string ADD_TO_CART { get; set; }
            public string WISH_CANCEL_WISHLIST { get; set; }
            public string BUY_NOW { get; set; }
            public string WISH_SYSTEM_ERROR { get; set; }
            public string SC_ADD_MAX { get; set; }
            public string SC_VIEW { get; set; }
            public string WISH_YOUMAYLIKE { get; set; }
            public string WISH_VIEW_WISH_LIST { get; set; }
            public string SC_RECOMMEND { get; set; }
            public string CONTINUE { get; set; }
            public string ADDED_TO { get; set; }
            public string ORDER_NOW { get; set; }
            public string SELECT_TIP { get; set; }
            public string NO_LONGER_AVAILABLE { get; set; }
            public string PREVIEW_PERIOD { get; set; }
            public string WISH_MAX_GROUP { get; set; }
            public string FIND_SIMILAR { get; set; }
            public string WISHLIST_PUBLIC_TIP { get; set; }
        }

        public class Storemodule
        {
            public string COUSTOMER_SERVICE { get; set; }
            public string VISIT_STORE { get; set; }
            public string CONTACT_SELLER { get; set; }
            public string FOLLOWING_STATE { get; set; }
            public string UNFOLLOWING_STATE { get; set; }
            public string POSITIVE_FEEDBACK { get; set; }
            public string FOLLOWERS { get; set; }
            public string FOLLOWER { get; set; }
            public string TOP_SELLER { get; set; }
            public string STORE_CATEGORIES { get; set; }
        }

        public class Recommendmodule
        {
            public string MORE_FROM_THIS_SELLER { get; set; }
            public string YOU_MAY_LIKE { get; set; }
            public string TOP_SELLING { get; set; }
            public string FROM_OTHER_SELLER { get; set; }
            public string VIEW_MORE_LINK { get; set; }
            public string PRODUCT_SOLD { get; set; }
        }

        public class Storeinfo
        {
            public string shippingSpeed { get; set; }
            public string below_average { get; set; }
            public string higher_seller { get; set; }
            public string storeId { get; set; }
            public string openSince { get; set; }
            public string above_average { get; set; }
            public string itemAsDescribed { get; set; }
            public string lower_seller { get; set; }
            public string businessInfo { get; set; }
            public string equal { get; set; }
            public string storeName { get; set; }
            public string storeInfo { get; set; }
            public string storeRating { get; set; }
            public string businessAddress { get; set; }
            public string communication { get; set; }
            public string equal_seller { get; set; }
        }

        public class Itemdetailresp
        {
            public string VIEW_MORE { get; set; }
            public string ASK_BUYERS { get; set; }
            public string PAGE_NOT_FOUND_NOTICE { get; set; }
            public string VIEW_5_MORE_ANSWERS { get; set; }
            public string PAGE_NOT_FOUND_RCMD_TITLE { get; set; }
        }

        public class Bpfreereturn30
        {
            public string brief { get; set; }
            public string name { get; set; }
            public string id { get; set; }
            public string desc { get; set; }
        }

        public class Bplocaldomesticreturn
        {
            public string brief { get; set; }
            public string name { get; set; }
            public string id { get; set; }
            public string desc { get; set; }
        }

        public class Pricemodule
        {
            public string LOT { get; set; }
            public string INSTALLMENT { get; set; }
            public string DEPOSIT { get; set; }
            public string PRE_ORDER_PRICE { get; set; }
        }

        public class Shippingmodule
        {
            public string HAB_BALLOON_TRAKING_AVAILABLE { get; set; }
            public string GENERAL_SHIPPING_TO { get; set; }
            public string SELECT_SHIP_FROM_TIP { get; set; }
            public string SHIPPING_TO { get; set; }
            public string HAB_SHIPPING_TO { get; set; }
            public string CARRIER { get; set; }
            public string TO_PROVINCE { get; set; }
            public string TO_COUNTRY { get; set; }
            public string TO_CITY { get; set; }
            public string CAN_NOE_DELIVER_NOTE { get; set; }
            public string ESTIMATED_DELIVERT_ON_DAYS { get; set; }
            public string CHOOSE_DELIVERT_METHOD { get; set; }
            public string HAB_BALLOON_DOOR_DELIVERY { get; set; }
            public string DELIVERED_BY { get; set; }
            public string HBA_SHIPPING_INFO { get; set; }
            public string IN { get; set; }
            public string SEARCH { get; set; }
            public string SELECT_SHIP_FROM { get; set; }
            public string LOGISTIC_COMPOSE_AE { get; set; }
            public string HBA_BALLOON_TIPS { get; set; }
            public string GENERAL_SHIPPING_DELIVERY { get; set; }
            public string VAT_DE_DETAIL { get; set; }
            public string SELECTED { get; set; }
            public string HBA_DOR_DELIVERY { get; set; }
            public string ESTIMATED_DELIVERT_ON_DATE { get; set; }
            public string OR_FULL_REFOUND { get; set; }
            public string LOGISTIC_COMPOSE_BRAND_MIND { get; set; }
            public string PLAZA_SHOP_NOW_RECEIVE_ON { get; set; }
            public string LOGISTIC_COMPOSE_ORDERS_OVER { get; set; }
            public string FAST_SHIPPING { get; set; }
            public string CAN_NOT_DELIVER { get; set; }
            public string HBA_TRAKING_AVAILABLE { get; set; }
            public string DAYS { get; set; }
            public string GENERAL_SHIPPIING_FROM { get; set; }
            public string GENERAL_SHPPING_MORE { get; set; }
            public string FREE_SHIPPING { get; set; }
            public string COST { get; set; }
            public string BALLOON_TIP { get; set; }
            public string SHIP_MY_ITEM_TO { get; set; }
            public string HAB_BALLOON_VAT_INCLUDED { get; set; }
            public string TRACKING { get; set; }
            public string LOGISTIC_COMPOSE_SPEED_UP { get; set; }
            public string PLAZA_BALLOON_TIP { get; set; }
            public string HBA_TVAT_INCLUDED { get; set; }
            public string ESTIMATED_DELIVERY { get; set; }
            public string TO_WHERE { get; set; }
            public string VAT_NUMBER { get; set; }
            public string TO_VIA { get; set; }
            public string APPLY { get; set; }
        }

        public class Buyerprotectionmodule
        {
            public string PLAZA_BUYER_PROTECTION_CONTENT { get; set; }
            public string PLAZA_BUYER_PROTECTION_TITLE { get; set; }
            public string PLAZA_BUYER_PROTECTION_TIP { get; set; }
        }

        public class Bpspainfreereturn
        {
            public string brief { get; set; }
            public string name { get; set; }
            public string id { get; set; }
            public string desc { get; set; }
        }

        public class Common
        {
            public string recommendationsFrom { get; set; }
            public string sharePc { get; set; }
            public string sharePcContent { get; set; }
            public string skuEditSelection { get; set; }
            public string description { get; set; }
            public string specification { get; set; }
            public string skuSelections { get; set; }
            public string feedback { get; set; }
            public string youMayAlsoLike { get; set; }
            public string deliveryBlockTitle { get; set; }
            public string outOfStock { get; set; }
            public string topSellingsFrom { get; set; }
            public string similarRecommond { get; set; }
            public string youMayAlsoLikeSeeMore { get; set; }
            public string qanda { get; set; }
        }

        public class Buyerprotection
        {
            public string brief { get; set; }
            public string name { get; set; }
            public string desc { get; set; }
        }

        public class Businessmodule
        {
            public string downloadImageButton { get; set; }
            public string addToCartMaximumTips { get; set; }
            public string downloadImageSelectAll { get; set; }
            public string downloadImageTitle { get; set; }
        }

        public class Quantitymodule
        {
            public string LOT { get; set; }
            public string LOTS { get; set; }
            public string BUY_LIMIT { get; set; }
            public string QUANTITY { get; set; }
            public string OFF_OR_MORE { get; set; }
            public string ONLY_QUANTITY_LEFT { get; set; }
            public string ADDTIONAL { get; set; }
            public string QUANTITY_AVAILABLE { get; set; }
        }

        public class Otherservicemodule
        {
            public string TAB_SPECS { get; set; }
            public string PLAZA_SERVICE_SUBTITLE_PC { get; set; }
            public string PLAZA_SERVICE_WARRANTY_EMAIL { get; set; }
            public string PLAZA_SERVICE_WARRANTY_PHONE { get; set; }
            public string TAB_STORE { get; set; }
            public string PLAZA_SERVICE_WARRANTY_HOURS { get; set; }
            public string TAB_CUSTOMER_REVIEWS { get; set; }
            public string PLAZA_SERVICE_WARRANTY_WEBSITE { get; set; }
            public string TAB_OVERVIEW { get; set; }
            public string PLAZA_SERVICE_WARRANTY_BRAND { get; set; }
            public string PLAZA_SERVICE_WARRANTY_CATEGORY { get; set; }
            public string PLAZA_SERVICE_TITLE_PC { get; set; }
            public string PLAZA_SERVICE_CONTENT3_3_PC { get; set; }
            public string TAB_CUSTOMER_SHOW { get; set; }
            public string PLAZA_SERVICE_WARRANTY_TITLE { get; set; }
            public string TAB_REPORT_ITEM { get; set; }
            public string TAB_SERVICE { get; set; }
            public string PLAZA_SERVICE_CONTENT3_1_PC { get; set; }
            public string PLAZA_SERVICE_CONTENT3_2_PC { get; set; }
            public string PLAZA_SERVICE_CONTENT1_PC { get; set; }
            public string PLAZA_SERVICE_SUBTITLE2_PC { get; set; }
            public string PLAZA_SERVICE_CONTENT2_PC { get; set; }
            public string PLAZA_SERVICE_SUBTITLE3_PC { get; set; }
        }

        public class Crosslinkmodule
        {
            public string BREADCRUMB_PART2 { get; set; }
            public string BREADCRUMB_PART1 { get; set; }
            public string CROSSLINK_RELATED_SEARCHES { get; set; }
        }

        public class Bplocalreturn15
        {
            public string brief { get; set; }
            public string name { get; set; }
            public string id { get; set; }
            public string desc { get; set; }
        }

        public class Titlemodule
        {
            public string REVIEWS { get; set; }
            public string VIEW_ALL_REVIEWS { get; set; }
            public string REVIEW { get; set; }
            public string VIEW_OTHER_TITLE { get; set; }
            public string VIEW_EN_TITLE { get; set; }
            public string FREEBIE_REVIEW { get; set; }
            public string FREEBIE_REVIEWS { get; set; }
        }

        public class Middlebannermodule
        {
            public string END_IN { get; set; }
            public string DAYS { get; set; }
            public string DAY { get; set; }
            public string STARTS_IN { get; set; }
        }

        public class Couponmodule
        {
            public string GET_COUPONS { get; set; }
            public string SCP_ERR_HAVE { get; set; }
            public string OFF_ON { get; set; }
            public string ORDER_OVER { get; set; }
            public string code52Title { get; set; }
            public string GET_IT { get; set; }
            public string GET_NOW { get; set; }
            public string GET_MORE { get; set; }
            public string systemError { get; set; }
            public string OFF_PER { get; set; }
            public string STORE_COUPON { get; set; }
            public string SHOP_COUPONE_TIME_START_END { get; set; }
            public string NEW_USER_COUPON_ACQUIRE_FAIL_ALREADY_HAVE { get; set; }
            public string code50Title { get; set; }
            public string NEW_USER_COUPON_ACQUIRE_FAIL_NOT_AVAILABLE_NOW { get; set; }
            public string NEW_USER_COUPON_ACQUIRE_FAIL_GROUP_LIMIT { get; set; }
            public string code14Title { get; set; }
            public string NEW_USER_COUPON_ACQUIRE_FAIL_NOT_NEW_USER { get; set; }
            public string SHOP_PROMO_CODE_COPIED { get; set; }
            public string ADDED { get; set; }
            public string NEW_USER_COUPON_ACQUIRE_FAIL_SECURITY { get; set; }
            public string SHOP_PROMO_CODE_TITLE { get; set; }
            public string NEW_USER_COUPON_ACQUIRE_FAIL { get; set; }
            public string NEW_USER_COUPON_ACQUIRE_FAIL_LIMIT_DAY { get; set; }
            public string NEW_USER_COUPON_ACQUIRE_FAIL_REGISTER_COUNTRY_LIMIT { get; set; }
            public string SCP_ERR_NONE { get; set; }
            public string NEW_USER_COUPON_ACQUIRE_FAIL_GRANT_ERROR { get; set; }
            public string code17Title { get; set; }
            public string SHOP_COUPONE_TIME_EXPIRES { get; set; }
            public string SHOPPONG_CREDIT { get; set; }
            public string EXCHANGE_MORE { get; set; }
            public string NEW_USER_COUPON_ACQUIRE_FAIL_SYSTEM_ERROR { get; set; }
            public string MY_BALANCE { get; set; }
            public string INSTANT_DISCOUNT { get; set; }
            public string EXCHANGE_NOW { get; set; }
            public string CROSS_STORE_VOUCHER_TIP { get; set; }
            public string NEW_USER_COUPON { get; set; }
            public string GET { get; set; }
            public string SHOP_PROMO_CODE_COP_FAILED { get; set; }
            public string code30005Title { get; set; }
            public string MY_COINS { get; set; }
            public string BUY_GET_OFF { get; set; }
            public string code51Title { get; set; }
            public string PANEL_TITLE { get; set; }
            public string NEW_USER_COUPON_ACQUIRE_FAIL_LIMIT_HOUR { get; set; }
            public string CROSS_STORE_VOUCHER { get; set; }
        }

        public class Bpdomesticreturn7
        {
            public string brief { get; set; }
            public string name { get; set; }
            public string id { get; set; }
            public string desc { get; set; }
        }

        public class Productinfo
        {
            public int categoryId { get; set; }
            public string detailUrl { get; set; }
            public bool hasStock { get; set; }
            public string productId { get; set; }
            public bool sexyItemShow { get; set; }
        }

        public class Breadcrumb
        {
            public string categoryName { get; set; }
            public string categoryUrl { get; set; }
            public int displayCatId { get; set; }
            public bool kidBaby { get; set; }
            public Pathlist[] pathList { get; set; }
        }

        public class Pathlist
        {
            public int cateId { get; set; }
            public string name { get; set; }
            public string remark { get; set; }
            public string url { get; set; }
        }

        public class Offlineinfo
        {
            public int itemStatus { get; set; }
        }

        public class Categorytaginfo
        {
            public bool canAddSkuSeries { get; set; }
            public bool virtualProduct { get; set; }
            public bool voucherProduct { get; set; }
        }

        public class Buriedloginfo
        {
            public string priceCenterBuriedData { get; set; }
        }

        public class PRICE
        {
            public string discountExt { get; set; }
            public bool isLot { get; set; }
            public string priceLocalConfig { get; set; }
            public long productId { get; set; }
            public string region { get; set; }
            public long selectedSkuId { get; set; }
            public Skuidstrpriceinfomap skuIdStrPriceInfoMap { get; set; }
            public object skuPriceInfoMap { get; set; }
            public Skusecondpriceinfomap skuSecondPriceInfoMap { get; set; }
            public Targetskupriceinfo targetSkuPriceInfo { get; set; }
        }
        public class SkuPriceDetail
        {
            public string discount { get; set; }
            public Originalprice originalPrice { get; set; }
            public string originalPricePrefixText { get; set; }
            public string priceFontColor { get; set; }
            public string salePriceLocal { get; set; }
            public string salePriceString { get; set; }
            public string sellerByLot { get; set; }
        }
        public class Skuidstrpriceinfomap
        {
            public _120000373801048201 _12000037380104820 { get; set; }
        }

        public class _120000373801048201
        {
            public string discount { get; set; }
            public Originalprice originalPrice { get; set; }
            public string originalPricePrefixText { get; set; }
            public string priceFontColor { get; set; }
            public string salePriceLocal { get; set; }
            public string salePriceString { get; set; }
            public bool sellerByLot { get; set; }
        }

        public class Originalprice
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class Skupriceinfomap
        {
            public _120000373801048202 _12000037380104820 { get; set; }
        }

        public class _120000373801048202
        {
            public string discount { get; set; }
            public Originalprice1 originalPrice { get; set; }
            public string originalPricePrefixText { get; set; }
            public string priceFontColor { get; set; }
            public string salePriceLocal { get; set; }
            public string salePriceString { get; set; }
            public bool sellerByLot { get; set; }
        }

        public class Originalprice1
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class Skusecondpriceinfomap
        {
        }

        public class Targetskupriceinfo
        {
            public string discount { get; set; }
            public Originalprice2 originalPrice { get; set; }
            public string originalPricePrefixText { get; set; }
            public string priceFontColor { get; set; }
            public string salePriceLocal { get; set; }
            public string salePriceString { get; set; }
            public bool sellerByLot { get; set; }
        }

        public class Originalprice2
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class SELLING_POINT_PC
        {
        }

        public class WISHLIST
        {
            public bool favorited { get; set; }
            public bool storeWished { get; set; }
            public int storeWishedCount { get; set; }
            public int wishItemCount { get; set; }
        }

        public class SHOP_CARD_PC
        {
            public Benefitinfolist[] benefitInfoList { get; set; }
            public string flag { get; set; }
            public string fontColor { get; set; }
            public bool hasStoreFeedback { get; set; }
            public string logo { get; set; }
            public Sellerinfo sellerInfo { get; set; }
            public string sellerLevel { get; set; }
            public int sellerPositiveNum { get; set; }
            public string sellerPositiveRate { get; set; }
            public int sellerScore { get; set; }
            public int sellerTotalNum { get; set; }
            public string storeAllProductPage { get; set; }
            public Storeheaderresult storeHeaderResult { get; set; }
            public string storeHomePage { get; set; }
            public string storeName { get; set; }
        }

        public class Sellerinfo
        {
            public long adminSeq { get; set; }
            public bool aeRuSelfOperation { get; set; }
            public bool boutiqueSeller { get; set; }
            public long companyId { get; set; }
            public bool esRetailOrConsignment { get; set; }
            public bool hasStore { get; set; }
            public bool localSeller { get; set; }
            public bool payPalAccount { get; set; }
            public bool plazaElectronicSeller { get; set; }
            public bool ruConsignmentAndMarketPlace { get; set; }
            public bool ruSelfOperation { get; set; }
            public bool showPlazaHeader { get; set; }
            public bool spanishPlaza { get; set; }
            public int storeNum { get; set; }
            public string storeURL { get; set; }
            public bool topRatedSeller { get; set; }
        }

        public class Storeheaderresult
        {
        }

        public class Benefitinfolist
        {
            public string title { get; set; }
            public string value { get; set; }
        }

        public class RECOMMEND_PC
        {
            public bool hideStoreRecommend { get; set; }
            public bool isSlider { get; set; }
            public int similarPageSize { get; set; }
            public string similarScenario { get; set; }
        }

        public class BOTTOM_BAR_PC
        {
            public string bottomBarType { get; set; }
            public Buttonmap buttonMap { get; set; }
            public Extrainfo extraInfo { get; set; }
            public bool hideAddCart { get; set; }
            public bool hideBuyNow { get; set; }
            public string orderUrl { get; set; }
            public string shoppingCartUrl { get; set; }
            public bool showCoinPromotionMark { get; set; }
        }

        public class Buttonmap
        {
            public Wishlist1 wishList { get; set; }
            public Buynow buyNow { get; set; }
            public Addtocart addToCart { get; set; }
            public Store store { get; set; }
            public Message message { get; set; }
        }

        public class Wishlist1
        {
            public string buttonType { get; set; }
            public bool enable { get; set; }
            public Extramap extraMap { get; set; }
            public string iconAddress { get; set; }
        }

        public class Extramap
        {
            public string iconSelectedAddress { get; set; }
            public bool wishState { get; set; }
        }

        public class Buynow
        {
            public string bgColorEnd { get; set; }
            public string bgColorStart { get; set; }
            public string buttonType { get; set; }
            public bool enable { get; set; }
            public Extramap1 extraMap { get; set; }
            public string strokeColor { get; set; }
            public string textColor { get; set; }
            public string textContent { get; set; }
        }

        public class Extramap1
        {
        }

        public class Addtocart
        {
            public string bgColorEnd { get; set; }
            public string bgColorStart { get; set; }
            public string buttonType { get; set; }
            public bool enable { get; set; }
            public Extramap2 extraMap { get; set; }
            public string strokeColor { get; set; }
            public string textColor { get; set; }
            public string textContent { get; set; }
        }

        public class Extramap2
        {
        }

        public class Store
        {
            public string actionTarget { get; set; }
            public string buttonType { get; set; }
            public bool enable { get; set; }
            public Extramap3 extraMap { get; set; }
            public string iconAddress { get; set; }
        }

        public class Extramap3
        {
        }

        public class Message
        {
            public string actionTarget { get; set; }
            public string buttonType { get; set; }
            public bool enable { get; set; }
            public Extramap4 extraMap { get; set; }
            public string iconAddress { get; set; }
            public string textContent { get; set; }
        }

        public class Extramap4
        {
            public string textContent { get; set; }
        }

        public class Extrainfo
        {
            public bool quickOrder { get; set; }
            public bool wishState { get; set; }
        }

        public class MIND
        {
            public string bgImage { get; set; }
            public string content { get; set; }
            public string icon { get; set; }
            public string textColor { get; set; }
            public bool useV2 { get; set; }
        }

        public class PRODUCT_TITLE
        {
            public string icon { get; set; }
            public string text { get; set; }
            public string titleMaxLine { get; set; }
        }

        public class PERSONAL_INFORMATION_SECURITY
        {
            public string icon { get; set; }
            public string popupTitle { get; set; }
            public int promiseId { get; set; }
            public string subTitle { get; set; }
            public string title { get; set; }
            public bool titleBold { get; set; }
            public string titleColor { get; set; }
        }

        public class COUPON_BLOCK_PC
        {
            public string arrowColor { get; set; }
            public string discountPanelTitle { get; set; }
            public string discountPanelUrl { get; set; }
            public string endColor { get; set; }
            public bool newCouponBlock { get; set; }
            public long productId { get; set; }
            public string startColor { get; set; }
            public string textColor { get; set; }
            public Uppercouponblockviewlist[] upperCouponBlockViewList { get; set; }
            public Webcouponblockparam webCouponBlockParam { get; set; }
        }

        public class Webcouponblockparam
        {
            public string discountExt { get; set; }
            public string recBizScene { get; set; }
            public string targetPanel { get; set; }
        }

        public class Uppercouponblockviewlist
        {
            public string icon { get; set; }
            public string text { get; set; }
        }

        public class SKU
        {
            public Extrainfo1 extraInfo { get; set; }
            public bool hitSkuNonSelectableExt { get; set; }
            public string selectedSkuAttr { get; set; }
            public long selectedSkuId { get; set; }
            public string selectedSkuIdStr { get; set; }
            public bool selectedSkuSaleable { get; set; }
            public List<Skupath> skuPaths { get; set; }
            public List<SkuProperties> skuProperties { get; set; }
            public bool stealthSelection { get; set; }
            public bool supportClothesSku { get; set; }
        }

        public class Extrainfo1
        {
            public int skuCountThreshold4ShowSmallImage { get; set; }
        }

        public class Skupath
        {
            public string path { get; set; }
            public bool salable { get; set; }
            public string skuAttr { get; set; }
            public long skuId { get; set; }
            public string skuIdStr { get; set; }
            public int skuStock { get; set; }
        }

        public class SkuProperties
        {
            public bool hasSkuImage { get; set; }
            public bool isShowTypeColor { get; set; }
            public string showType { get; set; }
            public bool showTypeColor { get; set; }
            public int skuPropertyId { get; set; }
            public string skuPropertyName { get; set; }
            public List<Skupropertyvalue> skuPropertyValues { get; set; }
            public Sizeinfo sizeInfo { get; set; }
        }
        public class Sizeinfo
        {
            public bool hasSizeInfo { get; set; }
            public string icon { get; set; }
            public string sizeInfoUrl { get; set; }
            public string title { get; set; }
        }

        public class Skupropertyvalue
        {
            public string propertyValueDisplayName { get; set; }
            public int propertyValueIdLong { get; set; }
            public string propertyValueName { get; set; }
            public bool showHot { get; set; }
            public string skuColorValue { get; set; }
            public string skuPropertyImagePath { get; set; }
            public string skuPropertyImageSummPath { get; set; }
            public string propertyValueDefinitionName { get; set; }
        }
        public class DELIVERY_GUARANTEE_SERVICE
        {
            public string icon { get; set; }
            public string popupTitle { get; set; }
            public int promiseId { get; set; }
            public Subcontent[] subContents { get; set; }
            public string title { get; set; }
            public bool titleBold { get; set; }
            public string titleColor { get; set; }
        }

        public class Subcontent
        {
            public string content { get; set; }
            public string contentDesc { get; set; }
            public string icon { get; set; }
        }

    }
}
