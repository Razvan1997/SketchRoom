using SketchRoom.Realtime.Server.Hubs;
using SketchRoom.Realtime.Server.Rooms;

var builder = WebApplication.CreateBuilder(args);
// Bind 0.0.0.0:5000 by default, but let --urls / ASPNETCORE_URLS / env override it.
var configuredUrls = builder.Configuration["urls"] ?? builder.Configuration["ASPNETCORE_URLS"];
if (string.IsNullOrWhiteSpace(configuredUrls))
    builder.WebHost.UseUrls("http://0.0.0.0:5000");
builder.Services.AddSignalR(o => o.MaximumReceiveMessageSize = 8 * 1024 * 1024).AddMessagePackProtocol();
builder.Services.AddSingleton<RoomManager>();

var app = builder.Build();
app.MapHub<WhiteboardHub>("/whiteboardhub");
app.MapGet("/health", () => "ok");
app.Run();

public partial class Program { }
