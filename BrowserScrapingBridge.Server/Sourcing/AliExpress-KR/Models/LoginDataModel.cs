#nullable disable warnings
using System.Collections.Generic;

namespace BrowserScrapingBridge.Server.Sourcing.AliExpress_KR.Models
{
    public class LoginDataModel
    {
        public Tradecomponent tradeComponent { get; set; }
        public Pagesizecomponent pageSizeComponent { get; set; }
        public Redirectcomponent redirectComponent { get; set; }
        public Metadatacomponent metaDataComponent { get; set; }
        public Sellercomponent sellerComponent { get; set; }
        public Plazasellerservicecomponent plazaSellerServiceComponent { get; set; }
        public Productpropcomponent productPropComponent { get; set; }
        public Skucomponent skuComponent { get; set; }
        public Webactionconfcomponent webActionConfComponent { get; set; }
        public Packagecomponent packageComponent { get; set; }
        public Producttagcomponent productTagComponent { get; set; }
        public Blacklistcomponent blacklistComponent { get; set; }
        public Pricecomponent priceComponent { get; set; }
        public Weblongimagecomponent webLongImageComponent { get; set; }
        public Wishlistcomponent wishListComponent { get; set; }
        public Multilanguageurlcomponent multiLanguageUrlComponent { get; set; }
        public Webcouponinfocomponent webCouponInfoComponent { get; set; }
        public I18ncomponent i18nComponent { get; set; }
        public Categorycomponent categoryComponent { get; set; }
        public Productinfocomponent productInfoComponent { get; set; }
        public Sellerguaranteecomponent sellerGuaranteeComponent { get; set; }
        public Buyercomponent buyerComponent { get; set; }
        public Storeheadercomponent storeHeaderComponent { get; set; }
        public Breadcrumbcomponent breadcrumbComponent { get; set; }
        public Simplebannercomponent simpleBannerComponent { get; set; }
        public Abtestcomponent abTestComponent { get; set; }
        public Gagacomponent gagaComponent { get; set; }
        public Siteinfocomponent siteInfoComponent { get; set; }
        public Remindscomponent remindsComponent { get; set; }
        public Shopcategorycomponent shopCategoryComponent { get; set; }
        public Promotioncomponent promotionComponent { get; set; }
        public Sellerpromisecomponent sellerPromiseComponent { get; set; }
        public Extracomponent extraComponent { get; set; }
        public Storefeedbackcomponent storeFeedbackComponent { get; set; }
        public Assurancecomponent assuranceComponent { get; set; }
        public Pricerulecomponent priceRuleComponent { get; set; }
        public Webgeneralfreightcalculatecomponent webGeneralFreightCalculateComponent { get; set; }
        public Inventorycomponent inventoryComponent { get; set; }
        public Webcouponpricecomponent webCouponPriceComponent { get; set; }
        public Installmentcomponent installmentComponent { get; set; }
        public Productdesccomponent productDescComponent { get; set; }
        public Categorytagcomponent categoryTagComponent { get; set; }
        public Supplementinfolayoutcomponent supplementInfoLayoutComponent { get; set; }
        public Imagecomponent imageComponent { get; set; }
        public Recommendcomponent recommendComponent { get; set; }
        public Usercomponent userComponent { get; set; }
        public Currencycomponent currencyComponent { get; set; }
        public Itemstatuscomponent itemStatusComponent { get; set; }
        public Refercomponent referComponent { get; set; }
        public Feedbackcomponent feedbackComponent { get; set; }
        public Vehiclecomponent vehicleComponent { get; set; }
        public Displaytitlecomponent displayTitleComponent { get; set; }

        public class Tradecomponent
        {
            public string formatTradeCount { get; set; }
            public string tradeCountUnit { get; set; }
        }

        public class Pagesizecomponent
        {
            public bool hasNewSizeInfo { get; set; }
            public bool hasSizeInfo { get; set; }
        }

        public class Redirectcomponent
        {
        }

        public class Metadatacomponent
        {
            public string keywords { get; set; }
            public string canonicalTag { get; set; }
            public string hreflangTag { get; set; }
            public string ogTitle { get; set; }
            public string description { get; set; }
            public string mediaTag { get; set; }
            public string ogurl { get; set; }
            public string amphtmlTag { get; set; }
            public string ogDescription { get; set; }
            public string title { get; set; }
        }

