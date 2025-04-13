using Microsoft.VisualBasic;
using SketchRoom.Models.Enums;
using SketchRoom.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WhiteBoard.Core.Events;
using WhiteBoard.Core.Factory.Interfaces;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;

namespace WhiteBoard.Core.Services
{
    public class DropService : IDropService
    {
        private readonly List<IInteractiveShape> _droppedShapes = new();
        private readonly Canvas _drawingCanvas;
        private readonly IBpmnShapeFactory _factory;
        private readonly IToolManager _toolManager;
        private readonly BpmnConnectorTool _connectorTool;
        private readonly BpmnConnectorCurvedTool _connectorCurvedTool;
        private readonly Dictionary<FrameworkElement, BPMNNode> _nodeMap;
        private readonly SelectedToolService _selectedToolService;

        public DropService(
            Canvas drawingCanvas,
            IBpmnShapeFactory factory,
            IToolManager toolManager,
            BpmnConnectorTool connectorTool,
            BpmnConnectorCurvedTool connectorCurvedTool,
            Dictionary<FrameworkElement, BPMNNode> nodeMap,
            SelectedToolService selectedToolService)
        {
            _drawingCanvas = drawingCanvas;
            _factory = factory;
            _toolManager = toolManager;
            _connectorTool = connectorTool;
            _connectorCurvedTool = connectorCurvedTool;
            _nodeMap = nodeMap;
            _selectedToolService = selectedToolService;
        }

        public FrameworkElement? HandleDrop(BPMNShapeModel shape, Point dropPos)
        {
            FrameworkElement? visualElement = null;

            if (shape.SvgUri != null)
            {
                visualElement = CreateSvgElement(shape, dropPos);
                visualElement.Tag = "interactive";
            }
            else if (shape.ShapeContent is IInteractiveShape prototype)
            {
                visualElement = CreateXamlElement(prototype, shape.Type, dropPos);
                visualElement.Tag = "interactive";
            }
            if (visualElement != null && shape.Type == ShapeType.ShapeText)
            {
                visualElement.Loaded += (_, _) =>
                {
                    // caută TextBox și setează focus
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

        private FrameworkElement? CreateSvgElement(BPMNShapeModel shape, Point dropPos)
        {
            var element = new BpmnWhiteBoardElement(shape.SvgUri, _factory);
            element.SetPosition(dropPos);
            if (element.Visual is not FrameworkElement visual) return null;

            if (element.Visual is IInteractiveShape interactive)
            {
                AttachEvents(interactive);
            }

            return visual;
        }

        private FrameworkElement? CreateXamlElement(IInteractiveShape prototype, ShapeType? type, Point dropPos)
        {
            var instance = Activator.CreateInstance(prototype.GetType()) as IInteractiveShape;
            if (instance == null) return null;

            if (type.HasValue)
            {
                if (instance is IInteractiveShape bindable)
                {
                    bindable.SetShape(type.Value);
                }
            }
            AttachEvents(instance);

            var element = new BpmnWhiteBoardElementXaml(instance);
            element.SetPosition(dropPos);

            if (element.Visual is not FrameworkElement visual) return null;

            return visual;
        }

        private void AttachEvents(IInteractiveShape shape)
        {
            if (_droppedShapes.Contains(shape)) return;

            _droppedShapes.Add(shape);
            shape.EnableConnectors = true;

            shape.ShapeClicked += (s, evt) =>
            {
                if (_toolManager.ActiveTool is BpmnTool bpmnTool)
                {
                    bpmnTool.OnMouseDown(evt.GetPosition(_drawingCanvas), evt);
                    if (shape is IUpdateStyle updateable)
                    {
                        ShapeSelectionEventBus.Publish(updateable);
                    }
                }
                evt.Handled = true;
            };

            shape.ConnectionPointClicked += (s, direction) =>
            {
                if (_selectedToolService.CurrentTool == WhiteBoardTool.CurvedArrow)
                {
                    _connectorCurvedTool?.SetSelected(shape, direction);
                }
                else
                {
                    _connectorTool?.SetSelected(shape, direction);
                }
            };
        }

        public void PlaceElementOnCanvas(FrameworkElement element, Point position)
        {
            Canvas.SetLeft(element, position.X);
            Canvas.SetTop(element, position.Y);
            _drawingCanvas.Children.Add(element);
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
                    var node = new BPMNNode(pos, width, height);
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
    }
}
