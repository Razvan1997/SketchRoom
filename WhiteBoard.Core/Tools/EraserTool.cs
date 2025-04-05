using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Tools
{
    public class EraserTool : IDrawingTool
    {
        public string Name => "Eraser";

        private readonly IDrawingService _drawingService;
        private readonly Canvas _canvas;

        private const double HitTestRadius = 8;

        public EraserTool(IDrawingService drawingService, Canvas canvas)
        {
            _drawingService = drawingService;
            _canvas = canvas;
        }

        public void OnMouseDown(Point position)
        {
            var toRemove = new List<WhiteBoardElement>();

            foreach (var element in _drawingService.RecentStrokes)
            {
                if (element.Points.Any(p => (p - position).Length < HitTestRadius))
                {
                    toRemove.Add(element);
                }
            }

            foreach (var el in toRemove)
            {
                _canvas.Children.Remove(el.Visual);

                if (el is FreeDrawStroke stroke)
                    _drawingService.RemoveStroke(stroke);
            }
        }

        public void OnMouseMove(Point position) { }
        public void OnMouseUp(Point position) { }
    }
}
