#nullable disable warnings
namespace BrowserScrapingBridge.Server.Sourcing.Amazon.Common.Models
{
    public class OptionModel
    {
        public string ASIN { get; set; }
        public string Type { get; set; }
        public string sortOfferInfo { get; set; }
        public string isPrimeEligible { get; set; }
        public Value value { get; set; }
        public class Value
        {
            public Content content { get; set; }
        }
        public class Content
        {
            public class TwisterSlotJson
            {
                public string price { get; set; }
            }
            public TwisterSlotJson twisterSlotJson { get; set; }
            public string twisterSlotDiv { get; set; }
            public string originalOptionPrice { get; set; }
        }
    }
}
