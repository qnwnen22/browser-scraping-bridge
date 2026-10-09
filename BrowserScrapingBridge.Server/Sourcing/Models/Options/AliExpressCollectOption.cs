#nullable disable warnings

namespace BrowserScrapingBridge.Server.Sourcing.Models.Options
{
    public class AliExpressCollectOption
    {
        /// <summary>
        /// 옵션가 최고가 기준으로 수집 여부
        /// - 옵션 중 최대 가격을 기준으로 -50%까지 수집
        /// </summary>        public bool? isMaxPriceOptionCollect { get; set; }        [Obsolete("수집 시 가격에 배송비가 포함되어 있기 때문에 사용안함")]
        public bool? useDeliveryPrice { get; set; }
    }
}
