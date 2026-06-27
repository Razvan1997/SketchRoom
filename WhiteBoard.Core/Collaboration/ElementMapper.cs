using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SketchRoom.Models.Enums;
using SketchRoom.Realtime.Contracts;
using WhiteBoard.Core.Models;

namespace WhiteBoard.Core.Collaboration;

/// <summary>
/// Maps between WPF app element representations and the transport <see cref="ElementDto"/>.
/// Pure geometry/style conversions are delegated to <see cref="ElementMapperCore"/> (net8.0,
/// tested independently).
///
/// Limitations (for Task 7):
///  - BPMN SVG shapes (SvgUri-based) lose their SvgUri on round-trip; they are transported
///    as ElementType.Rectangle with bounding-box geometry.  Task 7 must decide whether to
///    reconstruct them from SvgUri (stored in ExtraProperties["SvgUri"]) or treat them as
///    plain rectangles.
///  - BPMNConnectionExportModel carries FromId/ToId and BezierSegments which have no
///    ElementDto fields; they are dropped on ToDto and cannot be reconstructed from the DTO
///    alone.  Task 7 must re-link connectors after the canvas snapshot is applied.
///  - ImageElement.ToDto requires a base64 encoding step that reads the BitmapImage pixels;
///    the caller should prefer caching the base64 string to avoid repeated encoding.
/// </summary>
public static class ElementMapper
{
    // ── Freehand ─────────────────────────────────────────────────────────────

    /// <param name="id">
    /// Stable identifier for this stroke.  Pass a cached Guid so the same stroke
    /// keeps the same Id across multiple ToDto calls.
    /// </param>
    public static ElementDto ToDto(FreeDrawStroke stroke, Guid id)
    {
        ArgumentNullException.ThrowIfNull(stroke);

        var colorHex = TryGetHex(stroke.Color) ?? "#000000";

        return new ElementDto
        {
            Id = id,
            Type = ElementType.Freehand,
            Points = stroke.Points
                          .Select(p => ElementMapperCore.ToPointDto(p.X, p.Y))
                          .ToList(),
            StrokeColor = ElementMapperCore.NormalizeColorHex(colorHex),
            Thickness = stroke.Thickness
        };
    }

    public static FreeDrawStrokeExportModel FreehandFromDto(ElementDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new FreeDrawStrokeExportModel
        {
            Id = dto.Id,
            Points = dto.Points
                        .Select(p => new Point(p.X, p.Y))
                        .ToList(),
            StrokeColorHex = dto.StrokeColor,
            StrokeThickness = dto.Thickness
        };
    }

    // ── Rectangle ─────────────────────────────────────────────────────────────

    public static ElementDto ToDto(BPMNShapeModelWithPosition shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        var type = shape.Type switch
        {
            ShapeType.Ellipse   => ElementType.Ellipse,
            ShapeType.ShapeText => ElementType.Text,
            ShapeType.Image     => ElementType.Image,
            // All other BPMN shapes (SVG-backed) map to Rectangle as best-fit.
            // SvgUri is preserved in ExtraProperties["SvgUri"] for Task 7 reconstruction.
            _                   => ElementType.Rectangle
        };

        var strokeHex  = shape.StrokeHex ?? shape.ExtraProperties?.GetValueOrDefault("Stroke") ?? "#000000";
        var fillHex    = shape.BackgroundHex ?? shape.ExtraProperties?.GetValueOrDefault("Fill");
        var textContent = shape.Text
                          ?? shape.ExtraProperties?.GetValueOrDefault("Text")
                          ?? shape.ExtraProperties?.GetValueOrDefault("TextShape");

        var dto = new ElementDto
        {
            Id = shape.Id == Guid.Empty ? Guid.NewGuid() : shape.Id,
            Type = type,
            X = shape.Left,
            Y = shape.Top,
            Width = shape.Width,
            Height = shape.Height,
            StrokeColor = SafeNormalizeHex(strokeHex),
            FillColor = fillHex is null ? null : SafeNormalizeHex(fillHex),
            // BPMNShapeModelWithPosition has no stroke-thickness field; 2.0 is a best-effort default.
            Thickness = 2.0,
            Text = type == ElementType.Text ? textContent : null
        };

        // Preserve SvgUri so Task 7 can reconstruct BPMN SVG shapes.
        if (shape.SvgUri is not null && type == ElementType.Rectangle)
            dto.Text = $"bpmn:svguri={shape.SvgUri}";

        // Preserve Image base64 if stored in extra properties; otherwise encode the
        // local SvgUri file so the bitmap travels to peers that lack the source path.
        if (type == ElementType.Image)
            dto.ImageBase64 = shape.ExtraProperties?.GetValueOrDefault("ImageBase64")
                              ?? TryEncodeImageFile(shape.SvgUri);

        return dto;
    }

