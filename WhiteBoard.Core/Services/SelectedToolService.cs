using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WhiteBoard.Core.Services
{
    public class SelectedToolService : INotifyPropertyChanged
    {
        private WhiteBoardTool _currentTool;
        public WhiteBoardTool CurrentTool
        {
            get => _currentTool;
            set
            {
                if (_currentTool != value)
                {
                    _currentTool = value;
                    ToolChanged?.Invoke(this, _currentTool);
                    OnPropertyChanged();
                }
            }
        }

        public event EventHandler<WhiteBoardTool>? ToolChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
