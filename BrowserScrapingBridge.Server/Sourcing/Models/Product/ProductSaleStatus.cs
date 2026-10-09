#nullable disable warnings
namespace BrowserScrapingBridge.Server.Sourcing.Models.Product
{
    /// <summary>
    /// Product 상태 관리
    /// </summary>
    public enum ProductSaleStatus
    {
        /// <summary>
        /// 판매중
        /// </summary>
        OnSale = 0,
        /// <summary>
        /// 재고없음
        /// </summary>
        SoldOut = 1,
        /// <summary>
        /// 판매중지 / 상품 내려감
        /// </summary>
        Stopped = 2,
        /// <summary>
        /// 알 수 없음 (외부 데이터 오류 등)
        /// </summary>
        Unknown = -1
    }
}
