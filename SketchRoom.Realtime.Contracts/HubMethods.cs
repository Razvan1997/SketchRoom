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
