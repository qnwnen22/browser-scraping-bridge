using System.Collections.Concurrent;
using System.Text.Json;
using BrowserScrapingBridge.Server.Hubs;
using BrowserScrapingBridge.Server.Models;
using Microsoft.AspNetCore.SignalR;

namespace BrowserScrapingBridge.Server.Services;

public sealed class BrowserRequestCoordinator(IHubContext<BrowserHub> hubContext)
{
    private const int RequestTimeoutSeconds = 60;
    private readonly ConcurrentDictionary<string, PendingFetch> _pending = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _requestGate = new(1, 1);
    private readonly object _browserLock = new();
    private string? _browserConnectionId;

    public bool IsBrowserConnected
    {
        get { lock (_browserLock) return _browserConnectionId is not null; }
    }

    public string SelectBrowser()
    {
        lock (_browserLock)
            return _browserConnectionId ?? throw new BrowserUnavailableException(
                "브라우저 확장이 연결되지 않았습니다. 확장을 로드하고 연결 상태를 확인해주세요.");
    }

    public Task RegisterBrowserAsync(string connectionId)
    {
        lock (_browserLock)
        {
            if (_browserConnectionId is not null && _browserConnectionId != connectionId)
                FailPending(_browserConnectionId, "브라우저 연결이 변경되어 수집을 중단했습니다. 다시 요청해주세요.");
            _browserConnectionId = connectionId;
        }
        return Task.CompletedTask;
    }

    public Task UnregisterBrowserAsync(string connectionId)
    {
        lock (_browserLock)
        {
            if (_browserConnectionId == connectionId) _browserConnectionId = null;
            FailPending(connectionId, "수집 중 선택한 브라우저 연결이 끊어졌습니다.");
        }
        return Task.CompletedTask;
    }

    public Task<BrowserFetchResult> FetchAsync(Uri uri, FetchPageRequest request, CancellationToken cancellationToken,
        string? extract = null, string? connectionId = null, BrowserFetchMode mode = BrowserFetchMode.Dom)
    {
        connectionId ??= SelectBrowser();
        // JSON/JSONP와 HTML 조각은 DOM으로 감싸지 않고 응답 바이트를 반환합니다.
        if (mode == BrowserFetchMode.Response)
            return FetchResourceAsync(uri, "GET", request.Headers, request.Cookies, null, cancellationToken, connectionId);
        if (mode != BrowserFetchMode.Dom) throw new ArgumentOutOfRangeException(nameof(mode));
        return ExecuteAsync(connectionId, "FetchPage", requestId => new
            {
                requestId, url = uri.AbsoluteUri, renderWaitMilliseconds = request.RenderWaitMilliseconds,
                headers = request.Headers, cookies = request.Cookies, extract
            }, cancellationToken);
    }

    public Task<BrowserFetchResult> FetchResourceAsync(Uri uri, string method, Dictionary<string, string>? headers,
        Dictionary<string, string>? cookies, string? bodyBase64, CancellationToken cancellationToken, string connectionId)
        => ExecuteAsync(connectionId, "FetchResource", requestId => new
        {
            requestId, url = uri.AbsoluteUri, method, headers, cookies, bodyBase64
        }, cancellationToken);

    public Task<BrowserFetchResult> ReadCookiesAsync(string domain, CancellationToken cancellationToken, string connectionId)
        => ExecuteAsync(connectionId, "ReadCookies", requestId => new { requestId, domain }, cancellationToken);

    private async Task<BrowserFetchResult> ExecuteAsync(string connectionId, string target,
        Func<string, object> createJob, CancellationToken cancellationToken)
    {
        await _requestGate.WaitAsync(cancellationToken);
        try
        {
            var requestId = Guid.NewGuid().ToString("N");
            var completion = new TaskCompletionSource<BrowserFetchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_browserLock)
            {
                if (_browserConnectionId != connectionId)
                    throw new BrowserUnavailableException("수집에 사용하던 브라우저 연결이 끊기거나 변경되었습니다. 연결 상태를 확인한 후 다시 요청해주세요.");
                _pending[requestId] = new PendingFetch(connectionId, completion);
            }
            try
            {
                await hubContext.Clients.Client(connectionId).SendAsync(target, createJob(requestId), cancellationToken);
                return await completion.Task.WaitAsync(TimeSpan.FromSeconds(RequestTimeoutSeconds), cancellationToken);
            }
            catch (TimeoutException exception)
            {
                throw new TimeoutException("브라우저가 60초 이내에 결과를 반환하지 못했습니다.", exception);
            }
            finally
            {
                _pending.TryRemove(requestId, out _);
                try { await hubContext.Clients.Client(connectionId).SendAsync("CancelRequest", new { requestId }); }
                catch { }
            }
        }
        finally { _requestGate.Release(); }
    }

    public Task CompleteFetchAsync(string connectionId, BrowserFetchResult result)
    {
        if (string.IsNullOrWhiteSpace(result.RequestId) ||
            !_pending.TryGetValue(result.RequestId, out var pending) || pending.ConnectionId != connectionId)
            return Task.CompletedTask;

        result = result with { Html = result.Html ?? "" };
        if (JsonSerializer.SerializeToUtf8Bytes(result).Length > 15 * 1024 * 1024)
            result = new BrowserFetchResult(result.RequestId, result.Url, result.FinalUrl, "", result.HttpStatusCode, "",
                "브라우저 결과 크기가 반환 한도(15MiB)를 초과했습니다.");
        pending.Completion.TrySetResult(result);
        return Task.CompletedTask;
    }

    private void FailPending(string connectionId, string message)
    {
        foreach (var pending in _pending.Values)
            if (pending.ConnectionId == connectionId)
                pending.Completion.TrySetException(new BrowserUnavailableException(message));
    }

    private sealed record PendingFetch(string ConnectionId, TaskCompletionSource<BrowserFetchResult> Completion);
}

public sealed class BrowserUnavailableException(string message) : Exception(message);
