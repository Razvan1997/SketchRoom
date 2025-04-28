using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IShapeSelectionService
    {
        ShapePart Current { get; }

        void Select(ShapePart part, DependencyObject shapeRoot);
        void ApplyVisual(DependencyObject shapeRoot);
        void Deselect();
    }
}
