using SketchRoom.Realtime.Contracts;
using SketchRoom.Realtime.Server.Rooms;
using Xunit;

public class RoomManagerTests
{
    [Fact]
    public void CreateRoom_returns_code_and_makes_creator_host()
    {
        var m = new RoomManager();
        var (code, userId) = m.CreateRoom("conn1", "Ana", null);
        Assert.Equal(RoomLimits.CodeLength, code.Length);
        Assert.True(m.IsHost(code, "conn1"));
        var state = m.GetState(code)!;
        Assert.Single(state.Participants);
        Assert.True(state.Participants[0].IsHost);
        Assert.Equal(userId, state.Participants[0].UserId);
    }

    [Fact]
    public void Join_unknown_room_fails()
    {
        var m = new RoomManager();
        var ok = m.TryJoin("ZZZZZZ", "c2", "Bob", null, out var err, out _);
        Assert.False(ok);
        Assert.Equal("ROOM_NOT_FOUND", err);
    }

    [Fact]
    public void Join_full_room_fails()
    {
        var m = new RoomManager();
        var (code, _) = m.CreateRoom("host", "H", null);
        for (int i = 1; i < RoomLimits.MaxParticipants; i++)
            Assert.True(m.TryJoin(code, $"c{i}", $"U{i}", null, out _, out _));
        var ok = m.TryJoin(code, "overflow", "X", null, out var err, out _);
        Assert.False(ok);
        Assert.Equal("ROOM_FULL", err);
    }

    [Fact]
    public void Snapshot_reflects_applied_elements()
    {
        var m = new RoomManager();
        var (code, _) = m.CreateRoom("host", "H", null);
        var e = new ElementDto { Type = ElementType.Rectangle, X = 1, Y = 2 };
        m.ApplyAdd(code, e);
        Assert.Single(m.GetSnapshot(code));
        m.ApplyRemove(code, e.Id);
        Assert.Empty(m.GetSnapshot(code));
    }

    [Fact]
    public void ApplyClear_empties_snapshot()
    {
        var m = new RoomManager();
        var (code, _) = m.CreateRoom("host", "H", null);
        m.ApplyAdd(code, new ElementDto());
        m.ApplyClear(code);
        Assert.Empty(m.GetSnapshot(code));
    }

    [Fact]
    public void Host_leaving_promotes_oldest_remaining()
    {
        var m = new RoomManager();
        var (code, _) = m.CreateRoom("host", "H", null);
        m.TryJoin(code, "c2", "Second", null, out _, out var u2);
        var res = m.Leave("host");
        Assert.Equal(code, res.RoomCode);
        Assert.False(res.RoomRemoved);
        Assert.Equal(u2, res.NewHostUserId);
        Assert.True(m.IsHost(code, "c2"));
    }

    [Fact]
    public void Last_participant_leaving_removes_room()
    {
        var m = new RoomManager();
        var (code, _) = m.CreateRoom("host", "H", null);
        var res = m.Leave("host");
        Assert.True(res.RoomRemoved);
        Assert.Null(m.GetState(code));
    }

    [Fact]
    public void SetLock_toggles_and_reports()
    {
        var m = new RoomManager();
        var (code, _) = m.CreateRoom("host", "H", null);
        Assert.False(m.IsLocked(code));
        m.SetLock(code, true);
        Assert.True(m.IsLocked(code));
    }

    [Fact]
    public void FindConnectionId_returns_connection_then_null_after_leave()
    {
        var m = new RoomManager();
        var (code, _) = m.CreateRoom("host", "H", null);
        m.TryJoin(code, "c2", "Second", null, out _, out var u2);
        Assert.Equal("c2", m.FindConnectionId(code, u2));
        m.Leave("c2");
        Assert.Null(m.FindConnectionId(code, u2));
    }

    [Fact]
    public void NonHost_leaving_keeps_room_and_host()
    {
        var m = new RoomManager();
        var (code, _) = m.CreateRoom("host", "H", null);
        m.TryJoin(code, "c2", "Second", null, out _, out _);
        var res = m.Leave("c2");
        Assert.False(res.RoomRemoved);
        Assert.Null(res.NewHostUserId);
        Assert.True(m.IsHost(code, "host"));
    }

    [Fact]
    public void SetLock_can_unlock()
    {
        var m = new RoomManager();
        var (code, _) = m.CreateRoom("host", "H", null);
        m.SetLock(code, true);
        m.SetLock(code, false);
        Assert.False(m.IsLocked(code));
    }
}
