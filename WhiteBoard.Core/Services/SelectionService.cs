using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Services
{
    public class SelectionService : ISelectionService
    {
        public Rect GetBoundsFromPoints(IEnumerable<Point> points)
        {
            if (points == null || !points.Any())
                return Rect.Empty;

            double minX = points.Min(p => p.X);
            double minY = points.Min(p => p.Y);
            double maxX = points.Max(p => p.X);
            double maxY = points.Max(p => p.Y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public void HandleSelection(Rect bounds, Canvas canvas)
        {
            var selected = new List<UIElement>();

            foreach (UIElement element in canvas.Children)
            {
                if (element is FrameworkElement fe)
                {
                    double left = Canvas.GetLeft(fe);
                    double top = Canvas.GetTop(fe);
                    double width = fe.ActualWidth;
                    double height = fe.ActualHeight;

                    var elementRect = new Rect(left, top, width, height);

                    if (bounds.IntersectsWith(elementRect))
                        selected.Add(element);
                }
            }

            // exemplu: evidențiere (doar pentru demo)
            foreach (var el in selected)
            {
                if (el is Shape shape)
                    shape.Stroke = Brushes.Red;
            }

            // aici poți emite eveniment sau trimite către un handler
        }
    }
}
