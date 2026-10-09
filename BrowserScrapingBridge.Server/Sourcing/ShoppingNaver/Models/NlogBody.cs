#nullable disable warnings
using System;
using System.Linq;

namespace BrowserScrapingBridge.Server.Sourcing.ShoppingNaver.Models
{
    public class NlogBody
    {
        public string corp { get; set; }
        public string svc { get; set; }
        public string location { get; set; }
        public class Svc_Tags
        {
        }
        public Svc_Tags svc_tags { get; set; }
        public long send_ts { get; set; }
        public class Tool
        {
            public string name { get; set; }
            public string ver { get; set; }
        }
        public Tool tool { get; set; }
        public class Usr
        {
        }
        public Usr usr { get; set; }
        public class Env
        {
            public string os { get; set; }
            public string br_ln { get; set; }
            public string br_sr { get; set; }
            public string device_sr { get; set; }
            public string platform_type { get; set; }
            public int device_pr { get; set; }
            public string timezone { get; set; }
            public string ch_pltf { get; set; }
            public bool ch_mob { get; set; }
            public string ch_mdl { get; set; }
            public string ch_arch { get; set; }
            public string ch_pltfv { get; set; }
            public class Ch_Fvls
            {
                public string brand { get; set; }
                public string version { get; set; }
            }
            public Ch_Fvls[] ch_fvls { get; set; }
        }
        public Env env { get; set; }
        public class Evt
        {
            public string page_url { get; set; }
            public string page_ref { get; set; }
            public string page_id { get; set; }
            public class Timing
            {
                public string type { get; set; }
                public int unloadEventStart { get; set; }
                public int unloadEventEnd { get; set; }
                public int redirectStart { get; set; }
                public int redirectEnd { get; set; }
                public int workerStart { get; set; }
                public float fetchStart { get; set; }
                public float domainLookupStart { get; set; }
                public float domainLookupEnd { get; set; }
                public float connectStart { get; set; }
                public float secureConnectionStart { get; set; }
                public float connectEnd { get; set; }
                public float requestStart { get; set; }
                public float responseStart { get; set; }
                public float responseEnd { get; set; }
                public float domInteractive { get; set; }
                public float domContentLoadedEventStart { get; set; }
                public float domContentLoadedEventEnd { get; set; }
                public int domComplete { get; set; }
                public int loadEventStart { get; set; }
                public int loadEventEnd { get; set; }
                public int first_paint { get; set; }
                public int first_contentful_paint { get; set; }
            }
            public Timing timing { get; set; }
            public string type { get; set; }
            public string page_sti { get; set; }
            public string shp_action_uid { get; set; }
            public class Env
            {
                public string device_type { get; set; }
            }
            public Env env { get; set; }
            public string shp_pagekey { get; set; }
            public class Shp
            {
                public class Contents
                {
                }
                public Contents contents { get; set; }
            }
            public Shp shp { get; set; }
            public long evt_ts { get; set; }
        }
        public Evt[] evts { get; set; }
        private NlogBody() { }

        public static NlogBody CreateInstance()
        {
            string body = "{\"corp\":\"naver\",\"svc\":\"shopping\",\"location\":\"korea_real/korea\",\"svc_tags\":{},\"send_ts\":1742529910813,\"tool\":{\"name\":\"ntm-web\",\"ver\":\"nlogLibVersion=v0.1.42; verName=v2.0.9; ntmVersion=v1.4.1\"},\"usr\":{},\"env\":{\"os\":\"Win32\",\"br_ln\":\"ko\",\"br_sr\":\"1905x919\",\"device_sr\":\"1920x1080\",\"platform_type\":\"web\",\"device_pr\":1,\"timezone\":\"Asia/Seoul\",\"ch_pltf\":\"Windows\",\"ch_mob\":false,\"ch_mdl\":\"\",\"ch_arch\":\"x86\",\"ch_pltfv\":\"10.0.0\",\"ch_fvls\":[{\"brand\":\"Chromium\",\"version\":\"134.0.6998.118\"},{\"brand\":\"Not:A-Brand\",\"version\":\"24.0.0.0\"},{\"brand\":\"Google Chrome\",\"version\":\"134.0.6998.118\"}]},\"evts\":[{\"page_url\":\"https://shopping.naver.com/ns/home\",\"page_ref\":\"\",\"page_id\":\"9ce8965040d2accd3d5aec7b9d06f8b0\",\"timing\":{\"type\":\"navigate\",\"unloadEventStart\":0,\"unloadEventEnd\":0,\"redirectStart\":0,\"redirectEnd\":0,\"workerStart\":0,\"fetchStart\":2.2999999821186066,\"domainLookupStart\":2.2999999821186066,\"domainLookupEnd\":2.2999999821186066,\"connectStart\":4.299999982118607,\"secureConnectionStart\":11.599999994039536,\"connectEnd\":17.799999982118607,\"requestStart\":17.799999982118607,\"responseStart\":54.900000005960464,\"responseEnd\":55.599999994039536,\"domInteractive\":400.19999998807907,\"domContentLoadedEventStart\":680.7999999821186,\"domContentLoadedEventEnd\":680.7999999821186,\"domComplete\":0,\"loadEventStart\":0,\"loadEventEnd\":0,\"first_paint\":420,\"first_contentful_paint\":420},\"type\":\"pageview\",\"page_sti\":\"shopping\",\"shp_action_uid\":\"\",\"env\":{\"device_type\":\"PC Web\"},\"shp_pagekey\":\"100410625\",\"shp\":{\"contents\":{}},\"evt_ts\":1742529909971}]}";
            NlogBody newInstance = Newtonsoft.Json.JsonConvert.DeserializeObject<NlogBody>(body);
            newInstance.send_ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            newInstance.evts.First().evt_ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return newInstance;
        }

        public string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}
