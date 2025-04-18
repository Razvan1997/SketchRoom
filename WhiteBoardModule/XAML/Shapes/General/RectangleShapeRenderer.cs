using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Data;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Controls;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class RectangleShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;

        public RectangleShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            var previewContent = Render();

            return new Viewbox
            {
                Width = 48, // dimensiune de preview standard
                Height = 48,
                Stretch = Stretch.Uniform,
                Child = new Grid
                {
                    Width = 80,  // dimensiune internă consistentă pentru preview
                    Height = 80,
                    Background = Brushes.Transparent,
                    Children = { previewContent }
                }
            };
        }

        public UIElement Render()
        {
            var rect = new Rectangle
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
                rect.SetBinding(Shape.StrokeProperty, new Binding(nameof(preferences.SelectedColor))
                {
                    Source = preferences
                });
            }
            else
            {
                var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
                rect.Stroke = preferences.SelectedColor;
            }

            return rect;
        }
    }
}
