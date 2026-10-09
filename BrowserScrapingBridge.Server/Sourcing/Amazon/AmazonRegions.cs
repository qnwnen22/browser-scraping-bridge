#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;
using BrowserScrapingBridge.Server.Sourcing.Common;

namespace BrowserScrapingBridge.Server.Sourcing.Amazon
{
    public class AmazonUS : AmazonV2
    {
        public override string Site { get; } = "Amazon-US";
        public override Currency Currency { get; set; } = Currency.USD;
        public override Language Language { get; set; } = Language.EN;
        public override string Host => "www.amazon.com";
        public override string Name { get; } = "아마존 (US)";
        public override string Zipcode => "19701";
        public override string Lop => null;
        public override string Cop => null;
    }

    public class AmazonJP : AmazonV2
    {
        public override string Site { get; } = "Amazon-JP";
        public override Currency Currency { get; set; } = Currency.JPY;
        public override Language Language { get; set; } = Language.JP;
        public override string Host => "www.amazon.co.jp";
        public override string Name { get; } = "아마존 (JP)";
        public override string Zipcode => "273-0017";
        public override string Lop => null;
        public override string Cop => null;
    }

    public class AmazonDE : AmazonV2
    {
        public override string Site { get; } = "Amazon-DE";
        public override Currency Currency { get; set; } = Currency.EUR;
        public override Language Language { get; set; } = Language.EN;
        public override string Host => "www.amazon.de";
        public override string Name { get; } = "아마존 (DE)";
        public override string Zipcode => "64546";
        // public override string Lop => "de_DE";
        public override string Lop => "en_GB"; // 영문 설정
        public override string Cop => "EUR";
    }

    public class AmazonAU : AmazonV2
    {
        public override string Site { get; } = "Amazon-AU";
        public override Currency Currency { get; set; } = Currency.AUD;
        public override Language Language { get; set; } = Language.EN;
        public override string Host => "www.amazon.com.au";
        public override string Name { get; } = "아마존 (AU)";
        public override string Zipcode => "4215";
        public override string Lop => null;
        public override string Cop => null;

        public override string DefaultPostData()
        {
            string result = new
            {
                locationType = "POSTAL_CODE_WITH_CITY",
                zipCode = this.Zipcode,
                city = "SOUTHPORT",
                deviceType = "web",
                storeContext = "generic",
                pageType = "Gateway",
                actionSource = "glow",
            }.ToSerializeObject();

            return result;
        }
    }

    public class AmazonFR : AmazonV2
    {
        public override string Site { get; } = "Amazon-FR";
        public override Currency Currency { get; set; } = Currency.EUR;
        public override Language Language { get; set; } = Language.EN;
        public override string Host => "www.amazon.fr";
        public override string Name { get; } = "아마존 (FR)";
        public override string Zipcode => "77700";
        public override string Lop => null;
        public override string Cop => null;
    }

    public class AmazonUK : AmazonV2
    {
        public override string Site { get; } = "Amazon-UK";
        public override Currency Currency { get; set; } = Currency.GBP;
        public override Language Language { get; set; } = Language.EN;
        public override string Host => "www.amazon.co.uk";
        public override string Name { get; } = "아마존 (UK)";
        public override string Zipcode => "SL1 4NL";
        public override string Lop => "en_GB";
        public override string Cop => "GBP";

        public override string DefaultPostData()
        {
            string result = new
            {
                locationType = "POSTAL_CODE_WITH_CITY",
                zipCode = this.Zipcode,
                city = "SLOUGH",
                deviceType = "web",
                storeContext = "generic",
                pageType = "Gateway",
                actionSource = "glow",
            }.ToSerializeObject();

            return result;
        }
    }
}
