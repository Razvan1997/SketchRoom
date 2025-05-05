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
    public class GenericShapeFactory : IGenericShapeFactory
    {
        public IInteractiveShape Create(ShapeType shapeType)
        {
            var control = new GenericShapeControl();
            control.SetShape(shapeType);
            return control;
        }
    }
}
