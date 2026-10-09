#nullable disable warnings
using System;

namespace BrowserScrapingBridge.Server.Sourcing.Common.Exceptions
{
    public class SocketException : Exception
    {
        public SocketException() : base() { }
        public SocketException(string? message) : base(message) { }
        public SocketException(string? message, Exception? exception) : base(message, exception) { }
    }
}
