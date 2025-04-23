using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WhiteBoard.Core.Services
{
    public class DrawingPreferencesService : IDrawingPreferencesService, INotifyPropertyChanged
    {
        private Brush _selectedColor = Brushes.White;
        private double _fontSize = 12;
        private FontWeight _fontWeight = FontWeights.Normal;
        private bool _isApplyBackgroundColor = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Brush SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        public FontWeight FontWeight
        {
            get => _fontWeight;
            set
            {
                if (_fontWeight != value)
                {
                    _fontWeight = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsApplyBackgroundColor
        {
            get => _isApplyBackgroundColor;
            set
            {
                if (_isApplyBackgroundColor != value)
                {
                    _isApplyBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
