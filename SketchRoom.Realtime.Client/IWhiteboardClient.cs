using SketchRoom.Realtime.Contracts;

namespace SketchRoom.Realtime.Client;

public interface IWhiteboardClient
{
    event Action<ElementDto>? ElementAdded;
    event Action<ElementDto>? ElementUpdated;
    event Action<Guid>? ElementRemoved;
    event Action? BoardCleared;
    event Action<LivePointDto>? LivePointed;
    event Action<CursorDto>? CursorMoved;
    event Action<RoomParticipantDto>? UserJoined;
    event Action<string>? UserLeft;
    event Action<RoomStateDto>? RoomStateChanged;
    event Action? Kicked;
    event Func<Task>? Reconnected;

    Task ConnectAsync(string baseUrl);
    Task<JoinResultDto> CreateRoomAsync(string displayName, string? avatar);
    Task<JoinResultDto> JoinRoomAsync(string code, string displayName, string? avatar);
    Task AddElementAsync(string code, ElementDto e);
    Task UpdateElementAsync(string code, ElementDto e);
    Task RemoveElementAsync(string code, Guid id);
    Task ClearBoardAsync(string code);
    Task LivePointAsync(string code, LivePointDto p);
    Task MoveCursorAsync(string code, double x, double y);
    Task SetLockAsync(string code, bool locked);
    Task KickUserAsync(string code, string userId);
    Task DisposeAsync();
}
