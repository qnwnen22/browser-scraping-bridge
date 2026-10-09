#nullable disable warnings

namespace BrowserScrapingBridge.Server.Sourcing.Models.Options
{
    /// <summary>
    /// H몰(Hmall) 수집 설정
    /// </summary>
    public class HmallCollectOption
    {
        /// <summary>
        /// H몰 쿠폰 할인 적용 여부
        /// </summary>        public bool? UseCouponDiscount { get; set; }

        /// <summary>
        /// 카드 즉시할인 적용 여부
        /// </summary>        public bool? UseCardDiscount { get; set; }
    }
}
