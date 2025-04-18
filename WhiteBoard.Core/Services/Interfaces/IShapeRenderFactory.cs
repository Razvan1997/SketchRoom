using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IShapeRendererFactory
    {
        IShapeRenderer CreateRenderer(ShapeType type, bool withBindings = false);
        UIElement CreateRenderPreview(ShapeType type);

    }
}
