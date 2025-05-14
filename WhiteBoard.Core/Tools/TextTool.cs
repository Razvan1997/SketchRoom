using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Factory.Interfaces;
using WhiteBoard.Core.Services;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Tools
{
    public class TextTool : IDrawingTool
    {
        private readonly Canvas _canvas;
        private readonly IToolManager _toolManager;
        private readonly IBpmnShapeFactory _shapeFactory;
        private readonly ISnapService _snapService;

        private ITextInteractiveShape? _draggedShape = null;
        private Point _startPoint;
        private List<Line> _activeSnapLines = new();
        private readonly IDrawingPreferencesService _preferences;
        public string Name => "TextEdit";

        private readonly SelectedToolService _selectedToolService;
        private readonly List<ITextInteractiveShape> _textShapes = new();
        private bool _isDrawing = false;
        public bool IsDrawing => _isDrawing;

        public TextTool(Canvas canvas,
                        IToolManager toolManager,
                        IBpmnShapeFactory shapeFactory,
                        ISnapService snapService,
                        IDrawingPreferencesService preferences,
                        SelectedToolService selectedToolService, List<ITextInteractiveShape> sharedTextList) 
        {
            _canvas = canvas;
            _toolManager = toolManager;
            _shapeFactory = shapeFactory;
            _snapService = snapService;
            _preferences = preferences;
            _selectedToolService = selectedToolService;
            _textShapes = sharedTextList;
        }

        public void OnMouseDown(Point pos, MouseButtonEventArgs e )
        {
            //var hit = VisualTreeHelper.HitTest(_canvas, pos);

            //if (hit?.VisualHit is DependencyObject visualHit)
            //{
            //    // Ignoră dacă ai dat click pe un Thumb sau RotateIcon
            //    if (IsInsideThumbOrRotate(visualHit))
            //    {
            //        _toolManager.SetActive("RotateTool");
            //        return;
            //    }

            //    var existingText = FindParentTextElement(visualHit);
            //    if (existingText != null)
            //    {
            //        existingText.Select();
            //        existingText.FocusText();

            //        _draggedShape = existingText;
            //        _startPoint = pos;
            //        return;
            //    }
            //}

            //var shape = _shapeFactory.CreateShape(ShapeType.TextInput);
            //shape.SetShape(ShapeType.TextInput);
            //shape.SetPosition(pos);
            //_canvas.Children.Add(shape.Visual);

            //if (shape is ITextInteractiveShape textShape)
            //{
            //    textShape.SetPreferences(); // ✅ nou
            //    // stilul e aplicat și la creare + ulterior
            //    textShape.Select();
            //    textShape.FocusText();

            //    _draggedShape = textShape;
            //    _startPoint = pos;
            //    _selectedToolService.CurrentTool = WhiteBoardTool.None;
            //    _textShapes.Add(textShape);
            //}
            //_isDrawing = true;
        }

        public void OnMouseMove(Point pos, MouseEventArgs e)
        {
            if (_draggedShape != null)
            {
                var control = _draggedShape as FrameworkElement;
                if (control == null) return;

                // Șterge snap lines vechi
                foreach (var line in _activeSnapLines)
                    _canvas.Children.Remove(line);
                _activeSnapLines.Clear();

                // Snap Point
                var others = _canvas.Children.OfType<FrameworkElement>()
                    .Where(el => el != control && el.Visibility == Visibility.Visible)
                    .ToList();

                Point snapped = _snapService.GetSnappedPointCursor(pos, others, control, out var snapLines);

                // Aplică poziție
                var delta = snapped - _startPoint;
                _startPoint = snapped;

                double left = Canvas.GetLeft(control);
                double top = Canvas.GetTop(control);

                Canvas.SetLeft(control, left + delta.X);
                Canvas.SetTop(control, top + delta.Y);

                // Adaugă linii noi
                foreach (var line in snapLines)
                {
                    _canvas.Children.Add(line);
                    _activeSnapLines.Add(line);
                }
            }
        }

        public void OnMouseUp(Point pos, MouseButtonEventArgs e)
        {
            _draggedShape = null;

            // Curăță snap lines
            foreach (var line in _activeSnapLines)
                _canvas.Children.Remove(line);
            _activeSnapLines.Clear();

            _isDrawing = false;
        }

        private ITextInteractiveShape? FindParentTextElement(DependencyObject? element)
        {
            while (element != null)
            {
                if (element is ITextInteractiveShape interactive)
                    return interactive;

                element = VisualTreeHelper.GetParent(element);
            }

            return null;
        }

        private bool IsInsideThumbOrRotate(DependencyObject? element)
        {
            while (element != null)
            {
                if (element is Thumb || (element is FrameworkElement fe && fe.Name == "RotateIcon"))
                    return true;

                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }

        public void OnMouseDown(Point position)
        {
            throw new NotImplementedException();
        }
    }
}
