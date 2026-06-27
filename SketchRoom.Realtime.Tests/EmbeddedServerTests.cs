using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SketchRoom.Realtime.Client;
using SketchRoom.Realtime.Contracts;
using SketchRoom.Realtime.Server;
using Xunit;

public class EmbeddedServerTests
{
    [Fact]
    public async Task Embedded_server_accepts_a_client_and_creates_room()
    {
        var server = new EmbeddedRealtimeServer();
        var baseUrl = await server.StartAsync(5099);
        try
        {
            var conn = new HubConnectionBuilder().WithUrl($"{baseUrl}/whiteboardhub").AddMessagePackProtocol().Build();
            await conn.StartAsync();
            var res = await conn.InvokeAsync<JoinResultDto>(HubMethods.CreateRoom, "Host", null);
            Assert.True(res.Success);
            Assert.Equal(RoomLimits.CodeLength, res.State!.RoomCode.Length);
            await conn.DisposeAsync();
        }
        finally { await server.StopAsync(); }
    }

    [Fact]
    public async Task SignalRWhiteboardClient_guest_receives_ElementAdded_from_host()
    {
        var server = new EmbeddedRealtimeServer();
        var baseUrl = await server.StartAsync(5098);
        var host = new SignalRWhiteboardClient();
        var guest = new SignalRWhiteboardClient();
        try
        {
            await host.ConnectAsync(baseUrl);
            await guest.ConnectAsync(baseUrl);

            var hostResult = await host.CreateRoomAsync("Host", null);
            Assert.True(hostResult.Success);
            var code = hostResult.State!.RoomCode;

            var guestResult = await guest.JoinRoomAsync(code, "Guest", null);
            Assert.True(guestResult.Success);

            var tcs = new TaskCompletionSource<ElementDto>();
            guest.ElementAdded += e => tcs.TrySetResult(e);

            var element = new ElementDto { Id = Guid.NewGuid(), Type = ElementType.Rectangle, X = 10, Y = 20, Width = 100, Height = 50 };
            await host.AddElementAsync(code, element);

            var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(element.Id, received.Id);
        }
        finally
        {
            await host.DisposeAsync();
            await guest.DisposeAsync();
            await server.StopAsync();
        }
    }
}
