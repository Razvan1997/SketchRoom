using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using SketchRoom.Models.Enums;
using WhiteBoard.Core.Tools;

namespace WhiteBoard.Core.Services
{
    public class ToolInterceptorService
    {
        private readonly IToolManager _toolManager;
        private readonly SelectedToolService _selectedToolService;
        public bool IsUserActing { get; set; } = false;
        public ToolInterceptorService(IToolManager toolManager, SelectedToolService selectedToolService)
        {
            _toolManager = toolManager;
            _selectedToolService = selectedToolService;
        }

        public void InterceptToolSwitch(WhiteBoardTool tool)
        {
            switch (tool)
            {
                case WhiteBoardTool.TextEdit:
                    _toolManager.SetActive("TextEdit");
                    break;
                case WhiteBoardTool.CurvedArrow:
                    _toolManager.SetActive("ConnectorCurved");
                    break;
                case WhiteBoardTool.Drag:
                case WhiteBoardTool.Cursor:
                case WhiteBoardTool.None:
                    _toolManager.SetNone();
                    break;
                default:
                    _toolManager.SetNone();
                    break;
            }
        }

        public void InterceptToolSwitch(MouseButtonEventArgs e)
        {
            if (IsUserActing)
                return;

            if (e.OriginalSource is DependencyObject source)
            {
                // Dacă tool-ul activ este în stare de desenare, nu mai comuta
                if (_toolManager.ActiveTool is IDrawingTool tool && HasDrawingPriority(tool))
                    return;
                var currentTool = _toolManager.ActiveTool?.Name;
                var taggedElement = FindElementWithTag(source);
                if (taggedElement?.Tag is string tag)
                {
                    switch (tag)
                    {
                        case "text":
                            _toolManager.SetActive("TextEdit");
                            return;
                        case "interactive":
                            _toolManager.SetActive("BpmnTool");
                            return;
                        case "Connector":
                            if (_selectedToolService.CurrentTool == WhiteBoardTool.CurvedArrow)
                            {
                                _toolManager.SetActive("ConnectorCurved");
                            }
                            else
                            {
                                _toolManager.SetActive("Connector");
                            }
                            return;

                        case "Rotate":
                            _toolManager.SetActive("RotateTool");
                            return;

                        case "ConnectorCurved":
                            _toolManager.SetActive("ConnectorCurved");
                            return;
                    }
                }
                else
                {
                    if (_selectedToolService.CurrentTool == WhiteBoardTool.TextEdit)
                    {
                        _toolManager.SetActive("TextEdit");
                        return;
                    }
                }
            }

            _toolManager.SetNone();
        }

        private FrameworkElement? FindElementWithTag(DependencyObject? element)
        {
            while (element != null)
            {
                if (element is FrameworkElement fe && fe.Tag is string)
                    return fe;

                element = VisualTreeHelper.GetParent(element);
            }

            return null;
        }

        private bool HasDrawingPriority(IDrawingTool tool)
        {
            // Poți extinde aici cu orice alt tool care are o acțiune în desfășurare
            if (tool is IDrawingTool curved && curved.IsDrawing)
                return true;

            return false;
        }
    }
}
