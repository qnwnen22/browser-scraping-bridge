#nullable disable warnings
namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class EnumExtension
    {
        public static T ConvertEnum<T>(this string text) where T : Enum
        {
            object result = null;
            bool isTryParse = Enum.TryParse(typeof(T), text, out result);
            if (isTryParse == false) return default;
            return (T)result;
        }
    }
}
