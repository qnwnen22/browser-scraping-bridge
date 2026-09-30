using System.Collections.Concurrent;
using System.Text;
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

    public Task RegisterBrowserAsync(string connectionId)
    {
        lock (_browserLock) _browserConnectionId = connectionId;
        return Task.CompletedTask;
    }

    public Task UnregisterBrowserAsync(string connectionId)
    {
        lock (_browserLock)
        {
            if (_browserConnectionId == connectionId) _browserConnectionId = null;
        }
        foreach (var pending in _pending.Values)
        {
            if (pending.ConnectionId == connectionId)
                pending.Completion.TrySetException(new BrowserUnavailableException("HTML 요청 중 브라우저 연결이 끊어졌습니다."));
        }
        return Task.CompletedTask;
    }

    public async Task<BrowserFetchResult> FetchAsync(Uri uri, FetchPageRequest request, CancellationToken cancellationToken)
    {
        await _requestGate.WaitAsync(cancellationToken);
        try
        {
            string connectionId;
            lock (_browserLock)
                connectionId = _browserConnectionId ?? throw new BrowserUnavailableException(
                    "브라우저 확장이 연결되지 않았습니다. 확장을 로드하고 연결 상태를 확인해주세요.");

            var requestId = Guid.NewGuid().ToString("N");
            var completion = new TaskCompletionSource<BrowserFetchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[requestId] = new PendingFetch(connectionId, completion);

            try
            {
                await hubContext.Clients.Client(connectionId).SendAsync("FetchPage", new
                {
                    requestId,
                    url = uri.AbsoluteUri,
                    renderWaitMilliseconds = request.RenderWaitMilliseconds,
                    headers = request.Headers,
                    cookies = request.Cookies
                }, cancellationToken);

                return await completion.Task.WaitAsync(TimeSpan.FromSeconds(RequestTimeoutSeconds), cancellationToken);
            }
            catch (TimeoutException exception)
            {
                throw new TimeoutException("브라우저가 60초 이내에 HTML을 반환하지 못했습니다.", exception);
            }
            finally
            {
                _pending.TryRemove(requestId, out _);
            }
        }
        finally
        {
            _requestGate.Release();
        }
    }

    public Task CompleteFetchAsync(string connectionId, BrowserFetchResult result)
    {
        if (string.IsNullOrWhiteSpace(result.RequestId) ||
            !_pending.TryGetValue(result.RequestId, out var pending) ||
            pending.ConnectionId != connectionId)
            return Task.CompletedTask;

        result = result with { Html = result.Html ?? "" };
        if (Encoding.UTF8.GetByteCount(result.Html) > 15 * 1024 * 1024)
            result = result with { Html = "", Error = "HTML 크기가 반환 한도(15MiB)를 초과했습니다." };

        pending.Completion.TrySetResult(result);
        return Task.CompletedTask;
    }

    private sealed record PendingFetch(string ConnectionId, TaskCompletionSource<BrowserFetchResult> Completion);
}

public sealed class BrowserUnavailableException(string message) : Exception(message);

