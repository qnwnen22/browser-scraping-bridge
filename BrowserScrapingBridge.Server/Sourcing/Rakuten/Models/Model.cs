#nullable disable warnings
using System;
using System.Collections.Generic;

namespace BrowserScrapingBridge.Server.Sourcing.Rakuten.Models
{
    public class Model
    {
        public Api api { get; set; }
        public Shop shop { get; set; }
        public User user { get; set; }
        public Apiconfig apiConfig { get; set; }
        public Rat rat { get; set; }
        public Features1 features { get; set; }

        public class Api
        {
            public Data data { get; set; }
        }

        public class Data
        {
            public Iteminfosku itemInfoSku { get; set; }
        }

        public class Iteminfosku
        {
            public string shopId { get; set; }
            public string manageNumber { get; set; }
            public List<object> attributes { get; set; }
            public Itemreviewinfo itemReviewInfo { get; set; }
            public int itemId { get; set; }
            public bool is39Shop { get; set; }
            public string newItemNumber { get; set; }
            public string title { get; set; }
            public Featuresectioninfo featureSectionInfo { get; set; }
            public Pcfields pcFields { get; set; }
            public Purchaseinfo purchaseInfo { get; set; }
            public string itemNameEnc { get; set; }
            public string tagline { get; set; }
            public string catchCopyTrans { get; set; }
            public string newProductDescription { get; set; }
            public string salesDescription { get; set; }
            public int superSaleDiscount { get; set; }
            public Media media { get; set; }
            public string oldImage { get; set; }
            public string picImageUrl { get; set; }
            public Whitebgimage whiteBgImage { get; set; }
            public List<int> tags { get; set; }
            public bool hideItem { get; set; }
            public bool draftItem { get; set; }
            public bool unlimitedInventoryFlag { get; set; }
            public List<object> customizationOptions { get; set; }
            public Features features { get; set; }
            public int isRestricted { get; set; }
            public Accesscontrol accessControl { get; set; }
            public string rCategoryId { get; set; }
            public float taxIncludedReferencePrice { get; set; }
            public string referencePricePrefix { get; set; }
            public Payment payment { get; set; }
            public Pointcampaign pointCampaign { get; set; }
            public List<Pointcampaigninfo> pointCampaignInfo { get; set; }
            public bool superDeal { get; set; }
            public int itemDisplaySequence { get; set; }
            public Layout layout { get; set; }
            public List<Variantselector> variantSelectors { get; set; }
            public string newTagId { get; set; }
            public string inventoryType { get; set; }
            public string inventoryTypeId { get; set; }
            public int unit { get; set; }
            public bool showDeliveryExcuse { get; set; }
            public List<object> newShopPoints { get; set; }
            public Asurakuinfo asurakuInfo { get; set; }
            public Genreinfo genreInfo { get; set; }
            public string sellType { get; set; }
            public Identicalvariants identicalVariants { get; set; }
            public List<Sku2> sku { get; set; }
            public Additionalbanner additionalBanner { get; set; }
        }

        public class Itemreviewinfo
        {
            public Summary summary { get; set; }
        }

        public class Summary
        {
            public int itemReviewCount { get; set; }
            public float itemReviewRating { get; set; }
        }

        public class Featuresectioninfo
        {
            public bool addtocart { get; set; }
        }

        public class Pcfields
        {
            public string rCategoryId { get; set; }
            public List<Image> images { get; set; }
            public string itemNumber { get; set; }
            public string productDescription { get; set; }
        }

        public class Image
        {
            public string location { get; set; }
            public string alt { get; set; }
            public string type { get; set; }
        }

        public class Purchaseinfo
        {
            public int totalInventory { get; set; }
            public Purchasebyselltype purchaseBySellType { get; set; }
            public List<Variantmappedinventory> variantMappedInventories { get; set; }
            public List<Sku> sku { get; set; }
            public List<Axis> axis { get; set; }
        }

        public class Purchasebyselltype
        {
            public string purchaseCondition { get; set; }
            public Basketsettings basketSettings { get; set; }
            public Normalpurchase normalPurchase { get; set; }
        }

        public class Basketsettings
        {
            public string basketDomain { get; set; }
            public string directDomain { get; set; }
            public string cartSet { get; set; }
            public string cartAll { get; set; }
            public string directCartAdd { get; set; }
            public string directCartAddBulk { get; set; }
            public int timeout { get; set; }
            public int retry { get; set; }
        }

        public class Normalpurchase
        {
            public Price price { get; set; }
            public float preTaxPrice { get; set; }
        }

        public class Price
        {
            public float minPrice { get; set; }
        }

        public class Variantmappedinventory
        {
            public string sku { get; set; }
            public string inventoryId { get; set; }
            public int quantity { get; set; }
        }

        public class Sku
        {
            public string variantId { get; set; }
            public Newpurchasesku newPurchaseSku { get; set; }
            public Doubleprice doublePrice { get; set; }
        }

