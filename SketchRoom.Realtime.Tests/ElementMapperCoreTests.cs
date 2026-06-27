using System;
using System.Collections.Generic;
using System.Linq;
using SketchRoom.Realtime.Contracts;
using Xunit;

public class ElementMapperCoreTests
{
    // ── Color hex round-trip ──────────────────────────────────────────────────

    [Theory]
    [InlineData("#1A2B3C", "#1A2B3C")]
    [InlineData("#ff0000", "#FF0000")]
    [InlineData("#000000", "#000000")]
    [InlineData("#FFFFFF", "#FFFFFF")]
    [InlineData("#aabbcc", "#AABBCC")]
    public void NormalizeColorHex_RoundTrip(string input, string expected)
    {
        Assert.Equal(expected, ElementMapperCore.NormalizeColorHex(input));
    }

    [Theory]
    [InlineData("not-a-color")]
    [InlineData("1A2B3C")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("")]
    public void NormalizeColorHex_ThrowsOnInvalid(string bad)
    {
        Assert.ThrowsAny<Exception>(() => ElementMapperCore.NormalizeColorHex(bad));
    }

    // ── PointDto round-trip ───────────────────────────────────────────────────

    [Fact]
    public void PointDto_RoundTrip_PreservesValues()
    {
        var dto = new PointDto { X = 1.5, Y = 2.75 };
        var (x, y) = ElementMapperCore.FromPointDto(dto);
        var back = ElementMapperCore.ToPointDto(x, y);

        Assert.Equal(dto.X, back.X);
        Assert.Equal(dto.Y, back.Y);
    }

    [Fact]
    public void PointDto_ZeroValues_RoundTrip()
    {
        var dto = ElementMapperCore.ToPointDto(0.0, 0.0);
        var (x, y) = ElementMapperCore.FromPointDto(dto);
        Assert.Equal(0.0, x);
        Assert.Equal(0.0, y);
    }

    // ── Bounds round-trip ─────────────────────────────────────────────────────

    [Fact]
    public void Bounds_PackUnpack_RoundTrip()
    {
        var src = new ElementDto { X = 10, Y = 20, Width = 100, Height = 50 };
        var (x, y, w, h) = ElementMapperCore.UnpackBounds(src);

        var target = new ElementDto();
        ElementMapperCore.PackBounds(target, x, y, w, h);

        Assert.Equal(src.X, target.X);
        Assert.Equal(src.Y, target.Y);
        Assert.Equal(src.Width, target.Width);
        Assert.Equal(src.Height, target.Height);
    }

    // ── ElementDto deep-clone / round-trip ───────────────────────────────────

    [Fact]
    public void Clone_Freehand_PreservesAllFields()
    {
        var id = Guid.NewGuid();
        var src = new ElementDto
        {
            Id = id,
            Type = ElementType.Freehand,
            Points = new List<PointDto>
            {
                new() { X = 1, Y = 2 },
                new() { X = 3, Y = 4 }
            },
            StrokeColor = "#FF0000",
            Thickness = 3.0,
            ZOrder = 1,
            OwnerUserId = "u1"
        };

        var clone = ElementMapperCore.Clone(src);

        Assert.Equal(id, clone.Id);
        Assert.Equal(ElementType.Freehand, clone.Type);
        Assert.Equal(2, clone.Points.Count);
        Assert.Equal(1.0, clone.Points[0].X);
        Assert.Equal(2.0, clone.Points[0].Y);
        Assert.Equal("#FF0000", clone.StrokeColor);
        Assert.Equal(3.0, clone.Thickness);
        Assert.Equal(1, clone.ZOrder);
        Assert.Equal("u1", clone.OwnerUserId);

        // independence: mutating clone must not affect src
        clone.Points[0].X = 99;
        Assert.Equal(1.0, src.Points[0].X);
    }

    [Fact]
    public void Clone_Rectangle_PreservesGeometryAndStyle()
    {
        var id = Guid.NewGuid();
        var src = new ElementDto
        {
            Id = id,
            Type = ElementType.Rectangle,
            X = 10, Y = 20, Width = 80, Height = 60,
            StrokeColor = "#000000",
            FillColor = "#AABBCC",
            Thickness = 2.0
        };

        var clone = ElementMapperCore.Clone(src);

        Assert.Equal(id, clone.Id);
        Assert.Equal(ElementType.Rectangle, clone.Type);
        Assert.Equal(10.0, clone.X);
        Assert.Equal(20.0, clone.Y);
        Assert.Equal(80.0, clone.Width);
        Assert.Equal(60.0, clone.Height);
        Assert.Equal("#000000", clone.StrokeColor);
        Assert.Equal("#AABBCC", clone.FillColor);
    }

    [Fact]
    public void Clone_Ellipse_PreservesGeometryAndStyle()
    {
        var id = Guid.NewGuid();
        var src = new ElementDto
        {
            Id = id,
            Type = ElementType.Ellipse,
            X = 5, Y = 15, Width = 40, Height = 40,
            StrokeColor = "#112233",
            FillColor = "#FFFFFF",
            Thickness = 1.5
        };

        var clone = ElementMapperCore.Clone(src);

        Assert.Equal(id, clone.Id);
        Assert.Equal(ElementType.Ellipse, clone.Type);
        Assert.Equal(5.0, clone.X);
        Assert.Equal(15.0, clone.Y);
        Assert.Equal(40.0, clone.Width);
        Assert.Equal(40.0, clone.Height);
        Assert.Equal("#112233", clone.StrokeColor);
        Assert.Equal("#FFFFFF", clone.FillColor);
    }

