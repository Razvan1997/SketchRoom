using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SketchRoom.Models.Shapes
{
    public class BPMNShapeModel
    {
        public string Name { get; set; } = string.Empty;
        public Uri SvgUri { get; set; } = null!; // ex: new Uri("pack://application:,,,/Resources/SVG/rectangle.svg")
        public object? ShapeContent { get; set; }
        public ShapeType? Type { get; set; }
    }
}
