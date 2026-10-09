#nullable disable warnings
namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class NullableExtension
    {
        public static string ToString(this DateTime? dateTime, string format)
        {
            if (dateTime == null) return null;
            var tempDate = (DateTime)dateTime;
            return tempDate.ToString(format);
        }
    }
}
