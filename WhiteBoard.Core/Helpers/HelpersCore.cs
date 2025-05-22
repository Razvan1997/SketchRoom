using SharpVectors.Converters;
using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Helpers
{
    public static class HelpersCore
    {
        private static (double angleDeg, Point midPoint, Vector normal, Vector dir) GetClosestSegmentInfo(BPMNConnection conn, Point click)
        {
            double minDistance = double.MaxValue;
            double bestAngle = 0;
            Point bestMid = new Point();
            Vector bestNormal = new Vector();
            Vector bestDir = new Vector();

            var pts = conn.OriginalPathPoints;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p1 = pts[i];
                var p2 = pts[i + 1];
                var mid = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
                var dist = (mid - click).Length;

                if (dist < minDistance)
                {
                    minDistance = dist;

                    Vector dir = p2 - p1;
                    dir.Normalize();

                    Vector normal = new Vector(-dir.Y, dir.X);

                    double angleRad = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                    double angleDeg = angleRad * 180 / Math.PI;

                    bestAngle = angleDeg;
                    bestMid = mid;
                    bestNormal = normal;
                    bestDir = dir;
                }
            }

            return (bestAngle, bestMid, bestNormal, bestDir);
        }
        public static void AddTextAlignedToConnection(BPMNConnection connection, Point clickPosition, int offsetDirection, IShapeSelectionService selectionService,
            ConnectionTextAnnotationExportModel? restoreData = null)
        {
            var canvas = VisualTreeHelper.GetParent(connection.Visual) as Canvas;
            if (canvas == null) return;

            // ➕ Use restore data if available
            var rotation = restoreData?.Rotation ?? GetClosestSegmentInfo(connection, clickPosition).angleDeg;
            var position = restoreData?.Position ??
                           clickPosition + GetClosestSegmentInfo(connection, clickPosition).normal * 16 * offsetDirection;

            var rotateTransform = new RotateTransform(rotation);

            var grid = new Grid
            {
                RenderTransform = rotateTransform,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var fontSize = restoreData?.FontSize ?? 14;
            var fontWeight = !string.IsNullOrEmpty(restoreData?.FontWeight)
                             ? (FontWeight)new FontWeightConverter().ConvertFromString(restoreData.FontWeight)
                             : FontWeights.Normal;
            var foregroundBrush = !string.IsNullOrEmpty(restoreData?.ForegroundHex)
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(restoreData.ForegroundHex))
                : Brushes.White;

            var textBox = new TextBox
            {
                Text = restoreData?.Text ?? "New Text",
                Width = 100,
                Background = Brushes.Transparent,
                Foreground = foregroundBrush,
                BorderThickness = new Thickness(0),
                FontSize = fontSize,
                FontWeight = fontWeight,
                Tag = "ConnectionText",
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                AcceptsTab = true,
                Padding = new Thickness(0),
                Cursor = Cursors.SizeAll
            };

            textBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (!textBox.IsKeyboardFocusWithin)
                    textBox.Focus();

                selectionService.Select(ShapePart.Text, textBox);
            };

            // Creează modelul pentru export live
            var annotation = new ConnectionTextAnnotation
            {
                Text = textBox.Text,
                Position = position,
                Rotation = rotation,
                FontSize = fontSize,
                FontWeight = fontWeight.ToString(),
                ForegroundHex = (textBox.Foreground as SolidColorBrush)?.Color.ToString() ?? "#FFFFFFFF",
                TextBoxRef = textBox
            };
            connection.TextAnnotations.Add(annotation);

            var rotateIcon = new SvgViewbox
            {
                Source = new Uri("pack://application:,,,/SketchRoom.Toolkit.Wpf;component/Resources/rotate.svg"),
                Width = 18,
                Height = 18,
                Margin = new Thickness(130, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = Cursors.Hand
            };

            grid.Children.Add(textBox);
            grid.Children.Add(rotateIcon);

            canvas.Children.Add(grid);

            textBox.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Delete && textBox.IsKeyboardFocused)
                {
                    canvas.Children.Remove(grid);
                    connection.TextAnnotations.Remove(annotation);
                    e.Handled = true;
                }
            };

            grid.Dispatcher.BeginInvoke(new Action(() =>
            {
                grid.UpdateLayout();
                Canvas.SetLeft(grid, position.X - grid.ActualWidth / 2);
                Canvas.SetTop(grid, position.Y - grid.ActualHeight / 2);
            }));

            // Mutare
            Point? dragStart = null;
            textBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                dragStart = e.GetPosition(canvas);
                textBox.CaptureMouse();
                rotateIcon.Opacity = 1;
                rotateIcon.IsHitTestVisible = true;
            };
            textBox.PreviewMouseMove += (s, e) =>
            {
                if (dragStart.HasValue && e.LeftButton == MouseButtonState.Pressed)
                {
                    var current = e.GetPosition(canvas);
                    var delta = current - dragStart.Value;

                    double left = Canvas.GetLeft(grid);
                    double top = Canvas.GetTop(grid);

                    Canvas.SetLeft(grid, left + delta.X);
                    Canvas.SetTop(grid, top + delta.Y);

                    dragStart = current;

                    annotation.Position = new Point(Canvas.GetLeft(grid) + grid.ActualWidth / 2,
                                                    Canvas.GetTop(grid) + grid.ActualHeight / 2);
                }
            };
            textBox.PreviewMouseLeftButtonUp += (s, e) =>
            {
                dragStart = null;
                textBox.ReleaseMouseCapture();
            };

            // Rotire
            bool isRotating = false;
            Point rotateStart = new Point();

            rotateIcon.PreviewMouseLeftButtonDown += (s, e) =>
            {
                isRotating = true;
                rotateStart = e.GetPosition(canvas);
                rotateIcon.CaptureMouse();
                e.Handled = true;
            };
            rotateIcon.PreviewMouseMove += (s, e) =>
            {
                if (!isRotating || e.LeftButton != MouseButtonState.Pressed) return;

                var current = e.GetPosition(canvas);
                var center = new Point(
                    Canvas.GetLeft(grid) + grid.ActualWidth / 2,
                    Canvas.GetTop(grid) + grid.ActualHeight / 2
                );

                Vector v1 = rotateStart - center;
                Vector v2 = current - center;

                double angle = Vector.AngleBetween(v1, v2);
                rotateTransform.Angle += angle;
                rotateStart = current;
                annotation.Rotation = rotateTransform.Angle;

                e.Handled = true;
            };
            rotateIcon.PreviewMouseLeftButtonUp += (s, e) =>
            {
                isRotating = false;
                rotateIcon.ReleaseMouseCapture();
                e.Handled = true;
            };

            textBox.LostFocus += (s, e) =>
            {
                rotateIcon.Opacity = 0;
                rotateIcon.IsHitTestVisible = false;
            };

            textBox.TextChanged += (s, e) =>
            {
                annotation.Text = textBox.Text;
            };

            textBox.Focus();
            textBox.SelectAll();
        }
    }
}
