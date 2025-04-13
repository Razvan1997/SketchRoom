using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Data;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class TriangleShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;

        public TriangleShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            var content = Render();

            return new Viewbox
            {
                Width = 48,
                Height = 48,
                Stretch = Stretch.Uniform,
                Child = new Grid
                {
                    Width = 100,
                    Height = 100,
                    Background = Brushes.Transparent,
                    Children = { content }
                }
            };
        }

        public UIElement Render()
        {
            var triangle = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(50, 0),    // top center
                    new Point(100, 100), // bottom right
                    new Point(0, 100)    // bottom left
                },
                Fill = Brushes.Transparent,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };

            if (_withBindings)
            {
                var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
                triangle.SetBinding(Shape.StrokeProperty, new Binding(nameof(preferences.SelectedColor))
                {
                    Source = preferences
                });
            }
            else
            {
                triangle.Stroke = Brushes.Black;
            }

            return new Viewbox
            {
                Stretch = Stretch.Uniform,
                Margin = new Thickness(4),
                Child = new Canvas
                {
                    Width = 100,
                    Height = 100,
                    Children = { triangle }
                }
            };
        }
    }
}
