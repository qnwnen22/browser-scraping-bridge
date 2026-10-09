#nullable disable warnings
namespace BrowserScrapingBridge.Server.Sourcing.Domeggook.Models
{
    public class GetCatApi : GetBaseApi
    {
        /// <summary>
        /// API Version
        /// </summary>
        public string ver { get; set; } = "2.0";

        /// <summary>
        /// API 호출 모드값 : getCat
        /// </summary>
        public string mode { get; set; } = "getCat";

        /// <summary>
        /// dome : 도매꾹
        /// domeme : 도매매
        /// </summary>
        public string market { get; set; }

        /// <summary>
        /// 출력 형식 : json | xml
        /// </summary>
        public string om { get; set; }

        /// <summary>
        /// 상품이 0개인 카테고리 표시여부
        /// 0 : 표시안함 (기본값)
        /// 1 : 표시함
        /// </summary>
        public int? withZero { get; set; }

        public GetCatApi() { }
        public GetCatApi(string aid) : base(aid) { }
    }
}
