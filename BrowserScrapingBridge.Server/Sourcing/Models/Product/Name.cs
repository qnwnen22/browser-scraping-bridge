#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Common;

namespace BrowserScrapingBridge.Server.Sourcing.Models.Product
{
    public class Name
    {
        public string language { get; set; }
        public string text { get; set; }

        public Name()
        {
        }


        public Name(string language, string text)
        {
            this.language = language;
            this.text = text;
        }
        
        public Name(Language language, string text)
        {
            this.language = language.ToString();
            this.text = text;
        }
    }
}
