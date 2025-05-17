using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using Microsoft.VisualBasic;
using System.Windows.Input;
using WhiteBoard.Core.Services;
using WhiteBoard.Core.UndoRedo;
using WhiteBoard.Core.Helpers;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.ComponentModel.Design;
using SketchRoom.Models.Enums;

namespace WhiteBoard.Core.Tools
{
    public class BpmnTool : IDrawingTool
    {
        private Dictionary<UIElement, Point> _selectionInitialOffsets = new();
        private Vector _dragOffset;
        public string Name => "BpmnTool";

        private readonly Canvas _canvas;
        private readonly ISnapService _snapService;
        private readonly Canvas _snapCanvas;
        private readonly IToolManager _toolManager;
        private IInteractiveShape? _selectedShape;
        private IInteractiveShape? _draggingShape;
        private Point _lastMousePos;

        private bool _isDrawing = false;
        public bool IsDrawing => _isDrawing;
        public IInteractiveShape? SelectedShape => _selectedShape;

        public event Action<IInteractiveShape?>? ShapeSelected;
        private readonly UndoRedoService _undoRedoService;
        private readonly Services.Interfaces.ISelectionService _selectionService;

        public BpmnTool(Canvas canvas, ISnapService snapService, Canvas snapCanvas, IToolManager toolManager, 
            UndoRedoService undoRedoService, Services.Interfaces.ISelectionService selectionService)
        {
            _canvas = canvas;
            _snapService = snapService;
            _snapCanvas = snapCanvas;
            _toolManager = toolManager;
            _toolManager.ToolChanged += OnToolChanged;
            _undoRedoService = undoRedoService;
            _selectionService = selectionService;
        }

        private void OnToolChanged(IDrawingTool tool)
        {
            if (tool == null)
            {
                DeselectCurrent();
            }
        }

        public void OnMouseDown(Point pos, MouseButtonEventArgs e)
        {
            _draggingShape = null;
            _isDrawing = true;

            if (IsInsideResizeThumb(e.OriginalSource as DependencyObject))
                return;

            foreach (var el in _canvas.Children.OfType<FrameworkElement>().Reverse())
            {
                var bounds = new Rect(
                    Canvas.GetLeft(el),
                    Canvas.GetTop(el),
                    el.ActualWidth,
                    el.ActualHeight);

                if (bounds.Contains(pos) && el is IInteractiveShape interactive)
                {
                    if (_selectedShape != interactive)
                    {
                        DeselectCurrent();
                        _selectedShape = interactive;
                        _selectedShape.Select();
                        ShapeSelected?.Invoke(_selectedShape);
                    }

                    _draggingShape = interactive;
                    _lastMousePos = pos;

                    if (interactive is FrameworkElement fe)
                    {
                        var left = Canvas.GetLeft(fe);
                        var top = Canvas.GetTop(fe);
                        _dragOffset = pos - new Point(left, top);
                    }

                    // 📦 Stochează pozițiile relative pentru toată selecția
                    _selectionInitialOffsets.Clear();
                    foreach (var selected in _canvas.Children.OfType<FrameworkElement>())
                    {
                        if (selected != interactive && _selectionService.SelectedElements.Contains(selected))
                        {
                            var offset = new Point(
                                Canvas.GetLeft(selected) - Canvas.GetLeft((FrameworkElement)_draggingShape),
                                Canvas.GetTop(selected) - Canvas.GetTop((FrameworkElement)_draggingShape));

                            _selectionInitialOffsets[selected] = offset;
                        }
                    }

                    return;
                }
            }

            DeselectCurrent();
            ShapeSelected?.Invoke(null);
        }

