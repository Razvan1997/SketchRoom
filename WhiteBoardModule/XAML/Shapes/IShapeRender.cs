using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoardModule.XAML.Shapes
{
    public interface IShapeRenderer
    {
        UIElement Render();

        UIElement CreatePreview();
    }
}
