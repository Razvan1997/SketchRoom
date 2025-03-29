using System.Windows;
using System.Windows.Input;

namespace UsersInteractionsModule.ViewModels
{
    public class UsersInteractionsViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        public ICommand CreateSketchRoomCommand { get; }
        public ICommand ParticipateSketchRoomCommand { get; }

        public UsersInteractionsViewModel(IRegionManager regionManager)
        {
            CreateSketchRoomCommand = new DelegateCommand(OnCreateSketchRoom);
            ParticipateSketchRoomCommand = new DelegateCommand(OnParticipateToSketchRoom);
            _regionManager = regionManager;
        }

        private void OnCreateSketchRoom()
        {
            _regionManager.RequestNavigate("ContentRegion", "LobbyView");
        }

        private void OnParticipateToSketchRoom()
        {
            _regionManager.RequestNavigate("ContentRegion", "ParticipationView");
        }
    }
}
