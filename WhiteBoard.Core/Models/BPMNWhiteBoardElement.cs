using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WhiteBoard.Core.Factory.Interfaces;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Models
{
    public class BpmnWhiteBoardElement : WhiteBoardElement
    {
        private readonly UIElement _visual;

        public event MouseButtonEventHandler? Clicked;

        public BpmnWhiteBoardElement(Uri svgUri, IBpmnShapeFactory factory)
        {
            _visual = factory.CreateShape(svgUri);

            if (_visual is IInteractiveShape interactive)
            {
                interactive.ShapeClicked += (s, e) => Clicked?.Invoke(s, e);
            }
        }

        public override UIElement Visual => _visual;

        public override Rect Bounds => new Rect(
            Canvas.GetLeft(_visual),
            Canvas.GetTop(_visual),
            (_visual as FrameworkElement)?.ActualWidth ?? 0,
            (_visual as FrameworkElement)?.ActualHeight ?? 0
        );

        public override void SetPosition(Point position)
        {
            Canvas.SetLeft(_visual, position.X);
            Canvas.SetTop(_visual, position.Y);
        }

        public override string? Label { get; set; }
    }
}
