#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Common;

namespace BrowserScrapingBridge.Server.Sourcing.Models.Product
{
    public class Desc
    {
        public int index { get; set; }
        public string language { get; set; }
        public string title { get; set; }
        public string value { get; set; }
        public Desc() { }
        public Desc(int index, Language language, string title, string value)
        {
            this.index = index;
            this.language = language.ToString();
            this.title = title;
            this.value = value;
        }
        public Desc(int index, string language, string title, string value)
        {
            this.index = index;
            this.language = language;
            this.title = title;
            this.value = value;
        }
    }
}
