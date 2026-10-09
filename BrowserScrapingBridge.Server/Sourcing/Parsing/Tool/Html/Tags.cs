#nullable disable warnings
using HtmlAgilityPack;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Tool.Html
{
    #region Root Tags
    class Html : TagBase
    {
        public override bool IsClose => false;
    }
    class Body : TagBase
    {
        public override bool IsClose => false;
    }
    #endregion

    #region Disable Tags
    class Link : TagBase
    {
        public override bool IsClose => false;
        public override bool IsUse => false;
    }
    class Meta : TagBase
    {
        public override bool IsClose => false;
        public override bool IsUse => false;
    }
    class Head : TagBase
    {
        public override bool IsClose => false;
        public override bool IsUse => false;
    }
    class Style : TagBase
    {
        public override bool IsClose => false;
        public override bool IsUse => false;
    }
    class Script : TagBase
    {
        public override bool IsClose => false;
        public override bool IsUse => false;
    }
    class Noscript : TagBase
    {
        public override bool IsClose => false;
        public override bool IsUse => false;
    }
    class Input : TagBase
    {
        public override bool IsClose => false;
        public override bool IsUse => false;
    }
    class Select : TagBase
    {
        public override bool IsClose => false;
        public override bool IsUse => false;
    }
    class Iframe : TagBase
    {
        public override bool IsClose => false;
        public override bool IsUse => false;
    }
    class Button : TagBase
    {
        public override bool IsClose => false;
        public override bool IsUse => false;
    }
    #endregion

    class A : TagBase
    {
        private void Check(HtmlNode htmlNode)
        {
            HtmlAttribute href = htmlNode.Attributes["href"];
            HtmlAttribute target = htmlNode.Attributes["target"];
            bool isNull = (href == null && target == null);
            IsClose = isNull;
            IsUse = isNull;
        }

        public override string GetTag(HtmlNode htmlNode)
        {
            Check(htmlNode);
            return base.GetTag(htmlNode);
        }

        public override string GetEndTag(HtmlNode htmlNode)
        {
            Check(htmlNode);
            return base.GetEndTag(htmlNode);
        }
    }
    class Img : TagBase
    {
        public override bool IsClose => false;
        public override string GetTag(HtmlNode htmlNode)
        {
            return htmlNode.OuterHtml;
        }
    }

    #region Chart Tags
    class Col : TagBase
    {
        public override string GetTag(HtmlNode htmlNode)
        {
            return base.CreateOriginalTag(htmlNode);
        }
    }
    class Tr : TagBase
    {
        public override string GetTag(HtmlNode htmlNode)
        {
            return base.CreateOriginalTag(htmlNode);
        }
    }
    class Th : TagBase
    {
        public override string GetTag(HtmlNode htmlNode)
        {
            return base.CreateOriginalTag(htmlNode);
        }
    }
    class Td : TagBase
    {
        public override string GetTag(HtmlNode htmlNode)
        {
            return base.CreateOriginalTag(htmlNode);
        }
    }
    class Ul : TagBase
    {
        public override string GetTag(HtmlNode htmlNode)
        {
            return base.CreateOriginalTag(htmlNode);
        }
    }
    class Li : TagBase
    {
        public override string GetTag(HtmlNode htmlNode)
        {
            return base.CreateOriginalTag(htmlNode);
        }
    }
    class Ol : TagBase
    {
        public override string GetTag(HtmlNode htmlNode)
        {
            return base.CreateOriginalTag(htmlNode);
        }
    }
    class Dl : TagBase
    {
        public override string GetTag(HtmlNode htmlNode)
        {
            return base.CreateOriginalTag(htmlNode);
        }
    }
    class Dt : TagBase
    {
        public override string GetTag(HtmlNode htmlNode)
        {
            return base.CreateOriginalTag(htmlNode);
        }
    }
    #endregion

    #region Line Tags
    class Br : TagBase
    {
        public override bool IsClose => false;
        public override string GetTag(HtmlNode htmlNode)
        {
            return base.CreateTag(htmlNode);
        }
    }

    class Hr : TagBase
    {
        public override bool IsClose => false;
        public override string GetTag(HtmlNode htmlNode)
        {
            return base.CreateTag(htmlNode);
        }
    }
    #endregion

    class Div : TagBase
    {
        public override string GetTag(HtmlNode htmlNode)
        {
            return CreateTag(htmlNode);
        }
    }

    class Span : TagBase
    {
        public override string GetTag(HtmlNode htmlNode)
        {
            return CreateTag(htmlNode);
        }
    }

    class Default : TagBase
    {
        public override string GetTag(HtmlNode htmlNode)
        {
            return base.GetTag(htmlNode);
        }
    }
}
