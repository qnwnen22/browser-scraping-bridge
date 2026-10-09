#nullable disable warnings
using Newtonsoft.Json;

namespace BrowserScrapingBridge.Server.Sourcing.Models.Options
{
    /// <summary>
    /// 지마켓 스타배송 수집 옵션
    /// </summary>
    public class GmarketCollectOption
    {
        [JsonProperty("useDeliveryPrice")]        public bool? UseDeliveryPrice { get; set; }
    }
}
