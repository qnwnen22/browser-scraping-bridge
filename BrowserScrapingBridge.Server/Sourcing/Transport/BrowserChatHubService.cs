using System.Text;
using System.Text.Json;
using BrowserScrapingBridge.Server.Models;
using BrowserScrapingBridge.Server.Services;
using BrowserScrapingBridge.Server.Sourcing.Common;
using Newtonsoft.Json;

namespace BrowserScrapingBridge.Server.Sourcing.Transport;

public sealed class BrowserChatHubService(BrowserRequestCoordinator coordinator) : IChatHubService
{
    private Uri? _initialUri;
    private SourcingRequest _options = new();
    private CancellationToken _cancellationToken;
    private Lazy<string>? _target;
    public string ConnectionId => BindBrowser();
    internal BrowserRequestCoordinator Coordinator => coordinator;

    public void Configure(Uri uri, SourcingRequest options, CancellationToken cancellationToken)
    {
        _initialUri = uri;
        _options = options;
        _cancellationToken = cancellationToken;
        _target = new Lazy<string>(() => coordinator.SelectBrowser());
    }

    public string BindBrowser()
        => _target?.Value ?? throw new InvalidOperationException("브라우저 수집 설정이 필요합니다.");

    private void ValidateConnection(string connectionId)
    {
        if (!string.Equals(connectionId, BindBrowser(), StringComparison.Ordinal))
            throw new ArgumentException("수집 요청의 connectionId가 실행 범위의 브라우저 연결 ID와 일치하지 않습니다.");
    }

    public Task<string> GetRequest(string connectionId, string url,
        Dictionary<string, string>? header = null, string encodingType = "")
        => Request(connectionId, "GET", url, header, null, encodingType);

    public Task<string> PostRequest(string connectionId, string url,
        Dictionary<string, string>? header = null, string? body = null, string encodingType = "")
        => Request(connectionId, "POST", url, header, body, encodingType);

    public async Task<string> Request(string connectionId, string method, string url,
        Dictionary<string, string>? header = null, string? body = null, string encodingType = "")
    {
        ValidateConnection(connectionId);
        var uri = ValidateUri(url);
        if (_options.RenderHtml && method.Equals("GET", StringComparison.OrdinalIgnoreCase) && uri == _initialUri)
            return (await GetDomAsync(url, headers: header)).Html;
        var encoding = string.IsNullOrWhiteSpace(encodingType) ? Encoding.UTF8 : Encoding.GetEncoding(encodingType);
        var result = await SendAsync(method, uri, header, body is null ? null : encoding.GetBytes(body), null, _cancellationToken);
        var bytes = Convert.FromBase64String(result.BodyBase64 ?? "");
        if (string.IsNullOrWhiteSpace(encodingType)) encoding = ResponseEncoding(result.ResponseHeaders);
        return encoding.GetString(bytes);
    }

    public async Task<BrowserFetchResult> GetDomAsync(string url, string? extract = null, Dictionary<string, string>? headers = null)
    {
        var uri = ValidateUri(url);
        var options = CreateFetchRequest(uri, headers, null);
        return await GuardAsync(async () =>
        {
            var result = await coordinator.FetchAsync(uri, options, _cancellationToken, extract, BindBrowser());
            ThrowIfFailed(result);
            return result;
        });
    }

    public async Task<BrowserFetchResult> SendAsync(string method, Uri uri, Dictionary<string, string>? headers,
        byte[]? body, Dictionary<string, string>? cookies, CancellationToken cancellationToken)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        ValidateUri(uri.AbsoluteUri);
        method = method.ToUpperInvariant();
        if (!new[] { "GET", "POST", "HEAD", "PUT", "PATCH", "DELETE", "OPTIONS" }.Contains(method))
            throw new ArgumentException("지원하지 않는 HTTP 메서드입니다.");
        if (body is { Length: > 2 * 1024 * 1024 }) throw new ArgumentException("요청 본문은 2MiB 이하여야 합니다.");
        var options = CreateFetchRequest(uri, headers, cookies);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken);
        return await GuardAsync(async () =>
        {
            if (_options.RenderHtml && method == "GET" && uri == _initialUri)
            {
                var page = await coordinator.FetchAsync(uri, options, linked.Token, connectionId: BindBrowser());
                ThrowIfFailed(page);
                return page with { BodyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(page.Html)),
                    ResponseHeaders = new Dictionary<string, string> { ["content-type"] = "text/html; charset=utf-8" } };
            }
            var result = await coordinator.FetchResourceAsync(uri, method, options.Headers, options.Cookies,
                body is null ? null : Convert.ToBase64String(body), linked.Token, BindBrowser());
            ThrowIfFailed(result);
            return result;
        });
    }

    internal FetchPageRequest CreateFetchRequest(Uri uri, Dictionary<string, string>? headers, Dictionary<string, string>? cookies)
    {
        bool sameOrigin = _initialUri is not null && uri.GetLeftPart(UriPartial.Authority) == _initialUri.GetLeftPart(UriPartial.Authority);
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (sameOrigin && _options.Headers is not null)
            foreach (var item in _options.Headers) merged[item.Key] = item.Value;
        if (headers is not null) foreach (var item in headers) merged[item.Key] = item.Value;
        cookies = sameOrigin && _options.Cookies is not null ? _options.Cookies : cookies;
        if (cookies is not null) merged.Remove("Cookie");
        var request = new FetchPageRequest(uri.AbsoluteUri, _options.RenderWaitMilliseconds, merged, cookies);
        var error = FetchPageRequestValidator.ValidateOptions(request);
        if (error is not null) throw new ArgumentException(error);
        return request;
    }

    public async Task<string> ExtensionsCookie(string connectionId, string domain)
    {
        ValidateConnection(connectionId);
        return await GuardAsync(async () =>
        {
            var result = await coordinator.ReadCookiesAsync(domain, _cancellationToken, BindBrowser());
            ThrowIfFailed(result);
            return result.CookiesJson ?? "[]";
        });
    }

    public async Task<List<ExtensionsCookie>> GetCookies(string connectionId, string domain)
        => JsonConvert.DeserializeObject<List<ExtensionsCookie>>(await ExtensionsCookie(connectionId, domain)) ?? [];

    private static void ThrowIfFailed(BrowserFetchResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Error)) throw new BrowserOperationException(result.Error);
    }

    private static async Task<T> GuardAsync<T>(Func<Task<T>> action)
    {
        var context = SourcingExecution.Current;
        context.ThrowIfFailed();
        try { return await action(); }
        catch (Exception exception)
        {
            context.RecordFailure(exception);
            throw;
        }
    }

    private static Uri ValidateUri(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new ArgumentException("수집 요청은 HTTP/HTTPS URL이어야 합니다.");
        return uri;
    }

    internal static Encoding ResponseEncoding(Dictionary<string, string>? headers)
    {
        if (headers is not null && headers.TryGetValue("content-type", out var value) &&
            System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(value, out var contentType) &&
            !string.IsNullOrWhiteSpace(contentType.CharSet))
        {
            try { return Encoding.GetEncoding(contentType.CharSet.Trim('"')); } catch (ArgumentException) { }
        }
        return Encoding.UTF8;
    }
}

public sealed class BrowserOperationException(string message) : Exception(message);
