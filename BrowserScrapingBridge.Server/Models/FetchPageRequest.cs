namespace BrowserScrapingBridge.Server.Models;

public sealed record FetchPageRequest(
    string? Url,
    int RenderWaitMilliseconds = 900,
    Dictionary<string, string>? Headers = null,
    Dictionary<string, string>? Cookies = null);

