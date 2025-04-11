using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Factory.Interfaces
{
    public interface IBpmnShapeFactory
    {
        UIElement CreateShape(Uri svgUri);
        IInteractiveShape CreateShape(ShapeType shapeType);
    }
}
