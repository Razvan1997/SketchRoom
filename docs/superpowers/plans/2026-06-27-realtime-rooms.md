# SketchRoom Realtime Rooms — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a working real-time collaboration "rooms" system for SketchRoom where one user opens a room and others join (LAN or online via a central server) and everyone draws together live.

**Architecture:** A shared contracts assembly + an ASP.NET Core SignalR hub (`WhiteboardHub`) backed by an in-memory `RoomManager` that holds authoritative per-room element state. The same hub runs online (central server) and embedded in the desktop host for LAN. A typed client layer raises events the WPF canvas consumes; every element op is relayed to the room group and applied everywhere; late joiners get a snapshot.

**Tech Stack:** .NET 8, ASP.NET Core SignalR + MessagePack protocol, Prism 9 + DryIoc (WPF), xUnit.

## Global Constraints

- Transport: **SignalR with MessagePack** protocol on both client and server.
- Online rooms → **central ASP.NET Core server**. LAN rooms → **same hub embedded (Kestrel) in the desktop host**.
- Identity: **room code (6 chars) + display name + avatar**, no accounts/login.
- Roles: **everyone draws; host moderates (kick / clear / lock)** — moderation enforced **server-side** (caller must be host).
- State: **relay + in-memory authoritative snapshot**, **ephemeral** rooms; late-join receives full `Snapshot`; nothing persisted server-side.
- Sync **all** element types via a single `ElementDto` (not only freehand).
- `CursorMoved` carries **no avatar bytes** (avatar sent once on `UserJoined`, keyed by `userId`).
- Host leaves → **promote oldest remaining participant**; empty room is discarded.
- Max **12** participants per room (constant `RoomLimits.MaxParticipants`).
- Reconnect → auto re-join + re-request snapshot to reconcile.
- New test project `SketchRoom.Realtime.Tests` (xUnit) — the solution currently has **zero** tests.
- Reuse existing models where possible: `SketchRoom.Models.Participant`, `LocalUser`. New transport types live in `SketchRoom.Realtime.Contracts`.
- All new C# uses nullable reference types enabled (match solution default).

---

### Task 1: Contracts assembly — DTOs + hub/client method names

**Files:**
- Create: `SketchRoom.Realtime.Contracts/SketchRoom.Realtime.Contracts.csproj` (net8.0)
- Create: `SketchRoom.Realtime.Contracts/ElementDto.cs`
- Create: `SketchRoom.Realtime.Contracts/RoomDtos.cs`
- Create: `SketchRoom.Realtime.Contracts/HubMethods.cs`
- Create: `SketchRoom.Realtime.Contracts/RoomLimits.cs`
- Modify: `SketchRoom.sln` (add project)

**Interfaces:**
- Produces: `ElementDto`, `ElementType` enum, `RoomParticipantDto`, `RoomStateDto`, `JoinResultDto`, `CursorDto`, `LivePointDto`, static `HubMethods` (string constants for invoke/on names), `RoomLimits.MaxParticipants = 12`, `RoomLimits.CodeLength = 6`.

- [ ] **Step 1: Create the csproj**

`SketchRoom.Realtime.Contracts/SketchRoom.Realtime.Contracts.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Element model**

`SketchRoom.Realtime.Contracts/ElementDto.cs`:
```csharp
namespace SketchRoom.Realtime.Contracts;

public enum ElementType { Freehand, Rectangle, Ellipse, Line, Connector, Text, Image }

public sealed class PointDto
{
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class ElementDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ElementType Type { get; set; }
    public List<PointDto> Points { get; set; } = new();   // Freehand/Line/Connector
    public double X { get; set; }                          // bounds for shapes
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string StrokeColor { get; set; } = "#000000";
    public string? FillColor { get; set; }
    public double Thickness { get; set; } = 2.0;
    public string? Text { get; set; }
    public string? ImageBase64 { get; set; }
    public int ZOrder { get; set; }
    public string OwnerUserId { get; set; } = "";
}
```

- [ ] **Step 3: Room DTOs**

`SketchRoom.Realtime.Contracts/RoomDtos.cs`:
```csharp
namespace SketchRoom.Realtime.Contracts;

