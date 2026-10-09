#nullable disable warnings
using Newtonsoft.Json;

namespace BrowserScrapingBridge.Server.Sourcing.Models.Options
{
    public class LotteOnCollectOption
    {
        [JsonProperty("useProductCoupon")]        public bool? UseProductCoupon { get; set; }
        [JsonProperty("useOverlapCoupon")]        public bool? UseOverlapCoupon { get; set; }
        [JsonProperty("useStoreCoupong")]        public bool? UseStoreCoupong { get; set; }
        [JsonProperty("useCardOrCartCoupon")]        public bool? UseCardOrCartCoupon { get; set; }
    }
}
