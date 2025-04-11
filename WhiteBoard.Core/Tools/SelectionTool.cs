using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Shapes;

namespace WhiteBoard.Core.Tools
{
    public class SelectionTool : IToolBehavior, IDrawingTool
    {
        private readonly Canvas _canvas;
        private readonly ISelectionService _selectionService;
        private readonly IToolManager _toolManager;

        private bool _isSelecting = false;
        private Point _selectionStart;
        private Rectangle? _selectionRectangle;

        public string Name => "Selection";

        public SelectionTool(Canvas canvas, ISelectionService selectionService, IToolManager toolManager)
        {
            _canvas = canvas;
            _selectionService = selectionService;
            _toolManager = toolManager;
        }

        // Implementare IToolBehavior (folosit de ToolStateMachine)
        public void OnMouseDown(Point position, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                _selectionService.ClearSelection(_canvas);

                _isSelecting = true;
                _selectionStart = position;

                _selectionRectangle = new Rectangle
                {
                    Stroke = Brushes.Blue,
                    StrokeThickness = 1,
                    Fill = new SolidColorBrush(Color.FromArgb(60, 0, 120, 255)),
                    IsHitTestVisible = false
                };

                _canvas.Children.Add(_selectionRectangle);
                Canvas.SetLeft(_selectionRectangle, _selectionStart.X);
                Canvas.SetTop(_selectionRectangle, _selectionStart.Y);
            }
        }

        public void OnMouseMove(Point position, MouseEventArgs e)
        {
            if (_isSelecting && _selectionRectangle != null)
            {
                double x = Math.Min(_selectionStart.X, position.X);
                double y = Math.Min(_selectionStart.Y, position.Y);
                double width = Math.Abs(position.X - _selectionStart.X);
                double height = Math.Abs(position.Y - _selectionStart.Y);

                Canvas.SetLeft(_selectionRectangle, x);
                Canvas.SetTop(_selectionRectangle, y);
                _selectionRectangle.Width = width;
                _selectionRectangle.Height = height;
            }
        }

        public void OnMouseUp(Point position, MouseButtonEventArgs e)
        {
            if (_isSelecting && _selectionRectangle != null)
            {
                var bounds = new Rect(_selectionStart, position);

                _canvas.Children.Remove(_selectionRectangle);
                _selectionRectangle = null;
                _isSelecting = false;

                _selectionService.HandleSelection(bounds, _canvas);
            }
            _toolManager.SetActive("BpmnTool");
        }

        // Implementare goală pentru IDrawingTool (nefolosită)
        void IDrawingTool.OnMouseDown(Point position) { }

        void IDrawingTool.OnMouseMove(Point position) { }

        void IDrawingTool.OnMouseUp(Point position) { }
    }
}
