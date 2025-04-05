using System.Windows;
using WhiteBoard.Core.Factory.Interfaces;

namespace SketchRoom.Toolkit.Wpf.Factory
{
    public class BpmnShapeFactory : IBpmnShapeFactory
    {
        public UIElement CreateShape(Uri svgUri)
        {
            return new BpmnShapeControl(svgUri);
        }
    }
}
