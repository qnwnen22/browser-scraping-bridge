#nullable disable warnings
using System;

namespace BrowserScrapingBridge.Server.Sourcing.Musinsa.Models
{
    public class PostLogUserEventRequest
    {
        public class _Data
        {
            public class Default
            {
                public string appName { get; set; } = "";
                public string appVersion { get; set; } = "";
                public string client_id { get; set; } = "420807288.1774842644";
                public string page_title { get; set; } = "로그인/회원가입 | 무신사";
                public string page_location { get; set; } = "https://member.one.musinsa.com/login?entryToken=8eae7cfe666fdf1e86f06652a3b3947c43b966e0";
                public string page_path { get; set; } = "/login";
                public string page_host { get; set; } = "member.one.musinsa.com";
                public string page_hash { get; set; } = "";
                public string page_referrer { get; set; } = "https://www.musinsa.com/";
                public long event_timestamp { get; set; }
                public string event_date { get; set; }
                public Default()
                {
                    event_timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    event_date = DateTime.Now.ToString("yyyyMMdd");
                }
            }
            public Default _default { get; set; } = new Default();
            public class Session
            {
                public string ga_session_id { get; set; } = "s1774842643$o1$g1$t1774844562$j32$l0$h0";
                public object source { get; set; } = null;
                public object medium { get; set; } = null;
                public string campaign { get; set; } = "(not set)";
                public string content { get; set; } = "(not set)";
                public string term { get; set; } = "(not set)";
                public bool first_visit { get; set; } = false;
            }
            public Session session { get; set; } = new Session();
            public class UserProperty
            {
                public string ga_cookie_id { get; set; } = "";
                public string user_status_of_login { get; set; } = "N";
                public string client_id { get; set; } = "420807288.1774842644";
            }
            public UserProperty userProperty { get; set; } = new UserProperty();
            public class EventParameter
            {
                public string channel_type { get; set; } = "PC";
                public string device_os { get; set; } = "WINDOWS";
                public string gender_filter { get; set; } = "A";
                public string products_grid_type { get; set; } = "(not set)";
                public string host_type { get; set; } = "prod";
                public string page_id { get; set; } = "/auth/one/login";
                public string applied_tab { get; set; } = "false'";
                public string device_type { get; set; } = "WEB";
                public object brand { get; set; } = null;
                public string page_link { get; set; } = "(not set)";
                public bool ab_exp { get; set; } = false;
                public string button_id { get; set; } = "login_login";
                public string button_name { get; set; } = "로그인";
                public string category_id { get; set; } = "(not set)";
                public string item_category { get; set; } = "(not set)";
                public object tag_type { get; set; } = null;
                public string index { get; set; } = "(not set)";
                public string section_name { get; set; } = "member_login";
                public string section_title { get; set; } = "(not set)";
                public string section_index { get; set; } = "(not set)";
                public bool extra_info { get; set; } = false;
                public string brand_id { get; set; } = "(not set)";
                public bool landing_url { get; set; } = false;
                public object store_type { get; set; } = null;
                public object used_usedbag_no { get; set; } = null;
                public object used_hanger_no { get; set; } = null;
                public string search_term { get; set; } = "";
            }
            public EventParameter eventParameter { get; set; } = new EventParameter();
            public object ecommerce { get; set; } = null;
            public string event_name { get; set; } = "click_button";
        }
        public _Data Data { get; set; } = new _Data();
        public long PartitionKey { get; set; }

        public PostLogUserEventRequest()
        {
            PartitionKey = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public PostLogUserEventRequest(string client_id, string ga_session_id) : this()
        {
            this.Data._default.client_id = client_id;
            this.Data.userProperty.client_id = client_id;
        }
    }
}
