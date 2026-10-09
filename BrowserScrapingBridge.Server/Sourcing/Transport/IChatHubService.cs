using BrowserScrapingBridge.Server.Sourcing.Common;

namespace BrowserScrapingBridge.Server.Sourcing.Transport;

// SignalR ConnectionId로 선택한 브라우저에 HTML·리소스·쿠키 조회를 전달합니다.
public interface IChatHubService
{
    Task<string> Request(string connectionId, string method, string url,
        Dictionary<string, string>? header = null, string? body = null, string encodingType = "");
    Task<string> GetRequest(string connectionId, string url,
        Dictionary<string, string>? header = null, string encodingType = "");
    Task<string> PostRequest(string connectionId, string url,
        Dictionary<string, string>? header = null, string? body = null, string encodingType = "");
    Task<string> ExtensionsCookie(string connectionId, string domain);
    Task<List<ExtensionsCookie>> GetCookies(string connectionId, string domain);
}
