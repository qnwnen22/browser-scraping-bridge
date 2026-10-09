#nullable disable warnings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BrowserScrapingBridge.Server.Sourcing.ShoppingNaver.Models
{
    public class FetchPagedLogisticsProductsBody : BodyBase
    {
        public class ProductParam
        {
            public string vertical { get; set; } = "LOGISTICS";
            public string subVertical { get; set; } = "LOGISTICS";
            public bool soldOut { get; set; } = false;
            public string menuId { get; set; }
            public class Sort
            {
                public string target { get; set; } = "POPULAR";
                public string sortDirection { get; set; } = "DESC";
            }
            public List<Sort> sorts { get; set; } = new List<Sort>();
            public List<int> guaranteedDays { get; set; }
            public List<string> deliveries { get; set; }
            public List<string> brandIds { get; set; }
            public class AttributeValueId
            {
                public string category { get; set; }
                public List<string> value { get; set; }
            }
            public List<AttributeValueId> attributeValueIds { get; set; }
            public int? reviewScore { get; set; }
            public List<int> discountRatio { get; set; }
            public List<int> price { get; set; }
            public string zipCode { get; set; } = "06236";
            //public List<int> excludeGuaranteedDays { get; set; } = new List<int> { 0, 1 };

        }
        public ProductParam productParam { get; set; } = new ProductParam();
        public class ListParam
        {
            public int page { get; set; }
            public int pageSize { get; set; } = 20;
        }
        public ListParam listParam { get; set; } = new ListParam();
        public string menuId { get; set; }
        public bool includePriorityProducts { get; set; }
        public bool hasOptionFilter { get; set; }
        public bool includePromotionProducts { get; set; } = false;

        public FetchPagedLogisticsProductsBody() { }

        public FetchPagedLogisticsProductsBody(string uriString, int page)
        {
            Dictionary<string, string> uriQueries = GetUriQueries(uriString);

            string menuId;
            uriQueries.TryGetValue("menu", out menuId);
            this.menuId = menuId;
            this.productParam.menuId = menuId;
            this.listParam.page = page;

            string guaranteedDays;
            uriQueries.TryGetValue("guaranteedDays", out guaranteedDays);
            if (string.IsNullOrWhiteSpace(guaranteedDays))
            {
                this.productParam.guaranteedDays = new List<int>() { 1 };
            }
            else
            {
                guaranteedDays = HttpUtility.UrlDecode(guaranteedDays);
                string[] guaranteedDayArray = guaranteedDays.Split(',');
                this.productParam.guaranteedDays = new List<int>();
                foreach (string guaranteedDay in guaranteedDayArray)
                {
                    int value = Convert.ToInt32(guaranteedDay);
                    this.productParam.guaranteedDays.Add(value);
                }
            }

            string sort;
            uriQueries.TryGetValue("sort", out sort);
            #region 정렬 종류
            // RANKING: 빠른도착 랭킹순
            // LOW_PRICE: 낮은 가격순
            // HIGH_PRICE: 높은 가격순
            // PURCHASE: 구매순
            // RECENT: 최신순
            // REVIEW: 리뷰순 
            #endregion
            if (string.IsNullOrWhiteSpace(sort) || sort == "RANKING")
            {
                sort = "POPULAR";
            }
            this.productParam.sorts.Add(new FetchPagedLogisticsProductsBody.ProductParam.Sort { target = sort });

            string deliveries;
            uriQueries.TryGetValue("deliveries", out deliveries);
            if (string.IsNullOrWhiteSpace(deliveries) == false)
            {
                this.productParam.deliveries = new List<string>();

                deliveries = HttpUtility.UrlDecode(deliveries);
                var split = deliveries.Split(',');
                for (int i = 0; i < split.Length; i++)
                {
                    this.productParam.deliveries.Add(split[i]);
                }
            }

            string optionFilters;
            uriQueries.TryGetValue("optionFilters", out optionFilters);
            if (string.IsNullOrWhiteSpace(optionFilters) == false)
            {
                this.hasOptionFilter = true;
                optionFilters = HttpUtility.UrlDecode(optionFilters);
                var attrs = optionFilters.Split(',').ToList();

                foreach (var item in attrs)
                {
                    string[] values = item.Split('_');
                    string first = values.First();
                    string last = values.Last();
                    switch (first)
                    {
                        case "DR":
                            if (this.productParam.discountRatio == null)
                            {
                                this.productParam.discountRatio = new List<int>();
                            }
                            var discountRatioValue = Convert.ToInt32(last);
                            this.productParam.discountRatio.Add(discountRatioValue);
                            break;

                        case "RS":
                            this.productParam.reviewScore = Convert.ToInt32(last);
                            break;

                        case "BR":
                            if (this.productParam.brandIds == null)
                            {
                                this.productParam.brandIds = new List<string>();
                            }
                            this.productParam.brandIds.Add(last);
                            break;

                        case "PR":
                            if (this.productParam.price == null)
                            {
                                this.productParam.price = new List<int>();
                            }
                            this.productParam.price.Add(Convert.ToInt32(last));
                            break;

                        case "AT":
                            if (this.productParam.attributeValueIds == null)
                            {
                                this.productParam.attributeValueIds = new List<FetchPagedLogisticsProductsBody.ProductParam.AttributeValueId>();
                            }
                            this.productParam.attributeValueIds.Add(new FetchPagedLogisticsProductsBody.ProductParam.AttributeValueId
                            {
                                category = values[1],
                                value = new List<string>() { last }
                            });
                            break;

                        default:
                            break;
                    }
                }
            }
            else
            {
                this.hasOptionFilter = false;
            }

            if (page > 2) // 페이지가 3이상 부턴 자동으로 false값이 됨
            {
                this.includePriorityProducts = false;
            }
            else
            {
                this.includePriorityProducts = true;
            }
        }

        private Dictionary<string, string> GetUriQueries(string uriString)
        {
            Uri uri = new Uri(uriString);
            if (string.IsNullOrWhiteSpace(uri.Query))
            {
                UriBuilder uriBuilder = new UriBuilder(uri);
                uriBuilder.Query = "?productCategoryId=ALL";
                uri = uriBuilder.Uri;
            }
            var queries = uri.Query.Substring(1).Split('&');

            var dictionary = new Dictionary<string, string>();
            for (int i = 0; i < queries.Length; i++)
            {
                string query = queries[i];
                string[] keyValue = query.Split('=');
                dictionary.Add(keyValue[0], keyValue[1]);
            }
            return dictionary;
        }
    }
}
