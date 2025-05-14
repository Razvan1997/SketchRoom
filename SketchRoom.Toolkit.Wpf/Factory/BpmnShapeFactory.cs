using SharpVectors.Converters;
using SketchRoom.Models.Enums;
using SketchRoom.Toolkit.Wpf.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using WhiteBoard.Core.Factory.Interfaces;
using WhiteBoard.Core.Services.Interfaces;

namespace SketchRoom.Toolkit.Wpf.Factory
{
    public class BpmnShapeFactory : IBpmnShapeFactory
    {
        public UIElement CreateShape(Uri uri)
        {
            return new BpmnShapeControl(uri);
        }

        public IInteractiveShape CreateShape(ShapeType shapeType)
        {
            return shapeType switch
            {
                //ShapeType.TextInput => new TextElementControl(),
                _ => throw new NotImplementedException($"Shape {shapeType} not handled.")
            };
        }
    }
}
