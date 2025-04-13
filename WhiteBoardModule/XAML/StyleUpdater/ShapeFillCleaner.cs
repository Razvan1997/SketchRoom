using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace WhiteBoardModule.XAML.StyleUpdater
{
    public static class ShapeFillCleaner
    {
        public static void MakeFillsTransparent(UIElement element)
        {
            if (element is Shape shape)
            {
                shape.Fill = Brushes.Transparent;
            }
            else if (element is Panel panel)
            {
                foreach (UIElement child in panel.Children)
                    MakeFillsTransparent(child);
            }
            else if (element is Border border)
            {
                if (border.Child != null)
                    MakeFillsTransparent(border.Child);
            }
            else if (element is ContentControl contentControl && contentControl.Content is UIElement content)
            {
                MakeFillsTransparent(content);
            }
            else if (element is Decorator decorator && decorator.Child is UIElement decoratedChild)
            {
                MakeFillsTransparent(decoratedChild);
            }
            else if (element is Viewbox vb && vb.Child is UIElement vbChild)
            {
                MakeFillsTransparent(vbChild);
            }
        }
    }
}