        public class Newpurchasesku
        {
            public string deliveryMessage { get; set; }
            public int quantity { get; set; }
            public string stockCondition { get; set; }
        }

        public class Doubleprice
        {
            public string displayType { get; set; }
            public string displayText { get; set; }
            public int type { get; set; }
            public string value { get; set; }
            public float taxIncludedVerifiedReferencePrice { get; set; }
            public bool referencePriceVerified { get; set; }
        }

        public class Axis
        {
            public string key { get; set; }
            public string label { get; set; }
            public List<Value> values { get; set; }
        }

        public class Value
        {
            public string value { get; set; }
            public string label { get; set; }
            public bool isSoldOut { get; set; }
        }

        public class Media
        {
            public List<Image1> images { get; set; }
            public List<object> skuImages { get; set; }
        }

        public class Image1
        {
            public string alt { get; set; }
            public string location { get; set; }
        }

        public class Whitebgimage
        {
            public string location { get; set; }
            public string type { get; set; }
        }

        public class Features
        {
            public string searchVisibility { get; set; }
            public bool displayNormalCartButton { get; set; }
            public bool displaySubscriptionCartButton { get; set; }
            public string inventoryDisplay { get; set; }
            public bool shopContact { get; set; }
            public string review { get; set; }
            public bool displayManufacturerContents { get; set; }
        }

        public class Accesscontrol
        {
            public bool adultItem { get; set; }
        }

        public class Payment
        {
            public bool taxIncluded { get; set; }
            public bool cashOnDeliveryFeeIncluded { get; set; }
        }

        public class Pointcampaign
        {
            public Applicableperiod applicablePeriod { get; set; }
            public Benefits benefits { get; set; }
            public bool isPointOptimization { get; set; }
        }

        public class Applicableperiod
        {
            public DateTime start { get; set; }
            public DateTime end { get; set; }
        }

        public class Benefits
        {
            public int pointRate { get; set; }
        }

        public class Layout
        {
            public int itemLayoutId { get; set; }
            public int navigationId { get; set; }
            public int layoutSequenceId { get; set; }
            public int smallDescriptionId { get; set; }
            public int largeDescriptionId { get; set; }
            public int showcaseId { get; set; }
        }

        public class Asurakuinfo
        {
            public Deliveryarea deliveryArea { get; set; }
            public List<Sku1> sku { get; set; }
        }

        public class Deliveryarea
        {
            public List<Delvareamaster> delvAreaMaster { get; set; }
            public List<Delvareadetail> delvAreaDetail { get; set; }
        }

        public class Delvareamaster
        {
            public int shopId { get; set; }
            public int delvAreaId { get; set; }
            public int delvAreaKbn { get; set; }
            public string delvAreaName { get; set; }
            public string delvAreaCaption { get; set; }
            public string delvAreaLimit { get; set; }
            public DateTime registDatetime { get; set; }
            public string _operator { get; set; }
            public DateTime timeStamp { get; set; }
        }

        public class Delvareadetail
        {
            public int shopId { get; set; }
            public int delvAreaId { get; set; }
            public int delvAreaKbn { get; set; }
            public string delvArea { get; set; }
            public DateTime registDatetime { get; set; }
            public string _operator { get; set; }
            public DateTime timeStamp { get; set; }
        }

        public class Sku1
        {
            public string variantId { get; set; }
            public Deliveryarea1 deliveryArea { get; set; }
            public Deliverysetinfodetails deliverySetInfoDetails { get; set; }
        }

        public class Deliveryarea1
        {
            public List<Delvareamaster1> delvAreaMaster { get; set; }
            public List<Delvareadetail1> delvAreaDetail { get; set; }
        }

        public class Delvareamaster1
        {
            public int shopId { get; set; }
            public int delvAreaId { get; set; }
            public int delvAreaKbn { get; set; }
            public string delvAreaName { get; set; }
            public string delvAreaCaption { get; set; }
            public string delvAreaLimit { get; set; }
            public DateTime registDatetime { get; set; }
            public string _operator { get; set; }
            public DateTime timeStamp { get; set; }
        }

        public class Delvareadetail1
        {
            public int shopId { get; set; }
            public int delvAreaId { get; set; }
            public int delvAreaKbn { get; set; }
            public string delvArea { get; set; }
            public DateTime registDatetime { get; set; }
            public string _operator { get; set; }
            public DateTime timeStamp { get; set; }
        }

        public class Deliverysetinfodetails
        {
            public List<Deliverysetinfo> deliverySetInfo { get; set; }
            public List<Deliveryinfo> deliveryInfo { get; set; }
        }

        public class Deliverysetinfo
        {
            public int shopId { get; set; }
            public int deliverySetId { get; set; }
            public string deliverySetName { get; set; }
            public int deliverySetDefaultFlag { get; set; }
            public Deliverysetlist deliverySetList { get; set; }
        }

        public class Deliverysetlist
        {
            public List<Deliveryset> deliverySet { get; set; }
        }

        public class Deliveryset
        {
            public int deliveryId { get; set; }
        }

