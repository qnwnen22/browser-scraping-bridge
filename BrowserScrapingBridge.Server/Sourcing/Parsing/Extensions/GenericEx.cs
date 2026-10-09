#nullable disable warnings
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class GenericEx
    {
        public static bool IsNotNullGeneric<T>(this List<T> obj)
        {
            if (obj != null && obj.Count > 0) return true;
            else return false;
        }
        public static List<T> Shuffle<T>(this List<T> list)
        {
            Random random = new Random();

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }
        public static List<T> Shuffle<T>(this List<T> list, int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= list.Count || toIndex < 0 || toIndex > list.Count) return list;

            T temp = list[fromIndex];

            if (fromIndex < toIndex)
            {
                for (int i = fromIndex; i < toIndex; i++)
                {
                    list[i] = list[i + 1];
                }
            }
            else if (fromIndex > toIndex)
            {
                for (int i = fromIndex; i > toIndex; i--)
                {
                    list[i] = list[i - 1];
                }
            }

            list[toIndex] = temp;
            return list;
        }
        public static T ToJson<T>(this string jsonString, out Exception exception)
        {
            exception = null;
            try
            {
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            catch (Exception e) { exception = e; }
            return default(T);
        }
        public static List<string> FindPropertyValue(this JToken token, string propertyName)
        {
            List<string> fullNames = new List<string>();

            if (token.Type == JTokenType.Object)
            {
                foreach (JProperty property in token.Children<JProperty>())
                {
                    if (property.Name == propertyName)
                    {
                        fullNames.Add((string)property.Value);
                    }
                    else
                    {
                        fullNames.AddRange(FindPropertyValue(property.Value, propertyName));
                    }
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (JToken arrayItem in token.Children())
                {
                    fullNames.AddRange(FindPropertyValue(arrayItem, propertyName));
                }
            }

            return fullNames;
        }
        public static List<string> FindPropertyTargetValue(this JToken token, string key, string value, string targetKey)
        {
            List<string> result = new List<string>();

            try
            {
                foreach (JObject jObject in token)
                {
                    string test = jObject.Value<string>(key);
                    if (jObject.Value<string>(key) != value) continue;
                    if (jObject.Value<string>(targetKey) == null) continue;
                    
                    result.Add(jObject.Value<string>(targetKey));
                }
            }
            catch(Exception ex) { ex.Show(); }

            return result;
        }
        //public static List<JObject> FindPropertyTargetValue(this JToken token, string propertyName, string targetName)
        //{
        //    List<JObject> result = new List<JObject>();

        //    foreach (JObject jObject in token)
        //    {
        //        string name = jObject.Value<string>(propertyName);
        //        string value = jObject.Value<string>(targetName);

        //        if (string.IsNullOrWhiteSpace(name) == false && string.IsNullOrWhiteSpace(value) == false) result.Add(jObject);
        //    }

        //    return result;
        //}
        public static T ToReplication<T>(this T obj)
        {
            try
            {
                string temp = JsonConvert.SerializeObject(obj);

                return JsonConvert.DeserializeObject<T>(temp);
            }
            catch (Exception exception) { }
            return default;
        }
        public static List<List<T>> GenerateCombinations<T>(this List<List<T>> input)
        {
            List<List<T>> result = new List<List<T>>();
            GenerateCombinationsHelper(input, 0, new List<T>(), result);
            return result;
        }

        public static void GenerateCombinationsHelper<T>(List<List<T>> input, int depth, List<T> current, List<List<T>> result)
        {
            if (depth == input.Count)
            {
                // 현재 조합을 결과에 추가
                result.Add(new List<T>(current));
                return;
            }

            // 현재 리스트의 모든 요소를 현재 조합에 추가하고 재귀 호출
            foreach (var item in input[depth])
            {
                current.Add(item);
                GenerateCombinationsHelper(input, depth + 1, current, result);
                current.RemoveAt(current.Count - 1); // 백트래킹
            }
        }
        public static List<List<T>> Division<T>(this List<T> input, int chunkSize)
        {
            return Enumerable.Range(0, input.Count / chunkSize + 1).Select(i => input.Skip(i * chunkSize).Take(chunkSize).ToList()).ToList();
        }
    }
}
