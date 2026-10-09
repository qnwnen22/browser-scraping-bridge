#nullable disable warnings
using System.Collections.Generic;

namespace BrowserScrapingBridge.Server.Sourcing.AliExpress_KR.Models
{
    public class DataModel
    {
        public Actionmodule actionModule { get; set; }
        public Aeplusmodule aePlusModule { get; set; }
        public Buyerprotectionmodule buyerProtectionModule { get; set; }
        public Commonmodule commonModule { get; set; }
        public Couponmodule couponModule { get; set; }
        public Crosslinkmodule crossLinkModule { get; set; }
        public Descriptionmodule descriptionModule { get; set; }
        public Features7 features { get; set; }
        public Feedbackmodule feedbackModule { get; set; }
        public Groupsharemodule groupShareModule { get; set; }
        public I18nmap9 i18nMap { get; set; }
        public Imagemodule imageModule { get; set; }
        public Installmentmodule installmentModule { get; set; }
        public Middlebannermodule middleBannerModule { get; set; }
        public string name { get; set; }
        public Otherservicemodule otherServiceModule { get; set; }
        public Pagemodule pageModule { get; set; }
        public Presalemodule preSaleModule { get; set; }
        public string prefix { get; set; }
        public Pricemodule priceModule { get; set; }
        public Quantitymodule quantityModule { get; set; }
        public Recommendmodule recommendModule { get; set; }
        public Redirectmodule redirectModule { get; set; }
        public Shippingmodule shippingModule { get; set; }
        public Skumodule skuModule { get; set; }
        public Specsmodule specsModule { get; set; }
        public Storemodule storeModule { get; set; }
        public Titlemodule titleModule { get; set; }
        public Webenv webEnv { get; set; }


        public class Actionmodule
        {
            public Actionconfs actionConfs { get; set; }
            public string addToCartUrl { get; set; }
            public string aeOrderFrom { get; set; }
            public bool allowVisitorAddCart { get; set; }
            public string cartDetailUrl { get; set; }
            public int categoryId { get; set; }
            public bool comingSoon { get; set; }
            public long companyId { get; set; }
            public string confirmOrderUrl { get; set; }
            public Features features { get; set; }
            public string freightExt { get; set; }
            public I18nmap i18nMap { get; set; }
            public int id { get; set; }
            public bool invalidBuyNow { get; set; }
            public int itemStatus { get; set; }
            public bool itemWished { get; set; }
            public int itemWishedCount { get; set; }
            public bool localSeller { get; set; }
            public string name { get; set; }
            public bool preSale { get; set; }
            public long productId { get; set; }
            public int rootCategoryId { get; set; }
            public bool showCoinAnim { get; set; }
            public bool showShareHeader { get; set; }
            public int storeNum { get; set; }
            public Switchinfo switchInfo { get; set; }
            public int totalAvailQuantity { get; set; }
        }

        public class Actionconfs
        {
            public Buynowbtn buyNowBtn { get; set; }
        }

        public class Buynowbtn
        {
            public string bgColorEnd { get; set; }
            public string bgColorStart { get; set; }
            public string textColor { get; set; }
            public string textContent { get; set; }
        }

        public class Features
        {
        }

        public class I18nmap
        {
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
            public string WISHLIST_PUBLIC_TIP { get; set; }
        }

        public class Switchinfo
        {
            public bool deliveryMigrate { get; set; }
            public bool skuUnavailableRevision { get; set; }
        }

        public class Aeplusmodule
        {
            public Features1 features { get; set; }
            public I18nmap1 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public List<object> tags { get; set; }
        }

        public class Features1
        {
        }

        public class I18nmap1
        {
        }

        public class Buyerprotectionmodule
        {
            public Buyerprotection buyerProtection { get; set; }
            public Features2 features { get; set; }
            public I18nmap2 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
        }

        public class Buyerprotection
        {
            public string brief { get; set; }
            public string detailDescription { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public string redirectUrl { get; set; }
            public int type { get; set; }
        }

