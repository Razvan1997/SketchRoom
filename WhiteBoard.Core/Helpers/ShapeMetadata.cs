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

        public static void SetSvgUri(UIElement element, Uri value)
        {
            element.SetValue(SvgUriProperty, value);
        }

        public static Uri? GetSvgUri(UIElement element)
        {
            return (Uri?)element.GetValue(SvgUriProperty);
        }
    }
}
