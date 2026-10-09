#nullable disable warnings
using System.Collections.Generic;

namespace BrowserScrapingBridge.Server.Sourcing.Mytheresa
{
    internal class PageRequestBody
    {
        public string query { get; set; } = "query XProductListingPageQuery($categories: [String], $colors: [String], $designers: [String], $fta: Boolean, $materials: [String], $page: Int, $patterns: [String], $reductionRange: [String], $saleStatus: SaleStatusEnum, $size: Int, $sizesHarmonized: [String], $slug: String, $sort: String) {\n  xProductListingPage(categories: $categories, colors: $colors, designers: $designers, fta: $fta, materials: $materials, page: $page, patterns: $patterns, reductionRange: $reductionRange, saleStatus: $saleStatus, size: $size, sizesHarmonized: $sizesHarmonized, slug: $slug, sort: $sort) {\n    id\n    alternateUrls {\n      language\n      store\n      url\n    }\n    breadcrumb {\n      id\n      name\n      slug\n    }\n    combinedDepartmentGroupAndCategoryErpID\n    department\n    designerErpId\n    displayName\n    facets {\n      categories {\n        name\n        options {\n          id\n          name\n          slug\n          children {\n            id\n            name\n            slug\n            children {\n              id\n              name\n              slug\n            }\n          }\n        }\n        activeValue\n      }\n      designers {\n        name\n        options {\n          value\n          slug\n        }\n        activeValue\n      }\n      colors {\n        name\n        options {\n          value\n        }\n        activeValue\n      }\n      fta {\n        activeValue\n        name\n        options {\n          value\n        }\n        visibility\n      }\n      materials {\n        activeValue\n        name\n        options {\n          value\n        }\n        visibility\n      }\n      patterns {\n        name\n        options {\n          value\n        }\n        activeValue\n      }\n      reductionRange {\n        activeValue\n        name\n        options {\n          value\n        }\n        unit\n        visibility\n      }\n      saleStatus {\n        activeValue\n        name\n        options {\n          value\n        }\n        visibility\n      }\n      sizesHarmonized {\n        name\n        options {\n          value\n        }\n        activeValue\n      }\n    }\n    isMonetisationExcluded\n    isSalePage\n    pagination {\n      ...paginationData\n    }\n    products {\n      ...productData\n    }\n    sort {\n      currentParam\n      params\n    }\n  }\n}\n\nfragment paginationData on XPagination {\n  currentPage\n  itemsPerPage\n  totalItems\n  totalPages\n}\n\nfragment priceData on XSharedPrice {\n  currencyCode\n  currencySymbol\n  discount\n  discountEur\n  extraDiscount\n  finalDuties\n  hint\n  includesVAT\n  isPriceModifiedByRegionalRules\n  original\n  originalDuties\n  originalDutiesEur\n  originalEur\n  percentage\n  regionalRulesModifications {\n    priceColor\n  }\n  regular\n  vatPercentage\n}\n\nfragment productData on XSharedProduct {\n  color\n  combinedCategoryErpID\n  combinedCategoryName\n  department\n  description\n  designer\n  designerErpId\n  designerInfo {\n    designerId\n    displayName\n    slug\n  }\n  displayImages\n  enabled\n  features\n  fta\n  hasMultipleSizes\n  hasSizeChart\n  hasStock\n  isComingSoon\n  isInWishlist\n  isPurchasable\n  isSizeRelevant\n  labelObjects {\n    id\n    label\n  }\n  labels\n  mainPrice\n  mainWaregroup\n  name\n  price {\n    ...priceData\n  }\n  priceDescription\n  promotionLabels {\n    label\n    type\n  }\n  seasonCode\n  sellerOrigin\n  sets\n  sizeAndFit\n  sizesOnStock\n  sizeTag\n  sizeType\n  sku\n  slug\n  variants {\n    allVariants {\n      availability {\n        hasStock\n        lastStockQuantityHint\n      }\n      isInWishlist\n      size\n      sizeHarmonized\n      sku\n    }\n    availability {\n      hasStock\n      lastStockQuantityHint\n    }\n    isInWishlist\n    price {\n      currencyCode\n      currencySymbol\n      discount\n      discountEur\n      extraDiscount\n      includesVAT\n      isPriceModifiedByRegionalRules\n      original\n      originalEur\n      percentage\n      regionalRulesModifications {\n        priceColor\n      }\n      vatPercentage\n    }\n    size\n    sizeHarmonized\n    sku\n  }\n}\n";
        public class Variables
        {
            public List<string> categories { get; set; } = new List<string>();
            public List<string> colors { get; set; } = new List<string>();
            public List<string> designers { get; set; } = new List<string>();
            public object fta { get; set; }
            public List<string> materials { get; set; } = new List<string>();
            public int page { get; set; }
            public List<string> patterns { get; set; } = new List<string>();
            public List<string> reductionRange { get; set; } = new List<string>();
            public object saleStatus { get; set; } = null;
            public int size { get; set; } = 60;
            public List<string> sizesHarmonized { get; set; } = new List<string>();
            public string slug { get; set; }
            public string sort { get; set; } = null;
        }
        public Variables variables { get; set; } = new Variables();
    }
}