        public class Features2
        {
        }

        public class I18nmap2
        {
            public string PLAZA_BUYER_PROTECTION_TITLE { get; set; }
            public string PLAZA_BUYER_PROTECTION_TIP { get; set; }
            public string PLAZA_BUYER_PROTECTION_CONTENT { get; set; }
        }

        public class Commonmodule
        {
            public bool activity { get; set; }
            public List<object> aePlusTags { get; set; }
            public int categoryId { get; set; }
            public bool choiceProduct { get; set; }
            public bool crawler { get; set; }
            public string currencyCode { get; set; }
            public Features3 features { get; set; }
            public string gagaDataSite { get; set; }
            public I18nmap3 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public bool plaza { get; set; }
            public bool preSale { get; set; }
            public long productId { get; set; }
            public string productIdStr { get; set; }
            public Producttags productTags { get; set; }
            public List<object> reminds { get; set; }
            public long sellerAdminSeq { get; set; }
            public string tradeCurrencyCode { get; set; }
            public string userCountryCode { get; set; }
            public string userCountryName { get; set; }
        }

        public class Features3
        {
        }

        public class I18nmap3
        {
        }

        public class Producttags
        {
        }

        public class Couponmodule
        {
            public string currencyCode { get; set; }
            public Features4 features { get; set; }
            public I18nmap4 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public Webcouponinfo webCouponInfo { get; set; }
        }

        public class Features4
        {
        }

        public class I18nmap4
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

        public class Webcouponinfo
        {
            public Couponattributes couponAttributes { get; set; }
            public List<Couponlist> couponList { get; set; }
            public Promotionpaneldto promotionPanelDTO { get; set; }
            public bool usingNewCouponApi { get; set; }
        }

        public class Couponattributes
        {
            public string couponPackageText { get; set; }
            public string hasPlatformShippingFeeCoupon { get; set; }
        }

        public class Promotionpaneldto
        {
            public List<Shopcoupon> shopCoupon { get; set; }
            public List<Platformcoupon> platformCoupon { get; set; }
        }

        public class Shopcoupon
        {
            public string displayType { get; set; }
            public string promotionGroupTitle { get; set; }
            public List<Promotionpaneldetaildtolist> promotionPanelDetailDTOList { get; set; }
        }

        public class Promotionpaneldetaildtolist
        {
            public bool acquirable { get; set; }
            public string actionTips { get; set; }
            public Attributes attributes { get; set; }
            public Couponassignparam couponAssignParam { get; set; }
            public string displayType { get; set; }
            public long endTime { get; set; }
            public string promotionDesc { get; set; }
            public string promotionDetail { get; set; }
            public long startTime { get; set; }
            public int teasingStartTime { get; set; }
            public string toolCode { get; set; }
        }

        public class Attributes
        {
            public string activityId { get; set; }
            public string collectedTips { get; set; }
            public string codeScope { get; set; }
            public string couponCampaignStyle { get; set; }
            public string couponCode { get; set; }
        }

        public class Couponassignparam
        {
            public string couponMeta { get; set; }
            public string couponType { get; set; }
            public long promotionId { get; set; }
            public long sellerAdminId { get; set; }
            public int sellerCouponActivityType { get; set; }
        }

        public class Platformcoupon
        {
            public string displayType { get; set; }
            public string promotionGroupTitle { get; set; }
            public List<Promotionpaneldetaildtolist1> promotionPanelDetailDTOList { get; set; }
        }

        public class Promotionpaneldetaildtolist1
        {
            public bool acquirable { get; set; }
            public string actionTips { get; set; }
            public Attributes1 attributes { get; set; }
            public Couponassignparam1 couponAssignParam { get; set; }
            public string displayType { get; set; }
            public long endTime { get; set; }
            public string promotionDesc { get; set; }
            public string promotionDetail { get; set; }
            public long startTime { get; set; }
            public int teasingStartTime { get; set; }
            public string toolCode { get; set; }
        }

