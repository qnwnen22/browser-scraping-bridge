#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Models.Product;
using System;

namespace BrowserScrapingBridge.Server.Sourcing.Common
{
    public class CollectException : Exception
    {
        public ProductCollectStatus CollectStatus { get; set; }

        public CollectException() : base() { }
        public CollectException(string message) : base(message) { }
        public CollectException(string message, Exception? exception) : base(message, exception) { }
        public CollectException(ProductCollectStatus collectStatus, string message) : base(message)
        {
            CollectStatus = collectStatus;
        }
    }
}
