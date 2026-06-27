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