        public class Deliveryinfo
        {
            public int deliveryId { get; set; }
            public string carrier { get; set; }
            public string means { get; set; }
            public string caption { get; set; }
            public int methodId { get; set; }
            public string methodName { get; set; }
            public int carrierId { get; set; }
            public string carrierName { get; set; }
            public string name { get; set; }
            public int useFlag { get; set; }
        }

        public class Genreinfo
        {
            public int prohibitedReviewStatus { get; set; }
            public int ancestorGenreId { get; set; }
        }

        public class Identicalvariants
        {
            public Hidden hidden { get; set; }
            public Standardprice standardPrice { get; set; }
            public Shipping shipping { get; set; }
            public Asurakudeliveryid asurakuDeliveryId { get; set; }
            public Backorderflag backOrderFlag { get; set; }
        }

        public class Hidden
        {
            public bool identical { get; set; }
        }

        public class Standardprice
        {
            public bool identical { get; set; }
        }

        public class Shipping
        {
            public bool identical { get; set; }
        }

        public class Asurakudeliveryid
        {
            public bool identical { get; set; }
        }

        public class Backorderflag
        {
            public bool identical { get; set; }
        }

        public class Additionalbanner
        {
            public Newcardpromotionbanner newCardPromotionBanner { get; set; }
        }

        public class Newcardpromotionbanner
        {
            public string html { get; set; }
            public bool isLink { get; set; }
        }

        public class Pointcampaigninfo
        {
            public string itemSellType { get; set; }
            public Pointcampaign1 pointCampaign { get; set; }
            public bool isPointOptimization { get; set; }
        }

        public class Pointcampaign1
        {
            public Itempoints itemPoints { get; set; }
        }

        public class Itempoints
        {
            public int rate { get; set; }
            public DateTime startDate { get; set; }
            public DateTime endDate { get; set; }
            public Point point { get; set; }
        }

        public class Point
        {
        }

        public class Variantselector
        {
            public string key { get; set; }
            public string label { get; set; }
            public List<Value1> values { get; set; }
        }

        public class Value1
        {
            public string label { get; set; }
            public string value { get; set; }
        }

        public class Sku2
        {
            public List<string> selectorValues { get; set; }
            public string variantId { get; set; }
            public List<object> images { get; set; }
            public bool restockOnCancel { get; set; }
            public int normalDeliveryDateId { get; set; }
            public string merchantDefinedSkuId { get; set; }
            public Referenceprice referencePrice { get; set; }
            public List<object> specs { get; set; }
            public Articlenumber articleNumber { get; set; }
            public List<object> articleNumberForSet { get; set; }
            public bool hidden { get; set; }
            public int unit { get; set; }
            public Shipping1 shipping { get; set; }
            public bool noshi { get; set; }
            public bool isTelecom { get; set; }
            public bool showDeliveryExcuse { get; set; }
            public double taxIncludedPrice { get; set; }
            public float taxIncludedReferencePrice { get; set; }
            public string referencePricePrefix { get; set; }
            public List<object> attributes { get; set; }
        }

        public class Referenceprice
        {
            public string displayType { get; set; }
            public string value { get; set; }
        }

        public class Articlenumber
        {
            public string value { get; set; }
        }

        public class Shipping1
        {
            public bool postageIncluded { get; set; }
            public string shippingMethodGroup { get; set; }
            public int asurakuDeliveryId { get; set; }
            public int singleItemShipping { get; set; }
        }

        public class Shop
        {
            public string shopUrl { get; set; }
            public float taxRate { get; set; }
            public int taxPriceRoundType { get; set; }
            public string imageSize { get; set; }
            public string imageScale { get; set; }
        }

        public class User
        {
            public string prefectureId { get; set; }
            public string postalCode { get; set; }
        }

        public class Apiconfig
        {
            public Shipping2 shipping { get; set; }
            public int directStepTimeout { get; set; }
        }

        public class Shipping2
        {
            public string apiKey { get; set; }
            public string url { get; set; }
            public int retry { get; set; }
            public int timeout { get; set; }
        }

        public class Rat
        {
            public bool ratFlag { get; set; }
            public Genericparameter genericParameter { get; set; }
            public string customParameter { get; set; }
        }

        public class Genericparameter
        {
            public string ratAccountId { get; set; }
            public string ratServiceId { get; set; }
            public string ratSiteSection { get; set; }
            public string ratAdobeSiteSection { get; set; }
            public string ratPageType { get; set; }
            public string ratPageName { get; set; }
            public string ratItemId { get; set; }
            public string ratPrice { get; set; }
            public string ratItemGenre { get; set; }
            public string ratItemGenrePath { get; set; }
            public string ratItemTag { get; set; }
            public string ratShopUrl { get; set; }
            public string ratRanCode { get; set; }
            public string ratPageLayout { get; set; }
        }

        public class Features1
        {
            public bool isEnableAddToCart { get; set; }
            public bool isEnableMakerCreative { get; set; }
            public bool isDispPCFloatingCartReviewRate { get; set; }
        }

    }
}
