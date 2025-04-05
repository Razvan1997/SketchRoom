using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WhiteBoardModule.ViewModels
{
    public class BottomToolsActionsViewModel : BindableBase
    {
        private WhiteBoardTool _selectedTool;
        public WhiteBoardTool SelectedTool
        {
            get => _selectedTool;
            set => SetProperty(ref _selectedTool, value);
        }

        public ICommand SelectToolCommand { get; }

        public BottomToolsActionsViewModel()
        {
            SelectToolCommand = new DelegateCommand<object>(param =>
            {
                if (param is WhiteBoardTool tool)
                    SelectedTool = tool;
            });
        }
    }
}
