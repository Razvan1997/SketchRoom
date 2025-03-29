using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WhiteBoardModule.ViewModels;

namespace WhiteBoardModule.Views
{
    /// <summary>
    /// Interaction logic for WhiteBoardView.xaml
    /// </summary>
    public partial class WhiteBoardView : UserControl
    {
        private Point _lastMousePosition;
        private bool _isPanning;
        private const double MinZoom = 0.3;
        private const double MaxZoom = 3.0;

        // Dimensiuni A1 (96 DPI)
        private const double A1Width = 2244;
        private const double A1Height = 3185;

        public WhiteBoardView()
        {
            InitializeComponent();
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;

            var zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            var position = e.GetPosition(DrawingSurface);
            var newScale = ZoomScale.ScaleX * zoomFactor;

            if (newScale < MinZoom || newScale > MaxZoom)
                return;

            ZoomScale.ScaleX = newScale;
            ZoomScale.ScaleY = newScale;

            ZoomTranslate.X = (1 - zoomFactor) * position.X + ZoomTranslate.X * zoomFactor;
            ZoomTranslate.Y = (1 - zoomFactor) * position.Y + ZoomTranslate.Y * zoomFactor;

            e.Handled = true;
        }

        private void Canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(this);
                DrawingSurface.CaptureMouse();
                Cursor = Cursors.SizeAll;
            }
        }

        private void Canvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                DrawingSurface.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
            }
        }

        private void Canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                var currentPosition = e.GetPosition(this);
                var delta = currentPosition - _lastMousePosition;

                ZoomTranslate.X += delta.X;
                ZoomTranslate.Y += delta.Y;

                _lastMousePosition = currentPosition;

                // Opțional: limitează pan-ul aici dacă vrei să nu scoți zona din viewport
            }
        }

        // Desenare delegată spre ViewModel + transform
        private Point GetLogicalPosition(MouseEventArgs e)
        {
            var visual = DrawingSurface;
            var transform = visual.RenderTransform as TransformGroup;

            if (transform != null && transform.Inverse != null)
                return transform.Inverse.Transform(e.GetPosition(visual));

            return e.GetPosition(visual);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is WhiteBoardViewModel vm)
                vm.CanvasMouseDown(GetLogicalPosition(e));
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (DataContext is WhiteBoardViewModel vm)
                vm.CanvasMouseMove(GetLogicalPosition(e));
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is WhiteBoardViewModel vm)
                vm.CanvasMouseUp(GetLogicalPosition(e));
        }
    }
}
