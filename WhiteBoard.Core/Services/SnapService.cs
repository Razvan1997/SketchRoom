using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Services
{
    public class SnapService : ISnapService
    {
        private readonly double _snapThreshold = 10;

        // Snap la grid simplu
        public Point GetSnappedPoint(Point rawPoint, double gridSize = 10)
        {
            return new Point(
                Math.Round(rawPoint.X / gridSize) * gridSize,
                Math.Round(rawPoint.Y / gridSize) * gridSize);
        }

        // Snap la poziții ale altor elemente
        public Point GetSnappedPoint(Point rawPoint, IEnumerable<FrameworkElement> others, FrameworkElement movingElement, out List<Line> snapLines)
        {
            double snapX = rawPoint.X;
            double snapY = rawPoint.Y;

            snapLines = new List<Line>();

            foreach (var el in others)
            {
                if (el == movingElement || el.ActualWidth == 0 || el.ActualHeight == 0)
                    continue;

                double left = Canvas.GetLeft(el);
                double top = Canvas.GetTop(el);
                double right = left + el.ActualWidth;
                double bottom = top + el.ActualHeight;
                double centerX = left + el.ActualWidth / 2;
                double centerY = top + el.ActualHeight / 2;

                // X-axis
                if (Math.Abs(rawPoint.X - left) < 10)
                {
                    snapX = left;
                    snapLines.Add(CreateVerticalLine(left));
                }
                else if (Math.Abs(rawPoint.X - right) < 10)
                {
                    snapX = right;
                    snapLines.Add(CreateVerticalLine(right));
                }
                else if (Math.Abs(rawPoint.X - centerX) < 10)
                {
                    snapX = centerX;
                    snapLines.Add(CreateVerticalLine(centerX));
                }

                // Y-axis
                if (Math.Abs(rawPoint.Y - top) < 10)
                {
                    snapY = top;
                    snapLines.Add(CreateHorizontalLine(top));
                }
                else if (Math.Abs(rawPoint.Y - bottom) < 10)
                {
                    snapY = bottom;
                    snapLines.Add(CreateHorizontalLine(bottom));
                }
                else if (Math.Abs(rawPoint.Y - centerY) < 10)
                {
                    snapY = centerY;
                    snapLines.Add(CreateHorizontalLine(centerY));
                }
            }

            return new Point(snapX, snapY);
        }

        private Line CreateVerticalLine(double x)
        {
            return new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = 3000,
                Stroke = new SolidColorBrush(Color.FromRgb(79, 195, 247)), // #4FC3F7
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                StrokeStartLineCap = PenLineCap.Flat,
                StrokeEndLineCap = PenLineCap.Flat,
                Opacity = 0.7,
                IsHitTestVisible = false
            };
        }

        private Line CreateHorizontalLine(double y)
        {
            return new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = 3000,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromRgb(79, 195, 247)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                StrokeStartLineCap = PenLineCap.Flat,
                StrokeEndLineCap = PenLineCap.Flat,
                Opacity = 0.7,
                IsHitTestVisible = false
            };
        }
    }
}
