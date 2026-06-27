using SketchRoom.Realtime.Server.Hubs;
using SketchRoom.Realtime.Server.Rooms;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(o => o.MaximumReceiveMessageSize = 8 * 1024 * 1024).AddMessagePackProtocol();
builder.Services.AddSingleton<RoomManager>();

var app = builder.Build();
app.MapHub<WhiteboardHub>("/whiteboardhub");
app.MapGet("/health", () => "ok");
app.Run();

public partial class Program { }