        public class Sellercomponent
        {
            public bool boutiqueSeller { get; set; }
            public bool topRatedSeller { get; set; }
            public string formatOpenTime { get; set; }
            public bool spanishPlaza { get; set; }
            public bool ruConsignmentAndMarketPlace { get; set; }
            public bool esRetailOrConsignment { get; set; }
            public long openedYear { get; set; }
            public bool plazaElectronicSeller { get; set; }
            public bool ruSelfOperation { get; set; }
            public string storeURL { get; set; }
            public string storeFeedbackURL { get; set; }
            public string storeName { get; set; }
            public bool hasStore { get; set; }
            public string openTime { get; set; }
            public bool aeRuSelfOperation { get; set; }
            public bool showPlazaHeader { get; set; }
            public bool localSeller { get; set; }
            public long adminSeq { get; set; }
            public string encryptOwnerMemberId { get; set; }
            public string countryCompleteName { get; set; }
            public long companyId { get; set; }
            public string storeLogo { get; set; }
            public long storeNum { get; set; }
            public bool payPalAccount { get; set; }
        }

        public class Plazasellerservicecomponent
        {
            public Warranty warranty { get; set; }
            public bool hasWarrantyInfo { get; set; }
        }

        public class Warranty
        {
        }

        public class Productpropcomponent
        {
            public List<Prop> props { get; set; }
        }

        public class Prop
        {
            public string attrValue { get; set; }
            public string attrName { get; set; }
        }

        public class Skucomponent
        {
            public bool hasSkuProperty { get; set; }
            public List<Productskupropertylist> productSKUPropertyList { get; set; }
            public string skuPropertyJson { get; set; }
        }

        public class Productskupropertylist
        {
            public bool showTypeColor { get; set; }
            public List<Skupropertyvalue> skuPropertyValues { get; set; }
            public string showType { get; set; }
            public string skuPropertyName { get; set; }
            public long skuPropertyId { get; set; }
            public long order { get; set; }
        }

        public class Skupropertyvalue
        {
            public string skuPropertyTips { get; set; }
            public string propertyValueName { get; set; }
            public long propertyValueId { get; set; }
            public string skuPropertyValueTips { get; set; }
            public long skuPropertyValueShowOrder { get; set; }
            public long propertyValueIdLong { get; set; }
            public string propertyValueDisplayName { get; set; }
            public string skuPropertySendGoodsCountryCode { get; set; }
            public string skuColorValue { get; set; }
            public string skuPropertyImagePath { get; set; }
            public string propertyValueDefinitionName { get; set; }
            public string skuPropertyImageSummPath { get; set; }
        }

        public class Webactionconfcomponent
        {
            public Actionconfs actionConfs { get; set; }
            public string orderUrl { get; set; }
            public string shoppingCartUrl { get; set; }
        }

        public class Actionconfs
        {
            public Buynowbtn buyNowBtn { get; set; }
        }

        public class Buynowbtn
        {
            public string bgColorStart { get; set; }
            public string bgColorEnd { get; set; }
            public string textContent { get; set; }
            public string textColor { get; set; }
        }

        public class Packagecomponent
        {
            public string inch_length { get; set; }
            public bool lot { get; set; }
            public string unitType { get; set; }
            public long length { get; set; }
            public string lotNum { get; set; }
            public long width { get; set; }
            public string inch_height { get; set; }
            public float weight { get; set; }
            public string pound_weight { get; set; }
            public string packageType { get; set; }
            public long height { get; set; }
            public string inch_width { get; set; }
        }

        public class Producttagcomponent
        {
            public bool topItem { get; set; }
            public List<object> aePlusTags { get; set; }
            public Otherpctagmap otherPCTagMap { get; set; }
            public List<object> promotionTags { get; set; }
            public List<object> titleTags { get; set; }
            public bool invalidBuyNow { get; set; }
            public bool boutique { get; set; }
            public Switchinfo switchInfo { get; set; }
            public bool rechargeProduct { get; set; }
            public Extratags extraTags { get; set; }
            public bool x3MoneyBack { get; set; }
            public bool madeInItaly { get; set; }
            public bool installment { get; set; }
            public bool f1Scene { get; set; }
            public bool ruHidePruduct { get; set; }
            public bool frMall { get; set; }
            public bool choiceProduct { get; set; }
        }

        public class Otherpctagmap
        {
        }