    [Fact]
    public void Clone_Line_PreservesEndpoints()
    {
        var id = Guid.NewGuid();
        var src = new ElementDto
        {
            Id = id,
            Type = ElementType.Line,
            Points = new List<PointDto>
            {
                new() { X = 0, Y = 0 },
                new() { X = 100, Y = 100 }
            },
            StrokeColor = "#0000FF",
            Thickness = 2.0
        };

        var clone = ElementMapperCore.Clone(src);

        Assert.Equal(id, clone.Id);
        Assert.Equal(ElementType.Line, clone.Type);
        Assert.Equal(2, clone.Points.Count);
        Assert.Equal(0.0, clone.Points[0].X);
        Assert.Equal(0.0, clone.Points[0].Y);
        Assert.Equal(100.0, clone.Points[1].X);
        Assert.Equal(100.0, clone.Points[1].Y);
    }

    [Fact]
    public void Clone_Connector_PreservesMultiplePoints()
    {
        var id = Guid.NewGuid();
        var src = new ElementDto
        {
            Id = id,
            Type = ElementType.Connector,
            Points = new List<PointDto>
            {
                new() { X = 0, Y = 0 },
                new() { X = 50, Y = 25 },
                new() { X = 100, Y = 0 }
            },
            StrokeColor = "#333333",
            Thickness = 1.0
        };

        var clone = ElementMapperCore.Clone(src);

        Assert.Equal(id, clone.Id);
        Assert.Equal(ElementType.Connector, clone.Type);
        Assert.Equal(3, clone.Points.Count);
        Assert.Equal(50.0, clone.Points[1].X);
        Assert.Equal(25.0, clone.Points[1].Y);
    }

    [Fact]
    public void Clone_Text_PreservesContentAndPosition()
    {
        var id = Guid.NewGuid();
        var src = new ElementDto
        {
            Id = id,
            Type = ElementType.Text,
            X = 30, Y = 40, Width = 120, Height = 30,
            StrokeColor = "#000000",
            FillColor = "#FFFFFF",
            Text = "Hello World",
            Thickness = 1.0
        };

        var clone = ElementMapperCore.Clone(src);

        Assert.Equal(id, clone.Id);
        Assert.Equal(ElementType.Text, clone.Type);
        Assert.Equal(30.0, clone.X);
        Assert.Equal(40.0, clone.Y);
        Assert.Equal("Hello World", clone.Text);
    }

    [Fact]
    public void Clone_Image_PreservesBase64AndBounds()
    {
        var id = Guid.NewGuid();
        const string base64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwADhQGAWjR9awAAAABJRU5ErkJggg==";
        var src = new ElementDto
        {
            Id = id,
            Type = ElementType.Image,
            X = 50, Y = 60, Width = 200, Height = 150,
            ImageBase64 = base64
        };

        var clone = ElementMapperCore.Clone(src);

        Assert.Equal(id, clone.Id);
        Assert.Equal(ElementType.Image, clone.Type);
        Assert.Equal(50.0, clone.X);
        Assert.Equal(60.0, clone.Y);
        Assert.Equal(200.0, clone.Width);
        Assert.Equal(150.0, clone.Height);
        Assert.Equal(base64, clone.ImageBase64);
    }

    [Fact]
    public void Clone_IsDeepCopy_NotSameReference()
    {
        var src = new ElementDto
        {
            Type = ElementType.Freehand,
            Points = new List<PointDto> { new() { X = 1, Y = 2 } }
        };

        var clone = ElementMapperCore.Clone(src);

        Assert.NotSame(src, clone);
        Assert.NotSame(src.Points, clone.Points);
    }

    // ── GeometryEqual helper ──────────────────────────────────────────────────

    [Fact]
    public void GeometryEqual_SamePoints_ReturnsTrue()
    {
        var a = new ElementDto
        {
            Type = ElementType.Freehand,
            Points = new List<PointDto> { new() { X = 1, Y = 2 } }
        };
        var b = ElementMapperCore.Clone(a);

        Assert.True(ElementMapperCore.GeometryEqual(a, b));
    }

    [Fact]
    public void GeometryEqual_SameBounds_ReturnsTrue()
    {
        var a = new ElementDto { Type = ElementType.Rectangle, X = 1, Y = 2, Width = 3, Height = 4 };
        var b = ElementMapperCore.Clone(a);

        Assert.True(ElementMapperCore.GeometryEqual(a, b));
    }

    [Fact]
    public void GeometryEqual_DifferentTypes_ReturnsFalse()
    {
        var a = new ElementDto { Type = ElementType.Rectangle, X = 1, Y = 2, Width = 3, Height = 4 };
        var b = new ElementDto { Type = ElementType.Ellipse, X = 1, Y = 2, Width = 3, Height = 4 };

        Assert.False(ElementMapperCore.GeometryEqual(a, b));
    }
}
