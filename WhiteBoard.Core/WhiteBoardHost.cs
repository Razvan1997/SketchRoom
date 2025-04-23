
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Input;

namespace WhiteBoard.Core
{
    public class WhiteBoardHost
    {
        public IToolManager ToolManager { get; }
        public IDrawingService DrawingService { get; }
        public ICanvasRenderer CanvasRenderer { get; }

        private readonly Canvas _canvas;

        public WhiteBoardHost(
            Canvas canvas,
            IToolManager toolManager,
            IDrawingService drawingService,
            ICanvasRenderer canvasRenderer)
        {
            _canvas = canvas;
            ToolManager = toolManager;
            DrawingService = drawingService;
            CanvasRenderer = canvasRenderer;
        }

        public void HandleMouseDown(Point position) => ToolManager.ActiveTool?.OnMouseDown(position);
        public void HandleMouseDown(Point position, MouseButtonEventArgs e) => ToolManager.ActiveTool?.OnMouseDown(position, e);
        public void HandleMouseMove(Point position, MouseEventArgs e) => ToolManager.ActiveTool?.OnMouseMove(position, e);
        public void HandleMouseUp(Point position, MouseButtonEventArgs e) => ToolManager.ActiveTool?.OnMouseUp(position, e);

        public void RedrawAll(IEnumerable<WhiteBoardElement> elements)
        {
            CanvasRenderer.Clear(_canvas);
            foreach (var element in elements)
                CanvasRenderer.RenderElement(_canvas, element);
        }

        public void StartRemoteLine()
        {
            DrawingService.BeginExternalStroke();
        }

        public void AddRemoteLine(IEnumerable<Point> points, Brush color, double thickness)
        {
            DrawingService.AddExternalStroke(points, color, thickness);
            RedrawAll(DrawingService.GetElements());
        }

        public void AddRemoteLivePoint(Point point, Brush color)
        {
            var previewStroke = DrawingService.GetExternalPreviewStroke();
            if (previewStroke != null)
            {
                if (!CanvasRenderer.HasVisual(_canvas, previewStroke.Visual))
                {
                    CanvasRenderer.RenderElement(_canvas, previewStroke);
                }

                // 🔁 actualizează punctul
                previewStroke.AddPoint(point);
            }
        }

        public void UpdateCursor(Point position, BitmapImage? image)
        {
            CanvasRenderer.RenderRemoteCursor(_canvas, position, image);
        }
    }

}
