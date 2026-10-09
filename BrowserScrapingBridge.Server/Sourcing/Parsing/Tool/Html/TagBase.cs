#nullable disable warnings
using HtmlAgilityPack;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Tool.Html
{
    internal abstract class TagBase
    {
        public virtual bool IsClose { get; protected set; }
        public virtual bool IsUse { get; protected set; }
        public TagBase()
        {
            IsClose = true;
            IsUse = true;
        }
        public virtual string GetTag(HtmlNode htmlNode)
        {
            if (IsUse)
            {
                return CreateTag(htmlNode, "style");
            }
            else
            {
                return null;
            }
        }

        public virtual string GetEndTag(HtmlNode htmlNode)
        {
            if (IsClose)
            {
                return $"</{htmlNode.Name}>";
            }
            else
            {
                return null;
            }
        }

        protected string CutCloseTag(string tag)
        {
            int closeTag = tag.IndexOf("</");
            if (closeTag != -1)
            {
                tag = tag.Substring(0, closeTag);
                if (string.IsNullOrWhiteSpace(tag) == false)
                    tag = tag.Trim();
            }
            return tag;
        }
        protected bool CheckText(HtmlNode htmlNode)
        {
            int first = htmlNode.Name.IndexOf("#");
            bool isTag = first == 0;
            return isTag;
        }



        protected string CreateTag(HtmlNode htmlNode, params string[] attributes)
        {
            bool isText = CheckText(htmlNode);
            IsClose = !isText;
            if (isText)
            {
                return null;
            }

            HtmlAttributeCollection element = htmlNode.Attributes;
            string createTag = $"<{htmlNode.Name}>";

            HtmlNode createNode = HtmlNode.CreateNode(createTag);

            foreach (var attribute in attributes)
            {
                HtmlAttribute style = element[attribute];
                if (style != null)
                {
                    createNode.Attributes.Add(style);
                }
            }

            string result = CutCloseTag(createNode.OuterHtml);
            return result;
        }



        protected string CreateOriginalTag(HtmlNode htmlNode)
        {
            bool isText = CheckText(htmlNode);
            IsClose = !isText;
            if (isText)
            {
                return null;
            }

            HtmlNode create = HtmlNode.CreateNode($"<{htmlNode.Name}>");
            HtmlAttributeCollection attrs = htmlNode.Attributes;
            foreach (var attr in attrs)
            {
                if (attr.Name == "class") continue;
                create.Attributes.Add(attr);
            }
            string result = CutCloseTag(create.OuterHtml);
            return result;
        }
    }
}
