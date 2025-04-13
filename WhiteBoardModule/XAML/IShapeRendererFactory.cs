using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WhiteBoardModule.XAML.Shapes;

namespace WhiteBoardModule.XAML
{
    public interface IShapeRendererFactory
    {
        IShapeRenderer CreateRenderer(ShapeType type, bool withBindings = false);
        UIElement CreateRenderPreview(ShapeType type);

    }
}
