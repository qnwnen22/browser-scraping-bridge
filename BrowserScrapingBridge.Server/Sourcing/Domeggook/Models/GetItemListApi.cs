#nullable disable warnings
namespace BrowserScrapingBridge.Server.Sourcing.Domeggook.Models
{
    /// <summary>
    /// 상품리스트
    /// </summary>
    public class GetItemListApi : GetBaseApi
    {
        /// <summary>
        /// API Version
        /// </summary>
        public string ver { get; private set; } = "4.1";

        /// <summary>
        /// API 호출 모드값
        /// </summary>
        public string mode { get; private set; } = "getItemList";

        /// <summary>
        /// 출력 형식 : json | xml
        /// </summary>
        public string om { get; set; } = "json";

        /// <summary>
        /// 서비스 코드
        /// · dome : 도매꾹
        /// · supply : 도매매
        /// </summary>
        public string market { get; set; } = "dome";

        /// <summary>
        /// 페이지당 상품 출력개수
        /// · 1 이상 200 이하의 정수를 입력
        /// · 미입력시 20이 기본값
        /// </summary>
        public int? sz { get; set; }

        /// <summary>
        /// 페이지 번호
        /// </summary>
        public int? pg { get; set; }

        /// <summary>
        /// 검색결과 정렬방식
        /// · se : 정확도순 (검색어 사용시 외에는 사용불가 / 검색어 사용시 기본값)
        /// · rd : 도매꾹랭킹순 (검색어 미사용시 기본값)
        /// · ha : 인기상품순
        /// · aa : 낮은가격순
        /// · ad : 높은가격순
        /// · sd : 신규판매자순
        /// · qa : 적은판매단위순
        /// · qd : 많은판매단위순
        /// · da : 최근등록순(상품정보등록/수정일 최근순)
        /// </summary>
        public string so { get; set; }

        /// <summary>
        /// 기획전 번호
        /// · 특정 기획전에 속한 상품을 검색할 때에만 입력
        /// · market값이 "dome"일 때에만 적용됨
        /// </summary>
        public string ev { get; set; }

        /// <summary>
        /// 카테고리 검색
        /// · 2자리의 숫자쌍 5개가 언더 바(_)로 연결된 형태로 입력
        ///     ex) 01_01_00_00_00
        /// · 미입력시, 모든 카테고리의 상품을 검색
        /// · 카테고리 값이 잘못 입력된 경우, 미입력한 것으로 간주함
        /// · 대분류만으로 검색할 수 없으며, 중분류 이하로 검색 가능함
        ///     ex) 01_00_00_00_00 검색 불가 , 01_11_00_00_00 검색 가능
        /// </summary>
        public string ca { get; set; }

        /// <summary>
        /// 판매자 아이디 검색
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 검색어 검색
        /// </summary>
        public string kw { get; set; }

        /// <summary>
        /// 가격대 검색 / 최저가격대
        /// </summary>
        public int? mnp { get; set; }

        /// <summary>
        /// 가격대 검색 / 최고가격대
        /// ※ mnp에 입력된 값보다 작은 값을 입력한 경우, mxp 값은 미입력한 것으로 간주함
        /// </summary>
        public int? mxp { get; set; }

        /// <summary>
        /// 최소구매수량 검색 / 최저수량
        /// </summary>
        public int? mnq { get; set; }

        /// <summary>
        /// 최소구매수량 검색 / 최대수량
        /// ※ mnq에 입력된 값보다 작은 값을 입력한 경우, mxq 값은 미입력한 것으로 간주함
        /// </summary>
        public int? mxq { get; set; }

        /// <summary>
        /// 배송비 부담
        /// · S : 무료배송 상품만 검색
        /// · B : 착불 상품만 검색
        /// · P : 배송비 선결제 상품만 검색
        /// · C : 선결제/착불 선택가능 상품만 검색
        /// </summary>
        public string who { get; set; }

        /// <summary>
        /// 국산 여부
        /// · kr : 국내산 상품만 검색
        /// · fr : 국외산 상품만 검색
        /// · 이 외의 값이 입력된 경우, 미입력한 것으로 간주함
        /// </summary>
        public string org { get; set; }

        /// <summary>
        /// 우수판매자 상품 검색
        /// · true : 우수판매자 상품만 검색
        /// · false : 전체상품 검색 (기본값)
        /// </summary>
        public bool? sgd { get; set; }

        /// <summary>
        /// 빠른배송 상품 검색
        /// · true : 빠른배송 상품만 검색
        /// · false : 전체상품 검색 (기본값)
        /// </summary>
        public bool? fdl { get; set; }

        /// <summary>
        /// 상품번호 검색
        /// </summary>
        public int? itemNo { get; set; }

        public GetItemListApi() { }
        public GetItemListApi(string aid) : base(aid) { }
    }
}
