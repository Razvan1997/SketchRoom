using SketchRoom.Models.Enums;
using SketchRoom.Toolkit.Wpf.Controls;
using System.Windows;
using WhiteBoard.Core.Factory.Interfaces;
using WhiteBoard.Core.Services.Interfaces;

namespace SketchRoom.Toolkit.Wpf.Factory
{
    public class BpmnShapeFactory : IBpmnShapeFactory
    {
        public UIElement CreateShape(Uri svgUri)
        {
            return new BpmnShapeControl(svgUri);
        }

        public IInteractiveShape CreateShape(ShapeType shapeType)
        {
            return shapeType switch
            {
                ShapeType.TextInput => new TextElementControl(),
                _ => throw new NotImplementedException($"Shape {shapeType} not handled.")
            };
        }
    }
}
