using System.Text.Json;

namespace BrowserScrapingBridge.Server.Models;

public sealed class SourcingRequest
{
    public string? Site { get; set; }
    public string? Url { get; set; }
    public int Page { get; set; } = 1;
    public int RenderWaitMilliseconds { get; set; } = 900;
    public int TimeoutSeconds { get; set; } = 180;
    public bool RenderHtml { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public Dictionary<string, string>? Cookies { get; set; }
    public Dictionary<string, string>? ApiSettings { get; set; }
    public JsonElement? CollectOptions { get; set; }
    public string? Html { get; set; }
    public JsonElement? Data { get; set; }
}
