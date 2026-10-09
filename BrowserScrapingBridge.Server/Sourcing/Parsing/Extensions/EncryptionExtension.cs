#nullable disable warnings
using System.Security.Cryptography;
using System.Text;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class EncryptionExtension
    {
        public static string SHA256Hash(this string data)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes(data));
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in hash)
            {
                stringBuilder.AppendFormat("{0:x2}", b);
            }
            return stringBuilder.ToString();
        }
    }
}
