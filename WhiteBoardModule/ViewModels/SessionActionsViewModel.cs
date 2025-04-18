using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;
using WhiteBoardModule.Events;

namespace WhiteBoardModule.ViewModels
{
    public class SessionActionsViewModel : BindableBase
    {
        private bool _isHost;
        private bool _isSessionActive;
        private string _sessionCode;

        private readonly IDrawingPreferencesService _preferences;

        public string ActionLabel => _isHost ? "Close sketch room" : "Leave room";

        public ICommand ActionCommand { get; }
        public ICommand SelectColorCommand { get; }
        public ICommand ToggleBoldCommand { get; }
        public ICommand TransformToTextCommand { get; }

        public ObservableCollection<Brush> AvailableColors { get; } = new()
        {
            Brushes.Black, Brushes.Red, Brushes.Green, Brushes.Blue,
            Brushes.Yellow, Brushes.Orange, Brushes.Purple, Brushes.Brown,
            Brushes.Gray, Brushes.Cyan, Brushes.Magenta
        };

        public ObservableCollection<double> FontSizes { get; } = new()
        {
            8, 10, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72
        };

        public double SelectedFontSize
        {
            get => _preferences.FontSize;
            set
            {
                _preferences.FontSize = value;
                SelectedShape?.UpdateStyle(_preferences.FontWeight, value, _preferences.SelectedColor);
                RaisePropertyChanged();
            }
        }

        public Brush SelectedColor
        {
            get => _preferences.SelectedColor;
            set
            {
                _preferences.SelectedColor = value;
                SelectedShape?.UpdateStyle(_preferences.FontWeight, _preferences.FontSize, value);
                RaisePropertyChanged();
            }
        }

        public bool IsBold
        {
            get => _preferences.FontWeight == FontWeights.Bold;
            set
            {
                _preferences.FontWeight = value ? FontWeights.Bold : FontWeights.Normal;
                SelectedShape?.UpdateStyle(_preferences.FontWeight, _preferences.FontSize, _preferences.SelectedColor);
                RaisePropertyChanged();
            }
        }

        public bool IsSessionActive
        {
            get => _isSessionActive;
            set => SetProperty(ref _isSessionActive, value);
        }

        private IUpdateStyle? _selectedShape;

        public IUpdateStyle? SelectedShape
        {
            get => _selectedShape;
            set
            {
                _selectedShape = value;
                RaisePropertyChanged();
            }
        }

        private readonly DrawingStateService.DrawingStateService _stateService;

        public SessionActionsViewModel(
            IEventAggregator eventAggregator,
            DrawingStateService.DrawingStateService stateService,
            IDrawingPreferencesService preferences)
        {

            WhiteBoard.Core.Events.ShapeSelectionEventBus.Subscribe(OnShapeSelected);
            _preferences = preferences;
            _stateService = stateService;
            IsSessionActive = true;

            ActionCommand = new DelegateCommand(OnAction, CanExecuteActionCommand)
                                .ObservesProperty(() => IsSessionActive);

            TransformToTextCommand = new DelegateCommand(OnTransformToText, CanTransformToText);

            SelectColorCommand = new DelegateCommand<Brush>(color =>
            {
                SelectedColor = color;

                var toolManager = ContainerLocator.Container.Resolve<IToolManager>();
                if (toolManager.GetToolByName("FreeDraw") is FreeDrawTool freeDraw)
                    freeDraw.StrokeColor = color;
            });

            ToggleBoldCommand = new DelegateCommand(() =>
            {
                IsBold = !IsBold;
            });

            eventAggregator.GetEvent<SessionContextEvent>().Subscribe(ctx =>
            {
                _isHost = ctx.IsHost;
                _sessionCode = ctx.SessionCode;
                RaisePropertyChanged(nameof(ActionLabel));
            });
        }

        private void OnShapeSelected(IUpdateStyle? style)
        {
            SelectedShape = style;
        }

        private bool CanTransformToText() => true;

        private void OnTransformToText()
        {
            _stateService.IsSelectionModeEnabled = !_stateService.IsSelectionModeEnabled;
        }

        private bool CanExecuteActionCommand() => IsSessionActive;

        private void OnAction()
        {
            if (_isHost)
            {
                // Închide sesiunea
            }
            else
            {
                // Ieși din sesiune
            }
        }

        public void UpdateSelectedShape(IUpdateStyle? shape)
        {
            SelectedShape = shape;
        }
    }
}
