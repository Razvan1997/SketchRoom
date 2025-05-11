using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoard.Core.Helpers
{
    public static class ShapeMetadata
    {
        public static readonly DependencyProperty SvgUriProperty =
            DependencyProperty.RegisterAttached("SvgUri", typeof(Uri), typeof(ShapeMetadata), new PropertyMetadata(null));

        public static readonly DependencyProperty ShapeIdProperty =
        DependencyProperty.RegisterAttached("ShapeId", typeof(string), typeof(ShapeMetadata), new PropertyMetadata(null));

        public static void SetSvgUri(UIElement element, Uri value)
        {
            element.SetValue(SvgUriProperty, value);
        }

        public static Uri? GetSvgUri(UIElement element)
        {
            return (Uri?)element.GetValue(SvgUriProperty);
        }

        public static void SetShapeId(DependencyObject element, string value)
       => element.SetValue(ShapeIdProperty, value);

        public static string? GetShapeId(DependencyObject element)
        => element.GetValue(ShapeIdProperty) as string;
    }
}
