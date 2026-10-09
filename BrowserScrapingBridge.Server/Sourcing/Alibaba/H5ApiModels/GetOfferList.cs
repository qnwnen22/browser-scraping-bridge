#nullable disable warnings
using Newtonsoft.Json;

namespace BrowserScrapingBridge.Server.Sourcing.Alibaba.H5ApiModels
{
    public class GetOfferList
    {
        public string verticalProductFlag { get; set; } = "pcmarket";
        public string searchScene { get; set; } = "pcOfferSearch";
        public string charset { get; set; } = "utf8";
        public int beginPage { get; set; } = 1;
        public int pageSize { get; set; } = 60;
        public string keywords { get; set; } = "";
        public string spm { get; set; }
        public string featurePair { get; set; }
        public string method { get; set; } = "getOfferList";
        public GetOfferList(int beginPage)
        {
            this.beginPage = beginPage;
        }
        public string ToParams()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }
    }
}
