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
