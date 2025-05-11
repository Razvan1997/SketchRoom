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
        private readonly FrameworkElement _visual;

        public BPMNNode(FrameworkElement visual)
        {
            _visual = visual;
        }

        public override UIElement Visual => _visual;

        public Point Center => new Point(
            Canvas.GetLeft(_visual) + _visual.ActualWidth / 2,
            Canvas.GetTop(_visual) + _visual.ActualHeight / 2
        );

        public override Rect Bounds => new Rect(
            Canvas.GetLeft(_visual),
            Canvas.GetTop(_visual),
            _visual.ActualWidth,
            _visual.ActualHeight
        );
    }
}
