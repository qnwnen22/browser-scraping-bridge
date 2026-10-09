#nullable disable warnings
using Newtonsoft.Json;

namespace BrowserScrapingBridge.Server.Sourcing.Models.Options
{
    public class MusinsaCollectOption
    {
        [JsonProperty("useLevelDiscount")]
        public bool? UseLevelDiscount { get; set; }
        [JsonProperty("useCouponDiscount")]
        public bool? UseCouponDiscount { get; set; }
        [JsonProperty("usePointDiscount")]
        public bool? UsePointDiscount { get; set; }
        [JsonProperty("usePrePointDiscount")]
        public bool? UsePrePointDiscount { get; set; }
        [JsonProperty("useHyundaiCartInstantDiscount")]
        public bool? UseHyundaiCartInstantDiscount { get; set; }
        [JsonProperty("useHyundaiCartBillDiscount")]
        public bool? UseHyundaiCartBillDiscount { get; set; }
        [JsonProperty("hasTitleShippingText")]
        public bool? HasTitleShippingText { get; set; }
        [JsonProperty("isBoutiqueProduct")]
        public bool? IsBoutiqueProduct { get; set; }
    }
}
