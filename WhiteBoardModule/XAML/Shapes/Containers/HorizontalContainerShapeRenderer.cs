using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Data;
using SketchRoom.Models.Enums;

namespace WhiteBoardModule.XAML.Shapes.Containers
{
    public class HorizontalContainerShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;
        public HorizontalContainerShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
        }

        public UIElement CreatePreview()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var previewLabel = new TextBlock
            {
                Text = "Container",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                LayoutTransform = new RotateTransform(-90),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var previewSidePanel = new Border
            {
                Background = Brushes.Black,
                Width = 10,
                Child = previewLabel
            };

            var previewBorder = new Border
            {
                BorderBrush = preferences.SelectedColor,
                BorderThickness = new Thickness(1),
                Background = Brushes.Transparent
            };

            var previewGrid = new Grid
            {
                Width = 60,
                Height = 60
            };

            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(previewSidePanel, 0);
            Grid.SetColumn(previewBorder, 1);

            previewGrid.Children.Add(previewSidePanel);
            previewGrid.Children.Add(previewBorder);

            return new Viewbox
            {
                Width = 48,
                Height = 48,
                Stretch = Stretch.Uniform,
                Child = previewGrid
            };
        }

        public UIElement Render()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            // TextBox rotit (cu LayoutTransform pentru layout corect)
            var labelBox = new TextBox
            {
                Text = "Horizontal Container",
                FontSize = preferences.FontSize + 2,
                FontWeight = preferences.FontWeight,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                TextAlignment = TextAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                AcceptsReturn = false,
                IsReadOnly = false,
                Name = "SideLabel",
                Padding = new Thickness(0),
            };

            labelBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                _selectionService.Select(ShapePart.Text, labelBox);
            };

            labelBox.LayoutTransform = new RotateTransform(-90);

            // Container pentru etichetă (lățimea contează vizual, nu a TextBox-ului)
            var sidePanel = new Border
            {
                Background = Brushes.Black,
                Width = 30,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = labelBox,
                Name = "SidePanel"
            };

            // Container alb gol
            var containerBorder = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(2),
                Background = Brushes.Transparent,
                Name = "ContainerBorder"
            };

            // Structura principală
            var mainGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(sidePanel, 0);
            Grid.SetColumn(containerBorder, 1);

            mainGrid.Children.Add(sidePanel);
            mainGrid.Children.Add(containerBorder);

            if (_withBindings)
            {
                labelBox.SetBinding(TextBox.FontWeightProperty, new Binding(nameof(preferences.FontWeight)) { Source = preferences });
                labelBox.SetBinding(TextBox.FontSizeProperty, new Binding(nameof(preferences.FontSize)) { Source = preferences });
            }
            else
            {
                containerBorder.BorderBrush = preferences.SelectedColor;
            }

            containerBorder.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var pos = e.GetPosition(mainGrid);

                if (IsMouseOverMargin(mainGrid, pos))
                    _selectionService.Select(ShapePart.Margin, containerBorder);
                else
                    _selectionService.Select(ShapePart.Border, containerBorder);
            };

            // Tag pentru stilizare
            mainGrid.Tag = new Dictionary<string, object>
            {
                { "SideLabel", labelBox },
                { "ContainerBorder", containerBorder }
            };

            return mainGrid;
        }

        private bool IsMouseOverMargin(Grid grid, Point mousePos)
        {
            const double marginWidth = 6;

            return mousePos.X < marginWidth ||
                   mousePos.X > grid.ActualWidth - marginWidth ||
                   mousePos.Y < marginWidth ||
                   mousePos.Y > grid.ActualHeight - marginWidth;
        }
    }
}