    public static BPMNShapeModelWithPosition ShapeFromDto(ElementDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var shapeType = dto.Type switch
        {
            ElementType.Ellipse => ShapeType.Ellipse,
            ElementType.Text    => ShapeType.ShapeText,
            ElementType.Image   => ShapeType.Image,
            _                   => ShapeType.Rectangle
        };

        var shape = new BPMNShapeModelWithPosition
        {
            Id = dto.Id,
            Type = shapeType,
            Left = dto.X,
            Top = dto.Y,
            Width = dto.Width,
            Height = dto.Height,
            StrokeHex = dto.StrokeColor,
            BackgroundHex = dto.FillColor,
            Category = "General",
            ExtraProperties = new Dictionary<string, string>()
        };

        if (shapeType == ShapeType.ShapeText && dto.Text is not null)
        {
            shape.Text = dto.Text;
            // ShapeStyleRestorer reads the text body from ExtraProperties["Text"].
            shape.ExtraProperties["Text"] = dto.Text;
        }

        if (shapeType == ShapeType.Image && dto.ImageBase64 is not null)
        {
            shape.ExtraProperties["ImageBase64"] = dto.ImageBase64;
            // Materialize a local file so the existing SvgUri-based image render works.
            shape.SvgUri = TryMaterializeImageFile(dto.ImageBase64) ?? shape.SvgUri;
        }

        // Recover SvgUri hint for Task 7.
        if (shapeType == ShapeType.Rectangle && dto.Text?.StartsWith("bpmn:svguri=") == true)
        {
            var uriStr = dto.Text["bpmn:svguri=".Length..];
            if (Uri.TryCreate(uriStr, UriKind.RelativeOrAbsolute, out var svgUri))
                shape.SvgUri = svgUri;
        }

        return shape;
    }

    // ── Connection (Line / Connector) ─────────────────────────────────────────

    public static ElementDto ToDto(BPMNConnectionExportModel conn, Guid id)
    {
        ArgumentNullException.ThrowIfNull(conn);

        // IsCurved → Connector; straight polyline → Line (2 pts) or Connector (>2).
        var type = (conn.IsCurved || conn.PathPoints.Count > 2)
            ? ElementType.Connector
            : ElementType.Line;

        var strokeHex = conn.StrokeHex ?? "#000000";

        return new ElementDto
        {
            Id = id,
            Type = type,
            Points = conn.PathPoints
                         .Select(p => ElementMapperCore.ToPointDto(p.X, p.Y))
                         .ToList(),
            StrokeColor = SafeNormalizeHex(strokeHex),
            // BPMNConnectionExportModel has no stroke-thickness field; 2.0 is a best-effort default.
            Thickness = 2.0
        };
    }

