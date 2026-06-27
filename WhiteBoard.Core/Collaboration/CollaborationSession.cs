using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SketchRoom.Realtime.Client;
using SketchRoom.Realtime.Contracts;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Collaboration;

// Orchestrates an IWhiteboardClient against the local canvas.
//  - Local edits (freehand draw/erase/clear) -> mapped via ElementMapper -> sent to the hub.
//  - Remote ops -> applied to the canvas via IRemoteCanvasApplier under an _applyingRemote
//    guard so they are not re-broadcast (echo suppression).
// Singleton; the per-tab canvas attaches/detaches as tabs are selected.
public sealed class CollaborationSession
{
    public const string DefaultLocalUrl = "http://localhost:5000";

    private readonly IWhiteboardClient _client;
    private readonly object _gate = new();

    private IRemoteCanvasApplier? _applier;
    private IDrawingService? _localDrawing;

    private bool _applyingRemote;
    private readonly Dictionary<FreeDrawStroke, Guid> _localStrokeIds = new();
    private List<ElementDto>? _pendingSnapshot;

    private DateTime _lastCursorSentUtc = DateTime.MinValue;
    private static readonly TimeSpan CursorInterval = TimeSpan.FromMilliseconds(50);

    private string _displayName = string.Empty;
    private string? _avatar;

