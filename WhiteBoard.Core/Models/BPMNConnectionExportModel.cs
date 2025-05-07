using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoard.Core.Models
{
    public class BPMNConnectionExportModel
    {
        public List<Point> PathPoints { get; set; } = new();
        public bool IsCurved { get; set; }
        public string? FromId { get; set; }
        public string? ToId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? StrokeHex { get; set; }

        public List<BezierSegmentData>? BezierSegments { get; set; }
    }

    public class BezierSegmentData
    {
        public Point Point1 { get; set; } // control1
        public Point Point2 { get; set; } // control2
        public Point Point3 { get; set; } // end
    }
}
