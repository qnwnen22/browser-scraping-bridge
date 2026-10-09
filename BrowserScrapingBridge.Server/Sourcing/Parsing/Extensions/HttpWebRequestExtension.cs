#nullable disable warnings
using System.Net;
using System.Text;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class HttpWebRequestExtension
    {
        public static void Write(this BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest request, string postString, Encoding encoding = null)
        {
            if (encoding == null) { encoding = Encoding.UTF8; }
            byte[] bytes = encoding.GetBytes(postString);
            Write(request, bytes, encoding);
        }
        public static void Write(this BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest request, byte[] bytes, Encoding encoding = null)
        {
            if (encoding == null) { encoding = Encoding.UTF8; }
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public static async Task<string> ReadToEndAsync(this BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest request)
        {
            using (WebResponse webResponse = await request.GetResponseAsync())
            {
                using (Stream stream = webResponse.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        string result = streamReader.ReadToEnd();
                        return result;
                    }
                }
            }
        }

        public static string ReadToEnd(this BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest request, Encoding encoding = null)
        {
            if (encoding == null) { encoding = Encoding.UTF8; }
            using (WebResponse webResponse = request.GetResponse())
            {
                using (Stream stream = webResponse.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(stream, encoding))
                    {
                        string result = streamReader.ReadToEnd();
                        return result;
                    }
                }
            }
        }

        public static string ReadToEnd(this WebException webException, Encoding encoding = null)
        {
            try
            {
                if (encoding == null) encoding = Encoding.UTF8;
                string error = null;
                if (webException.Response == null)
                {
                    error = webException.Message;
                }
                else
                {
                    using (WebResponse? webResponse = webException.Response)
                    {
                        using (Stream stream = webResponse.GetResponseStream())
                        {
                            using (StreamReader streamReader = new StreamReader(stream, encoding))
                            {
                                error = streamReader.ReadToEnd();
                            }
                        }
                    }
                }
                return error;
            }
            catch (Exception e)
            {
                return webException.Message;
            }
        }

        public static void SetHeader(this HttpClient httpClient, Dictionary<string, string> headers)
        {
            if (headers == null) return;
            foreach (KeyValuePair<string, string> header in headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }
}
