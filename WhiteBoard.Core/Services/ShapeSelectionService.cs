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

namespace WhiteBoard.Core.Services
{
    public class ShapeSelectionService : IShapeSelectionService
    {
        private readonly IDrawingPreferencesService _preferences;
        private readonly IWhiteBoardTabService _tabService;
        private IToolManager _currentToolManager;

        private Border _selectedBorder;
        private TextBox _selectedTextBox;
        public ShapePart Current { get; private set; } = ShapePart.None;

        public ShapeSelectionService(
            IDrawingPreferencesService preferences,
            IWhiteBoardTabService tabService)
        {
            _preferences = preferences;
            _tabService = tabService;

            if (_preferences is INotifyPropertyChanged notifier)
                notifier.PropertyChanged += OnPreferencesChanged;

            // Ascultăm schimbarea tab-ului
            _tabService.TabChanged += OnTabChanged;

            // Abonare inițială dacă există deja un tab
            SubscribeToToolManager(_tabService.GetCurrentToolManager());
        }

        private void OnTabChanged(FooterTabModel tab)
        {
            // Se schimbă ToolManager-ul activ
            SubscribeToToolManager(_tabService.GetCurrentToolManager());
        }

        private void SubscribeToToolManager(IToolManager? newToolManager)
        {
            // Dezabonare de la vechiul ToolManager
            if (_currentToolManager != null)
                _currentToolManager.ToolChanged -= OnToolChanged;

            // Reabonare la noul ToolManager
            _currentToolManager = newToolManager;

            if (_currentToolManager != null)
                _currentToolManager.ToolChanged += OnToolChanged;
        }

        private void OnToolChanged(IDrawingTool tool)
        {
            if(_currentToolManager.ActiveTool == null)
            {
                if(_selectedBorder != null)
                {
                    _selectedBorder.BorderThickness = new Thickness(2);
                }
            }
            //ClearSelection();
        }

        private void OnPreferencesChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_preferences.SelectedColor))
                ApplyCurrentColor();
        }

        public void Select(ShapePart part, Border border, TextBox textBox)
        {
            Current = part;
            _selectedBorder = border;
            _selectedTextBox = textBox;
        }

        public void ApplyVisual(Border border, TextBox textBox)
        {
            border.BorderBrush = Brushes.Black;
            border.BorderThickness = new Thickness(2);
            textBox.Foreground = Brushes.Black;
        }

        private void ApplyCurrentColor()
        {
            if (_selectedBorder == null || _selectedTextBox == null)
                return;

            var color = _preferences.SelectedColor;

            switch (Current)
            {
                case ShapePart.Margin:
                    _selectedBorder.BorderBrush = color;
                    _selectedBorder.BorderThickness = new Thickness(4);
                    break;
                case ShapePart.Border:
                    _selectedBorder.Background = color;
                    break;
                case ShapePart.Text:
                    _selectedTextBox.Foreground = color;
                    break;
            }
        }
    }
}
