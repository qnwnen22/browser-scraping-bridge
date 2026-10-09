#nullable disable warnings
using System.Collections.Generic;
using System.Net;

namespace BrowserScrapingBridge.Server.Sourcing.Common.Models
{
    public enum RequestType
    {
        ChatHub,
        HttpClient,
    }

    public class CollectRequestInfo
    {
        public RequestType RequestType { get; set; }
        public string Method { get; set; } = "GET";
        public string Url { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public string Body { get; set; }
        public CookieContainer CookieContainer { get; set; }
        public bool UseProxy { get; set; } = false;
        public string Encoding { get; set; }
    }
}
