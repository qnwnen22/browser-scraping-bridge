using BrowserScrapingBridge.Server.Models;
using BrowserScrapingBridge.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace BrowserScrapingBridge.Server.Hubs;

public sealed class BrowserHub(BrowserRequestCoordinator coordinator) : Hub
{
    public Task RegisterBrowser() => coordinator.RegisterBrowserAsync(Context.ConnectionId);

    public Task CompleteFetch(BrowserFetchResult result) =>
        coordinator.CompleteFetchAsync(Context.ConnectionId, result);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await coordinator.UnregisterBrowserAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
