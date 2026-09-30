using BrowserScrapingBridge.Server.Hubs;
using BrowserScrapingBridge.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["Bridge:ListenUrl"] ?? "http://127.0.0.1:5180");
builder.Services.AddControllers();
builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 20 * 1024 * 1024);
builder.Services.AddSingleton<BrowserRequestCoordinator>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapHub<BrowserHub>("/hubs/browser");
app.Run();

