#nullable disable warnings
using Newtonsoft.Json.Linq;

namespace BrowserScrapingBridge.Server.Sourcing
{
    public class CollectRawData
    {
        /// <summary>
        /// 상품 URL
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 데이터 수집 시 필요한 원시 데이터
        /// </summary>
        public JToken? Data { get; set; }
        /// <summary>
        /// 수집한 페이지의 HTML 소스 코드(필요한 경우에만 반환, 그렇지 않으면 null)
        /// </summary>
        public string Html { get; set; }
        public string DetailHtml { get; set; }

        public CollectRawData()
        {

        }

        public CollectRawData(string url, JToken data, string html)
        {
            Url = url;
            Data = data;
            Html = html;    
        }
    }
}