        public class Attributes1
        {
            public string houyiTrack { get; set; }
            public string collectedTips { get; set; }
            public string privateText { get; set; }
            public string codeScope { get; set; }
            public string couponCampaignStyle { get; set; }
            public string promotionId { get; set; }
        }

        public class Couponassignparam1
        {
            public string buffettParam { get; set; }
            public string couponMeta { get; set; }
            public string couponSource { get; set; }
            public string couponType { get; set; }
        }

        public class Couponlist
        {
            public string bgColor { get; set; }
            public string color { get; set; }
            public string copy { get; set; }
            public int order { get; set; }
            public string title { get; set; }
            public string type { get; set; }
        }

        public class Crosslinkmodule
        {
            public List<Breadcrumbpathlist> breadCrumbPathList { get; set; }
            public List<Crosslinkgrouplist> crossLinkGroupList { get; set; }
            public Features5 features { get; set; }
            public I18nmap5 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public long productId { get; set; }
        }

        public class Features5
        {
        }

        public class I18nmap5
        {
            public string BREADCRUMB_PART2 { get; set; }
            public string BREADCRUMB_PART1 { get; set; }
            public string CROSSLINK_RELATED_SEARCHES { get; set; }
        }

        public class Breadcrumbpathlist
        {
            public int cateId { get; set; }
            public string name { get; set; }
            public string remark { get; set; }
            public string url { get; set; }
        }

        public class Crosslinkgrouplist
        {
            public string channel { get; set; }
            public List<Crosslinklist> crossLinkList { get; set; }
            public string name { get; set; }
        }

        public class Crosslinklist
        {
            public string displayName { get; set; }
            public string name { get; set; }
            public string remark { get; set; }
            public string url { get; set; }
        }

        public class Descriptionmodule
        {
            public string descriptionUrl { get; set; }
            public Features6 features { get; set; }
            public I18nmap6 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public long productId { get; set; }
            public long sellerAdminSeq { get; set; }
        }

        public class Features6
        {
        }

        public class I18nmap6
        {
        }

        public class Features7
        {
        }

        public class Feedbackmodule
        {
            public long companyId { get; set; }
            public Features8 features { get; set; }
            public string feedbackServer { get; set; }
            public I18nmap7 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public long productId { get; set; }
            public long sellerAdminSeq { get; set; }
            public int trialReviewNum { get; set; }
        }

        public class Features8
        {
        }

        public class I18nmap7
        {
        }

        public class Groupsharemodule
        {
            public Features9 features { get; set; }
            public I18nmap8 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
        }

        public class Features9
        {
        }

        public class I18nmap8
        {
        }

        public class I18nmap9
        {
            public string VIEW_MORE { get; set; }
            public string ASK_BUYERS { get; set; }
            public string PAGE_NOT_FOUND_NOTICE { get; set; }
            public string VIEW_5_MORE_ANSWERS { get; set; }
            public string openTrace { get; set; }
            public string PAGE_NOT_FOUND_RCMD_TITLE { get; set; }
        }

        public class Imagemodule
        {
            public Features10 features { get; set; }
            public I18nmap10 i18nMap { get; set; }
            public int id { get; set; }
            public List<string> imagePathList { get; set; }
            public string name { get; set; }
            public List<string> summImagePathList { get; set; }
            public long? videoId { get; set; }
            public string videoUid { get; set; }
        }

        public class Features10
        {
        }

        public class I18nmap10
        {
        }

        public class Installmentmodule
        {
            public Features11 features { get; set; }
            public I18nmap11 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
        }

        public class Features11
        {
        }

        public class I18nmap11
        {
        }

        public class Middlebannermodule
        {
            public Features12 features { get; set; }
            public I18nmap12 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public bool showUniformBanner { get; set; }
        }

        public class Features12
        {
        }

