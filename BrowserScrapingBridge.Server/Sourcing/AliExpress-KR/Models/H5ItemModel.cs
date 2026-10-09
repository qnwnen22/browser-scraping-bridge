#nullable disable warnings
using System.Collections.Generic;

namespace BrowserScrapingBridge.Server.Sourcing.AliExpress_KR.Models
{
    public class H5ItemModel
    {
        public string api { get; set; }
        public H5Response data { get; set; }
        public List<string> ret { get; set; }
        public string v { get; set; }
        public class H5Response
        {
            public int code { get; set; }
            public LoginDataModel data { get; set; }
        }
    }
}