        public class Switchinfo
        {
            public bool showPropOuter { get; set; }
            public bool deliveryMigrate { get; set; }
            public bool skuUnavailableRevision { get; set; }
            public bool allowUSVisitorCart { get; set; }
            public bool showPropGroups { get; set; }
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
            public long iconHeight { get; set; }
            public long iconWidth { get; set; }
            public string iconType { get; set; }
            public string iconAddress { get; set; }
            public string elementType { get; set; }
        }

        public class Blacklistcomponent
        {
            public bool inBlackList { get; set; }
        }

        public class Pricecomponent
        {
            public long skuBulkDiscount { get; set; }
            public string skuJson { get; set; }
            public string vatDesc { get; set; }
            public bool displayBulkInfo { get; set; }
            public List<Skupricelist> skuPriceList { get; set; }
            public long skuBulkOrder { get; set; }
            public Discountprice discountPrice { get; set; }
            public Origprice origPrice { get; set; }
            public string priceLocalConfig { get; set; }
        }

        public class Discountprice
        {
            public float actMinPrice { get; set; }
            public string actCurrencyFormatPrice { get; set; }
            public Minactivityamount minActivityAmount { get; set; }
            public string actMultiCurrencyMinPrice { get; set; }
            public float minActMultiCurrencyPrice { get; set; }
            public float maxActMultiCurrencyPrice { get; set; }
            public Maxactivityamount maxActivityAmount { get; set; }
            public string actMinDisplayPrice { get; set; }
            public string actMaxDisplayPrice { get; set; }
            public double actMaxPrice { get; set; }
            public string actMultiCurrencyDisplayPrice { get; set; }
            public string actMultiCurrencyMaxPrice { get; set; }
        }

        public class Minactivityamount
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class Maxactivityamount
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class Origprice
        {
            public Minamount minAmount { get; set; }
            public float minMultiCurrencyPrice { get; set; }
            public double minPrice { get; set; }
            public double maxPrice { get; set; }
            public string multiCurrencyFormatPrice { get; set; }
            public Maxamount maxAmount { get; set; }
            public string currencyFormatPrice { get; set; }
        }

        public class Minamount
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

        public class Skupricelist
        {
            public Skuval skuVal { get; set; }
            public string skuPropIds { get; set; }
            public string skuIdStr { get; set; }
            public string freightExt { get; set; }
            public bool salable { get; set; }
            public string skuAttr { get; set; }
            public long skuId { get; set; }
        }

        public class Skuval
        {
            public List<object> optionalWarrantyPrice { get; set; }
            public Skuamount skuAmount { get; set; }
            public string discount { get; set; }
            public long availQuantity { get; set; }
            public int inventory { get; set; }
            public string skuCalPrice { get; set; }
            public string skuAmountLocal { get; set; }
            public string skuActivityAmountLocal { get; set; }
            public string discountTips { get; set; }
            public bool isActivity { get; set; }
            public long bulkOrder { get; set; }
            public Skuactivityamount skuActivityAmount { get; set; }
        }

        public class Skuamount
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class Skuactivityamount
        {
            public string currency { get; set; }
            public string formatedAmount { get; set; }
            public float value { get; set; }
        }

        public class Weblongimagecomponent
        {
            public bool showLongImage { get; set; }
        }

        public class Wishlistcomponent
        {
            public long storeWishedCount { get; set; }
            public long itemWishedCount { get; set; }
            public bool storeWished { get; set; }
            public bool itemWished { get; set; }
        }

        public class Multilanguageurlcomponent
        {
            public List<Storemultilanguageurllist> storeMultilanguageUrlList { get; set; }
            public string storeDetailUrl { get; set; }
            public List<Multilanguageurllist> multilanguageUrlList { get; set; }
            public string itemDetailUrl { get; set; }
            public string mSiteUrl { get; set; }
        }

        public class Storemultilanguageurllist
        {
            public string language { get; set; }
            public string languageUrl { get; set; }
        }

        public class Multilanguageurllist
        {
            public string language { get; set; }
            public string languageUrl { get; set; }
        }

        public class Webcouponinfocomponent
        {
            public bool usingNewCouponApi { get; set; }
            public List<Couponlist> couponList { get; set; }
            public Promotionpaneldto promotionPanelDTO { get; set; }
            public Couponattributes couponAttributes { get; set; }
        }

        public class Promotionpaneldto
        {
            public List<Platformcoupon> platformCoupon { get; set; }
        }