    public bool IsActive { get; private set; }
    public bool IsHost { get; private set; }
    public string RoomCode { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public RoomStateDto? State { get; private set; }

    // True when the room is locked and the local user is not the host.
    // The UI can bind to StateChanged to refresh this.
    public bool IsLockedForMe => IsActive && !IsHost && (State?.IsLocked ?? false);

    public event Action<RoomStateDto>? StateChanged;
    public event Action? KickedFromRoom;

    public CollaborationSession(IWhiteboardClient client)
    {
        _client = client;

        _client.ElementAdded   += dto => RunOnUi(() => ApplyGuarded(() => _applier?.ApplyElement(dto)));
        _client.ElementUpdated += dto => RunOnUi(() => ApplyGuarded(() => _applier?.UpdateElement(dto)));
        _client.ElementRemoved += id  => RunOnUi(() => ApplyGuarded(() => _applier?.RemoveElement(id)));
        _client.BoardCleared   += ()  => RunOnUi(() => ApplyGuarded(() => _applier?.Clear()));
        _client.LivePointed    += p   => RunOnUi(() => ApplyGuarded(() => _applier?.ApplyLivePoint(p)));
        _client.CursorMoved    += c   => RunOnUi(() => ApplyGuarded(() => _applier?.MoveCursor(c.UserId, c.X, c.Y, null)));

        _client.RoomStateChanged += s =>
        {
            State = s;
            IsHost = s.Participants.FirstOrDefault(p => p.UserId == UserId)?.IsHost ?? IsHost;
            RunOnUi(() => StateChanged?.Invoke(s));
        };
        _client.UserJoined       += _ => { };
        _client.UserLeft         += _ => { };
        _client.Kicked           += () => RunOnUi(async () => { await LeaveAsync(); KickedFromRoom?.Invoke(); });
        _client.Reconnected      += OnReconnectedAsync;
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    public async Task<JoinResultDto> StartHostAsync(string baseUrl, string displayName, string? avatar)
    {
        _displayName = displayName;
        _avatar = avatar;
        await _client.ConnectAsync(string.IsNullOrWhiteSpace(baseUrl) ? DefaultLocalUrl : baseUrl);
        var res = await _client.CreateRoomAsync(displayName, avatar);
        if (res.Success)
        {
            UserId = res.UserId;
            RoomCode = res.State?.RoomCode ?? string.Empty;
            State = res.State;
            IsHost = true;
            IsActive = true;
        }
        return res;
    }

    public async Task<JoinResultDto> StartJoinAsync(string baseUrl, string code, string displayName, string? avatar)
    {
        _displayName = displayName;
        _avatar = avatar;
        await _client.ConnectAsync(string.IsNullOrWhiteSpace(baseUrl) ? DefaultLocalUrl : baseUrl);
        var res = await _client.JoinRoomAsync(code, displayName, avatar);
        if (res.Success)
        {
            UserId = res.UserId;
            RoomCode = res.State?.RoomCode ?? code;
            State = res.State;
            IsHost = false;
            IsActive = true;
            QueueSnapshot(res.Snapshot);
        }
        return res;
    }

    // ── Host moderation (no-op unless hosting an active room) ─────────────────────

    public Task ClearBoardAsync()
        => IsActive && IsHost ? _client.ClearBoardAsync(RoomCode) : Task.CompletedTask;

    public Task SetLockAsync(bool locked)
        => IsActive && IsHost ? _client.SetLockAsync(RoomCode, locked) : Task.CompletedTask;

    public Task KickUserAsync(string userId)
        => IsActive && IsHost ? _client.KickUserAsync(RoomCode, userId) : Task.CompletedTask;

    public async Task LeaveAsync()
    {
        if (!IsActive) return;
        IsActive = false;
        IsHost = false;
        RoomCode = string.Empty;
        lock (_gate) _localStrokeIds.Clear();
        _pendingSnapshot = null;
        try { await _client.DisposeAsync(); } catch { /* best-effort teardown */ }
    }

    // ── Canvas attach ────────────────────────────────────────────────────────────

    public void AttachCanvas(IRemoteCanvasApplier applier, IDrawingService localDrawing)
    {
        DetachCanvas();
        _applier = applier;
        _localDrawing = localDrawing;

        localDrawing.StrokeStarted    += OnLocalStrokeStarted;
        localDrawing.StrokePointAdded += OnLocalStrokePointAdded;
        localDrawing.StrokeFinished   += OnLocalStrokeFinished;
        localDrawing.StrokeRemoved    += OnLocalStrokeRemoved;
        localDrawing.BoardCleared     += OnLocalBoardCleared;

        if (_pendingSnapshot is { } snap)
        {
            _pendingSnapshot = null;
            RunOnUi(() => ApplyGuarded(() => _applier?.ApplySnapshot(snap)));
        }
    }

    public void DetachCanvas()
    {
        if (_localDrawing != null)
        {
            _localDrawing.StrokeStarted    -= OnLocalStrokeStarted;
            _localDrawing.StrokePointAdded -= OnLocalStrokePointAdded;
            _localDrawing.StrokeFinished   -= OnLocalStrokeFinished;
            _localDrawing.StrokeRemoved    -= OnLocalStrokeRemoved;
            _localDrawing.BoardCleared     -= OnLocalBoardCleared;
        }
        _localDrawing = null;
        _applier = null;
    }

    // ── Local -> remote (freehand + clear) ───────────────────────────────────────

    private void OnLocalStrokeStarted(FreeDrawStroke stroke)
    {
        if (!IsActive || _applyingRemote || IsLockedForMe) return;
        lock (_gate) _localStrokeIds[stroke] = Guid.NewGuid();
    }

    private void OnLocalStrokePointAdded(FreeDrawStroke stroke, Point pt)
    {
        if (!IsActive || _applyingRemote) return;
        Guid id;
        lock (_gate)
        {
            if (!_localStrokeIds.TryGetValue(stroke, out id)) return;
        }
        FireAndForget(_client.LivePointAsync(RoomCode, new LivePointDto
        {
            ElementId = id,
            OwnerUserId = UserId,
            X = pt.X,
            Y = pt.Y
        }));
    }

    private void OnLocalStrokeFinished(FreeDrawStroke stroke)
    {
        if (!IsActive || _applyingRemote) return;
        Guid id;
        lock (_gate)
        {
            if (!_localStrokeIds.TryGetValue(stroke, out id)) return;
        }
        FireAndForget(_client.AddElementAsync(RoomCode, ElementMapper.ToDto(stroke, id)));
    }

    private void OnLocalStrokeRemoved(FreeDrawStroke stroke)
    {
        if (!IsActive || _applyingRemote) return;
        Guid id;
        lock (_gate)
        {
            if (!_localStrokeIds.TryGetValue(stroke, out id)) return;
            _localStrokeIds.Remove(stroke);
        }
        FireAndForget(_client.RemoveElementAsync(RoomCode, id));
    }

    private void OnLocalBoardCleared()
    {
        if (!IsActive || _applyingRemote) return;
        lock (_gate) _localStrokeIds.Clear();
        FireAndForget(_client.ClearBoardAsync(RoomCode));
    }

    public void ReportLocalCursor(double x, double y)
    {
        if (!IsActive || _applyingRemote) return;
        var now = DateTime.UtcNow;
        if (now - _lastCursorSentUtc < CursorInterval) return;
        _lastCursorSentUtc = now;
        FireAndForget(_client.MoveCursorAsync(RoomCode, x, y));
    }

    // Send a non-freehand element produced locally (shapes/text/images/connectors).
    // Wired in once the canvas tools raise creation events (see Task 7 report).
    public void SendElementAdded(ElementDto dto)
    {
        if (!IsActive || _applyingRemote || IsLockedForMe) return;
        FireAndForget(_client.AddElementAsync(RoomCode, dto));
    }

    public void SendElementUpdated(ElementDto dto)
    {
        if (!IsActive || _applyingRemote || IsLockedForMe) return;
        FireAndForget(_client.UpdateElementAsync(RoomCode, dto));
    }

    public void SendElementRemoved(Guid id)
    {
        if (!IsActive || _applyingRemote || IsLockedForMe) return;
        FireAndForget(_client.RemoveElementAsync(RoomCode, id));
    }

    // ── Reconnect reconciliation ─────────────────────────────────────────────────

    private async Task OnReconnectedAsync()
    {
        if (!IsActive || string.IsNullOrEmpty(RoomCode)) return;
        try
        {
            var res = await _client.JoinRoomAsync(RoomCode, _displayName, _avatar);
            if (!res.Success) return;
            UserId = res.UserId;
            State = res.State;
            IsHost = res.State?.Participants.FirstOrDefault(p => p.UserId == UserId)?.IsHost ?? false;
            var snap = res.Snapshot ?? new List<ElementDto>();
            RunOnUi(() => ApplyGuarded(() =>
            {
                _applier?.Clear();
                _applier?.ApplySnapshot(snap);
            }));
            if (res.State != null)
                RunOnUi(() => StateChanged?.Invoke(res.State));
        }
        catch { /* best-effort: room may no longer exist after reconnect */ }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private void QueueSnapshot(List<ElementDto> snapshot)
    {
        if (_applier != null)
            RunOnUi(() => ApplyGuarded(() => _applier?.ApplySnapshot(snapshot)));
        else
            _pendingSnapshot = snapshot;
    }

    private void ApplyGuarded(Action apply)
    {
        _applyingRemote = true;
        try { apply(); }
        finally { _applyingRemote = false; }
    }

    private static void RunOnUi(Action action)
    {
        var d = Application.Current?.Dispatcher;
        if (d == null || d.CheckAccess()) action();
        else d.BeginInvoke(action, DispatcherPriority.Send);
    }

    private static void FireAndForget(Task task)
    {
        _ = task.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
