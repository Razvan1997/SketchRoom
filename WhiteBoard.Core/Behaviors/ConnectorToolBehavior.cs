using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;

namespace WhiteBoard.Core.Behaviors
{
    public class ConnectorToolBehavior : IToolBehavior
    {
        private readonly IDrawingTool _tool;
        private readonly IToolManager _toolManager;
        private readonly WhiteBoardHost _host;
        private readonly Canvas _drawingCanvas;

        public ConnectorToolBehavior(IDrawingTool tool, IToolManager toolManager, WhiteBoardHost host, Canvas canvas)
        {
            _tool = tool;
            _toolManager = toolManager;
            _host = host;
            _drawingCanvas = canvas;
        }

        public void OnMouseDown(Point position, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Path path && path.Data is Geometry)
                return;

            // 👇 Dacă tool-ul are un IsDrawing, îl verificăm dinamic
            if (_tool is BpmnConnectorTool standard && standard.IsDrawing)
            {
                standard.OnMouseDown(position);
                e.Handled = true;
                return;
            }

            if (_tool is BpmnConnectorCurvedTool curved)
            {
                if (!curved.IsDrawing)
                {
                    _toolManager.SetActive("ConnectorCurved");
                    return;
                }
                curved.OnMouseDown(position);
                e.Handled = true;
                return;
            }

            if (e.OriginalSource is Rectangle)
            {
                _tool.OnMouseDown(position);
                e.Handled = true;
                return;
            }

            if (e.OriginalSource == _drawingCanvas)
            {
                if (_tool is BpmnConnectorTool bpmnTool)
                {
                    bpmnTool.DeselectCurrent();
                    bpmnTool.DeselectAllConnections();
                }

                _toolManager.SetActive("FreeDraw");
                _host.HandleMouseDown(position);
                e.Handled = true;
            }
        }

        public void OnMouseMove(Point position, MouseEventArgs e)
        {
            _tool.OnMouseMove(position);
        }

        public void OnMouseUp(Point position, MouseButtonEventArgs e)
        {
            _tool.OnMouseUp(position);
        }
    }
}
