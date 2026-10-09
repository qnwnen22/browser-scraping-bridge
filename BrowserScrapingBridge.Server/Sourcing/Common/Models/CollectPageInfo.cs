#nullable disable warnings
namespace BrowserScrapingBridge.Server.Sourcing.Common.Models
{
    public class CollectPageInfo
    {
        public string Url { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }

        public override bool Equals(object? obj)
        {
            var collectPageInfo = obj as CollectPageInfo;
            if (collectPageInfo == null) return false;
            if (string.IsNullOrWhiteSpace(this.Code) == false && collectPageInfo.Code == this.Code) return true;
            if (string.IsNullOrWhiteSpace(this.Title) == false && collectPageInfo.Title == this.Title) return true;
            if (string.IsNullOrWhiteSpace(this.Url) == false && collectPageInfo.Url == this.Url) return true;
            return false;
        }
    }
}
