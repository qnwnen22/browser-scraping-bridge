using System.Text.Json.Serialization;

namespace BrowserScrapingBridge.Server.Models;

public sealed record BrowserFetchResult(
    string RequestId,
    string Url,
    string FinalUrl,
    string Title,
    int? HttpStatusCode,
    string Html,
    string? Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? BodyBase64 = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Dictionary<string, string>? ResponseHeaders = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CookiesJson = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Dictionary<string, string>? ExtractedData = null);