        public class Platformcoupon
        {
            public List<Promotionpaneldetaildtolist> promotionPanelDetailDTOList { get; set; }
            public string displayType { get; set; }
            public string promotionGroupTitle { get; set; }
        }

        public class Promotionpaneldetaildtolist
        {
            public bool acquirable { get; set; }
            public Couponassignparam couponAssignParam { get; set; }
            public string promotionDetail { get; set; }
            public string promotionDesc { get; set; }
            public string actionTips { get; set; }
            public string displayType { get; set; }
            public string toolCode { get; set; }
            public long teasingStartTime { get; set; }
            public Attributes attributes { get; set; }
            public long startTime { get; set; }
            public long endTime { get; set; }
        }

        public class Couponassignparam
        {
            public string couponSource { get; set; }
            public string buffettParam { get; set; }
            public string couponType { get; set; }
            public string couponMeta { get; set; }
        }

        public class Attributes
        {
            public string houyiTrack { get; set; }
            public string collectedTips { get; set; }
            public string privateText { get; set; }
            public string codeScope { get; set; }
            public string couponCampaignStyle { get; set; }
            public string promotionId { get; set; }
        }

        public class Couponattributes
        {
            public string couponPackageText { get; set; }
            public string hasPlatformShippingFeeCoupon { get; set; }
        }

        public class Couponlist
        {
            public string bgColor { get; set; }
            public string color { get; set; }
            public string copy { get; set; }
            public string title { get; set; }
            public string type { get; set; }
            public long order { get; set; }
        }

        public class I18ncomponent
        {
            public I18nmap i18nMap { get; set; }
        }

        public class I18nmap
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
            public string storeName { get; set; }
            public string storeInfo { get; set; }
            public string storeRating { get; set; }
            public string storeId { get; set; }
            public string businessAddress { get; set; }
            public string communication { get; set; }
            public string openSince { get; set; }
            public string itemAsDescribed { get; set; }
            public string businessInfo { get; set; }
        }

        public class Itemdetailresp
        {
            public string ASK_BUYERS { get; set; }
            public string VIEW_5_MORE_ANSWERS { get; set; }
            public string PAGE_NOT_FOUND_RCMD_TITLE { get; set; }
            public string VIEW_MORE { get; set; }
            public string PAGE_NOT_FOUND_NOTICE { get; set; }
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
            public string PRE_ORDER_PRICE { get; set; }
            public string INSTALLMENT { get; set; }
            public string DEPOSIT { get; set; }
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
            public string PLAZA_BUYER_PROTECTION_TIP { get; set; }
            public string PLAZA_BUYER_PROTECTION_TITLE { get; set; }
            public string PLAZA_BUYER_PROTECTION_CONTENT { get; set; }
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
            public string skuEditSelection { get; set; }
            public string description { get; set; }
            public string specification { get; set; }
            public string skuSelections { get; set; }
            public string feedback { get; set; }
            public string youMayAlsoLike { get; set; }
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

        public class Categorycomponent
        {
            public long topCategoryId { get; set; }
            public bool kidBaby { get; set; }
            public long secondLevelCategoryId { get; set; }
        }

        public class Productinfocomponent
        {
            public string idStr { get; set; }
            public string oddUnitName { get; set; }
            public string taobaoSellerName { get; set; }
            public string src { get; set; }
            public string subject { get; set; }
            public long numberPerLot { get; set; }
            public long adminSeq { get; set; }
            public List<object> propGroups { get; set; }
            public bool fromTaobao { get; set; }
            public bool lot { get; set; }
            public string multiCurrencyDisplayPrice { get; set; }
            public bool openOfferPriceRule { get; set; }
            public float minPrice { get; set; }
            public long id { get; set; }
            public float maxPrice { get; set; }
            public string multiUnitName { get; set; }
            public long categoryId { get; set; }
        }

        public class Sellerguaranteecomponent
        {
            public bool hasOverseaWarehouse { get; set; }
            public string warranyDetailJson { get; set; }
            public string forcePromiseWarrantyJson { get; set; }
        }

        public class Buyercomponent
        {
            public long adultCertificationStatus { get; set; }
        }

        public class Storeheadercomponent
        {
            public Storeheaderresult storeHeaderResult { get; set; }
        }

        public class Storeheaderresult
        {
            public List<Tablist> tabList { get; set; }
            public string allProductsText { get; set; }
            public string storeName { get; set; }
            public string shopTagUrl { get; set; }
            public string shopSignSelected { get; set; }
        }

