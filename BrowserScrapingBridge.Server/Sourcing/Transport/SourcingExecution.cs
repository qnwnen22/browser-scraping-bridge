using BrowserScrapingBridge.Server.Models;
using BrowserScrapingBridge.Server.Services;

namespace BrowserScrapingBridge.Server.Sourcing.Transport;

public sealed class SourcingExecution : IDisposable
{
    private static readonly AsyncLocal<SourcingExecution?> Slot = new();
    private readonly SourcingExecution? _previous;
    private readonly IReadOnlyDictionary<string, string> _settings;
    private Exception? _transportFailure;
    public BrowserChatHubService Browser { get; }
    public string ConnectionId => Browser.ConnectionId;
    public BrowserRequestCoordinator Coordinator => Browser.Coordinator;
    public CancellationToken CancellationToken { get; }
    public static SourcingExecution Current => Slot.Value ?? throw new InvalidOperationException("수집 실행 범위가 없습니다.");

    public SourcingExecution(BrowserChatHubService browser, Dictionary<string, string>? settings, CancellationToken token)
    {
        _previous = Slot.Value;
        Browser = browser;
        _settings = settings ?? new Dictionary<string, string>();
        CancellationToken = token;
        Slot.Value = this;
    }

    public string? GetSetting(string name) => _settings.TryGetValue(name, out var value) ? value : null;
    public FetchPageRequest CreateFetchRequest(Uri uri, Dictionary<string, string>? headers = null)
        => Browser.CreateFetchRequest(uri, headers, null);
    public void RecordFailure(Exception exception) => Interlocked.CompareExchange(ref _transportFailure, exception, null);
    public void ThrowIfFailed()
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (_transportFailure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_transportFailure).Throw();
    }
    public void Dispose() => Slot.Value = _previous;
}