        public class I18nmap12
        {
            public string END_IN { get; set; }
            public string DAYS { get; set; }
            public string DAY { get; set; }
            public string STARTS_IN { get; set; }
        }

        public class Otherservicemodule
        {
            public Features13 features { get; set; }
            public bool hasWarrantyInfo { get; set; }
            public I18nmap13 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
        }

        public class Features13
        {
        }

        public class I18nmap13
        {
            public string TAB_SPECS { get; set; }
            public string PLAZA_SERVICE_SUBTITLE_PC { get; set; }
            public string PLAZA_SERVICE_WARRANTY_EMAIL { get; set; }
            public string PLAZA_SERVICE_WARRANTY_PHONE { get; set; }
            public string PLAZA_SERVICE_WARRANTY_HOURS { get; set; }
            public string TAB_CUSTOMER_REVIEWS { get; set; }
            public string PLAZA_SERVICE_WARRANTY_WEBSITE { get; set; }
            public string TAB_OVERVIEW { get; set; }
            public string PLAZA_SERVICE_WARRANTY_BRAND { get; set; }
            public string PLAZA_SERVICE_WARRANTY_CATEGORY { get; set; }
            public string PLAZA_SERVICE_TITLE_PC { get; set; }
            public string PLAZA_SERVICE_CONTENT3_3_PC { get; set; }
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

        public class Pagemodule
        {
            public string aeOrderFrom { get; set; }
            public bool aerSelfOperation { get; set; }
            public string amphtmlTag { get; set; }
            public bool boutiqueSeller { get; set; }
            public string canonicalTag { get; set; }
            public string complaintUrl { get; set; }
            public string description { get; set; }
            public Features14 features { get; set; }
            public string hreflangTag { get; set; }
            public I18nmap14 i18nMap { get; set; }
            public int id { get; set; }
            public string imagePath { get; set; }
            public string itemDetailUrl { get; set; }
            public string keywords { get; set; }
            public bool kidBaby { get; set; }
            public string mSiteUrl { get; set; }
            public string mediaTag { get; set; }
            public List<Multilanguageurllist> multiLanguageUrlList { get; set; }
            public string name { get; set; }
            public string ogDescription { get; set; }
            public string ogTitle { get; set; }
            public string ogurl { get; set; }
            public string oldItemDetailUrl { get; set; }
            public bool plazaElectronicSeller { get; set; }
            public long productId { get; set; }
            public bool ruSelfOperation { get; set; }
            public bool showPlazaHeader { get; set; }
            public string siteType { get; set; }
            public bool spanishPlaza { get; set; }
            public string title { get; set; }
        }

        public class Features14
        {
        }

        public class I18nmap14
        {
        }

        public class Multilanguageurllist
        {
            public string language { get; set; }
            public string languageUrl { get; set; }
        }

        public class Presalemodule
        {
            public Features15 features { get; set; }
            public I18nmap15 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public bool preSale { get; set; }
        }

        public class Features15
        {
        }

        public class I18nmap15
        {
        }

        public class Pricemodule
        {
            public bool activity { get; set; }
            public int discount { get; set; }
            public bool discountPromotion { get; set; }
            public string discountRatioTips { get; set; }
            public string discountTips { get; set; }
            public Extratags extraTags { get; set; }
            public Features16 features { get; set; }
            public string formatedActivityPrice { get; set; }
            public string formatedPrice { get; set; }
            public bool hiddenBigSalePrice { get; set; }
            public I18nmap16 i18nMap { get; set; }
            public int id { get; set; }
            public bool installment { get; set; }
            public bool lot { get; set; }
            public Maxactivityamount maxActivityAmount { get; set; }
            public Maxamount maxAmount { get; set; }
            public Minactivityamount minActivityAmount { get; set; }
            public Minamount minAmount { get; set; }
            public string multiUnitName { get; set; }
            public string name { get; set; }
            public int numberPerLot { get; set; }
            public string oddUnitName { get; set; }
            public bool preSale { get; set; }
            public Priceruleinfo priceRuleInfo { get; set; }
            public List<Promotionsellingpointtag> promotionSellingPointTags { get; set; }
            public bool regularPriceActivity { get; set; }
            public bool showActivityMessage { get; set; }
            public string vatDesc { get; set; }
        }