        public class Tablist
        {
            public string tabName { get; set; }
            public long index { get; set; }
            public long id { get; set; }
            public string url { get; set; }
            public string groupId { get; set; }
            public List<Subtab> subTab { get; set; }
        }

        public class Subtab
        {
            public string tabName { get; set; }
            public string groupId { get; set; }
            public long index { get; set; }
            public string tabType { get; set; }
            public long id { get; set; }
            public bool selected { get; set; }
            public string url { get; set; }
        }

        public class Breadcrumbcomponent
        {
            public string categoryUrl { get; set; }
            public List<Pathlist> pathList { get; set; }
            public string categoryName { get; set; }
        }

        public class Pathlist
        {
            public long cateId { get; set; }
            public string name { get; set; }
            public string remark { get; set; }
            public string url { get; set; }
        }

        public class Simplebannercomponent
        {
            public bool usingSimpleBanner { get; set; }
        }

        public class Abtestcomponent
        {
            public long detailNewVersion { get; set; }
            public bool openDescHover { get; set; }
            public bool hideCustomerService { get; set; }
            public bool rmStoreLevelAB { get; set; }
        }

        public class Gagacomponent
        {
            public string gagaDataSite { get; set; }
        }

        public class Siteinfocomponent
        {
            public string wholesaleSubServer { get; set; }
            public bool crawler { get; set; }
            public string siteType { get; set; }
        }

        public class Remindscomponent
        {
            public List<object> reminds { get; set; }
        }

        public class Shopcategorycomponent
        {
            public Productgroupsresult productGroupsResult { get; set; }
        }

        public class Productgroupsresult
        {
            public List<Group> groups { get; set; }
        }

        public class Group
        {
            public string isExpand { get; set; }
            public string name { get; set; }
            public string url { get; set; }
        }

        public class Promotioncomponent
        {
            public bool discountPromotion { get; set; }
            public bool activity { get; set; }
            public long fixedPromotionLeftHours { get; set; }
            public long discount { get; set; }
            public bool fixedFreeShipping { get; set; }
            public bool regularPriceActivity { get; set; }
            public long fixedPromotionLeftSecs { get; set; }
            public long promotionLeftMinutes { get; set; }
            public long promotionLeftSecs { get; set; }
            public string discountTips { get; set; }
            public long fixedPromotionLeftMinutes { get; set; }
            public bool coinsEnough { get; set; }
            public bool fireDeals { get; set; }
            public bool superDeals { get; set; }
            public long fixedPromotionLeftDays { get; set; }
            public long promotionLeftHours { get; set; }
            public bool hbaFreeShipping { get; set; }
            public bool showStockPrompt { get; set; }
            public bool enableMultiDiscount { get; set; }
            public long purchaseLimitNumMax { get; set; }
            public string discountRatioTips { get; set; }
            public bool displayIcon { get; set; }
            public bool preSale { get; set; }
            public long promotionLeftDays { get; set; }
            public bool availableGroupShareActivity { get; set; }
            public bool coinPreSale { get; set; }
            public bool fixedDiscountPromotion { get; set; }
            public bool comingSoon { get; set; }
            public bool allProduct { get; set; }
            public bool memberPriceActivity { get; set; }
        }

        public class Sellerpromisecomponent
        {
            public List<object> returnPolicyGuarantees { get; set; }
            public bool hasOverseaWarehouse { get; set; }
            public bool hasNoReasonFreeReturn { get; set; }
            public Warranydetailmap warranyDetailMap { get; set; }
            public Servicedetails serviceDetails { get; set; }
            public List<object> sellerGuaranteePromiseList { get; set; }
            public Returnpromise returnPromise { get; set; }
        }

        public class Warranydetailmap
        {
        }

        public class Servicedetails
        {
            public List<Detail> details { get; set; }
            public string title { get; set; }
        }

        public class Detail
        {
            public string viewMoreText { get; set; }
            public string actionTarget { get; set; }
            public string title { get; set; }
            public string type { get; set; }
            public string content { get; set; }
        }

        public class Returnpromise
        {
            public string brief { get; set; }
            public string detailDescription { get; set; }
            public string oldPromiseId { get; set; }
            public string description { get; set; }
            public long type { get; set; }
            public string descriptionForSeller { get; set; }
            public bool isValuable { get; set; }
            public string descriptionPretty { get; set; }
            public string name { get; set; }
            public long id { get; set; }
        }

