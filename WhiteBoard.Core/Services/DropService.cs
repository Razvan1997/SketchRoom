using SketchRoom.Models.Enums;
using SketchRoom.Models.Shapes;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WhiteBoard.Core.Events;
using WhiteBoard.Core.Factory.Interfaces;
using WhiteBoard.Core.Helpers;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;
using WhiteBoard.Core.UndoRedo;

namespace WhiteBoard.Core.Services
{
    public class DropService : IDropService
    {
        private readonly List<IInteractiveShape> _droppedShapes = new();
        private readonly Canvas _drawingCanvas;
        private readonly IBpmnShapeFactory _factory;
        private readonly IToolManager _toolManager;
        private readonly IDrawingPreferencesService _drawingPreferncesService;
        private readonly BpmnConnectorTool _connectorTool;
        private readonly BpmnConnectorCurvedTool _connectorCurvedTool;
        public Dictionary<FrameworkElement, BPMNNode> _nodeMap { get; }
        private readonly SelectedToolService _selectedToolService;
        private readonly UndoRedoService _undoRedoService;
        private readonly IZOrderService _zOrderService;
        private IInteractiveShape? _lastClickedShape;
        private readonly IShapeRendererFactory _rendererFactory;

        public DropService(
            Canvas drawingCanvas,
            IBpmnShapeFactory factory,
            IToolManager toolManager,
            BpmnConnectorTool connectorTool,
            BpmnConnectorCurvedTool connectorCurvedTool,
            Dictionary<FrameworkElement, BPMNNode> nodeMap,
            SelectedToolService selectedToolService,
            UndoRedoService undoRedoService,
            IDrawingPreferencesService drawingPreferencesService,
            IZOrderService zOrderService,
            IShapeRendererFactory rendererFactory)
        {
            _drawingCanvas = drawingCanvas;
            _factory = factory;
            _toolManager = toolManager;
            _connectorTool = connectorTool;
            _connectorCurvedTool = connectorCurvedTool;
            _nodeMap = nodeMap;
            _selectedToolService = selectedToolService;
            _undoRedoService = undoRedoService;
            _drawingPreferncesService = drawingPreferencesService;
            _zOrderService = zOrderService;

            if (_drawingPreferncesService is INotifyPropertyChanged notifier)
                notifier.PropertyChanged += OnPreferencesChanged;
            _rendererFactory = rendererFactory;
        }