        public class Extratags
        {
            public Separatoricon separatorIcon { get; set; }
        }

        public class Separatoricon
        {
            public List<Elementlist> elementList { get; set; }
        }

        public class Elementlist
        {
            public string elementType { get; set; }
            public string iconAddress { get; set; }
            public int iconHeight { get; set; }
            public string iconType { get; set; }
            public int iconWidth { get; set; }
        }

        public class Features16
        {
        }

        public class I18nmap16
        {
            public string LOT { get; set; }
            public string INSTALLMENT { get; set; }
            public string DEPOSIT { get; set; }
            public string PRE_ORDER_PRICE { get; set; }
        }

        public class Maxactivityamount
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class Maxamount
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class Minactivityamount
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class Minamount
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class Priceruleinfo
        {
        }

        public class Promotionsellingpointtag
        {
            public List<Elementlist1> elementList { get; set; }
        }

        public class Elementlist1
        {
            public string elementType { get; set; }
            public string iconAddress { get; set; }
            public int iconHeight { get; set; }
            public string iconType { get; set; }
            public int iconWidth { get; set; }
            public string textColor { get; set; }
            public string textContent { get; set; }
        }

        public class Quantitymodule
        {
            public bool activity { get; set; }
            public bool displayBulkInfo { get; set; }
            public Features17 features { get; set; }
            public I18nmap17 i18nMap { get; set; }
            public int id { get; set; }
            public bool lot { get; set; }
            public string multiUnitName { get; set; }
            public string name { get; set; }
            public string oddUnitName { get; set; }
            public int purchaseLimitNumMax { get; set; }
            public int skuBulkDiscount { get; set; }
            public int skuBulkOrder { get; set; }
            public int totalAvailQuantity { get; set; }
        }

        public class Features17
        {
        }

        public class I18nmap17
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

        public class Recommendmodule
        {
            public int categoryId { get; set; }
            public long companyId { get; set; }
            public Features18 features { get; set; }
            public I18nmap18 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public int platformCount { get; set; }
            public long productId { get; set; }
            public int storeNum { get; set; }
        }

        public class Features18
        {
            public string recommendGpsScenarioOtherSellerProducts { get; set; }
            public string showSubTitle { get; set; }
            public string recommendGpsScenarioTopSelling { get; set; }
            public string recommendGpsScenarioSellerOtherProducts { get; set; }
        }

        public class I18nmap18
        {
            public string MORE_FROM_THIS_SELLER { get; set; }
            public string YOU_MAY_LIKE { get; set; }
            public string TOP_SELLING { get; set; }
            public string FROM_OTHER_SELLER { get; set; }
            public string VIEW_MORE_LINK { get; set; }
            public string PRODUCT_SOLD { get; set; }
        }

        public class Redirectmodule
        {
            public bool bigBossBan { get; set; }
            public string code { get; set; }
            public Features19 features { get; set; }
            public I18nmap19 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public string redirectUrl { get; set; }
        }

        public class Features19
        {
        }

        public class I18nmap19
        {
        }

        public class Shippingmodule
        {
            public string currencyCode { get; set; }
            public Features20 features { get; set; }
            public Freightcalculateinfo freightCalculateInfo { get; set; }
            public Generalfreightinfo generalFreightInfo { get; set; }
            public bool hbaFreeShipping { get; set; }
            public bool hbaFreight { get; set; }
            public I18nmap20 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public long productId { get; set; }
            public string regionCountryName { get; set; }
            public bool suppressFreightInvoke { get; set; }
            public string userCountryCode { get; set; }
            public string userCountryName { get; set; }
        }

        public class Features20
        {
        }

