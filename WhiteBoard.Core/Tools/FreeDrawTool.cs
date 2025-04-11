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

namespace WhiteBoard.Core.Tools
{
    public class FreeDrawTool : IDrawingTool
    {
        public string Name => "FreeDraw";

        private readonly IDrawingService _drawingService;
        private readonly Canvas _canvas;
        public Brush StrokeColor { get; set; } = Brushes.Black;
        private FreeDrawStroke? _currentStroke;

        public event Action<List<Point>>? StrokeCompleted;
        public event Action<Point>? PointDrawn;
        public event Action<Point>? PointerMoved;
        private bool _isDrawing = false;
        public bool IsDrawing => _isDrawing;

        public FreeDrawTool(IDrawingService drawingService, Canvas canvas)
        {
            _drawingService = drawingService;
            _canvas = canvas;
        }

        public void OnMouseDown(Point pos, MouseButtonEventArgs e)
        {
            _isDrawing = true;
            var color = StrokeColor;
            var thickness = 2.0;

            _currentStroke = _drawingService.StartStroke(pos, color, thickness);
            _canvas.Children.Add(_currentStroke.Visual);
        }

        public void OnMouseMove(Point pos, MouseEventArgs e)
        {
            if (_currentStroke == null)
                return;

            _drawingService.AddPointToStroke(_currentStroke, pos);
            _currentStroke.Points.Add(pos);

            PointDrawn?.Invoke(pos);   // pentru desen live
            PointerMoved?.Invoke(pos); // pentru cursor sincronizat
        }

        public void OnMouseUp(Point pos, MouseButtonEventArgs e)
        {
            if (_currentStroke == null)
                return;

            _drawingService.FinishStroke(_currentStroke);
            _currentStroke.Points.Add(pos);

            StrokeCompleted?.Invoke(_currentStroke.Points.ToList());
            _currentStroke = null;
            _isDrawing = false;
        }
    }
}
