using System.Net;

namespace BrowserScrapingBridge.Server.Sourcing.Transport;

// HttpClient의 메시지/응답 API를 유지하면서 네트워크 I/O는 SignalR 브라우저에 맡깁니다.
public sealed class BrowserHttpClient : HttpClient
{
    public BrowserHttpClient() : base(new BrowserHttpHandler()) { }
    public BrowserHttpClient(HttpMessageHandler originalHandler) : base(new BrowserHttpHandler(originalHandler)) { }
}

internal sealed class BrowserHttpHandler : HttpMessageHandler
{
    private readonly HttpMessageHandler? _original;
    private readonly CookieContainer? _cookieContainer;

    public BrowserHttpHandler(HttpMessageHandler? original = null)
    {
        _original = original;
        if (original is HttpClientHandler { UseCookies: true } handler) _cookieContainer = handler.CookieContainer;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = SourcingExecution.Current;
        var uri = request.RequestUri ?? throw new ArgumentException("요청 URL이 없습니다.");
        var headers = request.Headers.ToDictionary(header => header.Key, header => string.Join(", ", header.Value), StringComparer.OrdinalIgnoreCase);
        byte[]? body = null;
        if (request.Content is not null)
        {
            body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            foreach (var header in request.Content.Headers) headers[header.Key] = string.Join(", ", header.Value);
        }
        // fetch가 압축 해제와 본문 길이 계산을 담당합니다.
        headers.Remove("Content-Length");
        headers.Remove("Accept-Encoding");
        headers.Remove("Host");
        headers.Remove("Connection");
        headers.Remove("Transfer-Encoding");
        Dictionary<string, string>? cookies = null;
        if (!headers.ContainsKey("Cookie") && _cookieContainer is not null)
        {
            var stored = _cookieContainer.GetCookies(uri).Cast<Cookie>().ToArray();
            if (stored.Length > 0) cookies = stored.GroupBy(cookie => cookie.Name).ToDictionary(group => group.Key, group => group.First().Value);
        }
        var result = await context.Browser.SendAsync(request.Method.Method, uri, headers, body, cookies, cancellationToken);
        var response = new HttpResponseMessage((HttpStatusCode)(result.HttpStatusCode ?? 200))
        {
            RequestMessage = request,
            Content = new ByteArrayContent(Convert.FromBase64String(result.BodyBase64 ?? ""))
        };
        foreach (var header in result.ResponseHeaders ?? [])
        {
            // 브라우저 fetch 응답은 이미 압축 해제되어 있습니다.
            if (header.Key.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
            if (!response.Headers.TryAddWithoutValidation(header.Key, header.Value))
                response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return response;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _original?.Dispose();
        base.Dispose(disposing);
    }
}