public sealed class RoomParticipantDto
{
    public string UserId { get; set; } = "";       // stable per session (Guid string)
    public string DisplayName { get; set; } = "";
    public string? AvatarBase64 { get; set; }
    public bool IsHost { get; set; }
}

public sealed class RoomStateDto
{
    public string RoomCode { get; set; } = "";
    public bool IsLocked { get; set; }
    public List<RoomParticipantDto> Participants { get; set; } = new();
}

public sealed class JoinResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }              // e.g. "ROOM_NOT_FOUND","ROOM_FULL"
    public RoomStateDto? State { get; set; }
    public List<ElementDto> Snapshot { get; set; } = new();
    public string UserId { get; set; } = "";
}

public sealed class CursorDto
{
    public string UserId { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class LivePointDto
{
    public Guid ElementId { get; set; }
    public string OwnerUserId { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
}
```

- [ ] **Step 4: Method-name constants + limits**

`SketchRoom.Realtime.Contracts/HubMethods.cs`:
```csharp
namespace SketchRoom.Realtime.Contracts;

// Server methods invoked by client:
public static class HubMethods
{
    public const string CreateRoom = "CreateRoom";       // (DisplayName, AvatarBase64?) -> JoinResultDto
    public const string JoinRoom = "JoinRoom";           // (code, DisplayName, AvatarBase64?) -> JoinResultDto
    public const string AddElement = "AddElement";       // (code, ElementDto)
    public const string UpdateElement = "UpdateElement"; // (code, ElementDto)
    public const string RemoveElement = "RemoveElement"; // (code, Guid)
    public const string ClearBoard = "ClearBoard";       // (code)  host-only
    public const string LivePoint = "LivePoint";         // (code, LivePointDto)
    public const string MoveCursor = "MoveCursor";       // (code, x, y)
    public const string KickUser = "KickUser";           // (code, userId)  host-only
    public const string SetLock = "SetLock";             // (code, bool)    host-only
}

// Client callbacks invoked by server:
public static class ClientMethods
{
    public const string ElementAdded = "ElementAdded";       // ElementDto
    public const string ElementUpdated = "ElementUpdated";   // ElementDto
    public const string ElementRemoved = "ElementRemoved";   // Guid
    public const string BoardCleared = "BoardCleared";       // ()
    public const string LivePointed = "LivePointed";         // LivePointDto
    public const string CursorMoved = "CursorMoved";         // CursorDto
    public const string UserJoined = "UserJoined";           // RoomParticipantDto
    public const string UserLeft = "UserLeft";               // userId
    public const string RoomStateChanged = "RoomStateChanged"; // RoomStateDto
    public const string Kicked = "Kicked";                   // ()  (you were kicked)
}
```

`SketchRoom.Realtime.Contracts/RoomLimits.cs`:
```csharp
namespace SketchRoom.Realtime.Contracts;

public static class RoomLimits
{
    public const int MaxParticipants = 12;
    public const int CodeLength = 6;
}
```

- [ ] **Step 5: Add to solution & build**

```bash
cd /c/Users/Danut/source/repos/SketchRoom
dotnet sln add SketchRoom.Realtime.Contracts/SketchRoom.Realtime.Contracts.csproj
dotnet build SketchRoom.Realtime.Contracts/SketchRoom.Realtime.Contracts.csproj
```
Expected: build succeeds, 0 errors.

- [ ] **Step 6: Commit**
```bash
git add SketchRoom.Realtime.Contracts SketchRoom.sln
git commit -m "feat(realtime): contracts assembly (ElementDto, room DTOs, hub method names)"
```

---

### Task 2: RoomManager (in-memory authoritative state) — TDD

**Files:**
- Create: `SketchRoom.Realtime.Server/SketchRoom.Realtime.Server.csproj` (Microsoft.NET.Sdk.Web)
- Create: `SketchRoom.Realtime.Server/Rooms/Room.cs`
- Create: `SketchRoom.Realtime.Server/Rooms/RoomManager.cs`
- Create: `SketchRoom.Realtime.Tests/SketchRoom.Realtime.Tests.csproj` (xUnit)
- Create: `SketchRoom.Realtime.Tests/RoomManagerTests.cs`
- Modify: `SketchRoom.sln`

**Interfaces:**
- Consumes: `ElementDto`, `RoomParticipantDto`, `RoomStateDto`, `RoomLimits` (Task 1).
- Produces:
  - `RoomManager.CreateRoom(string connectionId, string displayName, string? avatar) -> (string roomCode, string userId)`
  - `RoomManager.TryJoin(string code, string connectionId, string displayName, string? avatar, out string? error, out string userId) -> bool`
  - `RoomManager.Leave(string connectionId) -> RoomLeaveResult` (`{ RoomCode, NewHostUserId?, RoomRemoved }`)
  - `RoomManager.GetState(string code) -> RoomStateDto?`
  - `RoomManager.GetSnapshot(string code) -> List<ElementDto>`
  - `RoomManager.ApplyAdd/Update(string code, ElementDto)`, `ApplyRemove(string code, Guid)`, `ApplyClear(string code)`
  - `RoomManager.IsHost(string code, string connectionId) -> bool`
  - `RoomManager.SetLock(string code, bool) `, `RoomManager.IsLocked(string code) -> bool`
  - `RoomManager.FindConnectionId(string code, string userId) -> string?`

- [ ] **Step 1: Create server + test projects, add references**

`SketchRoom.Realtime.Server/SketchRoom.Realtime.Server.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Protocols.MessagePack" Version="8.0.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\SketchRoom.Realtime.Contracts\SketchRoom.Realtime.Contracts.csproj" />
  </ItemGroup>
</Project>
```
`SketchRoom.Realtime.Tests/SketchRoom.Realtime.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.*" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Protocols.MessagePack" Version="8.0.*" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\SketchRoom.Realtime.Server\SketchRoom.Realtime.Server.csproj" />
    <ProjectReference Include="..\SketchRoom.Realtime.Contracts\SketchRoom.Realtime.Contracts.csproj" />
  </ItemGroup>
</Project>
```
```bash
dotnet sln add SketchRoom.Realtime.Server/SketchRoom.Realtime.Server.csproj SketchRoom.Realtime.Tests/SketchRoom.Realtime.Tests.csproj
```

- [ ] **Step 2: Write failing tests**

`SketchRoom.Realtime.Tests/RoomManagerTests.cs`:
```csharp
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
}
```

- [ ] **Step 3: Run tests — expect FAIL (types missing)**
```bash
dotnet test SketchRoom.Realtime.Tests
```
Expected: compile error / fail (RoomManager not defined yet).

- [ ] **Step 4: Implement Room + RoomManager**

`SketchRoom.Realtime.Server/Rooms/Room.cs`:
```csharp
using System.Collections.Concurrent;
using SketchRoom.Realtime.Contracts;

namespace SketchRoom.Realtime.Server.Rooms;

public sealed class RoomMember
{
    public required string ConnectionId { get; set; }
    public required string UserId { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarBase64 { get; set; }
    public bool IsHost { get; set; }
    public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Room
{
    public required string Code { get; init; }
    public bool IsLocked { get; set; }
    public List<RoomMember> Members { get; } = new();
    public Dictionary<Guid, ElementDto> Elements { get; } = new();

    public RoomStateDto ToState() => new()
    {
        RoomCode = Code,
        IsLocked = IsLocked,
        Participants = Members.Select(x => new RoomParticipantDto
        {
            UserId = x.UserId, DisplayName = x.DisplayName,
            AvatarBase64 = x.AvatarBase64, IsHost = x.IsHost
        }).ToList()
    };
}
```

`SketchRoom.Realtime.Server/Rooms/RoomManager.cs`:
```csharp
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

    public RoomStateDto? GetState(string code) => _rooms.TryGetValue(code, out var r) ? r.ToState() : null;
    public List<ElementDto> GetSnapshot(string code) => _rooms.TryGetValue(code, out var r) ? r.Elements.Values.OrderBy(e => e.ZOrder).ToList() : new();
    public bool IsHost(string code, string connectionId) => _rooms.TryGetValue(code, out var r) && r.Members.Any(x => x.ConnectionId == connectionId && x.IsHost);
    public bool IsLocked(string code) => _rooms.TryGetValue(code, out var r) && r.IsLocked;
    public void SetLock(string code, bool locked) { if (_rooms.TryGetValue(code, out var r)) r.IsLocked = locked; }
    public string? FindConnectionId(string code, string userId) => _rooms.TryGetValue(code, out var r) ? r.Members.FirstOrDefault(x => x.UserId == userId)?.ConnectionId : null;

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
```

- [ ] **Step 5: Run tests — expect PASS**
```bash
dotnet test SketchRoom.Realtime.Tests
```
Expected: all RoomManager tests pass.

- [ ] **Step 6: Commit**
```bash
git add SketchRoom.Realtime.Server SketchRoom.Realtime.Tests SketchRoom.sln
git commit -m "feat(realtime): RoomManager in-memory authoritative state + unit tests"
```

---

### Task 3: WhiteboardHub (SignalR) + server host + integration test

**Files:**
- Create: `SketchRoom.Realtime.Server/Hubs/WhiteboardHub.cs`
- Create: `SketchRoom.Realtime.Server/Program.cs`
- Create: `SketchRoom.Realtime.Server/appsettings.json`
- Create: `SketchRoom.Realtime.Tests/HubIntegrationTests.cs`

**Interfaces:**
- Consumes: `RoomManager` (Task 2), `HubMethods`, `ClientMethods`, all DTOs.
- Produces: hub at route `"/whiteboardhub"`; `RoomManager` registered as singleton; MessagePack enabled. A public `Program` partial class so `WebApplicationFactory<Program>` works in tests.

- [ ] **Step 1: Implement the hub**

`SketchRoom.Realtime.Server/Hubs/WhiteboardHub.cs`:
```csharp
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

    private string CurrentUserId(string code) =>
        _rooms.GetState(code)?.Participants.FirstOrDefault()?.UserId ?? ""; // replaced in Task 9 with connection→user map
}
```
> NOTE (Task 9 hardens `CurrentUserId` via a connectionId→userId lookup added to RoomManager; for now MoveCursor userId is approximate and only cosmetic).

- [ ] **Step 2: Program.cs + appsettings**

`SketchRoom.Realtime.Server/Program.cs`:
```csharp
using SketchRoom.Realtime.Server.Hubs;
using SketchRoom.Realtime.Server.Rooms;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR().AddMessagePackProtocol();
builder.Services.AddSingleton<RoomManager>();

var app = builder.Build();
app.MapHub<WhiteboardHub>("/whiteboardhub");
app.MapGet("/health", () => "ok");
app.Run();

public partial class Program { }
```
`SketchRoom.Realtime.Server/appsettings.json`:
```json
{ "Kestrel": { "Endpoints": { "Http": { "Url": "http://0.0.0.0:5000" } } },
  "Logging": { "LogLevel": { "Default": "Information" } } }
```

- [ ] **Step 3: Integration test (failing first)**

`SketchRoom.Realtime.Tests/HubIntegrationTests.cs`:
```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
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
```

- [ ] **Step 4: Run — expect PASS**
```bash
dotnet test SketchRoom.Realtime.Tests --filter HubIntegrationTests
```
Expected: passes (broadcast + snapshot work).

- [ ] **Step 5: Commit**
```bash
git add SketchRoom.Realtime.Server SketchRoom.Realtime.Tests
git commit -m "feat(realtime): WhiteboardHub + server host + integration test"
```

---

### Task 4: Embedded LAN server (host-in-app)

**Files:**
- Create: `SketchRoom.Realtime.Server/EmbeddedRealtimeServer.cs`
- Create: `SketchRoom.Realtime.Tests/EmbeddedServerTests.cs`

**Interfaces:**
- Produces: `EmbeddedRealtimeServer` with `Task<string> StartAsync(int port = 5000)` returning the bound base URL (`http://<lan-ip>:<port>`), `Task StopAsync()`, and `static string GetLanIpv4()`.

- [ ] **Step 1: Implement embedded server**

`SketchRoom.Realtime.Server/EmbeddedRealtimeServer.cs`:
```csharp
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
```

- [ ] **Step 2: Test it starts and a client connects**

`SketchRoom.Realtime.Tests/EmbeddedServerTests.cs`:
```csharp
using Microsoft.AspNetCore.SignalR.Client;
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
```

- [ ] **Step 3: Run — expect PASS**
```bash
dotnet test SketchRoom.Realtime.Tests --filter EmbeddedServerTests
```
Expected: passes (uses real localhost socket on port 5099).

- [ ] **Step 4: Commit**
```bash
git add SketchRoom.Realtime.Server SketchRoom.Realtime.Tests
git commit -m "feat(realtime): embedded Kestrel server for LAN hosting"
```

---

### Task 5: Client layer (`IWhiteboardClient` + SignalR impl)

**Files:**
- Create: `SketchRoom.Realtime.Client/SketchRoom.Realtime.Client.csproj` (net8.0-windows to match app usage; references SignalR.Client + MessagePack + Contracts)
- Create: `SketchRoom.Realtime.Client/IWhiteboardClient.cs`
- Create: `SketchRoom.Realtime.Client/SignalRWhiteboardClient.cs`
- Modify: `SketchRoom.sln`
- Modify (later removal): old `SketchRoom.Services/WhiteboardHubClient.cs` is replaced — leave it until Task 7 rewires, then delete in Task 7.

**Interfaces:**
- Produces `IWhiteboardClient`:
  - `Task ConnectAsync(string baseUrl)`
  - `Task<JoinResultDto> CreateRoomAsync(string displayName, string? avatar)`
  - `Task<JoinResultDto> JoinRoomAsync(string code, string displayName, string? avatar)`
  - `Task AddElementAsync(string code, ElementDto e)` / `UpdateElementAsync` / `RemoveElementAsync(string code, Guid id)` / `ClearBoardAsync(string code)`
  - `Task LivePointAsync(string code, LivePointDto p)` / `MoveCursorAsync(string code, double x, double y)`
  - `Task SetLockAsync(string code, bool locked)` / `KickUserAsync(string code, string userId)`
  - events: `event Action<ElementDto>? ElementAdded; ElementUpdated; event Action<Guid>? ElementRemoved; event Action? BoardCleared; event Action<LivePointDto>? LivePointed; event Action<CursorDto>? CursorMoved; event Action<RoomParticipantDto>? UserJoined; event Action<string>? UserLeft; event Action<RoomStateDto>? RoomStateChanged; event Action? Kicked;`
  - `Task DisposeAsync()`

- [ ] **Step 1: Project + interface**

csproj references: `Microsoft.AspNetCore.SignalR.Client` 8.0.*, `Microsoft.AspNetCore.SignalR.Protocols.MessagePack` 8.0.*, ProjectReference Contracts.

`SketchRoom.Realtime.Client/IWhiteboardClient.cs` — declare the interface exactly as in **Interfaces** above.

- [ ] **Step 2: Implementation**

`SketchRoom.Realtime.Client/SignalRWhiteboardClient.cs`:
```csharp
using Microsoft.AspNetCore.SignalR.Client;
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

    public async Task ConnectAsync(string baseUrl)
    {
        if (_conn is { State: HubConnectionState.Connected }) return;
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
    public async Task DisposeAsync() { if (_conn != null) await _conn.DisposeAsync(); }
}
```

- [ ] **Step 3: Integration test reusing the embedded server**

Add to `SketchRoom.Realtime.Tests/EmbeddedServerTests.cs` a `[Fact]` that starts `EmbeddedRealtimeServer` on port 5098, connects two `SignalRWhiteboardClient`, host `CreateRoomAsync`, guest `JoinRoomAsync`, host `AddElementAsync`, assert guest's `ElementAdded` event fires with same Id (use a `TaskCompletionSource<ElementDto>`).

- [ ] **Step 4: Run — expect PASS**, then **Commit**
```bash
dotnet test SketchRoom.Realtime.Tests
git add SketchRoom.Realtime.Client SketchRoom.Realtime.Tests SketchRoom.sln
git commit -m "feat(realtime): typed IWhiteboardClient + SignalR implementation"
```

---

### Task 6: Element ↔ ElementDto mapper (all element types) — TDD

**Files:**
- Read first: `WhiteBoard.Core/Services/DrawingService.cs`, `WhiteBoard.Core/Tools/*`, `SketchRoom.Models/Shapes/BPMNShapeModel.cs`, `WhiteBoardModule/XAML/Shapes/**` to learn the in-app element representations (freehand `Polyline`, shape `UserControl`s, connector `Path`, text).
- Create: `WhiteBoard.Core/Collaboration/ElementMapper.cs`
- Create: `SketchRoom.Realtime.Tests/ElementMapperTests.cs` (add ProjectReference to `WhiteBoard.Core` + needed WPF refs; if WPF types block headless test, put mapper's pure geometry/style conversion in a UI-free helper `ElementMapperCore` and test that)
- Modify: `WhiteBoard.Core/WhiteBoard.Core.csproj` (ProjectReference to Contracts)

**Interfaces:**
- Consumes: `ElementDto`, `ElementType`, app element model.
- Produces: `ElementMapper.ToDto(<appElement>) -> ElementDto` and `ElementMapper.ToElement(ElementDto) -> <appElement>` covering Freehand, Rectangle, Ellipse, Line, Connector, Text, Image. Round-trip must preserve geometry + style + Id.

- [ ] **Step 1: Write round-trip tests** for each `ElementType` (freehand points; rectangle/ellipse bounds; line/connector endpoints; text content+position; image base64+bounds). Assert `ToDto` then `ToElement` then `ToDto` again is value-equal on geometry/style/Id.
- [ ] **Step 2: Run — expect FAIL.**
- [ ] **Step 3: Implement `ElementMapper`** following the real app element types discovered in the read step. Keep WPF-specific construction isolated; pure conversions in `ElementMapperCore` for testability.
- [ ] **Step 4: Run — expect PASS.**
- [ ] **Step 5: Commit** `feat(realtime): element<->dto mapper for all element types`.

---

### Task 7: Wire collaboration into the canvas (replace dead wiring)

**Files:**
- Read first: `WhiteBoardModule/Views/WhiteBoardView.xaml.cs` (`WhiteBoardView_Loaded` — commented wiring), `WhiteBoardModule/ViewModels/WhiteBoardViewModel.cs`, `WhiteBoard.Core/Colaboration/Services/ColaborationService.cs` (host-only guards to remove), `WhiteBoard.Core/Services/DrawingService.cs` (local add/update/remove/clear hooks), `WhiteBoard.Core/Tools/FreeDrawTool.cs` (stroke start/point/finish events).
- Create: `WhiteBoard.Core/Collaboration/CollaborationSession.cs` (orchestrates client ↔ canvas; holds current room code + my userId + isHost)
- Modify: `WhiteBoardModule/Views/WhiteBoardView.xaml.cs` and `WhiteBoardViewModel.cs` (subscribe local element events → `CollaborationSession.Send*`; subscribe client events → apply to canvas via `DrawingService` + `ElementMapper`)
- Delete: `SketchRoom.Services/WhiteboardHubClient.cs` and the obsolete `ColaborationService.cs` host-only logic (replace with `CollaborationSession` using `IWhiteboardClient`)
- Modify: `SketchRoom/Bootstrapper.cs` — register `IWhiteboardClient -> SignalRWhiteboardClient` and `CollaborationSession` (singleton) in the DryIoc container.

**Interfaces:**
- Consumes: `IWhiteboardClient` (Task 5), `ElementMapper` (Task 6), `RoomManager` semantics.
- Produces: `CollaborationSession` with `StartHostAsync/StartJoinAsync(...)`, `bool IsActive`, `bool IsHost`, and it applies remote ops to the canvas + sends local ops; guards against echo (don't re-send elements that arrived remotely — tag by ownerUserId / suppress-during-apply flag).

- [ ] **Step 1:** Implement `CollaborationSession` that: on local element add/update/remove/clear → call client `*Async`; on client events → set a `_applyingRemote` guard, apply via `ElementMapper`+`DrawingService`, clear guard. Live freehand: forward `LivePoint` during draw, finalize with `AddElement`. Cursor: throttle `MoveCursorAsync` (e.g., max ~20/s).
- [ ] **Step 2:** Re-enable & rewrite `WhiteBoardView_Loaded` to subscribe the real events (no commented code). Remove `if(!_isHost) return` guards — everyone draws.
- [ ] **Step 3:** Register services in `Bootstrapper.cs`.
- [ ] **Step 4: Manual verification (documented):** Run two instances locally against `EmbeddedRealtimeServer` (or `dotnet run` the server); host creates a room, guest joins by code; draw freehand + a rectangle + text on each side; confirm both appear on the other; confirm late joiner gets snapshot; confirm cursor shows without lag/avatar spam. Record results in the commit message.
- [ ] **Step 5: Commit** `feat(realtime): wire collaboration into canvas; remove dead ColaborationService/WhiteboardHubClient`.

---

### Task 8: Lobby & Participation UI (create/join/moderate)

**Files:**
- Read first: `LobbyHostingModule/**`, `ParticipationModule/**`, `UsersInteractionsModule/ViewModels/UsersInteractionsViewModel.cs` (commented nav commands), `SketchRoom.Models/LocalUser.cs` (name/avatar source).
- Modify: `LobbyHostingModule` views/VMs — "Create room" with **Online/LAN** toggle; show room **code** (and **IP:port** for LAN); participants list; host controls (Kick per participant, Clear board, Lock toggle).
- Modify: `ParticipationModule` — "Join" screen: name/avatar (prefill from `LocalUser`), code field (+ host IP for LAN), error display for `ROOM_NOT_FOUND`/`ROOM_FULL`/`Kicked`.
- Modify: `UsersInteractionsModule/ViewModels/UsersInteractionsViewModel.cs` — re-enable navigation commands to Lobby/Participation.

**Interfaces:**
- Consumes: `CollaborationSession` (Task 7), `IWhiteboardClient`, `EmbeddedRealtimeServer` (LAN), `RoomStateDto`/`RoomParticipantDto` for the participant list + host controls.

- [ ] **Step 1:** Lobby create flow — Online: `CollaborationSession.StartHostAsync(centralUrl,...)`; LAN: start `EmbeddedRealtimeServer`, then host against returned base URL. Bind room code + (LAN) IP to UI.
- [ ] **Step 2:** Participants list bound to `RoomStateChanged`; host-only Kick/Clear/Lock buttons calling client methods.
- [ ] **Step 3:** Participation join flow → `StartJoinAsync`; handle errors + `Kicked` (navigate out + toast).
- [ ] **Step 4:** Re-enable `UsersInteractionsViewModel` nav.
- [ ] **Step 5: Manual verification:** full flow create→join→draw→kick→lock→clear across two instances (online via local `dotnet run` server, and LAN via embedded). Document in commit.
- [ ] **Step 6: Commit** `feat(realtime): lobby/participation UI + host moderation; re-enable session navigation`.

---

### Task 9: Robustness — reconnect reconciliation, accurate cursor identity, lock on canvas

**Files:**
- Modify: `SketchRoom.Realtime.Server/Rooms/RoomManager.cs` — add `string? GetUserId(string connectionId)` (connectionId→userId map) so the hub stamps `CursorMoved`/ownership accurately.
- Modify: `SketchRoom.Realtime.Server/Hubs/WhiteboardHub.cs` — replace `CurrentUserId` stub with `_rooms.GetUserId(Context.ConnectionId)`.
- Modify: `SketchRoom.Realtime.Client/SignalRWhiteboardClient.cs` — on `Reconnected`, raise an event so the session re-joins + re-requests snapshot.
- Modify: `WhiteBoard.Core/Collaboration/CollaborationSession.cs` — on reconnect: re-join room, clear canvas, apply fresh snapshot; honor `IsLocked` (block local draws when locked and not host).

**Interfaces:**
- Consumes: existing.
- Produces: `IWhiteboardClient.Reconnected` event; `RoomManager.GetUserId`.

- [ ] **Step 1: Test** `RoomManager.GetUserId` returns the right userId for a connection and null after leave. Run/fail/implement/pass.
- [ ] **Step 2: Test (integration)** reconnect path: simulate guest drop + reconnect → guest receives snapshot containing host's elements. (Use a fresh connection to emulate; assert snapshot.)
- [ ] **Step 3:** Implement lock-on-canvas: when `RoomStateDto.IsLocked` and not host, `CollaborationSession` blocks local element creation + shows a hint.
- [ ] **Step 4: Run all tests — PASS. Commit** `feat(realtime): reconnect reconciliation, cursor identity, lock enforcement`.

---

### Task 10: Configuration & final end-to-end

**Files:**
- Modify: `SketchRoom/` settings (use existing `SettingsData`/settings storage) — store central server base URL; default to a placeholder you set (e.g. `https://<your-server>`); LAN doesn't need it.
- Modify: remove any remaining hardcoded `http://localhost:5000` references (grep the solution).
- Create: `SketchRoom.Realtime.Server/README.md` — how to run locally (`dotnet run`) and deploy (container) the central server.

- [ ] **Step 1:** Grep + remove hardcoded URLs: `grep -rn "localhost:5000" --include=*.cs .` → none remain except tests.
- [ ] **Step 2:** Wire central URL from settings into Online create/join.
- [ ] **Step 3: Full manual E2E checklist** (record in commit): online (local server) 3 clients draw all element types simultaneously; LAN 2 clients; late join snapshot; kick; lock; clear; host-leave promotes; reconnect reconciles. Build whole solution: `dotnet build SketchRoom.sln`.
- [ ] **Step 4: Commit** `feat(realtime): server config + docs + final E2E`.

---

## Self-Review

**Spec coverage:** transport SignalR+MessagePack → Tasks 1,3,5; central server → Task 3; LAN embedded → Task 4; code+name/avatar identity → Tasks 3,8; everyone-draws + host moderation server-enforced → Tasks 3 (hub guards),7,8; relay+authoritative snapshot+ephemeral+late-join → Tasks 2,3; all element types → Tasks 1(ElementDto),6(mapper),7; cursor without avatar → Task 1(CursorDto)/3(MoveCursor); host-leave promotion → Task 2; max 12 → Task 1/2; reconnect reconciliation → Task 9; tests project → Tasks 2+; reuse Participant/LocalUser → Tasks 6,8. All spec sections covered.

**Placeholder scan:** Greenfield tasks (1–5, 9) contain complete code + concrete tests. WPF-integration tasks (6–8) intentionally start with a "read these exact files" step because the precise canvas code depends on the real element classes; each still names exact files, the interfaces produced, representative logic, and concrete manual-verification criteria — not vague "handle it" steps. The `CurrentUserId` stub in Task 3 is explicitly flagged and resolved in Task 9.

**Type consistency:** `HubMethods`/`ClientMethods` names used identically in hub (T3) and client (T5). `JoinResultDto`, `ElementDto`, `CursorDto`, `LivePointDto`, `RoomStateDto`, `RoomParticipantDto` defined in T1 and consumed unchanged in T2/T3/T5. `RoomManager` method signatures in T2 match hub calls in T3 and additions in T9 (`GetUserId`). `IWhiteboardClient` surface in T5 matches `CollaborationSession` usage in T7/T9.

**Known live-verify points:** ElementMapper must match real app element classes (T6 read step); canvas event names in WhiteBoardView (T7 read step). These are flagged to read-then-implement, not guessed.
