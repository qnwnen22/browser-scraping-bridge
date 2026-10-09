#nullable disable warnings
using System.Text;
using HtmlAgilityPack;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Tool.Html;
using static BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions.TextEx;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Tool
{
    public class HtmlReader
    {
        public static string WrapHtml(string html, WrapStyle wrapStyle)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<div style='text-align: {wrapStyle.ToString()}'>");
            sb.AppendLine(html);
            sb.AppendLine("</div>");
            return sb.ToString();
        }


        /// <summary>
        /// HTML 문자열을 읽어서 속성 값을 제거한 순수 HTML로 반환합니다.
        /// </summary>
        /// <param name="html">입력받을 HTML문자열</param>
        /// <returns>순수 HTML 문자열</returns>
        public static string GetDafaultHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            HtmlNode htmlNode = htmlDocument.DocumentNode;
            return GetDafaultHtml(htmlNode);
        }

        public static string CutCloseTag(string tag)
        {
            int closeTag = tag.IndexOf("</");
            if (closeTag != -1)
            {
                tag = tag.Substring(0, closeTag);
                if (string.IsNullOrWhiteSpace(tag) == false) tag = tag.Trim();
            }
            return tag;
        }

        private static dynamic instance = new object();

        public static string GetDafaultHtml(HtmlNode htmlNode)
        {
            var sb = new StringBuilder();

            TagBase baseClass = null;

            Type tagType = typeof(TagBase);

            string firtChar = htmlNode.Name.Substring(0, 1).ToUpper();
            string RestString = htmlNode.Name.Substring(1);
            string className = firtChar + RestString;

            Type getType = Type.GetType($"{tagType.Namespace}.{className}");
            if (getType == null)
            {
                baseClass = new Default();
            }
            else
            {
                if (getType.IsSubclassOf(tagType))
                {
                    instance = Activator.CreateInstance(getType);
                    baseClass = instance;
                }
                else
                {
                    throw new Exception($"{nameof(TagBase)}를 상속받지 않은 객체입니다.");
                }
            }

            string tag = baseClass.GetTag(htmlNode);
            sb.AppendLine(tag);

            if (htmlNode.HasChildNodes && baseClass.IsUse)
            {
                HtmlNodeCollection childNodes = htmlNode.ChildNodes;

                foreach (HtmlNode childNode in childNodes)
                {
                    if (childNode.Name == "#text")
                    {
                        string innerText = childNode.InnerText;
                        if (string.IsNullOrWhiteSpace(innerText) == false)
                        {
                            innerText = innerText.Trim();
                            sb.AppendLine($"{innerText}");
                        }
                    }
                    else
                    {
                        string result = GetDafaultHtml(childNode);
                        if (string.IsNullOrWhiteSpace(result) == false)
                        {
                            sb.AppendLine(result);
                        }
                    }
                }
            }

            string endTag = baseClass.GetEndTag(htmlNode);
            sb.Append(endTag);
            return sb.ToString();
        }
        /// <summary>
        /// html에서 img url만 추출해서 List<string> 전달</string>
        /// </summary>
        /// <param name="product"></param>
        /// <param name="html"></param>
        public static List<string> GetimgUrl(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {               
                return null;
            }
            string dafaultHtml = HtmlReader.GetDafaultHtml(html);
            if (string.IsNullOrWhiteSpace(dafaultHtml))
            {
                return null;
            }
            else
            {
                List<string> imgurls = new List<string>();

                List<string> imgs = dafaultHtml.GetValues("//img", XPathOption.OuterHtml);
                if (imgs != null || imgs.Count > 0)
                {
                   
                    foreach (string img in imgs)
                    {
                        string src = img.GetValue("//img", "src");
                        if (src.IndexOf("http") < 0)
                        {
                            src = "https://" + src;
                        }
                        imgurls.Add(src);
                    }
                }

                return imgurls;
            }
        }


        public static string SetAttribute(string html, string tag, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;
            List<string> tags = html.Split('\n').ToList();
            foreach (var item in tags)
            {
                var htmlNode = HtmlNode.CreateNode(item);
                if (htmlNode.Name == tag)
                {
                    HtmlAttributeCollection attrs = htmlNode.Attributes;
                    HtmlAttribute style = attrs[name];
                    if (style == null)
                    {
                        attrs.Add(name, value);
                    }
                    else
                    {
                        style.Value = value;
                    }

                    int first = item.IndexOf("<");
                    string pust = item.Substring(first);

                    int last = pust.LastIndexOf(">");
                    string cut = pust.Substring(0, last + 1);

                    string text = htmlNode.OuterHtml;
                    text = CutCloseTag(text);

                    html = html.Replace(cut, text);
                }
            }
            return html;
        }

        public static string RemoveAttribute(string html, string tag, string name)
        {
            List<string> tags = html.Split('\n').ToList();
            foreach (var item in tags)
            {
                var htmlNode = HtmlNode.CreateNode(item);
                if (htmlNode.Name == tag)
                {
                    HtmlAttributeCollection attrs = htmlNode.Attributes;
                    HtmlAttribute style = attrs[name];
                    if (style != null)
                    {
                        attrs.Remove(style);

                        int first = item.IndexOf("<");
                        string pust = item.Substring(first);

                        int last = pust.LastIndexOf(">");
                        string cut = pust.Substring(0, last + 1);

                        string str = htmlNode.OuterHtml;
                        str = CutCloseTag(str);

                        html = html.Replace(cut, str);
                    }

                }
            }
            return html;
        }
    }
}
