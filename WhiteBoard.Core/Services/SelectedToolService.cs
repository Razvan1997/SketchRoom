using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhiteBoard.Core.Services
{
    public class SelectedToolService
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
                }
            }
        }

        public event EventHandler<WhiteBoardTool>? ToolChanged;
    }
}
