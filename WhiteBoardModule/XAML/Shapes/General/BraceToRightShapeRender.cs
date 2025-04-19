using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Shapes;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class BraceToRightShapeRender : IShapeRenderer
    {
        private readonly bool _withBindings;

        public BraceToRightShapeRender(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            var brace = CreateBracePath();
            return new Viewbox
            {
                Width = 48,
                Height = 48,
                Stretch = Stretch.Uniform,
                Child = brace
            };
        }

        public UIElement Render()
        {
            return CreateBracePath();
        }

        private UIElement CreateBracePath()
        {
            // Înălțime totală: 100, lățime: 20
            var figure = new PathFigure { StartPoint = new Point(20, 0) };

            // Sus: curbă în interior
            figure.Segments.Add(new BezierSegment(
                new Point(5, 0),    // Control 1
                new Point(5, 30),   // Control 2
                new Point(20, 30),  // End
                true));

            // Mijloc spre stânga
            figure.Segments.Add(new LineSegment(new Point(10, 50), true));

            // Jos: curbă înapoi
            figure.Segments.Add(new BezierSegment(
                new Point(5, 70),
                new Point(5, 100),
                new Point(20, 100),
                true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            var path = new Path
            {
                Data = geometry,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Width = 24,
                Height = 100,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            return path;
        }
    }
}
