using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WhiteBoard.Core.Services;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;

namespace WhiteBoardModule.ViewModels
{
    public class BottomToolsActionsViewModel : BindableBase
    {
        private readonly SelectedToolService _selectedToolService;

        private WhiteBoardTool _selectedTool;
        public WhiteBoardTool SelectedTool
        {
            get => _selectedTool;
            set => SetProperty(ref _selectedTool, value);
        }

        public ICommand SelectToolCommand { get; }

        public BottomToolsActionsViewModel()
        {
            _selectedToolService = ContainerLocator.Container.Resolve<SelectedToolService>();
            var toolManager = ContainerLocator.Container.Resolve<IToolManager>();

            SelectToolCommand = new DelegateCommand<object>(param =>
            {
                if (param is WhiteBoardTool tool)
                {
                    _selectedToolService.CurrentTool = SelectedTool;

                    if(SelectedTool == WhiteBoardTool.CurvedArrow)
                    {
                        toolManager.SetActive("ConnectorCurved");
                    }
                }
            });
        }
    }
}
