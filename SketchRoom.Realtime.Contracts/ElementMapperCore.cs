namespace SketchRoom.Realtime.Contracts;

/// <summary>
/// Pure, UI-free helpers for color/point/bounds/DTO operations used by both
/// the testable layer (net8.0) and the WPF mapper (net8.0-windows).
/// </summary>
public static class ElementMapperCore
{
    // ── Color ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Normalizes a CSS hex color string to uppercase #RRGGBB or #AARRGGBB.
    /// Throws <see cref="ArgumentException"/> if the format is not valid.
    /// </summary>
    public static string NormalizeColorHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            throw new ArgumentException("Color hex must not be null or empty.", nameof(hex));

        var trimmed = hex.Trim();
        if (!trimmed.StartsWith('#'))
            throw new ArgumentException($"Color hex must start with '#': '{hex}'", nameof(hex));

        var body = trimmed[1..].ToUpperInvariant();
        if (body.Length is not (6 or 8))
            throw new ArgumentException($"Color hex must be 6 or 8 hex digits: '{hex}'", nameof(hex));

        // Validate all characters are hex digits
        foreach (var c in body)
            if (!Uri.IsHexDigit(c))
                throw new ArgumentException($"Invalid hex character in color: '{hex}'", nameof(hex));

        return "#" + body;
    }

    // ── PointDto ──────────────────────────────────────────────────────────────

    public static PointDto ToPointDto(double x, double y) => new() { X = x, Y = y };

    public static (double X, double Y) FromPointDto(PointDto pt)
    {
        ArgumentNullException.ThrowIfNull(pt);
        return (pt.X, pt.Y);
    }

    // ── Bounds ────────────────────────────────────────────────────────────────

    public static (double X, double Y, double Width, double Height) UnpackBounds(ElementDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return (dto.X, dto.Y, dto.Width, dto.Height);
    }

    public static void PackBounds(ElementDto dto, double x, double y, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.X = x;
        dto.Y = y;
        dto.Width = width;
        dto.Height = height;
    }

    // ── Deep clone ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a deep copy of <paramref name="src"/>. Points list is cloned so
    /// mutations on the copy do not affect the original.
    /// </summary>
    public static ElementDto Clone(ElementDto src)
    {
        ArgumentNullException.ThrowIfNull(src);

        return new ElementDto
        {
            Id = src.Id,
            Type = src.Type,
            Points = src.Points.Select(p => new PointDto { X = p.X, Y = p.Y }).ToList(),
            X = src.X,
            Y = src.Y,
            Width = src.Width,
            Height = src.Height,
            StrokeColor = src.StrokeColor,
            FillColor = src.FillColor,
            Thickness = src.Thickness,
            Text = src.Text,
            ImageBase64 = src.ImageBase64,
            ZOrder = src.ZOrder,
            OwnerUserId = src.OwnerUserId
        };
    }

    // ── Geometry equality ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when two DTOs have the same type and identical geometry
    /// (points for Freehand/Line/Connector; X/Y/Width/Height otherwise).
    /// </summary>
    public static bool GeometryEqual(ElementDto a, ElementDto b)
    {
        if (a is null || b is null) return false;
        if (a.Type != b.Type) return false;

        return a.Type switch
        {
            ElementType.Freehand or ElementType.Line or ElementType.Connector =>
                a.Points.Count == b.Points.Count &&
                a.Points.Zip(b.Points, (p1, p2) => p1.X == p2.X && p1.Y == p2.Y).All(eq => eq),
            _ =>
                a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height
        };
    }
}
