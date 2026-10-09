#nullable disable warnings
using System.Text;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Tool
{
    public class UriBuilderAdvance
    {
        private Uri uri;
        public Dictionary<string, string> queryDictionary;
        public List<string> ErrorMessages { get; set; }
        public string Scheme { get; set; }
        public string Host { get; set; }
        public List<string> Segments { get; set; }
        public Uri Uri
        {
            get { return uri; }
            private set { uri = value; }
        }

        private UriBuilderAdvance()
        {
            queryDictionary = new Dictionary<string, string>();
            ErrorMessages = new List<string>();
        }

        public UriBuilderAdvance(Uri uri) : this()
        {
            Uri = uri;
            this.Scheme = uri.Scheme;
            this.Host = uri.Host;
            if (uri.Segments.Length > 0)
            {
                this.Segments = uri.Segments.ToList();
            }
            SetQueryDictionary();
        }

        public UriBuilderAdvance(string uriString) : this(new Uri(uriString))
        {
        }

        private void SetQueryDictionary()
        {
            if (string.IsNullOrWhiteSpace(Uri.Query)) return;
            string query = Uri.Query.Substring(1);
            string[] keyValue = query.Split('&');

            foreach (string item in keyValue)
            {
                string[] split = item.Split('=');
                try
                {
                    string value = null;
                    bool isTryGetValue = queryDictionary.TryGetValue(split[0], out value);
                    if (isTryGetValue)
                    {
                        value += $",{split[1]}";
                        queryDictionary[split[0]] = value;
                    }
                    else
                    {
                        queryDictionary.Add(split[0], split[1]);
                    }
                }
                catch (Exception exception)
                {
                    ErrorMessages.Add(exception.Message);
                }
            }
        }

        public string GetQuery(string key)
        {
            string result = null;
            queryDictionary.TryGetValue(key, out result);
            return result;
        }

        public void SetQuery(string key, double? value)
        {
            SetQuery(key, value.ToString());
        }
        public void SetQuery(string key, long? value)
        {
            SetQuery(key, value.ToString());
        }
        public void SetQuery(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            queryDictionary[key] = value;
        }

        public void DeleteQuery(params string[] keys)
        {
            foreach (string key in keys)
            {
                queryDictionary.Remove(key);
            }
        }

        public void ClearQuery()
        {
            queryDictionary = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            var list = new List<string>();
            foreach (KeyValuePair<string, string> keyValuePair in queryDictionary)
            {
                list.Add($"{keyValuePair.Key}={keyValuePair.Value}");
            }
            string query = string.Join('&', list);
            string path = string.Join("", Segments);
            string address = $"{Scheme}://{Host}{path}";
            if (string.IsNullOrWhiteSpace(query) == false)
            {
                address += $"?{query}";
            }
            return address;
        }

        public string GetQueryString()
        {
            if (this.queryDictionary == null || this.queryDictionary.Count == 0) return null;
            var list = new List<string>();
            foreach (var item in queryDictionary)
            {
                list.Add($"{item.Key}={item.Value}");
            }
            string query = string.Join("&", list);
            query = $"?{query}";
            return query;
        }
    }
}
