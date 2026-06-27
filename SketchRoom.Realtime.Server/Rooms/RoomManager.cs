using System.Collections.Concurrent;
using SketchRoom.Realtime.Contracts;

namespace SketchRoom.Realtime.Server.Rooms;

public sealed record RoomLeaveResult(string? RoomCode, string? NewHostUserId, bool RoomRemoved);

public sealed class RoomManager
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new();
    private readonly object _gate = new();
    private static readonly Random _rng = new();
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no ambiguous chars

    public (string code, string userId) CreateRoom(string connectionId, string displayName, string? avatar)
    {
        lock (_gate)
        {
            string code;
            do { code = NewCode(); } while (_rooms.ContainsKey(code));
            var room = new Room { Code = code };
            var userId = Guid.NewGuid().ToString("N");
            room.Members.Add(new RoomMember { ConnectionId = connectionId, UserId = userId, DisplayName = displayName, AvatarBase64 = avatar, IsHost = true });
            _rooms[code] = room;
            return (code, userId);
        }
    }

    public bool TryJoin(string code, string connectionId, string displayName, string? avatar, out string? error, out string userId)
    {
        userId = ""; error = null;
        lock (_gate)
        {
            if (!_rooms.TryGetValue(code, out var room)) { error = "ROOM_NOT_FOUND"; return false; }
            if (room.Members.Count >= RoomLimits.MaxParticipants) { error = "ROOM_FULL"; return false; }
            userId = Guid.NewGuid().ToString("N");
            room.Members.Add(new RoomMember { ConnectionId = connectionId, UserId = userId, DisplayName = displayName, AvatarBase64 = avatar, IsHost = false });
            return true;
        }
    }

    public RoomLeaveResult Leave(string connectionId)
    {
        lock (_gate)
        {
            foreach (var room in _rooms.Values)
            {
                var m = room.Members.FirstOrDefault(x => x.ConnectionId == connectionId);
                if (m is null) continue;
                room.Members.Remove(m);
                if (room.Members.Count == 0) { _rooms.TryRemove(room.Code, out _); return new(room.Code, null, true); }
                if (m.IsHost)
                {
                    var next = room.Members.OrderBy(x => x.JoinedUtc).First();
                    next.IsHost = true;
                    return new(room.Code, next.UserId, false);
                }
                return new(room.Code, null, false);
            }
            return new(null, null, false);
        }
    }

    public RoomStateDto? GetState(string code) { lock (_gate) return _rooms.TryGetValue(code, out var r) ? r.ToState() : null; }
    public List<ElementDto> GetSnapshot(string code) { lock (_gate) return _rooms.TryGetValue(code, out var r) ? r.Elements.Values.OrderBy(e => e.ZOrder).ToList() : new(); }
    public bool IsHost(string code, string connectionId) { lock (_gate) return _rooms.TryGetValue(code, out var r) && r.Members.Any(x => x.ConnectionId == connectionId && x.IsHost); }
    public bool IsLocked(string code) { lock (_gate) return _rooms.TryGetValue(code, out var r) && r.IsLocked; }
    public void SetLock(string code, bool locked) { lock (_gate) { if (_rooms.TryGetValue(code, out var r)) r.IsLocked = locked; } }
    public string? FindConnectionId(string code, string userId) { lock (_gate) return _rooms.TryGetValue(code, out var r) ? r.Members.FirstOrDefault(x => x.UserId == userId)?.ConnectionId : null; }

    public void ApplyAdd(string code, ElementDto e) { if (_rooms.TryGetValue(code, out var r)) lock (_gate) r.Elements[e.Id] = e; }
    public void ApplyUpdate(string code, ElementDto e) => ApplyAdd(code, e);
    public void ApplyRemove(string code, Guid id) { if (_rooms.TryGetValue(code, out var r)) lock (_gate) r.Elements.Remove(id); }
    public void ApplyClear(string code) { if (_rooms.TryGetValue(code, out var r)) lock (_gate) r.Elements.Clear(); }

    private static string NewCode()
    {
        Span<char> c = stackalloc char[RoomLimits.CodeLength];
        for (int i = 0; i < c.Length; i++) c[i] = Alphabet[_rng.Next(Alphabet.Length)];
        return new string(c);
    }
}
