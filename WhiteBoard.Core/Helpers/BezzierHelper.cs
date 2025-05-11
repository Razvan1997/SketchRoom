using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WhiteBoard.Core.Helpers
{
    public static class BezierHelper
    {
        public static PathGeometry GenerateSmartBezierCore(
    Point start,
    Point end,
    string? startDirection,
    string? endDirection,
    out Point lastLineStart,
    out Point lastLineEnd)
        {
            Vector delta = end - start;
            double totalDistance = delta.Length;

            Vector startTangent = GetTangent(startDirection, delta);
            Vector endTangent = GetTangent(endDirection, -delta);

            double controlOffset = Math.Min(70, totalDistance / 2);

            Point control1 = start + startTangent * controlOffset;
            Point control2 = end + endTangent * controlOffset;

            var figure = new PathFigure { StartPoint = start };
            figure.Segments.Add(new BezierSegment(control1, control2, end, true));

            lastLineStart = control2;
            lastLineEnd = end;

            return new PathGeometry(new[] { figure });
        }

        private static Vector GetTangent(string? direction, Vector fallback)
        {
            return direction switch
            {
                "Top" => new Vector(0, -1),
                "Right" => new Vector(1, 0),
                "Bottom" => new Vector(0, 1),
                "Left" => new Vector(-1, 0),
                _ => fallback.Length > 0 ? fallback / fallback.Length : new Vector(1, 0)
            };
        }
    }
}
