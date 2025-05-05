using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SketchRoom.Models.Shapes
{
    public class BPMNShapeModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public Uri SvgUri { get; set; } = null!;
        public object? ShapeContent { get; set; }
        public ShapeType? Type { get; set; }
        public string Category { get; set; } = "General";
    }

    public class ShapeCategoryGroup
    {
        public string Category { get; set; } = "";
        public ObservableCollection<BPMNShapeModel> Items { get; set; } = new();
        public bool IsInitiallyExpanded { get; set; } = false;
    }
}
