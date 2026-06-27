using System;
using System.Collections.Generic;
using SketchRoom.Realtime.Contracts;

namespace WhiteBoard.Core.Collaboration;

// Canvas-side port for the collaboration layer. Implemented in the view layer
// (WhiteBoardModule) where the live WhiteBoardControl + shape factories live, so
// CollaborationSession (transport orchestration) stays free of upward dependencies.
// All members are invoked on the WPF UI thread by CollaborationSession.
public interface IRemoteCanvasApplier
{
    void ApplyElement(ElementDto dto);
    void UpdateElement(ElementDto dto);
    void RemoveElement(Guid id);
    void Clear();
    void ApplyLivePoint(LivePointDto p);
    void MoveCursor(string userId, double x, double y, string? avatarBase64);
    void ApplySnapshot(IReadOnlyList<ElementDto> elements);
}
