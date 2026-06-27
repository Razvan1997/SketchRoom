using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SketchRoom.Realtime.Contracts;
using Xunit;

public class HubIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public HubIntegrationTests(WebApplicationFactory<Program> f) => _factory = f;

    private HubConnection Connect()
    {
        var handler = _factory.Server.CreateHandler();
        return new HubConnectionBuilder()
            .WithUrl("http://localhost/whiteboardhub", o => { o.HttpMessageHandlerFactory = _ => handler; o.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling; })
            .AddMessagePackProtocol()
            .Build();
    }

    [Fact]
    public async Task Join_then_AddElement_is_broadcast_and_in_snapshot()
    {
        var host = Connect(); await host.StartAsync();
        var created = await host.InvokeAsync<JoinResultDto>(HubMethods.CreateRoom, "Host", null);
        Assert.True(created.Success);

        var guest = Connect(); await guest.StartAsync();
        var got = new TaskCompletionSource<ElementDto>();
        guest.On<ElementDto>(ClientMethods.ElementAdded, e => got.TrySetResult(e));
        var join = await guest.InvokeAsync<JoinResultDto>(HubMethods.JoinRoom, created.State!.RoomCode, "Guest", null);
        Assert.True(join.Success);

        var el = new ElementDto { Type = ElementType.Ellipse, X = 5 };
        await host.InvokeAsync(HubMethods.AddElement, created.State!.RoomCode, el);
        var received = await got.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(el.Id, received.Id);

        var late = Connect(); await late.StartAsync();
        var lateJoin = await late.InvokeAsync<JoinResultDto>(HubMethods.JoinRoom, created.State!.RoomCode, "Late", null);
        Assert.Contains(lateJoin.Snapshot, e => e.Id == el.Id);

        await host.DisposeAsync(); await guest.DisposeAsync(); await late.DisposeAsync();
    }
}
