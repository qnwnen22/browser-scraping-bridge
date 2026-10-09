#nullable disable warnings
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;

namespace BrowserScrapingBridge.Server.Sourcing.OwnerClan.Models
{
    /// <summary>
    /// 카테고리 목록
    /// </summary>
    public class GetCategoryListApi : GetBaseApi
    {
        /// <summary>
        /// API Version
        /// </summary>
        public string ver { get; set; } = "1.0";

        /// <summary>
        /// API 호출 모드값
        /// </summary>
        public string mode { get; set; } = "getCategoryList";

        /// <summary>
        /// 등록가능 카테고리 : true | false
        /// </summary>
        public bool? isReg { get; set; }

        /// <summary>
        /// 출력 형식 : json | xml
        /// </summary>
        public string om { get; set; } = "json";

        public GetCategoryListApi() { }
        public GetCategoryListApi(string aid) : base(aid) { }

        private void AddCategory(List<Category> categories, JObject jObject, string name = null)
        {
            string? getName = jObject.Value<string>("name");
            if (string.IsNullOrWhiteSpace(name))
            {
                name = getName;
            }
            else
            {
                name += $">{getName}";
            }

            if (jObject["child"] == null)
            {
                string? code = jObject.Value<string>("code");

                Category category = new Category();
                category.Names = name;
                category.Codes = code;
                categories.Add(category);
            }
            else
            {
                JEnumerable<JToken> children = jObject["child"].Children();
                foreach (var item in children)
                {
                    List<JObject> childrenList = item.Children<JObject>().ToList();
                    for (int i = 0; i < childrenList.Count; i++)
                    {
                        AddCategory(categories, childrenList[i], name);
                    }
                }
            }
        }

        private List<Category> GetCategories(string json)
        {
            var categories = new List<Category>();
            var responseJson = JObject.Parse(json);
            JToken items = responseJson["OwnerClan"]["items"];
            List<JToken> children = items.Children().ToList();
            for (int i = 0; i < children.Count; i++)
            {
                List<JObject> innerChildren = children[i].Children<JObject>().ToList();
                JObject? child = innerChildren.FirstOrDefault();
                AddCategory(categories, child);
            }
            return categories;
        }

        public List<Category> GetCategories()
        {
            string uriString = this.CreateRequestUri();
            var request = BrowserScrapingBridge.Server.Sourcing.Transport.BrowserWebRequest.CreateHttp(uriString);
            string responseJson = request.ReadToEnd();
            List<Category> categories = GetCategories(responseJson);
            return categories;
        }
    }
}
