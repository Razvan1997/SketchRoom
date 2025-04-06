using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace WhiteBoard.Core.Helpers
{
    public static class VisualTreeHelpers
    {
        public static FrameworkElement? FindChildByName(DependencyObject parent, string name)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement fe && fe.Name == name)
                    return fe;

                var result = FindChildByName(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
