using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SketchRoom.Realtime.Contracts;

namespace SketchRoom.Realtime.Client;

public sealed class SignalRWhiteboardClient : IWhiteboardClient
{
    private HubConnection? _conn;

    public event Action<ElementDto>? ElementAdded;
    public event Action<ElementDto>? ElementUpdated;
    public event Action<Guid>? ElementRemoved;
    public event Action? BoardCleared;
    public event Action<LivePointDto>? LivePointed;
    public event Action<CursorDto>? CursorMoved;
    public event Action<RoomParticipantDto>? UserJoined;
    public event Action<string>? UserLeft;
    public event Action<RoomStateDto>? RoomStateChanged;
    public event Action? Kicked;
    public event Func<Task>? Reconnected;

    public async Task ConnectAsync(string baseUrl)
    {
        if (_conn is { State: HubConnectionState.Connected }) return;
        if (_conn != null) { await _conn.DisposeAsync(); _conn = null; }
        _conn = new HubConnectionBuilder()
            .WithUrl($"{baseUrl.TrimEnd('/')}/whiteboardhub")
            .AddMessagePackProtocol()
            .WithAutomaticReconnect()
            .Build();
        _conn.On<ElementDto>(ClientMethods.ElementAdded, e => ElementAdded?.Invoke(e));
        _conn.On<ElementDto>(ClientMethods.ElementUpdated, e => ElementUpdated?.Invoke(e));
        _conn.On<Guid>(ClientMethods.ElementRemoved, id => ElementRemoved?.Invoke(id));
        _conn.On(ClientMethods.BoardCleared, () => BoardCleared?.Invoke());
        _conn.On<LivePointDto>(ClientMethods.LivePointed, p => LivePointed?.Invoke(p));
        _conn.On<CursorDto>(ClientMethods.CursorMoved, c => CursorMoved?.Invoke(c));
        _conn.On<RoomParticipantDto>(ClientMethods.UserJoined, u => UserJoined?.Invoke(u));
        _conn.On<string>(ClientMethods.UserLeft, u => UserLeft?.Invoke(u));
        _conn.On<RoomStateDto>(ClientMethods.RoomStateChanged, s => RoomStateChanged?.Invoke(s));
        _conn.On(ClientMethods.Kicked, () => Kicked?.Invoke());
        _conn.Reconnected += _ => Reconnected?.Invoke() ?? Task.CompletedTask;
        await _conn.StartAsync();
    }

    private HubConnection Req => _conn ?? throw new InvalidOperationException("Not connected.");
    public Task<JoinResultDto> CreateRoomAsync(string n, string? a) => Req.InvokeAsync<JoinResultDto>(HubMethods.CreateRoom, n, a);
    public Task<JoinResultDto> JoinRoomAsync(string c, string n, string? a) => Req.InvokeAsync<JoinResultDto>(HubMethods.JoinRoom, c, n, a);
    public Task AddElementAsync(string c, ElementDto e) => Req.InvokeAsync(HubMethods.AddElement, c, e);
    public Task UpdateElementAsync(string c, ElementDto e) => Req.InvokeAsync(HubMethods.UpdateElement, c, e);
    public Task RemoveElementAsync(string c, Guid id) => Req.InvokeAsync(HubMethods.RemoveElement, c, id);
    public Task ClearBoardAsync(string c) => Req.InvokeAsync(HubMethods.ClearBoard, c);
    public Task LivePointAsync(string c, LivePointDto p) => Req.InvokeAsync(HubMethods.LivePoint, c, p);
    public Task MoveCursorAsync(string c, double x, double y) => Req.InvokeAsync(HubMethods.MoveCursor, c, x, y);
    public Task SetLockAsync(string c, bool l) => Req.InvokeAsync(HubMethods.SetLock, c, l);
    public Task KickUserAsync(string c, string u) => Req.InvokeAsync(HubMethods.KickUser, c, u);
    public async Task DisposeAsync() { if (_conn != null) { await _conn.DisposeAsync(); _conn = null; } }
}