        private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_drawingPreferncesService.IsApplyZIndexOrder))
            {
                if (_lastClickedShape?.Visual is FrameworkElement fe)
                {
                    _zOrderService.BringToFront(fe, _drawingCanvas);
                }
                _drawingPreferncesService.IsApplyZIndexOrder = false;
            }
        }

        public FrameworkElement? HandleDrop(BPMNShapeModel shape, Point dropPos)
        {
            FrameworkElement? visualElement = null;

            if (shape.ShapeContent is IInteractiveShape prototype)
            {
                shape.Id = Guid.NewGuid();

                visualElement = CreateXamlElement(prototype, shape, dropPos);
                visualElement.Tag = "interactive";
                ShapeMetadata.SetShapeId(visualElement, shape.Id.ToString());
            }
           
            if (visualElement != null && shape.Type == ShapeType.ShapeText)
            {
                visualElement.Loaded += (_, _) =>
                {
                    if (visualElement is DependencyObject root)
                    {
                        var textBox = FindFirstTextBox(root);
                        textBox?.Focus();
                        textBox?.SelectAll();
                    }
                };
            }

            return visualElement;
        }

        public FrameworkElement? HandleDropSavedElements(BPMNShapeModelWithPosition shape, Point dropPos, IInteractiveShape? interactiveShape)
        {
            FrameworkElement? visualElement = null;

            // 1. Dacă este o imagine (ShapeType.Image), creăm controlul generic și setăm SvgUri
            if (shape.Type == ShapeType.Image && interactiveShape != null)
            {
                // Setăm tipul și asociem shape-ul
                interactiveShape.SetShape(ShapeType.Image);
                shape.ShapeContent = interactiveShape;

                // Creăm elementul vizual
                visualElement = CreateXamlElement(interactiveShape, shape, dropPos);
                visualElement.Tag = "interactive";
                ShapeMetadata.SetShapeId(visualElement, shape.Id.ToString());
                visualElement.Width = shape.Width;
                visualElement.Height = shape.Height;    

                if (shape.SvgUri != null)
                {
                    ShapeMetadata.SetSvgUri(visualElement, shape.SvgUri);
                }

                return visualElement; 
            }

            // 3. Pentru forme XAML cu control existent
            if (shape.Type.HasValue && interactiveShape != null)
            {
                interactiveShape.SetShape(shape.Type.Value);
                shape.ShapeContent = interactiveShape;

                visualElement = CreateXamlElement(interactiveShape, shape, dropPos);
                visualElement.Tag = "interactive";
                ShapeMetadata.SetShapeId(visualElement, shape.Id.ToString());

                ShapeStyleRestorer.ApplyStyle(shape, visualElement);
            }

            // 4. Autofocus pentru ShapeText
            if (visualElement != null && shape.Type == ShapeType.ShapeText)
            {
                visualElement.Loaded += (_, _) =>
                {
                    if (visualElement is DependencyObject root)
                    {
                        var textBox = FindFirstTextBox(root);
                        textBox?.Focus();
                        textBox?.SelectAll();
                    }
                };
            }

            return visualElement;
        }

        private FrameworkElement? CreateXamlElement(IInteractiveShape prototype, BPMNShapeModel shape, Point dropPos)
        {
            var type = shape.Type;

            var instance = Activator.CreateInstance(prototype.GetType()) as IInteractiveShape;
            if (instance == null)
                return null;

            var element = new BpmnWhiteBoardElementXaml(instance);
            element.SetPosition(dropPos);

            if (element.Visual is not FrameworkElement visual)
                return null;

            if (type == ShapeType.Image)
            {
                ShapeMetadata.SetSvgUri(visual, shape.SvgUri);
            }

            if (type.HasValue)
            {
                instance.SetShape(type.Value);
            }

            AttachEvents(instance);

            return visual;
        }

        private void AttachEvents(IInteractiveShape shape)
        {
            if (_droppedShapes.Contains(shape)) return;

            _droppedShapes.Add(shape);
            shape.EnableConnectors = true;

            shape.ShapeClicked += (s, evt) =>
            {
                _lastClickedShape = shape;
                if (_toolManager.ActiveTool is BpmnTool bpmnTool)
                {
                    bpmnTool.OnMouseDown(evt.GetPosition(_drawingCanvas), evt);
                    if (shape is IUpdateStyle updateable)
                    {
                        ShapeSelectionEventBus.Publish(updateable);
                    }

                    if (_drawingCanvas.Parent is UIElement parent)
                    {
                        parent.Focusable = true;
                        parent.Focus();
                        Keyboard.Focus(parent);
                    }
                }
                evt.Handled = true;
            };

            shape.ConnectionPointClicked += (s, args) =>
            {
                if (args.SourceElement is not FrameworkElement fe || fe.Tag?.ToString() != "Connector")
                    return;

                var direction = args.Direction;
                var mousePos = args.MouseArgs.GetPosition(_drawingCanvas);

                if (_selectedToolService.CurrentTool == WhiteBoardTool.CurvedArrow)
                {
                    _connectorCurvedTool?.SetSelected(shape, direction, args.SourceElement, mousePos);
                }
                else
                {
                    _connectorTool?.SetSelected(shape, direction);
                }

                args.MouseArgs.Handled = true;
            };
        }

        public void PlaceElementOnCanvas(FrameworkElement element, Point position)
        {
            Canvas.SetLeft(element, position.X);
            Canvas.SetTop(element, position.Y);

            if (double.IsNaN(element.Width))
                element.Width = element.ActualWidth > 0 ? element.ActualWidth : 120; // sau orice default logic

            if (double.IsNaN(element.Height))
                element.Height = element.ActualHeight > 0 ? element.ActualHeight : 120;

            var command = new AddShapeCommand(_drawingCanvas, element);
            _undoRedoService.ExecuteCommand(command);
        }

        public void RegisterNodeWhenReady(FrameworkElement element)
        {
            void RegisterNode()
            {
                var pos = new Point(Canvas.GetLeft(element), Canvas.GetTop(element));
                var width = element.ActualWidth;
                var height = element.ActualHeight;

                if (width > 0 && height > 0)
                {
                    var node = new BPMNNode(element); 
                    _nodeMap[element] = node;
                }
            }

            if (element.IsLoaded && element.ActualWidth > 0 && element.ActualHeight > 0)
            {
                RegisterNode();
            }
            else
            {
                element.Loaded += (_, _) => RegisterNode();
            }
        }

        public void RegisterNodeWhenReadyRestore(FrameworkElement element, string shapeId, Dictionary<string, BPMNNode> externalNodeMap)
        {
            void Register()
            {
                if (element.ActualWidth > 0 && element.ActualHeight > 0)
                {
                    var node = new BPMNNode(element);
                    _nodeMap[element] = node;

                    externalNodeMap[shapeId] = node;
                }
            }

            if (element.IsLoaded)
                Register();
            else
                element.Loaded += (_, _) => Register();
        }

        public void TryRegisterNodeImmediate(FrameworkElement element)
        {
            if (element.IsLoaded && element.ActualWidth > 0 && element.ActualHeight > 0)
            {
                var node = new BPMNNode(element);
                _nodeMap[element] = node;
            }
        }

        public void SetupConnectorButton(FrameworkElement element)
        {
            if (element is IInteractiveShape shape)
            {
                AttachEvents(shape);
            }
        }

        private TextBox? FindFirstTextBox(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is TextBox tb)
                    return tb;

                var result = FindFirstTextBox(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private FrameworkElement? CreateVisualElement(Uri uri, Point dropPos)
        {
            var element = new BpmnWhiteBoardElement(uri, _factory);
            element.SetPosition(dropPos);

            if (element.Visual is not FrameworkElement visual)
                return null;

            if (element.Visual is IInteractiveShape interactive)
            {
                AttachEvents(interactive);
            }
            return visual;
        }

        public void MoveOverlayImageToWhiteBoard(FrameworkElement element, Point absolutePosition)
        {
            DetachFromParent(element);
            var parent = VisualTreeHelper.GetParent(element) as Panel;
            parent?.Children.Remove(element);

            PlaceElementOnCanvas(element, absolutePosition);
            RegisterNodeWhenReady(element);
            SetupConnectorButton(element);

            if (element is IInteractiveShape shape && shape.Visual is FrameworkElement fe)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (double.IsNaN(fe.Width) || fe.Width == 0)
                        fe.Width = fe.ActualWidth > 0 ? fe.ActualWidth : 120;

                    if (double.IsNaN(fe.Height) || fe.Height == 0)
                        fe.Height = fe.ActualHeight > 0 ? fe.ActualHeight : 80;

                    shape.Select();

                    Debug.WriteLine($"[Dispatcher] Width: {fe.Width}, Height: {fe.Height}, Actual: {fe.ActualWidth} x {fe.ActualHeight}");
                }, System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void DetachFromParent(FrameworkElement element)
        {
            if (element.Parent is Panel panel)
            {
                panel.Children.Remove(element);
            }
            else if (element.Parent is ContentControl content)
            {
                if (content.Content == element)
                    content.Content = null;
            }
            else if (element.Parent is Decorator decorator)
            {
                if (decorator.Child == element)
                    decorator.Child = null;
            }
        }

        public bool TryGetShapeWrapper(FrameworkElement element, out BpmnWhiteBoardElementXaml? wrapper)
        {
            if (element is null)
            {
                wrapper = null;
                return false;
            }

            if (_droppedShapes.FirstOrDefault(s => s.Visual == element) is IInteractiveShape shape)
            {
                wrapper = new BpmnWhiteBoardElementXaml(shape);
                return true;
            }

            wrapper = null;
            return false;
        }
    }
}