        public void OnMouseMove(Point pos, MouseEventArgs e)
        {
            if (_draggingShape == null) return;

            if (_draggingShape is FrameworkElement fe)
            {
                Point desiredTopLeft = pos - _dragOffset;
                Point gridSnapped = _snapService.GetSnappedPoint(desiredTopLeft, gridSize: 20);

                // Mută forma principală
                Canvas.SetLeft(fe, gridSnapped.X);
                Canvas.SetTop(fe, gridSnapped.Y);

                // Mută restul formelor selectate proporțional
                foreach (var (element, offset) in _selectionInitialOffsets)
                {
                    Canvas.SetLeft(element, gridSnapped.X + offset.X);
                    Canvas.SetTop(element, gridSnapped.Y + offset.Y);
                }

                // Snap guides, doar pentru forma principală
                var others = _canvas.Children.OfType<FrameworkElement>()
                                 .Where(e => e != fe && IsSnappable(e)).ToList();
                var snapLines = _snapService.GetSnapGuides(gridSnapped, others, fe);

                _snapCanvas.Children.Clear();
                foreach (var line in snapLines)
                    _snapCanvas.Children.Add(line);

                _selectionService.UpdateSelectionMarkersPosition();

                var allConnections = _selectionService.GetAllConnections();
                var movedElements = _selectionService.SelectedElements.OfType<FrameworkElement>().ToHashSet();
                movedElements.Add(fe); // include și forma principală în mișcare

                foreach (var conn in allConnections)
                {
                    var fromFe = conn.From?.Visual as FrameworkElement;
                    var toFe = conn.To?.Visual as FrameworkElement;

                    var isFromMoved = fromFe != null && movedElements.Contains(fromFe);
                    var isToMoved = toFe != null && movedElements.Contains(toFe);
                    var isConnectedToConnection = conn.ConnectedToConnection != null && conn.ConnectionIntersectionPoint.HasValue;

                    if (isFromMoved || isToMoved || isConnectedToConnection)
                    {
                        conn.RecalculateGeometry();
                    }
                }
            }

            
        }

        public void OnMouseUp(Point pos, MouseButtonEventArgs e)
        {
            if (_draggingShape is FrameworkElement fe)
            {
                var finalPos = new Point(Canvas.GetLeft(fe), Canvas.GetTop(fe));

                if (_lastMousePos != finalPos)
                {
                    var initialPos = _lastMousePos - _dragOffset;
                    var moveCommand = new MoveShapeCommand(fe, initialPos, finalPos);
                    _undoRedoService.ExecuteCommand(moveCommand);
                }

                if (fe is IShapeAddedXaml shape)
                {
                    var shapeType = shape.GetShapeType();

                    // doar SVG-uri sau rich text
                    if (shapeType == ShapeType.Image || shapeType == ShapeType.ShapeText)
                    {
                        var table = FindTableUnderShape(fe);
                        if (table != null)
                        {
                            var globalPos = fe.TranslatePoint(new Point(0, 0), _canvas);
                            var tablePos = ((FrameworkElement)table).TranslatePoint(new Point(0, 0), _canvas);
                            var relative = new Point(globalPos.X - tablePos.X, globalPos.Y - tablePos.Y);

                            _canvas.Children.Remove(fe);
                            table.AddOverlayElement(fe, relative);
                        }
                    }
                }
            }

            _draggingShape = null;
            _snapCanvas.Children.Clear();
            _isDrawing = false;
        }

        public void DeselectCurrent()
        {
            if (_selectedShape != null)
            {
                _selectedShape.Deselect();
                _selectedShape = null;
            }
        }

        private bool IsSnappable(FrameworkElement element)
        {
            if (element is Thumb || element is Rectangle)
                return false;

            if (element is IInteractiveShape)
                return true;

            // exclude orice altceva ce nu este shape vizibil principal
            return !(element is Line or Ellipse or Path or Border or TextBlock or Canvas);
        }

        public void OnMouseDown(Point position)
        {
            throw new NotImplementedException();
        }

        public void DeleteSelectedShape()
        {
            if (_selectedShape is FrameworkElement fe)
            {
                _canvas.Children.Remove(fe);
                _selectedShape = null;
            }
        }


        private ITableShapeRender? FindTableUnderShape(FrameworkElement shape)
        {
            var shapeBounds = new Rect(Canvas.GetLeft(shape), Canvas.GetTop(shape), shape.ActualWidth, shape.ActualHeight);

            foreach (var child in _canvas.Children.OfType<FrameworkElement>())
            {
                if (child is IShapeAddedXaml shapeWithTable)
                {
                    var tableBounds = new Rect(Canvas.GetLeft(child), Canvas.GetTop(child), child.ActualWidth, child.ActualHeight);

                    if (tableBounds.IntersectsWith(shapeBounds) && shapeWithTable.TableShape != null)
                    {
                        return shapeWithTable.TableShape;
                    }
                }
            }

            return null;
        }

        private bool IsInsideResizeThumb(DependencyObject? source)
        {
            while (source != null)
            {
                if (source is Thumb thumb && thumb.Tag?.ToString() == "Resize")
                    return true;

                source = (source is Visual || source is Visual3D)
                    ? VisualTreeHelper.GetParent(source)
                    : LogicalTreeHelper.GetParent(source);
            }

            return false;
        }
    }
}
