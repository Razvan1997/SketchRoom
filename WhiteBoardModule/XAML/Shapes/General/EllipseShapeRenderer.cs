using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Data;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Controls;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class EllipseShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;

        public EllipseShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement Render()
        {
            var ellipse = new Ellipse
            {
                Fill = Brushes.Transparent,
                StrokeThickness = 2,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            if (_withBindings)
            {
                var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
                ellipse.SetBinding(Shape.StrokeProperty, new Binding(nameof(preferences.SelectedColor))
                {
                    Source = preferences
                });
            }
            else
            {
                // 🟢 pentru instanță individuală (copie a valorii)
                var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
                ellipse.Stroke = preferences.SelectedColor;
            }

            return ellipse;
        }

        public UIElement CreatePreview()
        {
            var renderer = new EllipseShapeRenderer(false);
            var shape = renderer.Render();

            return new Viewbox
            {
                Width = 48,
                Height = 48,
                Stretch = Stretch.Uniform,
                Child = new Grid
                {
                    Width = 80,
                    Height = 80,
                    Background = Brushes.Transparent,
                    Children = { shape }
                }
            };
        }
    }
}
