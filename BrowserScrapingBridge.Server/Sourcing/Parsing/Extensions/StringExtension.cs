#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool;
using System.Net;
using System.Web;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class StringExtension
    {
        /// <summary>
        /// 전체 문자열 중 시작 문자열과 마지막 문자열을 입력받아 반복 검색합니다.
        /// </summary>
        /// <param name="text">전체 문자열</param>
        /// <param name="startWord">시작 문자열</param>
        /// <param name="endWord">마지막 문자열</param>
        /// <param name="isInclude">문자열 포함 여부</param>
        /// <returns></returns>
        public static List<string> Substrings(this string text, string startWord, string endWord, bool isInclude = true)
        {
            var list = new List<string>();

            int nextStartIndex = 0;
            while (true)
            {
                int startIndex = text.IndexOf(startWord, nextStartIndex);
                if (startIndex < 0) break;
                int index1 = startIndex + startWord.Length;

                int endIndex = text.IndexOf(endWord, startIndex);
                if (endIndex < 0) break;
                int index2 = endIndex - index1;

                string substring = text.Substring(index1, index2);
                if (isInclude)
                {
                    substring = startWord + substring + endWord;
                }
                list.Add(substring);

                nextStartIndex = (endIndex + endWord.Length);
            }
            return list;
        }


        public static string SubstringBetween(this string text, string start, string end, bool isInclude = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            int startIdx = text.IndexOf(start);
            if (startIdx < 0) return null;
            if (!isInclude) { startIdx += start.Length; }
            text = text.Substring(startIdx);
            int lastIdx = text.LastIndexOf(end);
            if (lastIdx < 0) return null;
            if (isInclude) { lastIdx += end.Length; }
            text = text.Substring(0, lastIdx);
            return text;
        }

        public static string GetUriString(this string urlString, bool isReomveQuery = false)
        {
            if (string.IsNullOrWhiteSpace(urlString)) return null;
            try
            {
                var uri = new Uri(urlString);
                if (uri.IsFile)
                {
                    urlString = $"https:{urlString}";
                }
                if (isReomveQuery)
                {
                    var indexOf = urlString.IndexOf("?");
                    if (indexOf > 0)
                    {
                        urlString.Substring(0, indexOf);
                    }
                }
                return urlString;
            }
            catch (Exception e)
            {
                DebugConsole.WriteLine(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Web URL 디코딩, 더이상 디코딩이 필요 없을 때 까지 디코딩
        /// </summary>
        /// <param name="text">디코딩할 문자열</param>
        /// <returns></returns>
        public static string ToWebUrlDecodeComplete(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string urlDecode = WebUtility.UrlDecode(text);
            if (text == urlDecode) return urlDecode;
            else
            {
                return ToWebUrlDecodeComplete(urlDecode);
            }
        }

        /// <summary>
        /// Http URL 디코딩, 더이상 디코딩이 필요 없을 때 까지 디코딩
        /// </summary>
        /// <param name="text">디코딩할 문자열</param>
        /// <returns></returns>
        public static string ToHttpUrlDecodeComplete(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string urlDecode = HttpUtility.UrlDecode(text);
            if (text == urlDecode) return urlDecode;
            else
            {
                return ToHttpUrlDecodeComplete(urlDecode);
            }
        }

        /// <summary>
        /// Web URL 인코딩
        /// </summary>
        /// <param name="text">인코딩할 문자열</param>
        /// <param name="count">시도할 횟수</param>
        /// <returns></returns>
        public static string ToWebUrlEncoding(this string text, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string result = text;
            for (int i = 0; i < count; i++)
            {
                result = WebUtility.UrlEncode(result);
            }
            return result;
        }

        /// <summary>
        /// Http URL 인코딩
        /// </summary>
        /// <param name="text">인코딩할 문자열</param>
        /// <param name="count">시도할 횟수</param>
        /// <returns></returns>
        public static string ToHttpUrlEncoding(this string text, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string result = text;
            for (int i = 0; i < count; i++)
            {
                result = HttpUtility.UrlEncode(result);
            }
            return result;
        }

        /// <summary>
        /// Http URL 인코딩
        /// </summary>
        /// <param name="text">인코딩할 문자열</param>
        /// <param name="count">시도할 횟수</param>
        /// <returns></returns>
        public static string ToUriEscapeDataString(this string text, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string result = text;
            for (int i = 0; i < count; i++)
            {
                result = Uri.EscapeDataString(result);
            }
            return result;
        }

        public static string ToHtmlDecodeComplete(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string htmlDecode = HttpUtility.HtmlDecode(text);
            if (text == htmlDecode) return htmlDecode;
            else
            {
                return ToHtmlDecodeComplete(htmlDecode);
            }
        }

        public static string ToHtmlDecode(this string text, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string result = text;
            for (int i = 0; i < count; i++)
            {
                result = HttpUtility.HtmlDecode(result);
            }
            return result;
        }

        public static string Join<T>(this IEnumerable<T?> list, string? separator = "")
        {
            return string.Join(separator, list);
        }

        public static string NormalizeSpaces(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            List<string> keywords = text.Trim().Split(" ").ToList();
            keywords = keywords.FindAll(x => string.IsNullOrWhiteSpace(x) == false);
            string result = string.Join(" ", keywords);
            return result;
        }
    }
}
