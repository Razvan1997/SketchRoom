using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using SketchRoom.Models.Enums;
using System.Windows.Controls.Primitives;
using WhiteBoard.Core.Models;

namespace WhiteBoardModule.XAML.Shapes.Containers
{
    public class SimpleLinesRenderer : IShapeRenderer, ISimpleLinesContainer, IRestoreFromShape
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;
        private readonly IContextMenuService _contextMenuService;
        private Grid? _outerGrid;
        private Border? _lastRightClickedBorder;

        public SimpleLinesRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
            _contextMenuService = ContainerLocator.Container.Resolve<IContextMenuService>();
        }

        public UIElement CreatePreview()
        {
            var previewGrid = new Grid
            {
                Width = 60,
                Height = 60,
                Background = Brushes.Transparent
            };

            // 2 rânduri
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            previewGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // Primul Border cu text "Item 1"
            var border1 = new Border
            {
                Background = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8, 8, 0, 0) // rotunjit doar sus
            };
            var textBlock1 = new TextBlock
            {
                Text = "Item 1",
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 10
            };
            border1.Child = textBlock1;
            Grid.SetRow(border1, 0);
            previewGrid.Children.Add(border1);

            // Al doilea Border cu text "Item 2"
            var border2 = new Border
            {
                Background = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0, 0, 8, 8) // rotunjit doar jos
            };
            var textBlock2 = new TextBlock
            {
                Text = "Item 2",
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 10
            };
            border2.Child = textBlock2;
            Grid.SetRow(border2, 1);
            previewGrid.Children.Add(border2);

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
            _outerGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };

            _outerGrid.ColumnDefinitions.Add(new ColumnDefinition());

            AddLine(0);

            _outerGrid.ContextMenu = _contextMenuService.CreateContextMenu(ShapeContextType.SimpleLinesContainer, this);

            return _outerGrid;
        }

        private void AddLine(int insertAtRow)
        {
            if (_outerGrid == null)
                return;

            // 👉 1. Adaugăm Row pentru Border
            _outerGrid.RowDefinitions.Insert(insertAtRow, new RowDefinition { Height = new GridLength(50) });

            var border = new Border
            {
                Background = Brushes.White,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            border.PreviewMouseLeftButtonDown += (s, e) =>
            {
                _selectionService.Select(ShapePart.Border, border);
            };

            border.MouseRightButtonDown += (s, e) =>
            {
                _lastRightClickedBorder = border;
            };

            Grid.SetRow(border, insertAtRow);
            _outerGrid.Children.Add(border);

            // 👉 2. Adaugăm Row pentru Thumb (DOAR O DATĂ!)
            _outerGrid.RowDefinitions.Insert(insertAtRow + 1, new RowDefinition { Height = new GridLength(1) }); // fix 1 pixel

            var separator = new Thumb
            {
                Height = 1,
                Background = Brushes.Transparent,
                Cursor = System.Windows.Input.Cursors.SizeNS,
                Opacity = 0,
                IsHitTestVisible = true
            };

            separator.DragDelta += (s, e) => ResizeRows(insertAtRow, e.VerticalChange);

            Grid.SetRow(separator, insertAtRow + 1);
            _outerGrid.Children.Add(separator);

            UpdateCornerRadius();
        }

        private void ResizeRows(int aboveRowIndex, double deltaY)
        {
            if (_outerGrid == null) return;

            if (aboveRowIndex < 0 || aboveRowIndex + 2 >= _outerGrid.RowDefinitions.Count)
                return;

            var aboveRow = _outerGrid.RowDefinitions[aboveRowIndex];
            var belowRow = _outerGrid.RowDefinitions[aboveRowIndex + 2];

            double aboveNew = Math.Max(aboveRow.ActualHeight + deltaY, 30);
            double belowNew = Math.Max(belowRow.ActualHeight - deltaY, 30);

            aboveRow.Height = new GridLength(aboveNew);
            belowRow.Height = new GridLength(belowNew);
        }

        public void AddLine()
        {
            if (_outerGrid == null)
                return;

            int insertAt = _outerGrid.RowDefinitions.Count;
            AddLine(insertAt);
        }

        public void AddTextToCenter()
        {
            if (_outerGrid == null || _lastRightClickedBorder == null)
                return;

            if (_lastRightClickedBorder.Child is TextBox)
                return;

            var textBox = new TextBox
            {
                Text = "Enter text...",
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4),
                AcceptsReturn = true,
                AcceptsTab = true,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                MinWidth = 80,
                MaxWidth = 300,
                FontSize = 14,
                Tag = "interactive"
            };

            var selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();

            textBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (!textBox.IsKeyboardFocusWithin)
                    textBox.Focus();
                selectionService.Select(ShapePart.Text, textBox);
                e.Handled = true;
            };

            _lastRightClickedBorder.Child = textBox;
        }

        private void UpdateCornerRadius()
        {
            if (_outerGrid == null)
                return;

            var borders = _outerGrid.Children.OfType<Border>().ToList();

            for (int i = 0; i < borders.Count; i++)
            {
                borders[i].CornerRadius = new CornerRadius(0);
            }

            if (borders.Count > 0)
            {
                borders[0].CornerRadius = new CornerRadius(8, 8, 0, 0);
                borders[^1].CornerRadius = new CornerRadius(0, 0, 8, 8);
            }
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (_outerGrid == null || control is not FrameworkElement fe)
                return null;

            var position = new Point(Canvas.GetLeft(fe), Canvas.GetTop(fe));
            var size = new Size(fe.Width, fe.Height);
            var extraProps = new Dictionary<string, string>();

            int lineIndex = 1;

            foreach (var border in _outerGrid.Children.OfType<Border>())
            {
                if (border.Child is TextBox tb)
                {
                    extraProps[$"Line{lineIndex++}"] = tb.Text;
                }
                else if (border.Child is TextBlock txt)
                {
                    extraProps[$"Line{lineIndex++}"] = txt.Text;
                }
            }

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.SimpleContainer,
                Left = position.X,
                Top = position.Y,
                Width = size.Width,
                Height = size.Height,
                Name = fe.Name,
                Category = "Container",
                SvgUri = null,
                ExtraProperties = extraProps
            };
        }

        public UIElement Visual => _outerGrid ?? new Grid();

        public void Restore(Dictionary<string, string> extraProperties)
        {
            if (_outerGrid == null || extraProperties == null || extraProperties.Count == 0)
                return;

            _outerGrid.Children.Clear();
            _outerGrid.RowDefinitions.Clear();

            int lineIndex = 1;
            while (extraProperties.TryGetValue($"Line{lineIndex}", out var text))
            {
                AddRestoredLine(text);
                lineIndex++;
            }

            UpdateCornerRadius();
        }

        private void AddRestoredLine(string text)
        {
            if (_outerGrid == null)
                return;

            int insertAtRow = _outerGrid.RowDefinitions.Count;

            // Row for Border
            _outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

            var border = new Border
            {
                Background = Brushes.White,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var textBox = new TextBox
            {
                Text = text,
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 14,
                Tag = "interactive"
            };

            textBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (!textBox.IsKeyboardFocusWithin)
                    textBox.Focus();
                _selectionService.Select(ShapePart.Text, textBox);
                e.Handled = true;
            };

            border.Child = textBox;

            border.PreviewMouseLeftButtonDown += (s, e) =>
            {
                _selectionService.Select(ShapePart.Border, border);
            };

            border.MouseRightButtonDown += (s, e) =>
            {
                _lastRightClickedBorder = border;
            };

            Grid.SetRow(border, insertAtRow);
            _outerGrid.Children.Add(border);

            // Row for separator (Thumb)
            _outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1) });

            var separator = new Thumb
            {
                Height = 1,
                Background = Brushes.Transparent,
                Cursor = System.Windows.Input.Cursors.SizeNS,
                Opacity = 0,
                IsHitTestVisible = true
            };

            separator.DragDelta += (s, e) => ResizeRows(insertAtRow, e.VerticalChange);
            Grid.SetRow(separator, insertAtRow + 1);
            _outerGrid.Children.Add(separator);
        }
    }
}
