#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Models.Options;
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using BrowserScrapingBridge.Server.Sourcing.Common;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using BrowserScrapingBridge.Server.Sourcing.Common.Models;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing
{
    public interface ICollect
    {
        /// <summary>
        /// 사이트(영문)
        /// </summary>
        string Site { get; }
        /// <summary>
        /// 이름(한글)
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 사이트 언어
        /// </summary>
        Language Language { get; }
        /// <summary>
        /// 사이트 화폐 단위
        /// </summary>
        Currency Currency { get; }
        /// <summary>
        /// 크롬 확장프로그램 쳇 허브 서비스
        /// </summary>
        IChatHubService ChatHubService { get; set; }
        /// <summary>
        /// 수집 시 사용할 프록시(경우에 따라 사용)
        /// </summary>
        WebProxy Proxy { get; set; }
        /// <summary>
        /// 전달된 수집 원문을 상품으로 파싱하는 기능의 지원 여부
        /// true일 경우 GetCollectRawData() -> GetProduct() -> ApplyCollectOptionSetting() 
        /// false일 경우 GetProductAsync() 
        /// </summary>
        bool SupportsCaching { get; set; }
        /// <summary>
        /// URL에서 상품 코드 값 반환
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        string GetCode(string url);
        /// <summary>
        /// 로그인 필요 시 로그인 페이지 반환, 필요 없을 경우 null 반환
        /// </summary>
        /// <param name="connectionId">HTML 수집을 수행할 브라우저의 SignalR 연결 ID</param>
        /// <returns></returns>
        Task<string> GetLoginUrlAsync(string connectionId);
        /// <summary>
        /// 페이지 수집
        /// </summary>
        /// <param name="connectionId">HTML 수집을 수행할 브라우저의 SignalR 연결 ID</param>
        /// <param name="url"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        Task<List<CollectPageInfo>> GetCollectPageInfosAsync(string connectionId, string url, int page);

        /// <summary>
        /// 상품 수집(ver 1.0)
        /// </summary>
        Task<Product> GetProductAsync(string connectionId, string url, CollectOptionSetting? collectOptionSetting = null);

        #region 데이터 수집, Product 파싱, 수집설정 반영 역할분리
        /// <summary>
        /// 수집에 필요한 원시 데이터 가져오기
        /// </summary>
        Task<CollectRawData> GetCollectRawDataAsync(string connectionId, string url);
        /// <summary>
        /// 상품 수집(ver 2.0)
        /// </summary>
        Product GetProduct(CollectRawData rawData);
        /// <summary>
        /// 사이트 별 수집 설정 반영
        /// </summary>
        Task ApplyCollectOptionSetting(string connectionId, CollectRawData rawData, Product product, CollectOptionSetting setting);
        #endregion
    }
}
