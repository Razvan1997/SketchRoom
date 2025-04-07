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
        private const double _snapThreshold = 10;

        public Point GetSnappedPoint(Point rawPoint, double gridSize = 20)
        {
            return new Point(
                Math.Round(rawPoint.X / gridSize) * gridSize,
                Math.Round(rawPoint.Y / gridSize) * gridSize);
        }

        public Point GetSnappedPoint(Point rawPoint, IEnumerable<FrameworkElement> others, FrameworkElement movingElement, out List<Line> snapLines)
        {
            snapLines = new List<Line>();

            double elementLeft = rawPoint.X;
            double elementTop = rawPoint.Y;
            double elementRight = elementLeft + movingElement.ActualWidth;
            double elementBottom = elementTop + movingElement.ActualHeight;
            double elementCenterX = elementLeft + movingElement.ActualWidth / 2;
            double elementCenterY = elementTop + movingElement.ActualHeight / 2;

            var pointsToCheckX = new[] { elementLeft, elementRight, elementCenterX };
            var pointsToCheckY = new[] { elementTop, elementBottom, elementCenterY };

            foreach (var el in others)
            {
                // 🔒 Verificare de siguranță: să fie în canvas și valid
                if (el == null || el == movingElement || el.ActualWidth == 0 || el.ActualHeight == 0)
                    continue;

                if (VisualTreeHelper.GetParent(el) is not Canvas)
                    continue;

                double left = Canvas.GetLeft(el);
                double top = Canvas.GetTop(el);
                double right = left + el.ActualWidth;
                double bottom = top + el.ActualHeight;
                double centerX = left + el.ActualWidth / 2;
                double centerY = top + el.ActualHeight / 2;

                var snapTargetsX = new[] { left, right, centerX };
                var snapTargetsY = new[] { top, bottom, centerY };

                foreach (var x in pointsToCheckX)
                {
                    foreach (var tx in snapTargetsX)
                    {
                        if (Math.Abs(x - tx) < _snapThreshold)
                            snapLines.Add(CreateVerticalLine(tx));
                    }
                }

                foreach (var y in pointsToCheckY)
                {
                    foreach (var ty in snapTargetsY)
                    {
                        if (Math.Abs(y - ty) < _snapThreshold)
                            snapLines.Add(CreateHorizontalLine(ty));
                    }
                }
            }

            return rawPoint;
        }


        public Point GetSnappedPointCursor(
    Point rawPoint,
    IEnumerable<FrameworkElement> others,
    FrameworkElement movingElement,
    out List<Line> snapLines,
    bool snapX = true,
    bool snapY = true)
        {
            snapLines = new List<Line>();
            double closestX = rawPoint.X;
            double closestY = rawPoint.Y;

            double minXDelta = double.MaxValue;
            double minYDelta = double.MaxValue;

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

                var snapTargetsX = new[] { left, right, centerX };
                var snapTargetsY = new[] { top, bottom, centerY };

                if (snapX)
                {
                    foreach (var tx in snapTargetsX)
                    {
                        double delta = Math.Abs(tx - rawPoint.X);
                        if (delta < _snapThreshold && delta < minXDelta)
                        {
                            minXDelta = delta;
                            closestX = tx;
                        }
                    }
                }

                if (snapY)
                {
                    foreach (var ty in snapTargetsY)
                    {
                        double delta = Math.Abs(ty - rawPoint.Y);
                        if (delta < _snapThreshold && delta < minYDelta)
                        {
                            minYDelta = delta;
                            closestY = ty;
                        }
                    }
                }
            }

            // Adaugă ghidajele doar dacă sunt relevante
            if (minXDelta < _snapThreshold)
                snapLines.Add(CreateVerticalLine(closestX));
            if (minYDelta < _snapThreshold)
                snapLines.Add(CreateHorizontalLine(closestY));

            return new Point(closestX, closestY);
        }

        private Line CreateVerticalLine(double x)
        {
            return new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = 3000,
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                IsHitTestVisible = false,
                Opacity = 0.6
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
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                IsHitTestVisible = false,
                Opacity = 0.6
            };
        }

        public List<Line> GetSnapGuides(Point rawPoint, IEnumerable<FrameworkElement> others, FrameworkElement movingElement)
        {
            var snapLines = new List<Line>();

            double elementLeft = rawPoint.X;
            double elementTop = rawPoint.Y;
            double elementRight = elementLeft + movingElement.ActualWidth;
            double elementBottom = elementTop + movingElement.ActualHeight;
            double elementCenterX = elementLeft + movingElement.ActualWidth / 2;
            double elementCenterY = elementTop + movingElement.ActualHeight / 2;

            var pointsToCheckX = new[] { elementLeft, elementRight, elementCenterX };
            var pointsToCheckY = new[] { elementTop, elementBottom, elementCenterY };

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

                var snapTargetsX = new[] { left, right, centerX };
                var snapTargetsY = new[] { top, bottom, centerY };

                var seenX = new HashSet<double>();
                var seenY = new HashSet<double>();

                foreach (var x in pointsToCheckX)
                {
                    foreach (var tx in snapTargetsX)
                    {
                        if (Math.Abs(x - tx) < _snapThreshold && seenX.Add(tx))
                            snapLines.Add(CreateVerticalLine(tx));
                    }
                }

                foreach (var y in pointsToCheckY)
                {
                    foreach (var ty in snapTargetsY)
                    {
                        if (Math.Abs(y - ty) < _snapThreshold && seenY.Add(ty))
                            snapLines.Add(CreateHorizontalLine(ty));
                    }
                }
            }

            return snapLines;
        }
    }

}
