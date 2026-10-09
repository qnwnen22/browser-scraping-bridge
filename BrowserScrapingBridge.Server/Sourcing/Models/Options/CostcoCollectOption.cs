#nullable disable warnings
using Newtonsoft.Json;

namespace BrowserScrapingBridge.Server.Sourcing.Models.Options
{
    public class CostcoCollectOption
    {
        /// <summary>
        /// 행사 가격 사용 여부
        /// </summary>
        [JsonProperty("usePromotionSalePrice")]        public bool? UsePromotionSalePrice { get; set; }
    }
}