        public class Freightcalculateinfo
        {
            public bool displayMultipleFreight { get; set; }
            public bool hideFreight { get; set; }
        }

        public class Generalfreightinfo
        {
            public bool allowArouseLayer { get; set; }
            public string freightExt { get; set; }
            public bool hideShipFrom { get; set; }
            public List<Originallayoutresultlist> originalLayoutResultList { get; set; }
            public string shippingFeeText { get; set; }
            public bool usingNewFreight { get; set; }
        }

        public class Originallayoutresultlist
        {
            public List<Additionlayout> additionLayout { get; set; }
            public Bizdata bizData { get; set; }
            public Contentlayout[][] contentLayout { get; set; }
            public List<string> deliveryOptionPanelDisplayList { get; set; }
            public List<string> detailFirstScreenDisplayList { get; set; }
            public Dxtemplateinfo dxTemplateInfo { get; set; }
            public int finalPatternId { get; set; }
            public List<string> skuSwitchDisplayList { get; set; }
            public Titlelayout[][] titleLayout { get; set; }
        }

        public class Bizdata
        {
            public string deliveryOptionCode { get; set; }
            public string deliveryProviderName { get; set; }
            public List<int> leadTimeSort { get; set; }
            public int patternId { get; set; }
            public float discount { get; set; }
            public string utParams { get; set; }
            public string tracking { get; set; }
            public string provider { get; set; }
            public string itemShipFromType { get; set; }
            public string itemScene { get; set; }
            public string shipToCode { get; set; }
            public string company { get; set; }
            public string currency { get; set; }
            public string deliveryDate { get; set; }
            public string formattedAmount { get; set; }
            public string shipFrom { get; set; }
            public int deliveryDayMax { get; set; }
            public int guaranteedDeliveryTime { get; set; }
            public string formattedPreAmount { get; set; }
            public string freightCommitDay { get; set; }
            public string etaTraceId { get; set; }
            public long itemId { get; set; }
            public string solutionBusinessType { get; set; }
            public string shippingFee { get; set; }
            public int deliveryDayMin { get; set; }
            public string guaranteedDeliveryTimeProviderName { get; set; }
            public string showXDayDeliveryTips { get; set; }
            public int solutionId { get; set; }
            public string shipFromCode { get; set; }
            public string displayCurrency { get; set; }
            public string deliveryProviderCode { get; set; }
            public string shipTo { get; set; }
            public double displayAmount { get; set; }
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

        public class I18nmap20
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

        public class Skumodule
        {
            public int categoryId { get; set; }
            public Features21 features { get; set; }
            public string forcePromiseWarrantyJson { get; set; }
            public bool hasSizeInfo { get; set; }
            public bool hasSkuProperty { get; set; }
            public I18nmap21 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public List<Productskupropertylist> productSKUPropertyList { get; set; }
            public string selectedSkuAttr { get; set; }
            public List<Skupricelist> skuPriceList { get; set; }
            public string warrantyDetailJson { get; set; }
        }

        public class Features21
        {
        }

        public class I18nmap21
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

        public class Productskupropertylist
        {
            public bool isShowTypeColor { get; set; }
            public int order { get; set; }
            public string showType { get; set; }
            public bool showTypeColor { get; set; }
            public long skuPropertyId { get; set; }
            public string skuPropertyName { get; set; }
            public List<Skupropertyvalue> skuPropertyValues { get; set; }
        }

        public class Skupropertyvalue
        {
            public string propertyValueDisplayName { get; set; }
            public int propertyValueId { get; set; }
            public long propertyValueIdLong { get; set; }
            public string propertyValueName { get; set; }
            public string skuColorValue { get; set; }
            public string skuPropertyImagePath { get; set; }
            public string skuPropertyImageSummPath { get; set; }
            public string skuPropertyTips { get; set; }
            public int skuPropertyValueShowOrder { get; set; }
            public string skuPropertyValueTips { get; set; }
            public string propertyValueDefinitionName { get; set; }
        }

