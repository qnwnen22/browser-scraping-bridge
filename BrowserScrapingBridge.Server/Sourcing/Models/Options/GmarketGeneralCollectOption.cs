#nullable disable warnings

namespace BrowserScrapingBridge.Server.Sourcing.Models.Options
{
    /// <summary>
    /// 지마켓 일반상품(Gmarket_General) 수집 설정
    /// </summary>
    public class GmarketGeneralCollectOption
    {
        /// <summary>
        /// 쿠폰적용가 선택(기본 선택)
        /// true: 쿠폰적용가 사용, false: 일반 판매가만 사용
        /// </summary>        public bool? UseCouponAppliedPrice { get; set; }
    }
}
