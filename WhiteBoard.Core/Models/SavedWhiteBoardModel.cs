using SketchRoom.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoard.Core.Models
{
    public class SavedWhiteBoardModel
    {
        public Guid TabId { get; set; }               // ID-ul unic al tab-ului
        public string TabName { get; set; }           // Numele afișat în UI
        public string FolderName { get; set; }        // Numele folderului din SavedTabs (ex: "Airport", "Home")
        public List<BPMNShapeModelWithPosition> Shapes { get; set; } = new(); // Formele desenate
        public List<BPMNConnectionExportModel> Connections { get; set; } = new();
        public List<FreeDrawStrokeExportModel> FreeDrawStrokes { get; set; } = new();
    }

    public class FreeDrawStrokeExportModel
    {
        public List<Point> Points { get; set; } = new();
        public string? StrokeColorHex { get; set; }
        public double StrokeThickness { get; set; }
    }

    public class BPMNShapeModelWithPosition : BPMNShapeModel
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public string? BackgroundHex { get; set; }
        public string? StrokeHex { get; set; }
        public string? ForegroundHex { get; set; }

        public double FontSize { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }

        public string? Text { get; set; }

        public double RotationAngle { get; set; }

        public Dictionary<string, string>? ExtraProperties { get; set; }
    }

    public class SavedShapeWrapper
    {
        public BPMNShapeModel Shape { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }
    }
}
