#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Common;

namespace BrowserScrapingBridge.Server.Sourcing.Models.Product
{
    public class Price
    {        public string isoCode { get; set; }
        public string currency { get; set; }
        public double originalPrice { get; set; }
        public double salePrice { get; set; }
        public bool isdefalutPrice { get; set; }

        public Price()
        {
           
        }

        public Price(string currency, double originalPrice, double salePrice)
        {
            this.currency = currency;
            this.originalPrice = originalPrice;
            this.salePrice = salePrice;
        }

        public Price(Currency currency, double originalPrice, double salePrice)
        {
            this.currency = currency.ToString();
            this.originalPrice = originalPrice;
            this.salePrice = salePrice;

        }

        public Price(Currency currency, double originalPrice, double salePrice, bool isdefalutPrice)
        {
            this.currency = currency.ToString();
            this.originalPrice = originalPrice;
            this.salePrice = salePrice;
            this.isdefalutPrice = isdefalutPrice;
        }
    }
}
