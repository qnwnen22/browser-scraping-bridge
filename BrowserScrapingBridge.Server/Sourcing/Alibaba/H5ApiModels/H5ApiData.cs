#nullable disable warnings
using Newtonsoft.Json;

namespace BrowserScrapingBridge.Server.Sourcing.Alibaba.H5ApiModels
{
    public class H5ApiData
    {
        public int appId { get; set; } = 32517;
        [JsonProperty("params")]
        public string Params { get; set; }
    }
}