        public class Skupricelist
        {
            public string freightExt { get; set; }
            public bool salable { get; set; }
            public string skuAttr { get; set; }
            public long skuId { get; set; }
            public string skuIdStr { get; set; }
            public string skuPropIds { get; set; }
            public Skuval skuVal { get; set; }
        }

        public class Skuval
        {
            public int availQuantity { get; set; }
            public int bulkOrder { get; set; }
            public string discount { get; set; }
            public string discountTips { get; set; }
            public int inventory { get; set; }
            public bool isActivity { get; set; }
            public List<object> optionalWarrantyPrice { get; set; }
            public Skuactivityamount skuActivityAmount { get; set; }
            public Skuamount skuAmount { get; set; }
            public string skuCalPrice { get; set; }
        }

        public class Skuactivityamount
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class Skuamount
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class Specsmodule
        {
            public Features22 features { get; set; }
            public I18nmap22 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public List<Prop> props { get; set; }
        }

        public class Features22
        {
        }

        public class I18nmap22
        {
        }

        public class Prop
        {
            public string attrName { get; set; }
            public string attrValue { get; set; }
        }

        public class Storemodule
        {
            public long companyId { get; set; }
            public string countryCompleteName { get; set; }
            public string detailPageUrl { get; set; }
            public bool esRetailOrConsignment { get; set; }
            public Features23 features { get; set; }
            public string feedbackMessageServer { get; set; }
            public string feedbackServer { get; set; }
            public bool followed { get; set; }
            public int followingNumber { get; set; }
            public bool hasStore { get; set; }
            public bool hasStoreHeader { get; set; }
            public bool hideCustomerService { get; set; }
            public I18nmap23 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public string openTime { get; set; }
            public int openedYear { get; set; }
            public int positiveNum { get; set; }
            public string positiveRate { get; set; }
            public long productId { get; set; }
            public long sellerAdminSeq { get; set; }
            public string sessionId { get; set; }
            public string siteType { get; set; }
            public string storeName { get; set; }
            public int storeNum { get; set; }
            public string storeURL { get; set; }
            public string topBrandDescURL { get; set; }
            public bool topRatedSeller { get; set; }
        }

        public class Features23
        {
        }

        public class I18nmap23
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

        public class Titlemodule
        {
            public Features24 features { get; set; }
            public Feedbackrating feedbackRating { get; set; }
            public string formatTradeCount { get; set; }
            public I18nmap24 i18nMap { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public bool orig { get; set; }
            public bool origTitle { get; set; }
            public string subject { get; set; }
            public List<object> titleTags { get; set; }
            public int tradeCount { get; set; }
            public string tradeCountUnit { get; set; }
            public bool trans { get; set; }
            public bool transTitle { get; set; }
        }

        public class Features24
        {
        }

        public class Feedbackrating
        {
            public string averageStar { get; set; }
            public string averageStarRage { get; set; }
            public bool display { get; set; }
            public string evarageStar { get; set; }
            public string evarageStarRage { get; set; }
            public int fiveStarNum { get; set; }
            public string fiveStarRate { get; set; }
            public int fourStarNum { get; set; }
            public string fourStarRate { get; set; }
            public int oneStarNum { get; set; }
            public string oneStarRate { get; set; }
            public string positiveRate { get; set; }
            public int threeStarNum { get; set; }
            public string threeStarRate { get; set; }
            public int totalValidNum { get; set; }
            public int trialReviewNum { get; set; }
            public int twoStarNum { get; set; }
            public string twoStarRate { get; set; }
        }

        public class I18nmap24
        {
            public string REVIEWS { get; set; }
            public string VIEW_ALL_REVIEWS { get; set; }
            public string REVIEW { get; set; }
            public string VIEW_OTHER_TITLE { get; set; }
            public string VIEW_EN_TITLE { get; set; }
            public string FREEBIE_REVIEW { get; set; }
            public string FREEBIE_REVIEWS { get; set; }
        }

