#nullable disable warnings
using System.Collections;
using System.Net;
using System.Reflection;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class CookieExtension
    {
        public static CookieCollection GetCollection(this CookieContainer cookieContainer)
        {
            return cookieContainer.GetAllCookies();
        }

        public static List<Cookie> GetCookies(this CookieContainer cookieContainer)
        {
            var collection = GetCollection(cookieContainer);
            var cookies = new List<Cookie>();
            foreach (Cookie cookie in collection)
            {
                cookies.Add(cookie);
            }
            return cookies;
        }
    }
}
