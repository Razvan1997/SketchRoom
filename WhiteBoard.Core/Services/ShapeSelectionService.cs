using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.ComponentModel;
using WhiteBoard.Core.Models;
using System.Windows.Documents;
using System.Diagnostics.Metrics;
using WhiteBoard.Core.UndoRedo;
using System.Windows.Shapes;
using System.ComponentModel.Design;

namespace WhiteBoard.Core.Services
{
    public class ShapeSelectionService : IShapeSelectionService
    {
        private readonly IDrawingPreferencesService _preferences;
        private readonly UndoRedoService _undoRedoService;
        private DependencyObject _selectedElement;
        public ShapePart Current { get; private set; } = ShapePart.None;

        public ShapeSelectionService(IDrawingPreferencesService preferences, UndoRedoService undoRedoService)
        {
            _preferences = preferences;
            _undoRedoService = undoRedoService;

            if (_preferences is INotifyPropertyChanged notifier)
                notifier.PropertyChanged += OnPreferencesChanged;
        }

        private void OnPreferencesChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_preferences.SelectedColor) && !_preferences.IsApplyBackgroundColor)
                ApplyCurrentColor();
            if (e.PropertyName == nameof(_preferences.FontWeight))
                ApplyFontWeight();
            if(e.PropertyName == nameof(_preferences.FontSize))
                ApplyFontSize();
            if (e.PropertyName == nameof(_preferences.IsApplyBackgroundColor))
                ApplyBackgroundText();

            //_selectedElement = null;
        }

        public void Select(ShapePart part, DependencyObject shapeRoot)
        {
            Current = part;
            _selectedElement = shapeRoot;

            if (shapeRoot is Shape shape)
            {
                if (shape.Tag == null || shape.Tag?.ToString() != "Bracket")
                {
                    shape.StrokeThickness = (part == ShapePart.Margin) ? 4 : 2;
                }

            }
            else if (shapeRoot is Border border)
            {
                border.BorderThickness = (part == ShapePart.Margin) ? new Thickness(4) : new Thickness(2);
            }
        }

        public void ApplyVisual(DependencyObject shapeRoot)
        {
            if (shapeRoot is Shape shape)
            {
                shape.Stroke = Brushes.Black;
                shape.StrokeThickness = 2;
            }
            else if (shapeRoot is Border border)
            {
                border.BorderBrush = Brushes.Black;
                border.BorderThickness = new Thickness(2);
            }
        }

        private void ApplyCurrentColor()
        {
            if (_selectedElement == null)
                return;

            var color = _preferences.SelectedColor;

            if (_selectedElement is Shape shape)
            {
                if (Current == ShapePart.Margin)
                    shape.Stroke = color;
                else
                    shape.Fill = color;
            }
            else if (_selectedElement is Border border)
            {
                if (Current == ShapePart.Margin)
                    border.BorderBrush = color;
                else
                    border.Background = color;
            }
            else if (_selectedElement is TextBox textBox)
            {
                textBox.Foreground = color;
            }
            else if (_selectedElement is RichTextBox richTextBox)
            {
                var selection = richTextBox.Selection;
                if (!selection.IsEmpty)
                {
                    selection.ApplyPropertyValue(TextElement.ForegroundProperty, color);
                }
            }
            //else if (_selectedElement is Grid grid)
            //{
            //    if (Current == ShapePart.Margin)
            //        grid.BorderBrush = color;
            //    else
            //        grid.Background = color;
            //}
        }

        private void ApplyFontWeight()
        {
            if (_selectedElement == null)
                return;

            var fontWeight = _preferences.FontWeight;

            if (_selectedElement is TextBox textBox)
            {
                textBox.FontWeight = fontWeight;
            }
            else if (_selectedElement is RichTextBox richTextBox)
            {
                var selection = richTextBox.Selection;
                if (!selection.IsEmpty)
                {
                    selection.ApplyPropertyValue(TextElement.FontWeightProperty, fontWeight);
                }
            }
        }

        private void ApplyFontSize()
        {
            if (_selectedElement == null)
                return;

            var fontSize = _preferences.FontSize;

            if (_selectedElement is TextBox textBox)
            {
                textBox.FontSize = fontSize;
            }
            else if (_selectedElement is RichTextBox richTextBox)
            {
                var selection = richTextBox.Selection;
                if (!selection.IsEmpty)
                {
                    selection.ApplyPropertyValue(TextElement.FontSizeProperty, fontSize);
                }
            }
        }

        private void ApplyBackgroundText()
        {
            if (_selectedElement == null)
                return;

            if (_selectedElement is RichTextBox richTextBox)
            {
                if (_preferences.IsApplyBackgroundColor)
                {
                    richTextBox.Background = _preferences.SelectedColor;
                }
                else
                {
                    richTextBox.Background = Brushes.Transparent;
                }
            }
        }

        public void Deselect()
        {
            _selectedElement = null;
        }
    }
}