        public class Webenv
        {
            public string country { get; set; }
            public string currency { get; set; }
            public Env env { get; set; }
            public string host { get; set; }
            public string hostname { get; set; }
            public string ip { get; set; }
            public string lang { get; set; }
            public string language { get; set; }
            public string locale { get; set; }
            public string reqHost { get; set; }
            public string site { get; set; }
            public string traceId { get; set; }
        }

        public class Env
        {
            public Valmap valMap { get; set; }
            public string zone { get; set; }
        }

        public class Valmap
        {
            public string g11nlocale { get; set; }
            public string g11ntimezone { get; set; }
            public string uadevice { get; set; }
            public string userid { get; set; }
            public string g11ncountry { get; set; }
            public string pagename { get; set; }
            public string g11nsite { get; set; }
            public string pageapp { get; set; }
            public string uabrowser { get; set; }
            public string uaplatform { get; set; }
            public string usertype { get; set; }
            public string pageid { get; set; }
            public string usermember { get; set; }
            public string g11ncurrency { get; set; }
        }

        public class Attribute
        {
            public string attrValue { get; set; }
            public string attrName { get; set; }
        }
        public class PriceModel
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public double value { get; set; }
        }
        public class DetailOptionModel
        {

            public List<Sku> SkuList { get; set; }


            public class Sku
            {
                public Skuval skuVal { get; set; }
                public string skuPropIds { get; set; }
                public string skuIdStr { get; set; }
                public string freightExt { get; set; }
                public bool salable { get; set; }
                public string skuAttr { get; set; }
                public string skuId { get; set; }
                public string skuPropertyIds { get; set; }
                public int skuStock { get; set; }
            }

            public class Skuval
            {
                public string skuCalPrice { get; set; }
                public List<object> optionalWarrantyPrice { get; set; }
                public Skuamount skuAmount { get; set; }
                public string discountTips { get; set; }
                public string discount { get; set; }
                public int availQuantity { get; set; }
                public bool isActivity { get; set; }
                public int inventory { get; set; }
                public Skuactivityamount skuActivityAmount { get; set; }
            }

            public class Skuamount
            {
                public string currency { get; set; }
                public string formatedAmount { get; set; }
                public double value { get; set; }
            }

            public class Skuactivityamount
            {
                public string currency { get; set; }
                public string formatedAmount { get; set; }
                public double value { get; set; }
            }

        }
        public class OptionGroupModel
        {
            public List<Property> PropertyList { get; set; }

            public class Property
            {
                public bool showTypeColor { get; set; }
                public List<Skupropertyvalue> skuPropertyValues { get; set; }
                public string showType { get; set; }
                public string skuPropertyName { get; set; }
                public int skuPropertyId { get; set; }
                public int order { get; set; }
                public bool hasSizeInfo { get; set; }
            }

            public class Skupropertyvalue
            {
                public string skuColorValue { get; set; }
                public string skuPropertyTips { get; set; }
                public string propertyValueName { get; set; }
                public int propertyValueId { get; set; }
                public string skuPropertyImagePath { get; set; }
                public string skuPropertyValueTips { get; set; }
                public int skuPropertyValueShowOrder { get; set; }
                public string propertyValueDefinitionName { get; set; }
                public string propertyMaterialImage { get; set; }
                public List<string> propertyImages { get; set; }
                public long propertyValueIdLong { get; set; }
                public string skuPropertyImageSummPath { get; set; }
                public string propertyValueDisplayName { get; set; }
                public List<Propertysizechartinfo> propertySizeChartInfo { get; set; }
                public object skuPropertySizeInfoMaps { get; set; }
            }

            //public class Skupropertysizeinfomaps
            //{
            //    public string 가슴폭 { get; set; }
            //    public string 허리폭 { get; set; }
            //    public string 바디길이 { get; set; }
            //}

            public class Propertysizechartinfo
            {
                public string name { get; set; }
                public string value { get; set; }
            }

        }
    }
}
