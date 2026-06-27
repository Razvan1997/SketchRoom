using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SketchRoom.Realtime.Contracts;
using SketchRoom.Toolkit.Wpf.Controls;
using WhiteBoard.Core.Collaboration;
using WhiteBoard.Core.Helpers;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.Collaboration;

// View-layer implementation of IRemoteCanvasApplier. Renders remote ops onto the
// live WhiteBoardControl. Freehand is fully supported; shapes/connectors reuse the
// snapshot-restore pipeline (HandleSavedElements) defensively. All members run on
// the WPF UI thread (CollaborationSession marshals).
public sealed class WhiteBoardCanvasApplier : IRemoteCanvasApplier
{
    private readonly WhiteBoardControl _wb;
    private readonly IDrawingService _drawing;
    private readonly IGenericShapeFactory _shapeFactory;

    private readonly Dictionary<Guid, FreeDrawStroke> _remoteStrokes = new();
    private readonly Dictionary<string, BitmapImage?> _avatarCache = new();
    private Guid _liveStrokeId = Guid.Empty;

    public WhiteBoardCanvasApplier(WhiteBoardControl wb, IDrawingService drawing, IGenericShapeFactory shapeFactory)
    {
        _wb = wb;
        _drawing = drawing;
        _shapeFactory = shapeFactory;
    }

    public void ApplyElement(ElementDto dto)
    {
        if (dto.Type == ElementType.Freehand)
        {
            ApplyFreehand(dto);
            return;
        }
        TryRenderNonFreehand(dto);
    }

    public void UpdateElement(ElementDto dto)
    {
        // Coarse update: drop the prior visual (freehand) then re-apply.
        RemoveElement(dto.Id);
        ApplyElement(dto);
    }

    public void RemoveElement(Guid id)
    {
        if (_remoteStrokes.TryGetValue(id, out var stroke))
        {
            _drawing.RemoveStroke(stroke);
            _remoteStrokes.Remove(id);
            return;
        }

        var idStr = id.ToString();
        var canvas = _wb.DrawingCanvasPublic;

        var shapeVisual = canvas.Children.OfType<FrameworkElement>()
            .FirstOrDefault(fe => string.Equals(ShapeMetadata.GetShapeId(fe), idStr, StringComparison.OrdinalIgnoreCase));
        if (shapeVisual != null)
            canvas.Children.Remove(shapeVisual);

        var conn = _wb._connections.FirstOrDefault(c =>
            c.Visual is FrameworkElement cfe &&
            string.Equals(ShapeMetadata.GetShapeId(cfe), idStr, StringComparison.OrdinalIgnoreCase));
        if (conn != null)
        {
            if (conn.Visual is FrameworkElement cv) canvas.Children.Remove(cv);
            if (conn.ConnectionDot != null) canvas.Children.Remove(conn.ConnectionDot);
            _wb._connections.Remove(conn);
        }
    }

    public void Clear()
    {
        _drawing.Clear();
        _remoteStrokes.Clear();
    }

    public void ApplyLivePoint(LivePointDto p)
    {
        if (_liveStrokeId != p.ElementId)
        {
            _wb.StartNewRemoteLine();
            _liveStrokeId = p.ElementId;
        }
        _wb.AddLivePoint(new Point(p.X, p.Y), Brushes.DarkBlue);
    }

    public void MoveCursor(string userId, double x, double y, string? avatarBase64)
    {
        _wb.MoveCursorImage(new Point(x, y), ResolveAvatar(userId, avatarBase64));
    }

    public void ApplySnapshot(IReadOnlyList<ElementDto> elements)
    {
        Clear();

        var shapes = new List<BPMNShapeModelWithPosition>();
        var connections = new List<BPMNConnectionExportModel>();

        foreach (var dto in elements)
        {
            if (dto.Type == ElementType.Freehand)
            {
                ApplyFreehand(dto);
                continue;
            }

            var model = ElementMapper.ToElement(dto);
            switch (model)
            {
                case BPMNShapeModelWithPosition shape: shapes.Add(shape); break;
                case BPMNConnectionExportModel conn: connections.Add(conn); break;
            }
        }

        // Re-link connectors against the freshly restored shapes (handoff note 1):
        // connectors carry only path geometry over the wire, so the node map binds
        // whatever endpoints survived; missing endpoints fall back to path points.
        try
        {
            HandleSavedElements.RestoreShapes(shapes, _wb, _shapeFactory, nodeMap =>
                HandleSavedElements.RestoreConnections(connections, _wb, nodeMap));
        }
        catch { /* best-effort shape restore; freehand already applied */ }
    }

    private void ApplyFreehand(ElementDto dto)
    {
        var color = ParseBrush(dto.StrokeColor);
        _wb.StartNewRemoteLine();
        _wb.AddLine(dto.Points.Select(p => new Point(p.X, p.Y)), color, dto.Thickness);
        _liveStrokeId = Guid.Empty;

        var stroke = _drawing.RecentStrokes.LastOrDefault();
        if (stroke != null)
            _remoteStrokes[dto.Id] = stroke;
    }

    private void TryRenderNonFreehand(ElementDto dto)
    {
        try
        {
            var model = ElementMapper.ToElement(dto);
            switch (model)
            {
                case BPMNShapeModelWithPosition shape:
                    HandleSavedElements.RestoreShapes(new List<BPMNShapeModelWithPosition> { shape }, _wb, _shapeFactory, _ => { });
                    break;
                case BPMNConnectionExportModel conn:
                    HandleSavedElements.RestoreConnections(new List<BPMNConnectionExportModel> { conn }, _wb, new Dictionary<string, BPMNNode>());
                    break;
            }
        }
        catch { /* best-effort; unsupported remote element types are ignored */ }
    }

    private BitmapImage? ResolveAvatar(string userId, string? avatarBase64)
    {
        if (_avatarCache.TryGetValue(userId, out var cached))
            return cached;

        BitmapImage? image = null;
        if (!string.IsNullOrEmpty(avatarBase64))
        {
            try
            {
                using var ms = new System.IO.MemoryStream(Convert.FromBase64String(avatarBase64));
                image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
            }
            catch { image = null; }
        }

        _avatarCache[userId] = image;
        return image;
    }

    private static Brush ParseBrush(string hex)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { return Brushes.Black; }
    }
}
