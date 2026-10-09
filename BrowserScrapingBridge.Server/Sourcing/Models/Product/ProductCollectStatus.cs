#nullable disable warnings
namespace BrowserScrapingBridge.Server.Sourcing.Models.Product
{
    public enum ProductCollectStatus
    {
        /// <summary>
        /// 수집 성공
        /// </summary>
        Success = 0,
        /// <summary>
        /// 수집 실패
        /// </summary>
        Failed = 1,
        /// <summary>
        /// 수집 예외
        /// </summary>
        Exception = 2,
        /// <summary>
        /// 수집 이후 오류 발생
        /// </summary>
        Error = 99
    }
}
