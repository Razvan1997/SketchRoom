using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WhiteBoard.Core.Models
{
    public class BPMNNode : WhiteBoardElement
    {
        private readonly Rectangle _rect;

        public BPMNNode(Point position, double width = 100, double height = 60)
        {
            _rect = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = Brushes.LightBlue,
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 2
            };

            Canvas.SetLeft(_rect, position.X);
            Canvas.SetTop(_rect, position.Y);
        }

        public override UIElement Visual => _rect;

        public Point Center => new Point(
            Canvas.GetLeft(_rect) + _rect.Width / 2,
            Canvas.GetTop(_rect) + _rect.Height / 2
        );

        public override Rect Bounds
        {
            get
            {
                if (_rect.IsLoaded)
                {
                    var bounds = VisualTreeHelper.GetDescendantBounds(_rect);
                    var offset = new Point(Canvas.GetLeft(_rect), Canvas.GetTop(_rect));
                    return new Rect(offset, bounds.Size);
                }

                return new Rect(
                    Canvas.GetLeft(_rect),
                    Canvas.GetTop(_rect),
                    _rect.Width,
                    _rect.Height);
            }
        }
    }
}
