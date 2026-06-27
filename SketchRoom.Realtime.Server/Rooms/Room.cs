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