        public class Extracomponent
        {
            public string expressionExt { get; set; }
            public string aeOrderFrom { get; set; }
            public bool showCoinAnim { get; set; }
            public string abTrace { get; set; }
            public Uitestinfo uiTestInfo { get; set; }
            public string freightExt { get; set; }
            public string seoTitle { get; set; }
            public string detailPageUrl { get; set; }
            public Envinfo envInfo { get; set; }
            public string complaintUrl { get; set; }
            public string productDetailImageUrl { get; set; }
        }

        public class Uitestinfo
        {
            public Uitest UITest { get; set; }
        }

        public class Uitest
        {
            public string adaptExtension { get; set; }
            public string showSkuItem { get; set; }
        }

        public class Envinfo
        {
            public bool domestic { get; set; }
            public string traceId { get; set; }
            public bool crawler { get; set; }
        }

        public class Storefeedbackcomponent
        {
            public long sellerScore { get; set; }
            public long sellerTotalNum { get; set; }
            public string sellerLevel { get; set; }
            public string sellerPositiveRate { get; set; }
            public long sellerPositiveNum { get; set; }
        }

        public class Assurancecomponent
        {
        }

        public class Pricerulecomponent
        {
        }

        public class Webgeneralfreightcalculatecomponent
        {
            public bool hideShipFrom { get; set; }
            public bool allowArouseLayer { get; set; }
            public string shippingFeeText { get; set; }
            public List<Originallayoutresultlist> originalLayoutResultList { get; set; }
            public Deliveryextrainfomap deliveryExtraInfoMap { get; set; }
            public string freightExt { get; set; }
            public string styleCode { get; set; }
            public bool usingNewFreight { get; set; }
        }

        public class Deliveryextrainfomap
        {
            public CAINIAO_STANDARD CAINIAO_STANDARD { get; set; }
        }

        public class CAINIAO_STANDARD
        {
            public bool allowArouseLayer { get; set; }
            public string shippingFeeText { get; set; }
            public bool xdays { get; set; }
            public Servicedetails1 serviceDetails { get; set; }
        }

        public class Servicedetails1
        {
            public List<Detail1> details { get; set; }
            public string title { get; set; }
        }

        public class Detail1
        {
            public string viewMoreText { get; set; }
            public string actionTarget { get; set; }
            public string title { get; set; }
            public string type { get; set; }
            public string content { get; set; }
        }

        public class Originallayoutresultlist
        {
            public Contentlayout[][] contentLayout { get; set; }
            public List<string> deliveryOptionPanelDisplayList { get; set; }
            public List<Additionlayout> additionLayout { get; set; }
            public Titlelayout[][] titleLayout { get; set; }
            public long finalPatternId { get; set; }
            public Bizdata bizData { get; set; }
            public Dxtemplateinfo dxTemplateInfo { get; set; }
            public List<string> detailFirstScreenDisplayList { get; set; }
            public List<string> skuSwitchDisplayList { get; set; }
        }

        public class Bizdata
        {
            public string deliveryOptionCode { get; set; }
            public string deliveryProviderName { get; set; }
            public List<long> leadTimeSort { get; set; }
            public long patternId { get; set; }
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
            public string shipFrom { get; set; }
            public long deliveryDayMax { get; set; }
            public long guaranteedDeliveryTime { get; set; }
            public string freightCommitDay { get; set; }
            public string etaTraceId { get; set; }
            public long itemId { get; set; }
            public string solutionBusinessType { get; set; }
            public string shippingFee { get; set; }
            public long deliveryDayMin { get; set; }
            public string guaranteedDeliveryTimeProviderName { get; set; }
            public string showXDayDeliveryTips { get; set; }
            public long solutionId { get; set; }
            public string shipFromCode { get; set; }
            public string deliveryProviderCode { get; set; }
            public string shipTo { get; set; }
            public double displayAmount { get; set; }
        }

        public class Dxtemplateinfo
        {
        }

        public class Contentlayout
        {
            public string displayCondition { get; set; }
            public string componentId { get; set; }
            public long distance { get; set; }
            public string blockType { get; set; }
            public string type { get; set; }
            public string blockGroup { get; set; }
            public string content { get; set; }
            public bool useInDeliveryOptionPanel { get; set; }
            public bool useInDetailFirstScreen { get; set; }
            public string medusaKey { get; set; }
            public bool useInSkuSwitch { get; set; }
            public long order { get; set; }
        }

