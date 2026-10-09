#nullable disable warnings
using Newtonsoft.Json;

namespace BrowserScrapingBridge.Server.Sourcing.ShoppingNaver.Models
{
    public class BodyBase
    {
        public virtual string ToJson()
        {
            // 값이 들어있지 않은 프로퍼티는 JSON문자열에서 표시 안함
            var setting = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(this, setting);
        }
    }
}
