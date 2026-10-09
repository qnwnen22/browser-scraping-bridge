using BrowserScrapingBridge.Server.Hubs;
using BrowserScrapingBridge.Server.Services;
using BrowserScrapingBridge.Server.Sourcing.Transport;
using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["Bridge:ListenUrl"] ?? "http://127.0.0.1:5180");
builder.Services.AddControllers();
builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 20 * 1024 * 1024);
builder.Services.AddSingleton<BrowserRequestCoordinator>();
builder.Services.AddSingleton<SiteCollectorCatalog>();
builder.Services.AddScoped<BrowserChatHubService>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapHub<BrowserHub>("/hubs/browser");
app.Run();

