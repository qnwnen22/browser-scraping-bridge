#nullable disable warnings
using System;

namespace BrowserScrapingBridge.Server.Sourcing.Common
{
    public class CaptchaException : Exception
    {
        public CaptchaInfo CaptchaInfo { get; set; }
        public CaptchaException() : base() { }
        public CaptchaException(string? message) : base(message) { }
        public CaptchaException(string? message, Exception? exception) : base(message, exception) { }
    }

    public class CaptchaInfo
    {
        public string Message { get; set; }
        public string Url { get; set; }
    }

}
