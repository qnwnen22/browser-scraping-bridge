#nullable disable warnings
using System;

namespace BrowserScrapingBridge.Server.Sourcing.OwnerClan.Models
{
    /// <summary>
    /// 이미지허용상품정보
    /// </summary>
    [Obsolete("API문서의 내용이 이상함, 사용에 주의")]
    public class GetImageAllowItemsApi : GetBaseApi
    {
        public string ver { get; set; } = "1.1";

        public string mode { get; set; } = "getImageAllowItems";

        /// <summary>
        /// 해당값이 없을경우 기본 0이 들어 감.
        /// </summary>
        public int? page { get; set; }

        /// <summary>
        /// 최소 주문수량이 1인 상품을 찾을 경우 true 를 입력.
        /// </summary>
        public string moq { get; set; }
        public GetImageAllowItemsApi() { }
        public GetImageAllowItemsApi(string aid) : base(aid) { }
    }
}
