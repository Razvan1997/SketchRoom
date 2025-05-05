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
using System.Windows.Documents;

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
                case WhiteBoardTool.Pan:
                    _toolManager.SetActive("Pan");
                    break;
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

                var taggedElement = FindElementWithTag(source);

                if (taggedElement?.Tag is string tag)
                {
                    switch (tag)
                    {
                        case "text":
                            _toolManager.SetActive("TextEdit");
                            return;

                        case "interactive":
                            if (_selectedToolService.CurrentTool == WhiteBoardTool.None)
                                _toolManager.SetActive("BpmnTool");
                            return;

                        case "Connector":
                            if (_selectedToolService.CurrentTool == WhiteBoardTool.CurvedArrow)
                                _toolManager.SetActive("ConnectorCurved");
                            else
                                _toolManager.SetActive("Connector");
                            return;

                        case "Rotate":
                            _toolManager.SetActive("RotateTool");
                            return;

                        case "ConnectorCurved":
                            _toolManager.SetActive("ConnectorCurved");
                            return;

                        case "Resize":
                            _toolManager.SetActive("BpmnTool");
                            return;
                    }
                }
                else
                {
                    // dacă e Run și nu am găsit niciun tag
                    if (source is System.Windows.Documents.Run && _selectedToolService.CurrentTool == WhiteBoardTool.None)
                    {
                        _toolManager.SetActive("BpmnTool");
                        return;
                    }

                    // fallback - dacă tool-ul actual e TextEdit, îl păstrăm
                    if (_selectedToolService.CurrentTool == WhiteBoardTool.TextEdit)
                    {
                        _toolManager.SetActive("TextEdit");
                        return;
                    }
                    if (_selectedToolService.CurrentTool == WhiteBoardTool.FreeDraw)
                    {
                        _toolManager.SetActive("FreeDraw");
                        return;
                    }
                    if (_selectedToolService.CurrentTool == WhiteBoardTool.RemoveStroke)
                    {
                        _toolManager.SetActive("RemoveStroke");
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

                // dacă e Run, mergem în LogicalTree
                if (element is TextElement textElement)
                {
                    element = textElement.Parent as DependencyObject;
                }
                else
                {
                    element = GetParent(element);
                }
            }

            return null;
        }

        private DependencyObject? GetParent(DependencyObject obj)
        {
            try
            {
                DependencyObject parent = VisualTreeHelper.GetParent(obj);
                if (parent == null && obj is FrameworkElement fe)
                    parent = fe.Parent;
                return parent;
            }
            catch
            {
                return null;
            }
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
