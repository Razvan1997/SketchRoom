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

        public Point GetSnappedPoint(Point rawPoint, double gridSize = 10)
        {
            return new Point(
                Math.Round(rawPoint.X / gridSize) * gridSize,
                Math.Round(rawPoint.Y / gridSize) * gridSize);
        }

        public Point GetSnappedPoint(Point rawPoint, IEnumerable<FrameworkElement> others, FrameworkElement movingElement, out List<Line> snapLines)
        {
            snapLines = new List<Line>();

            // 1. Coordonatele elementului mutat
            double elementLeft = rawPoint.X;
            double elementTop = rawPoint.Y;
            double elementRight = elementLeft + movingElement.ActualWidth;
            double elementBottom = elementTop + movingElement.ActualHeight;
            double elementCenterX = elementLeft + movingElement.ActualWidth / 2;
            double elementCenterY = elementTop + movingElement.ActualHeight / 2;

            // 2. Toate punctele de interes ale elementului mutat
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

                // Compară toate punctele de pe X cu targetele
                foreach (var x in pointsToCheckX)
                {
                    foreach (var tx in snapTargetsX)
                    {
                        if (Math.Abs(x - tx) < _snapThreshold)
                            snapLines.Add(CreateVerticalLine(tx));
                    }
                }

                // Compară toate punctele de pe Y cu targetele
                foreach (var y in pointsToCheckY)
                {
                    foreach (var ty in snapTargetsY)
                    {
                        if (Math.Abs(y - ty) < _snapThreshold)
                            snapLines.Add(CreateHorizontalLine(ty));
                    }
                }
            }

            // Nu ajustăm poziția efectivă, deci returnăm rawPoint
            return rawPoint;
        }


        private bool IsClose(double a, double b)
        {
            return Math.Abs(a - b) < _snapThreshold;
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

            return snapLines;
        }
    }

}
