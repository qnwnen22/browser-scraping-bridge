#nullable disable warnings
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class JsonExtension
    {
        public static string ToSerializeObject<T>(this T t)
        {
            if (t == null) return null;
            return JsonConvert.SerializeObject(t);
        }

        public static string ToSerializeObject<T>(this T t, Formatting formatting)
        {
            if (t == null) return null;
            return JsonConvert.SerializeObject(t, formatting);
        }

        public static string ToSerializeObject<T>(this T t, NullValueHandling nullValueHandling)
        {
            if (t == null) return null;
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = nullValueHandling
            };
            return JsonConvert.SerializeObject(t, settings);
        }

        public static T? ToDeserializeObject<T>(this string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return default;
                var deserializeObject = JsonConvert.DeserializeObject<T>(json);
                return deserializeObject;
            }
            catch (Exception e)
            {
                return default;
            }
        }

        public static T DeepCopyObject<T>(this T t)
        {
            string json = t.ToSerializeObject();
            T? result = json.ToDeserializeObject<T>();
            return result;
        }

        public static JObject ToJObject(this string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JObject.Parse(json);
            }
            catch
            {
                return null;
            }
        }

        public static JArray ToJArray(this string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JArray.Parse(json);
            }
            catch
            {
                return null;
            }
        }

        public static JToken ToJToken(this string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JToken.Parse(json);
            }
            catch
            {
                return null;
            }
        }

        public static JToken Value(this JToken token, params string[] keys)
        {
            if (token == null) return null;
            JToken result = token;
            foreach (string key in keys)
            {
                if (key == null)
                {
                    result = null;
                    break;
                }
                JToken get = result[key];
                if (get != null && get.HasValues)
                {
                    result = get;
                }
                else
                {
                    result = null;
                    break;
                }
            }
            return result;
        }
    }
}
