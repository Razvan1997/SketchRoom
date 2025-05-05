using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoardModule.XAML;

namespace WhiteBoardModule
{
    public interface IGenericShapeFactory
    {
        IInteractiveShape Create(ShapeType shapeType);
    }
}
