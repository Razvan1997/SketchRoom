using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace WhiteBoardModule.XAML.Shapes.Tables
{
    public class ResizeAdorner : Adorner
    {
        private readonly Thumb _verticalThumb;
        private readonly Thumb _horizontalThumb;
        private readonly Grid _grid;

        public ResizeAdorner(Grid adornedElement) : base(adornedElement)
        {
            _grid = adornedElement;

            _verticalThumb = CreateThumb(Cursors.SizeNS);
            _horizontalThumb = CreateThumb(Cursors.SizeWE);

            AddVisualChild(_verticalThumb);
            AddVisualChild(_horizontalThumb);

            _verticalThumb.DragDelta += (s, e) =>
            {
                var row = _grid.RowDefinitions[0];
                row.Height = new GridLength(Math.Max(20, row.ActualHeight + e.VerticalChange));
            };

            _horizontalThumb.DragDelta += (s, e) =>
            {
                var col = _grid.ColumnDefinitions[0];
                col.Width = new GridLength(Math.Max(30, col.ActualWidth + e.HorizontalChange));
            };
        }

        private Thumb CreateThumb(Cursor cursor)
        {
            return new Thumb
            {
                Width = 6,
                Height = 6,
                Cursor = cursor,
                Background = Brushes.Transparent,
                Opacity = 0.01,
                IsHitTestVisible = true
            };
        }

        protected override int VisualChildrenCount => 2;
        protected override Visual GetVisualChild(int index) => index switch
        {
            0 => _verticalThumb,
            1 => _horizontalThumb,
            _ => throw new ArgumentOutOfRangeException()
        };

        protected override Size ArrangeOverride(Size finalSize)
        {
            _verticalThumb.Arrange(new Rect(0, finalSize.Height - 3, finalSize.Width, 6));
            _horizontalThumb.Arrange(new Rect(finalSize.Width - 3, 0, 6, finalSize.Height));
            return finalSize;
        }
    }
}