        public class Additionlayout
        {
            public string displayCondition { get; set; }
            public string componentId { get; set; }
            public long distance { get; set; }
            public string blockType { get; set; }
            public string type { get; set; }
            public string blockGroup { get; set; }
            public string content { get; set; }
            public bool useInDeliveryOptionPanel { get; set; }
            public string medusaKey { get; set; }
            public long order { get; set; }
        }

        public class Titlelayout
        {
            public string displayCondition { get; set; }
            public string componentId { get; set; }
            public long distance { get; set; }
            public string blockType { get; set; }
            public string type { get; set; }
            public string blockGroup { get; set; }
            public string content { get; set; }
            public bool useInDeliveryOptionPanel { get; set; }
            public bool useInDetailFirstScreen { get; set; }
            public string medusaKey { get; set; }
            public bool useInSkuSwitch { get; set; }
            public long order { get; set; }
        }

        public class Inventorycomponent
        {
            public long totalQuantity { get; set; }
            public long totalAvailQuantity { get; set; }
        }

        public class Webcouponpricecomponent
        {
        }

        public class Installmentcomponent
        {
        }

        public class Productdesccomponent
        {
            public string descriptionUrl { get; set; }
        }

        public class Categorytagcomponent
        {
            public bool falseHair { get; set; }
            public bool cardCoupon { get; set; }
            public bool giftCard { get; set; }
        }

        public class Supplementinfolayoutcomponent
        {
            public Modulelayoutinfo moduleLayoutInfo { get; set; }
        }

        public class Modulelayoutinfo
        {
            public Questionandanswer questionAndAnswer { get; set; }
            public Review review { get; set; }
            public Description description { get; set; }
            public Specification specification { get; set; }
        }

        public class Questionandanswer
        {
            public string spreadStatus { get; set; }
            public long order { get; set; }
        }

        public class Review
        {
            public string spreadStatus { get; set; }
            public long order { get; set; }
        }

        public class Description
        {
            public string spreadStatus { get; set; }
            public long order { get; set; }
        }

        public class Specification
        {
            public string spreadStatus { get; set; }
            public long order { get; set; }
        }

        public class Imagecomponent
        {
            public List<string> image250PathList { get; set; }
            public List<string> imagePathList { get; set; }
            public List<string> image640PathList { get; set; }
            public bool imageExist { get; set; }
            public string imageServer { get; set; }
            public List<string> summImagePathList { get; set; }
        }

        public class Recommendcomponent
        {
            public long similarPageSize { get; set; }
            public string afChannel { get; set; }
            public string similarScenario { get; set; }
            public bool hideStoreRecommend { get; set; }
            public bool hideTopSelling { get; set; }
        }

        public class Usercomponent
        {
            public long currentTime { get; set; }
            public string regionCountryName { get; set; }
            public string userCountryName { get; set; }
            public string userCountryCode { get; set; }
        }

        public class Currencycomponent
        {
            public string baseCurrencyCode { get; set; }
            public bool enableTransaction { get; set; }
            public string currencySymbol { get; set; }
            public bool symbolFront { get; set; }
            public float currencyRate { get; set; }
            public bool baseSymbolFront { get; set; }
            public string currencyCode { get; set; }
            public string baseCurrencySymbol { get; set; }
            public bool multiCurrency { get; set; }
        }

        public class Itemstatuscomponent
        {
            public long status { get; set; }
        }

        public class Refercomponent
        {
        }

        public class Feedbackcomponent
        {
            public string positiveRate { get; set; }
            public long fiveStarNum { get; set; }
            public long threeStarNum { get; set; }
            public long twoStarRate { get; set; }
            public bool display { get; set; }
            public long oneStarRate { get; set; }
            public long fourStarNum { get; set; }
            public string evarageStar { get; set; }
            public long fiveStarRate { get; set; }
            public long threeStarRate { get; set; }
            public long totalValidNum { get; set; }
            public string evarageStarRage { get; set; }
            public long twoStarNum { get; set; }
            public long trialReviewNum { get; set; }
            public long oneStarNum { get; set; }
            public long fourStarRate { get; set; }
        }

        public class Vehiclecomponent
        {
        }

        public class Displaytitlecomponent
        {
            public bool origTitle { get; set; }
            public bool orig { get; set; }
            public bool transTitle { get; set; }
            public bool trans { get; set; }
        }

    }
}
