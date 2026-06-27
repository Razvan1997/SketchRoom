var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR().AddMessagePackProtocol();
builder.Services.AddSingleton<SketchRoom.Realtime.Server.Rooms.RoomManager>();
var app = builder.Build();
app.Run();

public partial class Program { }
