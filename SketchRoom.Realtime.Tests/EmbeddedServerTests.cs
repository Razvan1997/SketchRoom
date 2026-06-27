using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
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
}
