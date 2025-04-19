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
        private readonly ToolInterceptorService _interceptor;
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
            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            var toolManager = tabService.GetCurrentToolManager();
            _interceptor = new ToolInterceptorService(toolManager, _selectedToolService);
            _selectedToolService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedToolService.CurrentTool))
                {
                    SelectedTool = _selectedToolService.CurrentTool;

                    if (SelectedTool == WhiteBoardTool.None)
                    {
                        toolManager.SetNone();
                    }
                }
            };

            SelectToolCommand = new DelegateCommand<object>(param =>
            {
                if (param is WhiteBoardTool tool)
                {
                    _selectedToolService.CurrentTool = SelectedTool;

                    if(SelectedTool == WhiteBoardTool.CurvedArrow)
                    {
                        toolManager.SetActive("ConnectorCurved");
                    }
                    if (SelectedTool == WhiteBoardTool.TextEdit)
                    {
                        toolManager.SetActive("TextEdit");
                    }

                    _interceptor.InterceptToolSwitch(SelectedTool);
                }
            });
        }
    }
}