    public static BPMNConnectionExportModel ConnectionFromDto(ElementDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BPMNConnectionExportModel
        {
            ShapeId = dto.Id.ToString(),
            PathPoints = dto.Points.Select(p => new Point(p.X, p.Y)).ToList(),
            StrokeHex = dto.StrokeColor,
            IsCurved = dto.Type == ElementType.Connector,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ── Image ─────────────────────────────────────────────────────────────────

    /// <param name="base64Png">
    /// Caller-supplied base64 encoding of the image.  The caller must encode
    /// once and cache; this method does not read <see cref="ImageElement.Image"/>
    /// pixels to avoid repeated expensive encoding.
    /// </param>
    public static ElementDto ToDto(ImageElement image, string base64Png, Guid id)
    {
        ArgumentNullException.ThrowIfNull(image);

        return new ElementDto
        {
            Id = id,
            Type = ElementType.Image,
            X = image.Bounds.X,
            Y = image.Bounds.Y,
            Width = image.Bounds.Width,
            Height = image.Bounds.Height,
            ImageBase64 = base64Png
        };
    }

    /// <summary>
    /// Encodes a <see cref="BitmapImage"/> to PNG base64 synchronously.
    /// Must be called on the WPF UI thread.
    /// </summary>
    public static string EncodeBitmapToPngBase64(BitmapImage bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return Convert.ToBase64String(stream.ToArray());
    }

    // ── Generic entry points ──────────────────────────────────────────────────

    /// <summary>
    /// Dispatches to the appropriate overload.  Freehand strokes and connections
    /// require a stable <paramref name="id"/> from a caller-maintained registry.
    /// <para>
    /// <see cref="ImageElement"/> is handled by encoding its bitmap inline via
    /// <see cref="EncodeBitmapToPngBase64"/>; this must run on the WPF UI thread.
    /// For hot paths, prefer calling <c>ToDto(image, cachedBase64, id)</c> directly
    /// to avoid repeated encoding.
    /// </para>
    /// </summary>
    public static ElementDto? TryToDto(object element, Guid id = default)
    {
        if (id == Guid.Empty) id = Guid.NewGuid();

        return element switch
        {
            FreeDrawStroke stroke          => ToDto(stroke, id),
            BPMNShapeModelWithPosition s   => ToDto(s),
            BPMNConnectionExportModel conn => ToDto(conn, id),
            ImageElement img               => ToDto(img, EncodeBitmapToPngBase64(img.Image), id),
            _ => null
        };
    }

    /// <summary>
    /// Reconstructs an export model from <paramref name="dto"/>.
    /// Returns one of: <see cref="FreeDrawStrokeExportModel"/>,
    /// <see cref="BPMNShapeModelWithPosition"/>, or <see cref="BPMNConnectionExportModel"/>.
    /// </summary>
    public static object? ToElement(ElementDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return dto.Type switch
        {
            ElementType.Freehand  => FreehandFromDto(dto),
            ElementType.Line or
            ElementType.Connector => ConnectionFromDto(dto),
            _                     => ShapeFromDto(dto)
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? TryGetHex(Brush? brush)
        => (brush as SolidColorBrush)?.Color.ToString();

    // Encodes a local image file as "ext:base64" so peers can reconstruct it. Best-effort.
    private static string? TryEncodeImageFile(Uri? svgUri)
    {
        try
        {
            if (svgUri is null || !svgUri.IsFile) return null;
            var path = svgUri.LocalPath;
            if (!File.Exists(path)) return null;
            var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = "png";
            return $"{ext}:{Convert.ToBase64String(File.ReadAllBytes(path))}";
        }
        catch { return null; }
    }

    // Writes an "ext:base64" payload to a temp file and returns its file Uri. Best-effort.
    public static Uri? TryMaterializeImageFile(string? encoded)
    {
        try
        {
            if (string.IsNullOrEmpty(encoded)) return null;
            var sep = encoded.IndexOf(':');
            var ext = sep > 0 ? encoded[..sep] : "png";
            var b64 = sep > 0 ? encoded[(sep + 1)..] : encoded;
            var bytes = Convert.FromBase64String(b64);
            var dir = Path.Combine(Path.GetTempPath(), "SketchRoomRemoteImages");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"{Guid.NewGuid():N}.{ext}");
            File.WriteAllBytes(file, bytes);
            return new Uri(file);
        }
        catch { return null; }
    }

    private static string SafeNormalizeHex(string hex)
    {
        try { return ElementMapperCore.NormalizeColorHex(hex); }
        catch { return "#000000"; }
    }
}
