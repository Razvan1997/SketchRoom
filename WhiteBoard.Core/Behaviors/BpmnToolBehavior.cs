using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;

namespace WhiteBoard.Core.Behaviors
{
    public class BpmnToolBehavior : IToolBehavior
    {
        private readonly BpmnTool _bpmnTool;
        private readonly WhiteBoardHost _host;
        private readonly Canvas _drawingCanvas;
        private readonly IToolManager _toolManager;

        public BpmnToolBehavior(BpmnTool bpmnTool, WhiteBoardHost host, IToolManager toolManager, Canvas canvas)
        {
            _bpmnTool = bpmnTool;
            _host = host;
            _toolManager = toolManager;
            _drawingCanvas = canvas;
        }

        public void OnMouseDown(Point position, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == _drawingCanvas)
            {
                _bpmnTool.DeselectCurrent();
                _toolManager.SetActive("FreeDraw");
                _host.HandleMouseDown(position);
                e.Handled = true;
            }
        }

        public void OnMouseMove(Point position, MouseEventArgs e)
        {
            _host.HandleMouseMove(position); // poate fi schimbat dacă e nevoie de preview logică separată
        }

        public void OnMouseUp(Point position, MouseButtonEventArgs e)
        {
            _host.HandleMouseUp(position);
        }
    }
}
