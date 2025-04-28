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
using SketchRoom.Models.Enums;
using WhiteBoardModule.XAML.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class RectangleShapeRenderer : IShapeRenderer, IBackgroundChangable, IStrokeChangable
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;

        private Rectangle? _rectangle; // 🔵 Rectangle-ul real

        public RectangleShapeRenderer(bool withBindings = false)
        {
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var previewContent = new Rectangle
            {
                Fill = Brushes.Transparent,
                StrokeThickness = 2,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stroke = preferences.SelectedColor
            };

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

            rect.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var pos = e.GetPosition(rect);

                if (IsMouseOverMargin(rect, pos))
                    _selectionService.Select(ShapePart.Margin, rect);
                else
                    _selectionService.Select(ShapePart.Border, rect);
            };

            _rectangle = rect; // 🔵 Stocăm Rectangle-ul ca să-l putem modifica ulterior

            return rect;
        }

        public void SetBackground(Brush brush)
        {
            _rectangle?.SetValue(Shape.FillProperty, brush);
        }

        public void SetStroke(Brush brush)
        {
            _rectangle?.SetValue(Shape.StrokeProperty, brush);
        }

        private bool IsMouseOverMargin(Rectangle rect, Point mousePos)
        {
            const double marginWidth = 6;

            return mousePos.X < marginWidth ||
                   mousePos.X > rect.ActualWidth - marginWidth ||
                   mousePos.Y < marginWidth ||
                   mousePos.Y > rect.ActualHeight - marginWidth;
        }
    }
}
