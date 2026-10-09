#nullable disable warnings
using System.Collections.Generic;
using System.Reflection;

namespace BrowserScrapingBridge.Server.Sourcing.Domeggook.Models
{
    /// <summary>
    /// 도매꾹 API 
    /// </summary>
    public class GetBaseApi
    {
        public const string _url = "https://domeggook.com/ssl/api/";
        /// <summary>
        /// API Key
        /// </summary>
        /// <see cref="https://openapi.domeggook.com/main/reference/lst_open">레퍼런스</see>>
        public string aid { get; private set; } /*= "0e1104b0e11de2022217a066ae74983c";*/
        public GetBaseApi() { }

        public GetBaseApi(string aid)
        {
            this.aid = aid;
        }

        public string CreateRequestUri()
        {
            var queries = new List<string>();

            PropertyInfo[] getProperties = this.GetType().GetProperties();
            for (int i = 0; i < getProperties.Length; i++)
            {
                string name = getProperties[i].Name;
                string value = getProperties[i].GetValue(this)?.ToString();
                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    queries.Add($"{name}={value}");
                }
            }

            string query = string.Join("&", queries);
            string requestString = $"{_url}?{query}";
            return requestString;
        }
    }
}
