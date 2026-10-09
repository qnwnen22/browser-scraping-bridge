#nullable disable warnings
using Newtonsoft.Json;

namespace BrowserScrapingBridge.Server.Sourcing.Models.Options
{
    public class SsgCollectOption
    {
        [JsonProperty("useCardBenefitPrice")]        public bool? UseCardBenefitPrice { get; set; }
    }
}
