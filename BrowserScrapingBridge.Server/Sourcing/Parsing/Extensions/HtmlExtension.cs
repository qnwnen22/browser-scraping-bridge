#nullable disable warnings
using HtmlAgilityPack;
using System.Text;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public enum HtmlType
    {
        InnerText,
        InnerHtml,
        OuterHtml,
    }

    public static class HtmlExtension
    {
        public static HtmlNode SelectNode(this string html, string xpath)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            HtmlNode htmlNode = htmlDocument.DocumentNode.SelectSingleNode(xpath);
            return htmlNode;
        }

        public static string SelectNode(this string html, string xpath, HtmlType htmlType)
        {
            HtmlNode selectNode = html.SelectNode(xpath);
            if (selectNode == null) return null;

            string result = null;
            switch (htmlType)
            {
                case HtmlType.InnerText:
                    result = selectNode.InnerText;
                    break;
                case HtmlType.InnerHtml:
                    result = selectNode.InnerHtml;
                    break;
                case HtmlType.OuterHtml:
                    result = selectNode.OuterHtml;
                    break;
            }
            return result;
        }

        public static string SelectNode(this string html, string xpath, string value)
        {
            HtmlNode selectNode = html.SelectNode(xpath);
            if (selectNode == null) return null;

            HtmlAttributeCollection attres = selectNode.Attributes;
            string result = attres[value]?.Value;
            return result;
        }

        public static HtmlNodeCollection SelectNodes(this string html, string xpath)
        {
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            HtmlNodeCollection selectNodes = htmlDocument.DocumentNode.SelectNodes(xpath);
            return selectNodes;
        }

        public static List<string> SelectNodes(this string html, string xpath, HtmlType htmlType)
        {
            HtmlNodeCollection selectNodes = html.SelectNodes(xpath);
            if (selectNodes == null || selectNodes.Count <= 0) return null;

            var nodes = new List<string>();

            foreach (var selectNode in selectNodes)
            {
                switch (htmlType)
                {
                    case HtmlType.InnerText:
                        nodes.Add(selectNode.InnerText);
                        break;
                    case HtmlType.InnerHtml:
                        nodes.Add(selectNode.InnerHtml);
                        break;
                    case HtmlType.OuterHtml:
                        nodes.Add(selectNode.OuterHtml);
                        break;
                }
            }
            if (nodes.Count <= 0) return null;
            return nodes;
        }

        public static List<string> SelectNodes(this string html, string xpath, string value)
        {
            HtmlNodeCollection selectNodes = html.SelectNodes(xpath);
            if (selectNodes == null || selectNodes.Count <= 0) return null;

            var list = new List<string>();
            foreach (var selectNode in selectNodes)
            {
                string attriebuteValue = selectNode.Attributes[value]?.Value;
                if (string.IsNullOrWhiteSpace(attriebuteValue)) continue;
                list.Add(attriebuteValue);
            }
            if (list.Count <= 0) return null;
            return list;
        }

        public static string GetSelectNodeInnerText(this string html, string xpath)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            HtmlNode documentNode = htmlDocument.DocumentNode;
            HtmlNode node = documentNode.SelectSingleNode(xpath);
            if (node == null) return null;
            HtmlNodeCollection childNodes = node.ChildNodes;

            var sb = new StringBuilder();
            foreach (HtmlNode? childNode in childNodes)
            {
                if (childNode.Name != "#text") continue;
                sb.Append(childNode.InnerText);
            }

            return sb.ToString();
        }
    }
}
