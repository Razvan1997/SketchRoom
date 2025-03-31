using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using WhiteBoardModule.Events;

namespace WhiteBoardModule.ViewModels
{
    public class SessionActionsViewModel : BindableBase
    {
        private bool _isHost;
        private bool _isSessionActive;
        private string _sessionCode;
        private Brush _selectedColor;

        public string ActionLabel => _isHost ? "Close sketch room" : "Leave room";
        public ICommand ActionCommand { get; }
        public ICommand SelectColorCommand { get; }

        public ICommand TransformToTextCommand { get; }

        public bool IsSessionActive
        {
            get => _isSessionActive;
            set => SetProperty(ref _isSessionActive, value);
        }

        public ObservableCollection<Brush> AvailableColors { get; } = new()
        {
            Brushes.Black,
            Brushes.Red,
            Brushes.Green,
            Brushes.Blue,
            Brushes.Yellow,
            Brushes.Orange,
            Brushes.Purple,
            Brushes.Brown,
            Brushes.Gray,
            Brushes.Cyan,
            Brushes.Magenta
        };

        public Brush SelectedColor
        {
            get => _selectedColor;
            set => SetProperty(ref _selectedColor, value);
        }
        private DrawingStateService.DrawingStateService _stateService;
        public SessionActionsViewModel(IEventAggregator eventAggregator, DrawingStateService.DrawingStateService stateService)
        {
            _stateService = stateService;
            IsSessionActive = true;
            SelectedColor = Brushes.Black;

            ActionCommand = new DelegateCommand(OnAction, CanExecuteActionCommand)
                                .ObservesProperty(() => IsSessionActive);

            TransformToTextCommand = new DelegateCommand(OnTransformToText, CanTransformToText);

            SelectColorCommand = new DelegateCommand<Brush>(color =>
            {
                SelectedColor = color;
                _stateService.SelectedColor = color;
            });

            eventAggregator.GetEvent<SessionContextEvent>().Subscribe(ctx =>
            {
                _isHost = ctx.IsHost;
                _sessionCode = ctx.SessionCode;
                RaisePropertyChanged(nameof(ActionLabel));
            });
        }

        private bool CanTransformToText()
        {
            return true;
        }

        private void OnTransformToText()
        {
            var drawingService = ContainerLocator.Container.Resolve<DrawingStateService.DrawingStateService>();

            if (!drawingService.IsSelectionModeEnabled)
            {
                drawingService.IsSelectionModeEnabled = true;
            }
            else
            {
                drawingService.IsSelectionModeEnabled = false;
            }
        }

        private bool CanExecuteActionCommand()
        {
            return IsSessionActive;
        }

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
    }
}
