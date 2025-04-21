using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private ToolInterceptorService? _interceptor;
        private IToolManager? _toolManager;

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

            SelectToolCommand = new DelegateCommand<object>(param =>
            {
                if (_toolManager == null)
                    return;

                // UI binding a actualizat deja SelectedTool, nu ai nevoie de `param` aici
                _selectedToolService.CurrentTool = SelectedTool;

                switch (SelectedTool)
                {
                    case WhiteBoardTool.CurvedArrow:
                        _toolManager.SetActive("ConnectorCurved");
                        break;
                    case WhiteBoardTool.TextEdit:
                        _toolManager.SetActive("TextEdit");
                        break;
                    case WhiteBoardTool.Cursor:
                        _toolManager.SetActive("Cursor");
                        break;
                    case WhiteBoardTool.None:
                        _toolManager.SetNone();
                        break;
                    default:
                        _toolManager.SetNone();
                        break;
                }

                _interceptor?.InterceptToolSwitch(SelectedTool);
            });
        }

        public void InitializeAfterLoad()
        {
            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            _toolManager = tabService.GetCurrentToolManager();

            if (_toolManager != null)
            {
                _interceptor = new ToolInterceptorService(_toolManager, _selectedToolService);

                _selectedToolService.PropertyChanged += OnSelectedToolChanged;
            }
        }

        private void OnSelectedToolChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedToolService.CurrentTool) && _toolManager != null)
            {
                SelectedTool = _selectedToolService.CurrentTool;

                if (SelectedTool == WhiteBoardTool.None)
                    _toolManager.SetNone();
            }
        }
    }
}
