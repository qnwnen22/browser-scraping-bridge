#nullable disable warnings
using System;

namespace BrowserScrapingBridge.Server.Sourcing.Common.Exceptions
{
    public class LogException : Exception
    {
        public LogException() : base() { }
        public LogException(string? message) : base(message) { }
        public LogException(string? message, Exception? exception) : base(message, exception) { }
    }
}
