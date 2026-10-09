#nullable disable warnings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace BrowserScrapingBridge.Server.Sourcing.ShoppingNaver.Models
{
    public class FetchSasBody : BodyBase
    {
        public class Searchparam
        {
            public string q { get; set; }
            public List<string> verticals { get; set; } = new List<string> { "FRESH" };
            public List<string> subVerticals { get; set; } = new List<string> { "DIRECT_FARM" };
            public int start { get; set; }
            public int display { get; set; } = 20;
            public string sort { get; set; } = "REL_DSC";
            public List<string> brandIds { get; set; }
            public int? minDispPrice { get; set; }
            public int? maxDispPrice { get; set; }
            public int? minDispDiscountRate { get; set; }
            public int? maxDispDiscountRate { get; set; }
            public List<string> deliveries { get; set; } = new List<string> { "" };
        }
        public Searchparam searchParam { get; set; } = new Searchparam();

        public FetchSasBody() { }

        public FetchSasBody(string uriString, int page)
        {
            Dictionary<string, string> uriQueries = GetUriQueries(uriString);

            string q;
            uriQueries.TryGetValue("q", out q);
            q = HttpUtility.UrlDecode(q);
            this.searchParam.q = q;
            this.searchParam.start = (this.searchParam.display * (page - 1)) + 1;

            string optionFilters;
            uriQueries.TryGetValue("optionFilters", out optionFilters);
            if (string.IsNullOrWhiteSpace(optionFilters) == false)
            {
                string[] optionFilterArray = optionFilters.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (string optionFilter in optionFilterArray)
                {
                    string[] optionFilterSplit = optionFilter.Split('_');
                    string type = optionFilterSplit.First();
                    switch (type)
                    {
                        case "BR":
                            if (this.searchParam.brandIds == null || this.searchParam.brandIds.Count <= 0)
                            {
                                this.searchParam.brandIds = new List<string>();
                            }
                            this.searchParam.brandIds.Add(optionFilterSplit[1]);
                            break;

                        case "PR":
                            if (this.searchParam.minDispPrice == null) { this.searchParam.minDispPrice = 0; }
                            if (this.searchParam.maxDispPrice == null) { this.searchParam.maxDispPrice = 0; }
                            string dispPriceValue = optionFilterSplit[2];
                            switch (optionFilterSplit[1])
                            {
                                case "OVER":
                                    this.searchParam.minDispPrice = Convert.ToInt32(dispPriceValue);
                                    break;

                                case "UNDER":
                                    this.searchParam.maxDispPrice = Convert.ToInt32(dispPriceValue);
                                    break;

                                default:
                                    throw new Exception($"확인이 필요한 쿼리입니다 : {uriString}");
                            }
                            break;

                        case "DR":
                            if (this.searchParam.minDispDiscountRate == null) { this.searchParam.minDispDiscountRate = 0; }
                            if (this.searchParam.maxDispDiscountRate == null) { this.searchParam.maxDispDiscountRate = 0; }
                            string dispDiscountRateValue = optionFilterSplit[2];
                            switch (optionFilterSplit[1])
                            {
                                case "OVER":
                                    this.searchParam.minDispDiscountRate = Convert.ToInt32(dispDiscountRateValue);
                                    break;

                                case "UNDER":
                                    this.searchParam.maxDispDiscountRate = Convert.ToInt32(dispDiscountRateValue);
                                    break;

                                default:
                                    throw new Exception($"확인이 필요한 쿼리입니다 : {uriString}");
                            }
                            break;
                        default:
                            throw new Exception($"확인이 필요한 쿼리입니다 : {uriString}");
                    }
                }
            }

            string deliveries;
            uriQueries.TryGetValue("", out deliveries);
            if (string.IsNullOrWhiteSpace(deliveries) == false)
            {
                string[] deliverieArray = deliveries.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (deliverieArray.Length > 0)
                {
                    this.searchParam.deliveries = deliverieArray.ToList();
                }
            }
        }

        private Dictionary<string, string> GetUriQueries(string uriString)
        {
            Uri uri = new Uri(uriString);
            if (string.IsNullOrWhiteSpace(uri.Query))
            {
                throw new Exception($"확인이 필요한 쿼리입니다 : {uriString}");
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

        private void SetPropertyNames(Type type, List<string> names)
        {
            PropertyInfo[] properties = type.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                names.Add(property.Name);
                if (property.PropertyType.IsClass)
                {
                    if (property.PropertyType.Namespace.IndexOf("System") == 0)
                    {
                        continue;
                    }
                    Type innerType = Type.GetType(property.PropertyType.FullName);
                    SetPropertyNames(innerType, names);
                }
            }
        }

        //public override string ToJson()
        //{
        //    var names = new List<string>();
        //    Type type = this.GetType();
        //    SetPropertyNames(type, names);
        //    var json = base.ToJson();
        //    var urlEncodeJson = WebUtility.UrlEncode(json);
        //    urlEncodeJson = urlEncodeJson.ToUpper();
        //    foreach (string name in names)
        //    {
        //        urlEncodeJson = urlEncodeJson.Replace("", "");
        //    }
        //    return null;
        //}
    }
}
