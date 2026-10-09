#nullable disable warnings
using System.Text.RegularExpressions;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class RegexExtension
    {
        public static double? ParseEurToDouble(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string replaceTxt = Regex.Replace(value, @"[^\d,]", "");
            replaceTxt = replaceTxt.Replace(",", ".");
            double result;
            if (double.TryParse(replaceTxt, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public static double? ParseToDouble(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string replaceTxt = Regex.Replace(value, @"[^\d.]", "");
            double result;
            if (double.TryParse(replaceTxt, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public static bool IsPhoneNumber(this string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;
            var regex = new Regex("^[0-9-]+$");
            bool isMatch = regex.IsMatch(phoneNumber);
            return isMatch;
        }
    }
}
