using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SketchRoom.Realtime.Server.Hubs;
using SketchRoom.Realtime.Server.Rooms;

namespace SketchRoom.Realtime.Server;

public sealed class EmbeddedRealtimeServer
{
    private WebApplication? _app;

    public async Task<string> StartAsync(int port = 5000)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        // Override any appsettings Kestrel:Endpoints that would supersede UseUrls:
        builder.Configuration["Kestrel:Endpoints:Http:Url"] = $"http://0.0.0.0:{port}";
        builder.Services.AddSignalR().AddMessagePackProtocol();
        builder.Services.AddSingleton<RoomManager>();
        _app = builder.Build();
        _app.MapHub<WhiteboardHub>("/whiteboardhub");
        await _app.StartAsync();
        return $"http://{GetLanIpv4()}:{port}";
    }

    public async Task StopAsync() { if (_app != null) { await _app.StopAsync(); await _app.DisposeAsync(); _app = null; } }

    public static string GetLanIpv4()
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            s.Connect("8.8.8.8", 65530);
            return (s.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "127.0.0.1";
        }
        catch { return "127.0.0.1"; }
    }
}
