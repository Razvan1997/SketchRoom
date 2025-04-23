using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Tools
{
    public class PanTool : IDrawingTool, IToolBehavior
    {
        public string Name => "Pan";

        private readonly IZoomPanService _zoomPanService;
        private readonly TranslateTransform _translate;
        private bool _isPanning;
        private Point _lastPoint;
        private bool _isDrawing = false;
        public bool IsDrawing => _isDrawing;
        public PanTool(IZoomPanService zoomPanService, TranslateTransform translate)
        {
            _zoomPanService = zoomPanService;
            _translate = translate;
        }

        public void OnMouseDown(Point position, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastPoint = e.GetPosition(null); // e.g., canvas
            }
        }

        public void OnMouseMove(Point position, MouseEventArgs e)
        {
            if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
            {
                Point current = e.GetPosition(null);
                _lastPoint = _zoomPanService.Pan(current, _lastPoint, _translate);
            }
        }

        public void OnMouseUp(Point position, MouseButtonEventArgs e)
        {
            _isPanning = false;
        }

        public void OnMouseDown(Point position)
        {
            //throw new NotImplementedException();
        }

        public void OnMouseMove(Point position)
        {
            //throw new NotImplementedException();
        }

        public void OnMouseUp(Point position)
        {
            //throw new NotImplementedException();
        }
    }
}
