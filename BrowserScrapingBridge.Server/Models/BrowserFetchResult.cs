namespace BrowserScrapingBridge.Server.Models;

public sealed record BrowserFetchResult(
    string RequestId,
    string Url,
    string FinalUrl,
    string Title,
    int? HttpStatusCode,
    string Html,
    string? Error);
