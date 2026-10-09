#nullable disable warnings
using System.Text.Json.Serialization;

namespace BrowserScrapingBridge.Server.Sourcing.Alibaba.Model
{
    public class GetTaobaoH5UrlModel
    {
        public string appId { get; set; }
        [JsonPropertyName("params")]
        public string json { get; set; }
        public class TaobaoJson
        {
            public string device { get; set; }
            public string isBeta { get; set; }
            public string grayHair { get; set; }
            public string from { get; set; }
            public string brand { get; set; }
            public string info { get; set; }
            public string index { get; set; }
            public string rainbow { get; set; }
            public string schemaType { get; set; }
            public string elderHome { get; set; }
            public string isEnterSrpSearch { get; set; }
            public string newSearch { get; set; }
            public string network { get; set; }
            public string subtype { get; set; }
            public string hasPreposeFilter { get; set; }
            public string prepositionVersion { get; set; }
            public string client_os { get; set; }
            public string gpsEnabled { get; set; }
            public string searchDoorFrom { get; set; }
            public string debug_rerankNewOpenCard { get; set; }
            public string homePageVersion { get; set; }
            public string searchElderHomeOpen { get; set; }
            public string search_action { get; set; }
            public string sugg { get; set; }
            public string sversion { get; set; }
            public string style { get; set; }
            public string ttid { get; set; }
            public string needTabs { get; set; }
            public string areaCode { get; set; }
            public string vm { get; set; }
            public string countryNum { get; set; }
            public string m { get; set; }
            public int page { get; set; }
            public int n { get; set; }
            public string q { get; set; }
            public string tab { get; set; }
            public string pageSize { get; set; }
            public string totalPage { get; set; }
            public string totalResults { get; set; }
            public string sourceS { get; set; }
            public string sort { get; set; }
            public string bcoffset { get; set; }
            public string ntoffset { get; set; }
            public string filterTag { get; set; }
            public string service { get; set; }
            public string prop { get; set; }
            public string loc { get; set; }
            public float? start_price { get; set; }
            public float? end_price { get; set; }
            public float? startPrice { get; set; }
            public float? endPrice { get; set; }


        }
    }
}
