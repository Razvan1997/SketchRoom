using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Models
{
    public class BpmnWhiteBoardElementXaml : WhiteBoardElement
    {
        private readonly IInteractiveShape _shape;

        public event MouseButtonEventHandler? Clicked;

        public BpmnWhiteBoardElementXaml(IInteractiveShape shape)
        {
            _shape = shape ?? throw new ArgumentNullException(nameof(shape));

            _shape.ShapeClicked += (s, e) =>
            {
                Clicked?.Invoke(s, e);
            };
        }

        public override UIElement Visual => _shape.Visual;

        public override Rect Bounds => new Rect(
            Canvas.GetLeft(_shape.Visual),
            Canvas.GetTop(_shape.Visual),
            (_shape.Visual as FrameworkElement)?.ActualWidth ?? 0,
            (_shape.Visual as FrameworkElement)?.ActualHeight ?? 0
        );

        public override void SetPosition(Point position)
        {
            Canvas.SetLeft(_shape.Visual, position.X);
            Canvas.SetTop(_shape.Visual, position.Y);
        }

        public override string? Label { get; set; }
    }
}
