#nullable disable warnings
namespace BrowserScrapingBridge.Server.Sourcing.OwnerClan.Models
{
    /// <summary>
    /// 상품상세정보
    /// </summary>
    public class GetItemViewApi : GetBaseApi
    {
        /// <summary>
        /// API Version
        /// </summary>
        public string ver { get; private set; } = "4.5";

        /// <summary>
        /// API 호출 모드값
        /// </summary>
        public string mode { get; private set; } = "getItemView";

        /// <summary>
        /// 열람하고자 하는 상품번호
        /// ※ 4.1 버전부터 도매꾹과 오너클랜 상품정보가 모두 표시되도록 수정되었으므로, market 파라미터는 필요하지 않음
        /// </summary>
        public string? no { get; set; }

        /// <summary>
        /// 출력 형식 : json | xml
        /// </summary>
        public string om { get; private set; } = "json";

        /// <summary>
        /// 모든 상태의 상품 표시 여부 (승인대기,승인거부 상태 포함)
        /// · true : 노출
        /// · false : 비노출
        /// ※ sellerId 및 sId를 입력할 경우 allItem 항목으로 조회 가능 (공급사 본인의 상품일 경우)
        /// ※ 존재하지 않는 상품일 경우엔 40번 오류 노출
        /// </summary>
        public bool? allItem { get; set; }

        /// <summary>
        /// 공급사 아이디
        /// ※ allItem 항목을 조회할 경우에 필수 입력
        /// </summary>
        public string sellerId { get; set; }

        /// <summary>
        /// 로그인 세션 아이디
        /// ※ allItem 항목을 조회할 경우에 필수 입력
        /// </summary>
        public string sId { get; set; }
        public GetItemViewApi() { }
        public GetItemViewApi(string aid) : base(aid) { }
    }
}
