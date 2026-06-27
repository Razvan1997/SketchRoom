using Microsoft.AspNetCore.SignalR;
using SketchRoom.Realtime.Contracts;
using SketchRoom.Realtime.Server.Rooms;

namespace SketchRoom.Realtime.Server.Hubs;

public sealed class WhiteboardHub : Hub
{
    private readonly RoomManager _rooms;
    public WhiteboardHub(RoomManager rooms) => _rooms = rooms;

    public async Task<JoinResultDto> CreateRoom(string displayName, string? avatar)
    {
        var (code, userId) = _rooms.CreateRoom(Context.ConnectionId, displayName, avatar);
        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        return new JoinResultDto { Success = true, UserId = userId, State = _rooms.GetState(code), Snapshot = _rooms.GetSnapshot(code) };
    }

    public async Task<JoinResultDto> JoinRoom(string code, string displayName, string? avatar)
    {
        if (!_rooms.TryJoin(code, Context.ConnectionId, displayName, avatar, out var err, out var userId))
            return new JoinResultDto { Success = false, Error = err };
        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        var state = _rooms.GetState(code)!;
        var me = state.Participants.First(p => p.UserId == userId);
        await Clients.OthersInGroup(code).SendAsync(ClientMethods.UserJoined, me);
        await Clients.Group(code).SendAsync(ClientMethods.RoomStateChanged, state);
        return new JoinResultDto { Success = true, UserId = userId, State = state, Snapshot = _rooms.GetSnapshot(code) };
    }

    public async Task AddElement(string code, ElementDto element)
    {
        if (_rooms.IsLocked(code) && !_rooms.IsHost(code, Context.ConnectionId)) return;
        _rooms.ApplyAdd(code, element);
        await Clients.OthersInGroup(code).SendAsync(ClientMethods.ElementAdded, element);
    }

    public async Task UpdateElement(string code, ElementDto element)
    {
        if (_rooms.IsLocked(code) && !_rooms.IsHost(code, Context.ConnectionId)) return;
        _rooms.ApplyUpdate(code, element);
        await Clients.OthersInGroup(code).SendAsync(ClientMethods.ElementUpdated, element);
    }

    public async Task RemoveElement(string code, Guid id)
    {
        if (_rooms.IsLocked(code) && !_rooms.IsHost(code, Context.ConnectionId)) return;
        _rooms.ApplyRemove(code, id);
        await Clients.OthersInGroup(code).SendAsync(ClientMethods.ElementRemoved, id);
    }

    public async Task ClearBoard(string code)
    {
        if (!_rooms.IsHost(code, Context.ConnectionId)) return;
        _rooms.ApplyClear(code);
        await Clients.Group(code).SendAsync(ClientMethods.BoardCleared);
    }

    public async Task LivePoint(string code, LivePointDto p)
        => await Clients.OthersInGroup(code).SendAsync(ClientMethods.LivePointed, p);

    public async Task MoveCursor(string code, double x, double y)
        => await Clients.OthersInGroup(code).SendAsync(ClientMethods.CursorMoved, new CursorDto { UserId = CurrentUserId(code), X = x, Y = y });

    public async Task SetLock(string code, bool locked)
    {
        if (!_rooms.IsHost(code, Context.ConnectionId)) return;
        _rooms.SetLock(code, locked);
        await Clients.Group(code).SendAsync(ClientMethods.RoomStateChanged, _rooms.GetState(code));
    }

    public async Task KickUser(string code, string userId)
    {
        if (!_rooms.IsHost(code, Context.ConnectionId)) return;
        var conn = _rooms.FindConnectionId(code, userId);
        if (conn is null) return;
        await Clients.Client(conn).SendAsync(ClientMethods.Kicked);
        await Groups.RemoveFromGroupAsync(conn, code);
        var res = _rooms.Leave(conn);
        await BroadcastLeave(res, userId);
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        var res = _rooms.Leave(Context.ConnectionId);
        await BroadcastLeave(res, null);
        await base.OnDisconnectedAsync(ex);
    }

    private async Task BroadcastLeave(RoomLeaveResult res, string? userId)
    {
        if (res.RoomCode is null || res.RoomRemoved) return;
        var state = _rooms.GetState(res.RoomCode);
        if (state is null) return;
        await Clients.Group(res.RoomCode).SendAsync(ClientMethods.RoomStateChanged, state);
    }

    // NOTE (Task 9 hardens this via a connectionId→userId lookup added to RoomManager; for now MoveCursor userId is approximate and only cosmetic)
    private string CurrentUserId(string code) =>
        _rooms.GetState(code)?.Participants.FirstOrDefault()?.UserId ?? "";
}
