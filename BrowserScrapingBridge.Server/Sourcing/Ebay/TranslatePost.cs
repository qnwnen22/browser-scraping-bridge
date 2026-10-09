#nullable disable warnings
using System;
using System.Collections.Generic;

namespace BrowserScrapingBridge.Server.Sourcing.Ebay
{
    public class TranslatePost
    {
        public class Ctx
        {
            public string bt { get; set; } = "1d53c387";
            public string c1 { get; set; } = "fd9ac76f-6f16-4a03-81b1-5e950654d5f7";
            public string lc { get; set; } = "bc4ba16b-f925-4f5d-94e8-e70df6ffadfc";
            public string ls { get; set; } = "5b548814-1430-48b3-b796-6c4300ad7ba1";
            public string r { get; set; } = "93670c1f-95a8-480f-96dc-f158aa3225b5";
            public string si { get; set; } = "3mL9msdvbxYIA_2xXpUGVNX3uRnjqvRMGY0TmEIdW1SIFBQwtrMIlmxDAK17oRfTpF-pZic7HeHSPDN6ZQOmQFBf3W3y1mWp5c__-YFUgL29znLq2uq8S6Fr-SVXXTfo5w32_638eMrNvVPoGKSTZwwflagCDy3c8ViqMiW1X-YoYQW_";
            public string ss { get; set; } = "337a6503-a640-465f-b56d-f2d665a9e5cf";
            public bool bg { get; set; } = true;
            public bool df { get; set; } = true;
            public string rf { get; set; } = "https://www.ebay.com/";
            public int sh { get; set; } = 1440;
            public int sw { get; set; } = 2560;
            public string ua { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36";
            public string url { get; set; } = "https://www.ebay.com/";
            public string v { get; set; } = "3.46.2-2505191832";
            public List<int> a { get; set; } = new List<int> { 1, 25, 26, 37, 0 };
            public List<int> p { get; set; } = new List<int> { 857, 857, 840, 945, 1572 };
            public int pv { get; set; } = 2;
            public int rd { get; set; } = 7288;
            public long sa { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            public int sps { get; set; } = 5716;
            public long ts { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            public string d_a { get; set; } = "x86";
            public string d_bvs { get; set; } = "[,{\"brand\":\"Chromium\",\"version\":\"136.0.7103.114\"}{\"brand\":\"Google Chrome\",\"version\":\"136.0.7103.114\"}{\"brand\":\"Not.A/Brand\",\"version\":\"99.0.0.0\"}]";
            public string d_ot { get; set; } = "Windows";
            public string d_os { get; set; } = "10.0.0";
            public bool huah { get; set; } = true;
            public List<object> cbt { get; set; } = new List<object>();
        }
        public Ctx ctx { get; set; } = new();
        public class Req
        {
            public class Md
            {
                public List<string> pids { get; set; } = new List<string>() { "192d9db6-f3e3-45a3-81fb-65af3d16cc9a" };
                public bool ps { get; set; } = false;
            }
            public Md md { get; set; } = new();
            public int del { get; set; } = 0;
        }
        public List<Req> req { get; set; } = new List<Req>() { new Req() };
        public string u_scsid { get; set; } = "337a6503-a640-465f-b56d-f2d665a9e5cf";
    }
}
