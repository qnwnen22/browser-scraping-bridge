#nullable disable warnings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.OwnerClan.Models
{
    public class ResponseProductModel
    {

        public Data data { get; set; }


        public class Data
        {
            public Item item { get; set; }
        }

        public class Item
        {
            public long createdAt { get; set; }
            public long updatedAt { get; set; }
            public string key { get; set; }
            public string name { get; set; }
            public string model { get; set; }
            public string production { get; set; }
            public string origin { get; set; }
            public string id { get; set; }
            public int price { get; set; }
            public string pricePolicy { get; set; }
            public object fixedPrice { get; set; }
            public string[] searchKeywords { get; set; }
            public Category category { get; set; }
            public string content { get; set; }
            public int shippingFee { get; set; }
            public string shippingType { get; set; }
            public List<string> images { get; set; }
            public string status { get; set; }
            public List<Option> options { get; set; }
            public bool taxFree { get; set; }
            public bool adultOnly { get; set; }
            public bool returnable { get; set; }
            public object noReturnReason { get; set; }
            public int guaranteedShippingPeriod { get; set; }
            public bool openmarketSellable { get; set; }
            public int boxQuantity { get; set; }
            public string[] attributes { get; set; }
            public string closingTime { get; set; }
            public Metadata metadata { get; set; }
        }

        public class Category
        {
            public string key { get; set; }
            public string name { get; set; }
        }

        public class Metadata
        {
            public int returnAddressCode { get; set; }
            public int vendorKey { get; set; }
            public object vendorNotice { get; set; }
            public string coupangCategoryCode { get; set; }
            public string coupangCategoryFullName { get; set; }
            public string auctionCategoryCode { get; set; }
            public string auctionCategoryFullName { get; set; }
            public string gmarketCategoryCode { get; set; }
            public string gmarketCategoryFullName { get; set; }
            public string st11CategoryCode { get; set; }
            public string st11CategoryFullName { get; set; }
            public string interparkCategoryCode { get; set; }
            public string interparkCategoryFullName { get; set; }
            public string smartstoreCategoryCode { get; set; }
            public string smartstoreCategoryFullName { get; set; }
            public string playautoCategoryCode { get; set; }
            public string playautoCategoryFullName { get; set; }
            public string esellersCategoryCode { get; set; }
            public string esellersCategoryFullName { get; set; }
            public Productnotificationinformation productNotificationInformation { get; set; }
            public object[] certificateInformation { get; set; }
            public int returnShippingFee { get; set; }
            public string grade { get; set; }
            public Gradedetail gradeDetail { get; set; }
        }

        public class Productnotificationinformation
        {
            public int code { get; set; }
            public string[] categorySpecific { get; set; }
            public string[] common { get; set; }
        }

        public class Gradedetail
        {
            public string releaseRate { get; set; }
            public string averageShip { get; set; }
            public string qnaAnswer { get; set; }
        }

        public class Option
        {
            public List<OptionAttr> optionAttributes { get; set; }
            public int price { get; set; }
            public int quantity { get; set; }
            public string key { get; set; }
        }

        public class OptionAttr
        {
            public string name { get; set; }
            public string value { get; set; }
        }
    }
}
