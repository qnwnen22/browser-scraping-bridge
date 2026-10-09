#nullable disable warnings
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Common.Exceptions;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.ShoppingNaver
{
    public abstract class ShoppingNaver : ICollect
    {
        public abstract string Site { get; }
        public abstract string Name { get; }
        public Language Language => Language.KR;
        public Currency Currency => Currency.KRW;
        public IChatHubService ChatHubService { get; set; }
        public WebProxy Proxy { get; set; }
        public bool SupportsCaching { get; set; }
        protected long _nc_
        {
            get
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                long _nc_ = DateTimeOffset.Parse(today).ToUnixTimeMilliseconds();
                return _nc_;
            }
        }
        /// <summary>
        /// 메인 매뉴 ID값
        /// 네이버 쇼핑 매뉴 최신화 필요 시 SetLogisticsMenuIdsAsync() 사용
        /// 수집할 때 마다 ShoppingNaver 인스턴스가 새로 생성되기 때문에 고정 입력 값 사용(추후 변경)
        /// </summary>
        public List<string> MainMeunItems { get; set; } = new List<string>
        {
            "10016394",
            "10011321",
            "10031175",
            "10031174",
            "10000011",
            "10000013",
            "10000014",
            "10000015",
            "10000016",
            "10000017",
            "10000018",
            "10000020",
            "10000021",
            "10000022",
            "10000023",
            "10000027",
            "10000025",
            "10000026",
            "10000028",
            "10000030",
            "10000031",
            "10000032",
            "10000033",
            "10000034",
            "10000035",
            "10000036",
            "10000957",
            "10000038",
            "10000037",
            "10000039",
            "10000876",
            "10000833",
            "10000877",
            "10030794",
            "10030797"
        };

        protected int GetDepth(string text)
        {
            int depth = 0;
            List<char> spl = text.ToList();
            for (int c = 0; c < spl.Count; c++)
            {
                if (spl[c] == ' ')
                {
                    depth++;
                    continue;
                }
                else
                {
                    break;
                }
            }
            return depth;
        }
        protected string GetGraphQLQuery(string script)
        {
            try
            {
                string scriptString = System.Text.RegularExpressions.Regex.Unescape(script);
                scriptString = scriptString.Trim();

                string[] scripts = scriptString.Split("\n");

                var sb = new StringBuilder();

                int onDepth = 0;
                bool hasTypename = true;
                int index = 0;
                for (int i = 0; i < scripts.Length; i++)
                {
                    string value = scripts[i];
                    if (i != 0)
                    {
                        try
                        {
                            var tempValue = value.Substring(2);
                            value = tempValue;
                        }
                        catch (Exception exception)
                        {

                        }
                    }
                    else
                    {
                        if (value.Contains("fragment"))
                        {
                            hasTypename = scripts[i + 1].Contains("__typename");
                        }
                    }

                    if (value.Contains("... on"))
                    {
                        onDepth = GetDepth(value);
                        index = i;
                    }

                    bool isContains = value.Contains("}");
                    if (isContains)
                    {
                        int currentValueDepth = GetDepth(value);
                        string nextValue = null;
                        int next = index + 1;
                        if (next < scripts.Length)
                        {
                            nextValue = scripts[next];
                        }

                        if (onDepth == currentValueDepth && string.IsNullOrWhiteSpace(nextValue) == false && nextValue.Contains("__typename"))
                        {
                            onDepth = 0;
                            index = 0;
                        }
                        else if (i != scripts.Length - 1)
                        {
                            string before = scripts[i - 1].Substring(2);
                            string trim = before.Trim();
                            string __typename = before.Replace(trim, "__typename");
                            sb.AppendLine(__typename);
                        }

                        if (hasTypename == false && i == scripts.Length - 1)
                        {
                            string before = scripts[i - 1].Substring(2);
                            string trim = before.Trim();
                            string __typename = before.Replace(trim, "__typename");
                            sb.AppendLine(__typename);
                        }
                    }
                    sb.AppendLine(value);
                }
                string newScripts = sb.ToString();
                return newScripts;
            }
            catch (Exception exception)
            {
                exception.Show();
                return null;
            }
        }
        protected string GetGraphQLQuery(string script, string start, string end)
        {
            int indexOf = script.IndexOf(start);
            string substring = script.Substring(indexOf);
            //string substring1 = script.Substring(indexOf + start.Length);
            int endIndexOf = substring.IndexOf(end);
            substring = substring.Substring(0, endIndexOf);
            return GetGraphQLQuery(substring);
        }
        // 출처: https://shopv.pstatic.net/web/r/20250430132416/_next/static/chunks/2763-03226ce08adaff63.js
        //       https://shopv.pstatic.net/web/r/20250515085815/_next/static/chunks/2763-35f4a3c6e6cfcc93.js
        //       https://shopv.pstatic.net/web/r/20250813155916/_next/static/chunks/pages/_app-05bb70fa27be03a9.js
        //      } while (void 0 !== c);
        //      return 0 !== d.length && (g = d[d.length - 1][1])
        //      g
        // var r, i, o, a, u, l, c, s, d, p, y = t(7328),
        // g = t(10081),
        // f = (0, g.Ps)(r || (r = (0, y.Z)(["\n  fragment GroupProductFields on GroupProductResponse {\n    groupProduct {\n      averageReviewScore\n      totalReviewCount\n      name\n      unitPriceUnit\n      unitPriceSpelledQuantity\n      isCountableMeasurement\n      useUnitPrice\n      totalMeasurement\n    }\n    groupProductEpId\n    channelProductId\n    channelProduct {\n      name\n      totalReviewCount\n      averageReviewScore\n      arrivalDatetime\n      orderDeadline\n      nextOrderDeadline\n      stockQuantity\n      representativeImageUrl\n      baseFee\n      purchaseOptions {\n        standardPurchaseOptionValueId\n        standardPurchaseOptionName\n        standardPurchaseOptionId\n        standardPurchaseOptionOrder\n        purchaseOptionValueName\n        purchaseOptionValueOrder\n        image\n        exposureOrder\n        id\n      }\n      preOrder {\n        orderQty\n        minOrderQty\n        maxOrderQty\n        directConfigStockQty\n        deliveryStartDate\n        deliveryEndDate\n      }\n      subscription {\n        subscriptionType\n      }\n      channelServiceType\n      baseFee\n      dispDiscountedSalePrice\n      dispDiscountedRatio\n      deliveryFeeType\n      deliveryAttributeType\n      freeReturnInsurance\n      installation\n      dispSalePrice\n      rmidList\n      nextRmidList\n    }\n  }\n"]))),
        // v = (0, g.Ps)(i || (i = (0, y.Z)(["\n  ", "\n  fragment LogisticsProductFields on LogisticsProduct {\n    __typename\n    order\n    productId\n    name\n    channel {\n      channelSiteFullUrl\n      channelSiteMobileUrl\n      channelName\n      channelNo\n    }\n    subVerticalType\n    benefitsView {\n      discountedSalePrice\n      discountedRatio\n      sellerPurchasePoint\n    }\n    productImages {\n      imageType\n      url\n    }\n    reviewAmount {\n      totalReviewCount\n      averageReviewScore\n    }\n    productDeliveryInfo {\n      deliveryFeeType\n      deliveryAttributeType\n      installation\n    }\n    claimDeliveryInfo {\n      freeReturnInsurance\n    }\n    rmidList\n    nextRmidList\n    orderDeadline\n    nextOrderDeadline\n    arrivalDatetime\n    stockQuantity\n    subscription {\n      subscriptionType\n    }\n    mobileProductUrl\n    productUrl\n    regDate\n    salePrice\n    groupKey\n    authenticationType\n    similarity\n    isSaleProduct\n    loungeProductType\n    releaseDate\n    unitPriceConfig {\n      isUnitPrice\n      unitPriceSpellingQuantity\n      unitPriceSpellingUnit\n      totalMeasurement\n    }\n    promotionTags {\n      type\n      key\n      typeText\n      startAt\n      endAt\n      tagName\n      tagImage {\n        imageUrl\n        width\n        height\n      }\n    }\n    groupProductId\n    groupProductEpId\n    dispName\n    soldout\n    groupProducts {\n      ...GroupProductFields\n    }\n  }\n"])), f),
        // P = (0, g.Ps)(o || (o = (0, y.Z)(["\n  ", "\n  query FetchLogisticsProducts($productParam: VerticalProductParam, $pageSize: Int) {\n    logisticsProducts(productParam: $productParam, pageSize: $pageSize) {\n      ...LogisticsProductFields\n    }\n  }\n"])), v),
        // m = (0, g.Ps)(a || (a = (0, y.Z)(["\n  ", "\n  query FetchPagedLogisticsProducts(\n    $productParam: VerticalProductParam\n    $listParam: ListParam\n    $menuId: String!\n    $includePriorityProducts: Boolean!\n    $first: String\n    $hasOptionFilter: Boolean!\n    $includePromotionProducts: Boolean!\n  ) {\n    pagedLogisticsProducts(productParam: $productParam, listParam: $listParam) {\n      products {\n        ...LogisticsProductFields\n      }\n      hasMore\n      page\n    }\n    logisticsPriorityProducts(\n      menuId: $menuId\n      first: $first\n      hasOptionFilter: $hasOptionFilter\n      includePromotionProducts: $includePromotionProducts\n      productParam: $productParam\n    ) @include(if: $includePriorityProducts) {\n      ...LogisticsProductFields\n    }\n  }\n"])), v),
        // h = ((0, g.Ps)(u || (u = (0, y.Z)(["\n  query FetchGroupProducts($groupProductId: String, $groupProductParam: GroupProductParam) {\n    logisticsGroupProducts(groupProductId: $groupProductId, groupProductParam: $groupProductParam) {\n      groupProductId\n      groupProductEpId\n      channelProductId\n      groupProduct {\n        averageReviewScore\n        totalReviewCount\n        isCountableMeasurement\n        name\n        isCountableMeasurement\n        unitPriceSpelledQuantity\n        unitPriceUnit\n        useUnitPrice\n      }\n      channelProduct {\n        name\n        totalReviewCount\n        averageReviewScore\n        dispSalePrice\n        installation\n        deliveryAttributeType\n        deliveryFeeType\n        dispDiscountedRatio\n        dispDiscountedSalePrice\n        channelServiceType\n        rmidList\n        nextRmidList\n        orderDeadline\n        nextOrderDeadline\n        arrivalDatetime\n        purchaseOptions {\n          id\n          exposureOrder\n          purchaseOptionValueName\n          standardPurchaseOptionId\n          standardPurchaseOptionName\n          standardPurchaseOptionValueId\n        }\n        representativeImageUrl\n        stockQuantity\n      }\n    }\n  }\n"]))), (0, g.Ps)(l || (l = (0, y.Z)(["\n  ", "\n  query FetchPagedLogisticsRankingProducts($param: ServiceEditorPagedRankedProductParam) {\n    serviceEditorPagedRankedProduct(param: $param) {\n      hasMore\n      page\n      products {\n        id\n        name\n        salePrice\n        discountedSalePrice\n        discountedRatio\n        channelName\n        channelNo\n        brandId\n        brandName\n        matchingModelId\n        syncNvMid\n        soldout\n        imageUrl\n        channelImageUrl\n        landingUrl\n        productUrl\n        deliveryAttributeType\n        thisDayDispatchBasisTime\n        todayDispatchLocalTime\n        channelSiteFullUrl\n        stockQuantity\n        deliveryFeeType\n        freeReturnInsurance\n        subscriptionType\n        totalReviewCount\n        averageReviewScore\n        sellerPurchasePoint\n        pcSellerPurchasePoint\n        rmids\n        nextRmids\n        rank\n        maxDiscountPrice\n        customerManageBenefitType\n        groupKey\n        authenticationType\n        isSaleProduct\n        orderDeadline\n        nextOrderDeadline\n        loungeProductType\n        regDate\n        releaseDate\n        unitPriceConfig {\n          isUnitPrice\n          unitPriceSpellingQuantity\n          unitPriceSpellingUnit\n          totalMeasurement\n        }\n        standardGroupProduct {\n          id\n          standardGroupEpId\n        }\n        groupProducts {\n          ...GroupProductFields\n        }\n        channelNo\n        installation\n      }\n    }\n  }\n"])), f)),
        // O = (0, g.Ps)(c || (c = (0, y.Z)(["\n  ", "\n  query FetchLogisticsProductsByIds($ids: [String!]!, $zipCode: String!) {\n    logisticsProductsByIds(ids: $ids, zipCode: $zipCode) {\n      ...LogisticsProductFields\n    }\n  }\n"])), v),
        // F = ((0, g.Ps)(s || (s = (0, y.Z)(["\n  query FetchLogisticsPromotions {\n    logisticsPromotions {\n      type\n      url\n      discountPriceThreshold\n    }\n  }\n"]))), (0, g.Ps)(d || (d = (0, y.Z)(["\n  query FetchSimpleLogisticsPromotionProducts {\n    logisticsSimplePromotionProducts {\n      productId\n      endAt\n    }\n  }\n"])))),
        // ※f,v 안에 있는 문자열을 sha256해쉬로 변환하여 값으로 사용(문자열을 파싱하는 과정에서 '__typename'가 섞이기 때문에 추가 분석 필요)
        protected string GetSha256Hash(params string?[] scripts)
        {
            try
            {
                var scriptList = new List<string>();
                for (int i = 0; i < scripts.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(scripts[i])) continue;
                    scriptList.Add(scripts[i].TrimEnd());
                }
                string joinScripts = string.Join("\n\n", scriptList);
                joinScripts = joinScripts.Replace("\r", string.Empty);
                joinScripts += "\n";
                //var sampleText = "fragment GroupProductFields on GroupProductResponse {\n  groupProduct {\n    averageReviewScore\n    totalReviewCount\n    name\n    unitPriceUnit\n    unitPriceSpelledQuantity\n    isCountableMeasurement\n    useUnitPrice\n    totalMeasurement\n    __typename\n  }\n  groupProductEpId\n  channelProductId\n  channelProduct {\n    name\n    totalReviewCount\n    averageReviewScore\n    arrivalDatetime\n    orderDeadline\n    nextOrderDeadline\n    stockQuantity\n    representativeImageUrl\n    baseFee\n    purchaseOptions {\n      standardPurchaseOptionValueId\n      standardPurchaseOptionName\n      standardPurchaseOptionId\n      standardPurchaseOptionOrder\n      purchaseOptionValueName\n      purchaseOptionValueOrder\n      image\n      exposureOrder\n      id\n      __typename\n    }\n    preOrder {\n      orderQty\n      minOrderQty\n      maxOrderQty\n      directConfigStockQty\n      deliveryStartDate\n      deliveryEndDate\n      __typename\n    }\n    subscription {\n      subscriptionType\n      __typename\n    }\n    channelServiceType\n    baseFee\n    dispSalePrice\n    dispDiscountedSalePrice\n    dispDiscountedRatio\n    minPurchaseQuantity\n    deliveryFeeType\n    deliveryAttributeType\n    freeReturnInsurance\n    installation\n    rmidList\n    nextRmidList\n    __typename\n  }\n  __typename\n}\n\nfragment LogisticsProductFields on LogisticsProduct {\n  __typename\n  order\n  productId\n  name\n  channel {\n    channelSiteFullUrl\n    channelSiteMobileUrl\n    channelName\n    channelNo\n    __typename\n  }\n  subVerticalType\n  benefitsView {\n    discountedSalePrice\n    discountedRatio\n    dispDiscountedSalePrice\n    dispDiscountedRatio\n    sellerPurchasePoint\n    __typename\n  }\n  productImages {\n    imageType\n    url\n    __typename\n  }\n  reviewAmount {\n    totalReviewCount\n    averageReviewScore\n    __typename\n  }\n  productDeliveryInfo {\n    deliveryFeeType\n    deliveryAttributeType\n    installation\n    __typename\n  }\n  claimDeliveryInfo {\n    freeReturnInsurance\n    __typename\n  }\n  rmidList\n  nextRmidList\n  orderDeadline\n  nextOrderDeadline\n  arrivalDatetime\n  stockQuantity\n  minPurchaseQuantity\n  subscription {\n    subscriptionType\n    __typename\n  }\n  mobileProductUrl\n  productUrl\n  regDate\n  salePrice\n  dispSalePrice\n  groupKey\n  authenticationType\n  similarity\n  isSaleProduct\n  loungeProductType\n  releaseDate\n  unitPriceConfig {\n    isUnitPrice\n    unitPriceSpellingQuantity\n    unitPriceSpellingUnit\n    totalMeasurement\n    __typename\n  }\n  promotionTags {\n    type\n    key\n    typeText\n    startAt\n    endAt\n    tagName\n    tagImage {\n      imageUrl\n      width\n      height\n      __typename\n    }\n    __typename\n  }\n  groupProductId\n  groupProductEpId\n  dispName\n  soldout\n  groupProducts {\n    ...GroupProductFields\n    __typename\n  }\n}\n\nfragment MisterProductFields on MisterProduct {\n  __typename\n  ... on MisterProduct {\n    productId\n    name\n    channel {\n      channelSiteFullUrl\n      channelSiteMobileUrl\n      channelName\n      __typename\n    }\n    subVerticalType\n    benefitsView {\n      discountedSalePrice\n      discountedRatio\n      sellerPurchasePoint\n      __typename\n    }\n    productImages {\n      imageType\n      url\n      __typename\n    }\n    reviewAmount {\n      totalReviewCount\n      averageReviewScore\n      __typename\n    }\n    productDeliveryInfo {\n      deliveryAttributeType\n      deliveryFeeType\n      todayDelivery\n      todayDispatchLocalTime\n      __typename\n    }\n    claimDeliveryInfo {\n      freeReturnInsurance\n      __typename\n    }\n    stockQuantity\n    mobileProductUrl\n    productUrl\n    regDate\n    releaseDate\n    salePrice\n    groupKey\n    authenticationType\n    isMustHave\n    loungeProductType\n    verticalBrand {\n      _id\n      englishName\n      __typename\n    }\n    isOfficialMall\n    order\n    __typename\n  }\n}\n\nquery FetchSas($searchParam: SearchFilterParam!) {\n  searchSasFilter(searchParam: $searchParam) {\n    query\n    start\n    itemCount\n    total\n    hasMore\n    page\n    cmp {\n      categories {\n        id\n        __typename\n      }\n      __typename\n    }\n    products {\n      ... on GiftProduct {\n        __typename\n        productId\n        name\n        productUrl\n        mobileProductUrl\n        soldout\n        channel {\n          channelNo\n          channelName\n          channelSiteFullUrl\n          __typename\n        }\n        benefitsView {\n          discountedSalePrice\n          discountedRatio\n          __typename\n        }\n        reviewAmount {\n          totalReviewCount\n          averageReviewScore\n          __typename\n        }\n        productDeliveryInfo {\n          todayDispatchLocalTime\n          todayDelivery\n          deliveryAttributeType\n          __typename\n        }\n        order\n        giftbest\n        representativeImageUrl\n        isFreeDelivery\n        isFreeReturn\n        userScore {\n          reOrderCount\n          orderCount\n          uvScore\n          __typename\n        }\n        groupKey\n        searchGateUrl\n        loungeProductType\n      }\n      ... on LuxuryProduct {\n        productId\n        name\n        channel {\n          channelNo\n          __typename\n        }\n        authenticationType\n        subVerticalType\n        benefitsView {\n          discountedSalePrice\n          discountedRatio\n          __typename\n        }\n        productImages {\n          imageType\n          url\n          __typename\n        }\n        seoInfo {\n          sellerTags {\n            code\n            text\n            __typename\n          }\n          __typename\n        }\n        reviewAmount {\n          totalReviewCount\n          averageReviewScore\n          __typename\n        }\n        productDeliveryInfo {\n          todayDelivery\n          todayDispatchLocalTime\n          deliveryAttributeType\n          __typename\n        }\n        claimDeliveryInfo {\n          freeReturnInsurance\n          __typename\n        }\n        exposureStoreName\n        order\n        loungeProductType\n        groupKey\n        regDate\n        releaseDate\n        __typename\n      }\n      ... on LogisticsProduct {\n        ...LogisticsProductFields\n        __typename\n      }\n      ... on MisterProduct {\n        ...MisterProductFields\n        __typename\n      }\n      ... on SasChannelProduct {\n        __typename\n        productId\n        name\n        productUrl\n        mobileProductUrl\n        salePrice\n        dispSalePrice\n        authenticationType\n        soldout\n        channel {\n          channelNo\n          channelName\n          __typename\n        }\n        productImages {\n          imageType\n          url\n          __typename\n        }\n        benefitsView {\n          discountedSalePrice\n          discountedRatio\n          dispDiscountedSalePrice\n          dispDiscountedRatio\n          __typename\n        }\n        regDate\n        releaseDate\n        loungeProductType\n        reviewAmount {\n          totalReviewCount\n          averageReviewScore\n          __typename\n        }\n        productDeliveryInfo {\n          todayDelivery\n          todayDispatchLocalTime\n          deliveryAttributeType\n          deliveryFeeType\n          __typename\n        }\n        claimDeliveryInfo {\n          freeReturnInsurance\n          __typename\n        }\n        purchaseQuantityInfo {\n          minPurchaseQuantity\n          __typename\n        }\n        rmidList\n        nextRmidList\n        stockQuantity\n        order\n        exposureWindowStoreName\n        orderDeadline\n        nextOrderDeadline\n      }\n      __typename\n    }\n    __typename\n  }\n}\n";
                //string sha256Hash1 = SHA256Hash(sampleText);
                //sampleText = System.Text.RegularExpressions.Regex.Unescape(sampleText);
                //sha256Hash = SHA256Hash("fragment GroupProductFields on GroupProductResponse {\n  groupProduct {\n    averageReviewScore\n    totalReviewCount\n    name\n    unitPriceUnit\n    unitPriceSpelledQuantity\n    isCountableMeasurement\n    useUnitPrice\n    totalMeasurement\n    __typename\n  }\n  groupProductEpId\n  channelProductId\n  channelProduct {\n    name\n    totalReviewCount\n    averageReviewScore\n    arrivalDatetime\n    orderDeadline\n    nextOrderDeadline\n    stockQuantity\n    representativeImageUrl\n    baseFee\n    purchaseOptions {\n      standardPurchaseOptionValueId\n      standardPurchaseOptionName\n      standardPurchaseOptionId\n      standardPurchaseOptionOrder\n      purchaseOptionValueName\n      purchaseOptionValueOrder\n      image\n      exposureOrder\n      id\n      __typename\n    }\n    preOrder {\n      orderQty\n      minOrderQty\n      maxOrderQty\n      directConfigStockQty\n      deliveryStartDate\n      deliveryEndDate\n      __typename\n    }\n    subscription {\n      subscriptionType\n      __typename\n    }\n    channelServiceType\n    baseFee\n    dispDiscountedSalePrice\n    dispDiscountedRatio\n    deliveryFeeType\n    deliveryAttributeType\n    freeReturnInsurance\n    installation\n    dispSalePrice\n    rmidList\n    nextRmidList\n    __typename\n  }\n  __typename\n}\n\nfragment LogisticsProductFields on LogisticsProduct {\n  __typename\n  order\n  productId\n  name\n  channel {\n    channelSiteFullUrl\n    channelSiteMobileUrl\n    channelName\n    channelNo\n    __typename\n  }\n  subVerticalType\n  benefitsView {\n    discountedSalePrice\n    discountedRatio\n    sellerPurchasePoint\n    __typename\n  }\n  productImages {\n    imageType\n    url\n    __typename\n  }\n  reviewAmount {\n    totalReviewCount\n    averageReviewScore\n    __typename\n  }\n  productDeliveryInfo {\n    deliveryFeeType\n    deliveryAttributeType\n    installation\n    __typename\n  }\n  claimDeliveryInfo {\n    freeReturnInsurance\n    __typename\n  }\n  rmidList\n  nextRmidList\n  orderDeadline\n  nextOrderDeadline\n  arrivalDatetime\n  stockQuantity\n  subscription {\n    subscriptionType\n    __typename\n  }\n  mobileProductUrl\n  productUrl\n  regDate\n  salePrice\n  groupKey\n  authenticationType\n  similarity\n  isSaleProduct\n  loungeProductType\n  releaseDate\n  unitPriceConfig {\n    isUnitPrice\n    unitPriceSpellingQuantity\n    unitPriceSpellingUnit\n    totalMeasurement\n    __typename\n  }\n  promotionTags {\n    type\n    key\n    typeText\n    startAt\n    endAt\n    tagName\n    tagImage {\n      imageUrl\n      width\n      height\n      __typename\n    }\n    __typename\n  }\n  groupProductId\n  groupProductEpId\n  dispName\n  soldout\n  groupProducts {\n    ...GroupProductFields\n    __typename\n  }\n}\n\nfragment MisterProductFields on MisterProduct {\n  __typename\n  ... on MisterProduct {\n    productId\n    name\n    channel {\n      channelSiteFullUrl\n      channelSiteMobileUrl\n      channelName\n      __typename\n    }\n    subVerticalType\n    benefitsView {\n      discountedSalePrice\n      discountedRatio\n      sellerPurchasePoint\n      __typename\n    }\n    productImages {\n      imageType\n      url\n      __typename\n    }\n    reviewAmount {\n      totalReviewCount\n      averageReviewScore\n      __typename\n    }\n    productDeliveryInfo {\n      deliveryAttributeType\n      deliveryFeeType\n      todayDelivery\n      todayDispatchLocalTime\n      __typename\n    }\n    claimDeliveryInfo {\n      freeReturnInsurance\n      __typename\n    }\n    stockQuantity\n    mobileProductUrl\n    productUrl\n    regDate\n    releaseDate\n    salePrice\n    groupKey\n    authenticationType\n    isMustHave\n    loungeProductType\n    verticalBrand {\n      _id\n      englishName\n      __typename\n    }\n    isOfficialMall\n    order\n    __typename\n  }\n}\n\nquery FetchSas($searchParam: SearchFilterParam!) {\n  searchSasFilter(searchParam: $searchParam) {\n    query\n    start\n    itemCount\n    total\n    hasMore\n    page\n    cmp {\n      categories {\n        id\n        __typename\n      }\n      __typename\n    }\n    products {\n      ... on GiftProduct {\n        __typename\n        productId\n        name\n        productUrl\n        mobileProductUrl\n        soldout\n        channel {\n          channelNo\n          channelName\n          channelSiteFullUrl\n          __typename\n        }\n        benefitsView {\n          discountedSalePrice\n          discountedRatio\n          __typename\n        }\n        reviewAmount {\n          totalReviewCount\n          averageReviewScore\n          __typename\n        }\n        productDeliveryInfo {\n          todayDispatchLocalTime\n          todayDelivery\n          deliveryAttributeType\n          __typename\n        }\n        order\n        giftbest\n        representativeImageUrl\n        isFreeDelivery\n        isFreeReturn\n        userScore {\n          reOrderCount\n          orderCount\n          uvScore\n          __typename\n        }\n        groupKey\n        searchGateUrl\n        loungeProductType\n      }\n      ... on LuxuryProduct {\n        productId\n        name\n        channel {\n          channelNo\n          __typename\n        }\n        authenticationType\n        subVerticalType\n        benefitsView {\n          discountedSalePrice\n          discountedRatio\n          __typename\n        }\n        productImages {\n          imageType\n          url\n          __typename\n        }\n        seoInfo {\n          sellerTags {\n            code\n            text\n            __typename\n          }\n          __typename\n        }\n        reviewAmount {\n          totalReviewCount\n          averageReviewScore\n          __typename\n        }\n        productDeliveryInfo {\n          todayDelivery\n          todayDispatchLocalTime\n          deliveryAttributeType\n          __typename\n        }\n        claimDeliveryInfo {\n          freeReturnInsurance\n          __typename\n        }\n        exposureStoreName\n        order\n        loungeProductType\n        groupKey\n        regDate\n        releaseDate\n        __typename\n      }\n      ... on LogisticsProduct {\n        ...LogisticsProductFields\n        __typename\n      }\n      ... on MisterProduct {\n        ...MisterProductFields\n        __typename\n      }\n      ... on SasChannelProduct {\n        __typename\n        productId\n        name\n        productUrl\n        mobileProductUrl\n        salePrice\n        dispSalePrice\n        authenticationType\n        soldout\n        channel {\n          channelNo\n          channelName\n          __typename\n        }\n        productImages {\n          imageType\n          url\n          __typename\n        }\n        benefitsView {\n          discountedSalePrice\n          discountedRatio\n          dispDiscountedSalePrice\n          dispDiscountedRatio\n          __typename\n        }\n        regDate\n        releaseDate\n        loungeProductType\n        reviewAmount {\n          totalReviewCount\n          averageReviewScore\n          __typename\n        }\n        productDeliveryInfo {\n          todayDelivery\n          todayDispatchLocalTime\n          deliveryAttributeType\n          deliveryFeeType\n          __typename\n        }\n        claimDeliveryInfo {\n          freeReturnInsurance\n          __typename\n        }\n        purchaseQuantityInfo {\n          minPurchaseQuantity\n          __typename\n        }\n        rmidList\n        nextRmidList\n        stockQuantity\n        order\n        exposureWindowStoreName\n        orderDeadline\n        nextOrderDeadline\n      }\n      __typename\n    }\n    __typename\n  }\n}\n");
                //string sha256Hash = joinScripts.SHA256Hash();
                string sha256Hash = joinScripts.SHA256Hash();
                return sha256Hash;
            }
            catch (Exception exception)
            {
                exception.Show();
                return null;
            }
        }
        protected string GetScriptFile(string uriString, string chunksPath = "2763")
        {
            try
            {
                string? scriptUrl = null;
                string str = "";
                using (HttpClient httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient())
                {
                    HttpResponseMessage httpResponseMessage = httpClient.GetAsync(uriString).Result;
                    var html = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    str = html;
                    scriptUrl = html.GetValue($"//script[contains(@src,'next/static/chunks/{chunksPath}')]", "src");
                    if (string.IsNullOrWhiteSpace(scriptUrl)) throw new Exception("스크립트를 찾을 수 없습니다.");
                }

                if (string.IsNullOrWhiteSpace(scriptUrl))
                {
                    throw new Exception(str);
                }

                using (HttpClient httpClient = new BrowserScrapingBridge.Server.Sourcing.Transport.BrowserHttpClient())
                {
                    HttpResponseMessage httpResponseMessage = httpClient.GetAsync(scriptUrl)?.Result;
                    if (httpResponseMessage == null)
                    {
                        Console.WriteLine("httpResponseMessage is null");
                    }
                    string script = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    return script;
                }
            }
            catch (Exception exception)
            {
                exception.Show();
                return null;
            }
        }
        protected Dictionary<string, string> GetUriQueries(string uriString)
        {
            var dictionary = new Dictionary<string, string>();

            Uri uri = new Uri(uriString);
            if (string.IsNullOrWhiteSpace(uri.Query))
            {
                return dictionary;
                //UriBuilder uriBuilder = new UriBuilder(uri);
                //uriBuilder.Query = "?productCategoryId=ALL";
                //uri = uriBuilder.Uri;
            }
            var queries = uri.Query.Substring(1).Split('&');

            for (int i = 0; i < queries.Length; i++)
            {
                string query = queries[i];
                string[] keyValue = query.Split('=');
                dictionary.Add(keyValue[0], keyValue[1]);
            }
            return dictionary;
        }
        public string GetCode(string url)
        {
            var uri = new Uri(url);
            string code = uri.Segments.Last();
            return code;
        }
        public string GetPath(string uriString, string code)
        {
            var uri = new Uri(uriString);
            string path = null;
            if (string.IsNullOrWhiteSpace(code))
            {
                code = uri.Segments.Last();
            }
            int indexOf = uri.LocalPath.IndexOf(code);
            string localPath = uri.LocalPath.Substring(0, indexOf);
            switch (localPath)
            {
                case "/logistics/products/":
                    path = "logistics";
                    break;
                case "/window-products/directfarm/":
                    path = "directfarm";
                    break;
            }
            return path;
        }
        public async Task<string> GetLoginUrlAsync(string connectionId) { return null; }
        public async Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page)
        {
            #region 네이버 쇼핑 매뉴 ID 최신화(추후에 사용)
            //if (_sourcingManager.shoppingNaver.MainMeunItems == null || _sourcingManager.shoppingNaver.MainMeunItems.Count <= 0)
            //{
            //    string mainHtml = (await ExtentionsProxy(connectionId, "GET", "https://shopping.naver.com/ns/home/logistics", emptyHeader, "")).body;
            //    string __NEXT_DATA__ = mainHtml.GetValue("//script[@id='__NEXT_DATA__']", TextEx.XPathOption.InnerText);
            //    JObject next_data = JObject.Parse(__NEXT_DATA__);
            //    string buildId = next_data["buildId"].ToString();
            //    _sourcingManager.shoppingNaver.SetLogisticsMenuIdsAsync(buildId);
            //} 
            #endregion

            string html = await ChatHubService.GetRequest(connectionId, url);
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new CaptchaException()
                {
                    CaptchaInfo = new CaptchaInfo
                    {
                        Message = "네이버 쇼핑 차단을 확인해주세요",
                        Url = url
                    }
                };
            }

            //string url2763 = GetScriptUrl(html, "2763");
            //string url7257 = GetScriptUrl(html, "7257");
            //string script2763 = await ChatHubService.GetRequest(connectionId, url2763);
            //string script7257 = await ChatHubService.GetRequest(connectionId, url7257);
            //string productPageUriString = GetPageDataUriString(url, page, script2763, script7257);
            string productPageUriString = GetPageDataUriString(url, page, null, null);
            string json = await ChatHubService.GetRequest(connectionId, productPageUriString);
            if (string.IsNullOrWhiteSpace(json)) throw new LogException($"{page}페이지 조회에 실패했습니다.");

            var uri = new Uri(url);

            var collectPageInfos = new List<CollectPageInfo>();
            List<string> codes = GetProductCodesInPageData(uri.LocalPath, json);
            if (codes != null && codes.Count > 0)
            {
                string doCollectUrl = null;
                JObject data = JObject.Parse(json);
                switch (uri.LocalPath)
                {
                    case "/logistics/category":
                        doCollectUrl = "https://shopping.naver.com/logistics/products/";
                        break;
                    case "/fresh/directfarm/home":
                    case "/fresh/directfarm/category":
                    case "/fresh/directfarm/search":
                        doCollectUrl = "https://shopping.naver.com/window-products/directfarm/";
                        break;
                }

                for (int i = 0; i < codes.Count; i++)
                {
                    string code = codes[i];
                    string collectUrl = $"{doCollectUrl}{codes[i]}";
                    CollectPageInfo collectPageInfo = new CollectPageInfo();
                    collectPageInfo.Code = code;
                    collectPageInfo.Url = collectUrl;
                    collectPageInfos.Add(collectPageInfo);
                }
            }
            return collectPageInfos;
        }
        public string GetScriptUrl(string html, string chunksPath = "2763")
        {
            return html.GetValue($"//script[contains(@src,'next/static/chunks/{chunksPath}')]", "src");
        }
        public string GetPageDataUriString(string uriString, int page, string script2763, string script7257 = null)
        {
            string result = null;
            Uri uri = new Uri(uriString);

            switch (uri.LocalPath)
            {
                case "/logistics/category":
                case "/logistics/search":
                    result = new ShoppingNaverLogistics().GetLogisticsCategoryProductPageUriString(uriString, page, script2763, script7257);
                    break;
                case "/fresh/directfarm/home":
                    result = new ShoppingNaverDirectfarm().GetDirectfarmHomeProductPageUriString(uriString, page);
                    break;
                case "/fresh/directfarm/category":
                    result = new ShoppingNaverDirectfarm().GetDirectfarmCategoryProductPageUriString(uriString, page);
                    break;
                case "/fresh/directfarm/search":
                    result = new ShoppingNaverDirectfarm().GetDirectfarmSearchProductPageUriString(uriString, page, script2763);
                    break;
                default:
                    return null;
            }
            return result;
        }
        public List<string> GetProductCodesInPageData(string path, string json)
        {
            var codes = new List<string>();
            JObject data = JObject.Parse(json);

            var errors = data["errors"];
            if (errors != null)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("네이버 수집 중 예상치 못한 문제가 발생했습니다. 관리자에게 문의해주세요.");
                List<JToken> errorList = errors.ToList();
                for (int i = 0; i < errorList.Count; i++)
                {
                    try
                    {
                        var message = errorList[i].Value<string>("message");
                        stringBuilder.AppendLine($"{i + 1}.{message}");
                    }
                    catch { }
                }
                string errorMessage = stringBuilder.ToString();
                throw new CollectException(errorMessage);
            }
            List<JToken> products = null;
            switch (path)
            {
                case "/logistics/category":
                case "/logistics/search":
                    products = data["data"]["pagedLogisticsProducts"]["products"].ToList();
                    codes = products.Select(x => x["productId"].ToString()).ToList();
                    break;
                case "/fresh/directfarm/home":
                case "/fresh/directfarm/category":
                    products = data["products"].ToList();
                    codes = products.Select(x => x["_id"].ToString()).ToList();
                    break;
                case "/fresh/directfarm/search":
                    products = data["data"]["searchSasFilter"]["products"].ToList();
                    codes = products.Select(x => x["productId"].ToString()).ToList();
                    break;
            }
            return codes;
        }
        public string GetProductJsonUriString(string path, string code)
        {
            switch (path)
            {
                case "logistics":
                    return new ShoppingNaverLogistics().GetLogisticsCategoryProductUriString(code);
                case "directfarm":
                    return new ShoppingNaverDirectfarm().GetDirectfarmProductDataUriString(code);
                default:
                    throw new Exception($"네이버N 쇼핑 카테고리 경로가 잘못 되었습니다. path: {path}");
            }
        }
        protected List<Name> GetTitie(JToken? contents)
        {
            try
            {
                string name = contents["name"].ToString();
                var result = new List<Name>();
                result.Add(new Name("KR", name));
                return result;
            }
            catch (Exception exception)
            {
                throw new Exception("상품명을 찾을 수 없습니다.", exception);
            }
        }
        protected List<Desc> GetDesc(List<JToken> jTokens)
        {
            var descs = new List<Desc>();
            for (int i = 0; i < jTokens.Count; i++)
            {
                dynamic token = jTokens[i];
                string name = token.Name.ToString();
                var value = jTokens[i].First().ToString();
                descs.Add(new Desc(i + 1, "KR", name, value));
            }
            return descs;
        }
        protected List<Desc> GetDesc(params List<JToken>[] jTokens)
        {
            var descs = new List<JToken>();
            for (int i = 0; i < jTokens.Length; i++)
            {
                if (jTokens[i] == null) continue;
                descs.AddRange(jTokens[i]);
            }
            return GetDesc(descs);
        }
        protected List<Desc> GetDescs(JToken? contents)
        {
            try
            {
                List<JToken> jTokens = new List<JToken>();
                JToken? viewAttributes = contents["viewAttributes"];
                if (viewAttributes != null)
                {
                    jTokens.AddRange(viewAttributes.ToList());
                }

                var detailAttributes = contents["detailAttributes"];
                if (detailAttributes != null)
                {
                    jTokens.AddRange(detailAttributes.ToList());
                }
                List<Desc> result = GetDesc(jTokens);
                return result;
            }
            catch (Exception exception)
            {
                throw new Exception("상품 설명을 찾을 수 없습니다.", exception);
            }
        }
        protected List<Tag> GetTags(JToken? contents)
        {
            List<Tag> result = new List<Tag>();
            try
            {
                var sellerTags = contents["seoInfo"]?["sellerTags"];
                if (sellerTags == null) { return result; }
                List<JToken> sellerTagList = sellerTags.ToList();
                foreach (JToken? item in sellerTagList)
                {
                    string key = null;
                    JToken? sellerTagCode = item["code"];
                    if (sellerTagCode != null)
                    {
                        key = sellerTagCode.ToString();
                    }
                    JToken? sellerTagText = item["text"];
                    List<string> values = null;
                    if (sellerTagText != null)
                    {
                        values = new List<string> { sellerTagText.ToString() };
                    }
                    result.Add(new Tag { key = key, values = values });
                }
                return result;
            }
            catch (Exception exception)
            {
                throw new Exception("태그 정보를 찾을 수 없습니다.", exception);
            }
        }
        protected List<Price> GetPrices(JToken contents)
        {
            try
            {
                var result = new List<Price>();

                double salePrice = 0;
                double originalPrice = 0;
                JToken? discountedSalePrice = contents["discountedSalePrice"];
                if (discountedSalePrice != null)
                {
                    salePrice = Convert.ToDouble(discountedSalePrice);
                }
                else
                {
                    throw new Exception("할인가격이 없습니다.");
                }

                JToken? contentsSalePrice = contents["salePrice"];
                if (contentsSalePrice != null)
                {
                    originalPrice = Convert.ToDouble(contentsSalePrice);
                }
                else
                {
                    throw new Exception("판매가격이 없습니다.");
                }
                var price = new Price("KRW", originalPrice, salePrice);
                price.isdefalutPrice = true;
                result.Add(price);

                return result;
            }
            catch (Exception exception)
            {
                throw new Exception("상품 가격을 찾을 수 없습니다.", exception);
            }
        }
        protected List<string> GetMainImages(JToken contents)
        {
            try
            {
                var result = new List<string>();

                JToken? productImages = contents["productImages"];
                foreach (var item in productImages)
                {
                    string url = item["url"].ToString();
                    result.Add(url);
                }
                return result;
            }
            catch (Exception exception)
            {
                throw new Exception("상품 이미지를 찾을 수 없습니다.", exception);
            }
        }
        protected Option GetOption(JToken contents)
        {
            try
            {
                bool optionUsable = Convert.ToBoolean(contents["optionUsable"]);
                if (optionUsable)
                {
                    var result = new Option();
                    result.optionGroups = new List<Option.OptionGroup>();

                    List<JToken> options = contents["options"].ToList();
                    List<JToken> optionCombinations = contents["optionCombinations"].ToList();

                    IEnumerable<JToken?> groupNames = options.Select(x => x["groupName"]);

                    List<string> optionGroupNameList = new List<string>();
                    foreach (JToken? item in groupNames)
                    {
                        optionGroupNameList.Add(item.ToString());
                    }
                    string optionGroupNames = string.Join("ㅡ", optionGroupNameList);
                    var optionGroupName = new List<Name>() { new Name(Language.KR, optionGroupNames) };

                    var noneQuantityOptionIds = new List<string>();
                    result.detailOptions = new List<Option.DetailOption>();
                    for (int i = 0; i < optionCombinations.Count; i++)
                    {
                        string? id = optionCombinations[i].Value<string>("id");

                        int stockQuantity = optionCombinations[i].Value<int>("stockQuantity");
                        if (stockQuantity <= 0)
                        {
                            noneQuantityOptionIds.Add(id);
                            continue;
                        }

                        JToken? optionCombinationsPrice = optionCombinations[i]["price"];
                        double detailOptionPrice = Convert.ToDouble(optionCombinationsPrice);

                        var detailOption = new Option.DetailOption();
                        detailOption.optionIds = id;
                        detailOption.isSoldOut = true;
                        detailOption.optionGroupName = optionGroupName;
                        var optionNameList = new List<string>();

                        int j = 1;
                        while (true)
                        {
                            string? optionName = optionCombinations?[i]?[$"optionName{j}"]?.ToString();
                            if (string.IsNullOrWhiteSpace(optionName)) break;
                            optionNameList.Add(optionName);
                            j++;
                        }

                        string optionNames = string.Join("ㅡ", optionNameList);
                        detailOption.optionName = new List<Name>() { new Name(Language.KR, optionNames) };
                        detailOption.quantity = stockQuantity;
                        detailOption.price = new List<Price>();
                        detailOption.price.Add(new Price("KRW", detailOptionPrice, detailOptionPrice));
                        detailOption.optionImage = null;
                        result.detailOptions.Add(detailOption);
                    }

                    for (int i = 0; i < options.Count; i++)
                    {
                        string id = options[i].Value<string>("id");
                        string groupName = options[i].Value<string>("groupName");
                        var optionGroup = new Option.OptionGroup();
                        optionGroup.optionId = id;
                        optionGroup.groupName = new List<Name>();
                        optionGroup.groupName.Add(new Name("KR", groupName));
                        optionGroup.groups = new List<Option.OptionGroup.Group>();
                        var optionNameList = new List<string>();
                        for (int j = 0; j < optionCombinations.Count; j++)
                        {
                            int propertyNumber = i + 1;
                            string optionId = optionCombinations[j].Value<string>("id");
                            bool isExists = noneQuantityOptionIds.Exists(x => x == optionId);
                            if (isExists)
                            {
                                continue;
                            }
                            string optionName = optionCombinations[j].Value<string>($"optionName{propertyNumber}");
                            bool isContains = optionNameList.Contains(optionName);
                            if (isContains) continue;

                            var group = new Option.OptionGroup.Group();
                            group.groupId = optionId;

                            group.optionName = new List<Name>();
                            group.optionName.Add(new Name("KR", optionName));

                            group.price = null;
                            //group.price.Add(new Price("KRW", 0, 0));

                            group.quantity = 0;
                            group.optionImage = null;

                            optionGroup.groups.Add(group);
                            optionNameList.Add(optionName);
                        }
                        result.optionGroups.Add(optionGroup);
                    }
                    return result;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exception)
            {
                throw new Exception("옵션 정보를 찾을 수 없습니다.", exception);
            }

        }
        /// <summary>
        /// 네이버는 옵션 가격이 원본가격에 +/-로 되어 있기 때문에 원본가격+옵션가격로 수정
        /// </summary>
        /// <param name="product"></param>
        protected void SetOptionPrice(Product product)
        {
            if (product.option != null && product.option.detailOptions != null)
            {
                for (int i = 0; i < product.option.detailOptions.Count; i++)
                {
                    foreach (Price price in product.price)
                    {
                        string currency = price.currency;

                        for (int j = 0; j < product.option.detailOptions[i].price.Count; j++)
                        {
                            if (product.option.detailOptions[i].price[j].currency == currency)
                            {
                                product.option.detailOptions[i].price[j].salePrice += price.salePrice;
                                product.option.detailOptions[i].price[j].originalPrice += price.originalPrice;
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 최소주문개수가 있는 상품의 경우 각 옵션마다 옵션*최소주문개수으로 수정
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="product"></param>
        protected void SetMinPurchaseQuantityToOption(JToken contents, Product product)
        {
            if (product.option == null) return;
            JToken? purchaseQuantityInfo = contents["purchaseQuantityInfo"];
            if (purchaseQuantityInfo != null)
            {
                int minPurchaseQuantity = purchaseQuantityInfo.Value<int>("minPurchaseQuantity");
                if (minPurchaseQuantity <= 0 == false)
                {
                    for (int i = 0; i < product.option.detailOptions.Count; i++)
                    {
                        for (int j = 0; j < product.option.detailOptions[i].optionName.Count; j++)
                        {
                            string originalOptionName = product.option.detailOptions[i].optionName[j].text;
                            string newOptionName = product.option.detailOptions[i].optionName[j].text + $" x {minPurchaseQuantity}";
                            IEnumerable<Option.OptionGroup.Group> groups = product.option.optionGroups.SelectMany(x => x.groups);
                            IEnumerable<Name> optionNames = groups.SelectMany(x => x.optionName);
                            IEnumerator<Name> enumerator = optionNames.GetEnumerator();
                            while (true)
                            {
                                enumerator.MoveNext();
                                if (enumerator.Current == null) break;
                                if (enumerator.Current.text == originalOptionName)
                                {
                                    enumerator.Current.text = newOptionName;
                                    break;
                                }
                            }
                            product.option.detailOptions[i].optionName[j].text = newOptionName;
                        }

                        for (int j = 0; j < product.option.detailOptions[i].price.Count; j++)
                        {
                            product.option.detailOptions[i].price[j].originalPrice *= minPurchaseQuantity;
                            product.option.detailOptions[i].price[j].salePrice *= minPurchaseQuantity;
                        }
                    }
                }
            }
        }
        protected void SetDeliveryPriceToProduct(JToken contents, Product product)
        {
            JToken? productDeliveryInfo = contents["productDeliveryInfo"];
            if (productDeliveryInfo != null)
            {
                string? deliveryFeeType = productDeliveryInfo.Value<string>("deliveryFeeType");
                if (deliveryFeeType.ToUpper() != "FREE")
                {
                    switch (deliveryFeeType)
                    {
                        case "CONDITIONAL_FREE":
                        case "UNIT_QUANTITY_PAID":
                        case "PAID":
                            double baseFee = productDeliveryInfo.Value<double>("baseFee");
                            for (int i = 0; i < product.price.Count; i++)
                            {
                                product.price[i].originalPrice += baseFee;
                                product.price[i].salePrice += baseFee;
                            }

                            if (product.option != null)
                            {
                                for (int i = 0; i < product.option.detailOptions.Count; i++)
                                {
                                    for (int j = 0; j < product.option.detailOptions[i].price.Count; j++)
                                    {
                                        product.option.detailOptions[i].price[j].originalPrice += baseFee;
                                        product.option.detailOptions[i].price[j].salePrice += baseFee;
                                    }
                                }
                            }
                            break;
                        default:
                            // 추후에 발견되는 다른 타입의 상품이 있을 경우 기능 추가
                            throw new Exception($"기능 추가가 필요한 배송 정보입니다. deliveryFeeType: {deliveryFeeType}");
                    }
                }
            }
        }

        public string GetDetailPageDataUriString(string path, string code, JObject data)
        {
            switch (path)
            {
                case "logistics":
                    return new ShoppingNaverLogistics().GetVerticalDetailPageUriString(code, data);
                case "directfarm":
                    return new ShoppingNaverDirectfarm().GetDetailPageDataUriString(code, data);
                default:
                    return null;
            }
        }

        protected string GetVerticalDetailHtml(string json)
        {
            try
            {
                var jObject = JObject.Parse(json);
                JToken? renderContent = jObject["renderContent"];
                if (renderContent != null)
                {
                    return renderContent.ToString();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exception)
            {
                throw new Exception("상세 페이지를 찾을 수 없습니다.", exception);
            }
        }
        protected string GetDetailPageHtml(string json)
        {
            try
            {
                var jObject = JObject.Parse(json);
                JToken? renderContent = jObject["renderContent"];
                if (renderContent != null)
                {
                    string html = renderContent.ToString();
                    return html;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exception)
            {
                throw new Exception("상세 페이지를 찾을 수 없습니다.", exception);
            }
        }
        public string GetDetailPageHtml(string path, string json)
        {
            string html = null;
            switch (path)
            {
                case "logistics":
                    html = GetVerticalDetailHtml(json);
                    break;
                case "directfarm":
                    html = GetDetailPageHtml(json);
                    break;
                default:
                    return null;
            }

            string dafaultHtml = HtmlReader.GetDafaultHtml(html);

            List<string> imgTags = dafaultHtml.GetValues("//img", TextEx.XPathOption.OuterHtml);
            for (int i = 0; i < imgTags.Count; i++)
            {
                var src = imgTags[i].GetValue("//img", "data-src");
                if (string.IsNullOrWhiteSpace(src)) continue;
                string newImgTag = $"<img src=\"{src}\">";
                dafaultHtml = dafaultHtml.Replace(imgTags[i], newImgTag);
            }
            return dafaultHtml;
        }
        /// <summary>
        /// 품절체크
        /// </summary>
        private void CheckSoldOut(JToken data)
        {
            var productStatusType = data.Value<string>("productStatusType");
            if (productStatusType?.ToUpper() == "OUTOFSTOCK") throw new SaleException(ProductSaleStatus.SoldOut, "품절상품입니다.");
            var statusType = data.Value<string>("statusType");
            if (statusType?.ToUpper() == "OUTOFSTOCK") throw new SaleException(ProductSaleStatus.SoldOut, "품절상품입니다.");
        }
        public async Task<Product> GetProduct(string connectionId, string path, string code, string html)
        {
            Product product = null;
            JObject data;
            switch (path)
            {
                case "logistics":
                    product = new ShoppingNaverLogistics().GetLogisticsCategoryProduct(code, html, out data);
                    break;
                case "directfarm":
                    product = new ShoppingNaverDirectfarm().GetDirectfarmProduct(code, html, out data);
                    break;
                default:
                    data = null;
                    throw new Exception($"네이버N 쇼핑 카테고리 경로가 잘못 되었습니다. path: {path}");
            }

            string getDetailPageDataUriString = GetDetailPageDataUriString(path, code, data);
            string json = await ChatHubService.GetRequest(connectionId, getDetailPageDataUriString);
            string detailPageHtml = GetDetailPageHtml(path, json);
            product.detailPageHtml = detailPageHtml;

            #region Product의 참조 방식이기 때문에 순서 중요
            SetOptionPrice(product);
            SetMinPurchaseQuantityToOption(data, product);
            SetDeliveryPriceToProduct(data, product);
            try
            {
                CheckSoldOut(data); // 품절 체크
            }
            catch (SaleException se)
            {
                product.collectStatus = ProductCollectStatus.Success;
                product.saleStatus = se.SaleStatus;
                product.memo = se.Message;
            }
            #endregion
            return product;
        }
        //public async Task<Product> GetProduct(string connectionId, string url, string html)
        //{
        //    Product product = new Product();
        //    try
        //    {
        //        string code = GetCode(url);
        //        string path = GetPath(url, code);
        //        product = await GetProduct(connectionId, path, code, html);
        //    }
        //    catch (SaleException se)
        //    {
        //        product.collectStatus = ProductCollectStatus.Success;
        //        product.saleStatus = se.SaleStatus;
        //        product.memo = se.Message;
        //    }
        //    catch (Exception exception)
        //    {
        //        product.collectStatus = ProductCollectStatus.Failed;
        //        product.memo = exception.Message;
        //    }
        //    return product;
        //}
        public async Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting collectOptionSetting = null)
        {
            string code = GetCode(url);
            string path = GetPath(url, code);
            string productJsonUriString = GetProductJsonUriString(path, code);
            string html = await ChatHubService.GetRequest(connectionId, productJsonUriString);
            return await GetProduct(connectionId, path, code, html);
        }

        public Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url)
        {
            throw new NotImplementedException();
        }

        public Product GetProduct(CollectRawData data)
        {
            throw new NotImplementedException();
        }

        public async Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting)
        {
            throw new NotImplementedException();
        }
    }
}
