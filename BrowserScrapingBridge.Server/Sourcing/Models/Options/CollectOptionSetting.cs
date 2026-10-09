#nullable disable warnings
using Newtonsoft.Json;

namespace BrowserScrapingBridge.Server.Sourcing.Models.Options
{
    public class CollectOptionSetting
    {
        /// <summary>
        /// 알리익스프레스 수집 설정
        /// </summary>
        [JsonProperty("aliExpressCollectOption")]        public AliExpressCollectOption? AliExpressCollectOption { get; set; }
        /// <summary>
        /// 무신사 수집 설정
        /// </summary>
        [JsonProperty("musinsaCollectOption")]        public MusinsaCollectOption? MusinsaCollectOption { get; set; }
        /// <summary>
        /// 신세계 수집 설정
        /// </summary>
        [JsonProperty("ssgCollectOption")]        public SsgCollectOption? SsgCollectOption { get; set; }

        /// <summary>
        /// 롯데온 수집 설정
        /// </summary>
        [JsonProperty("lotteOnCollectOption")]        public LotteOnCollectOption? LotteOnCollectOption { get; set; }

        /// <summary>
        /// 코스트코 수집 설정
        /// </summary>
        [JsonProperty("costcoCollectOption")]        public CostcoCollectOption? CostcoCollectOption { get; set; }

        /// <summary>
        /// 지마켓 일반상품(Gmarket_General) 수집 설정
        /// </summary>
        [JsonProperty("gmarketGeneralCollectOption")]        public GmarketGeneralCollectOption? GmarketGeneralCollectOption { get; set; }

        /// <summary>
        /// 지마켓 스타배송(StarDelivery) 수집 옵션
        /// </summary>
        [JsonProperty("gmarketCollectOption")]        public GmarketCollectOption? GmarketCollectOption { get; set; }

        /// <summary>
        /// H몰 수집 설정
        /// </summary>
        [JsonProperty("hmallCollectOption")]        public HmallCollectOption? HmallCollectOption { get; set; }
    }
}
