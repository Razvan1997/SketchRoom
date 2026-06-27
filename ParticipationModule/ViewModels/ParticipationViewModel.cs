using Prism.Navigation.Regions;
using SketchRoom.Database;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using WhiteBoard.Core.Collaboration;

namespace ParticipationModule.ViewModels
{
    public class ParticipationViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly CollaborationSession _session;
        private string _sessionCode = string.Empty;
        private string _statusMessage = string.Empty;
        private string _serverUrl = CollaborationSession.DefaultLocalUrl;
        private bool _isStartParticipationEnabled = true;

        public bool IsStartParticipationEnabled
        {
            get => _isStartParticipationEnabled;
            set => SetProperty(ref _isStartParticipationEnabled, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string SessionCode
        {
            get => _sessionCode;
            set => SetProperty(ref _sessionCode, value);
        }

        // Host server address; defaults to localhost for same-machine testing.
        // Set to the host LAN IP (e.g. http://192.168.1.10:5000) to join over LAN.
        public string ServerUrl
        {
            get => _serverUrl;
            set => SetProperty(ref _serverUrl, value);
        }

        public ICommand StartParticipationCommand { get; }

        public ParticipationViewModel(IRegionManager regionManager, CollaborationSession session)
        {
            _regionManager = regionManager;
            _session = session;

            StartParticipationCommand = new DelegateCommand(async () => await JoinSessionAsync(), () => IsStartParticipationEnabled)
                                .ObservesProperty(() => IsStartParticipationEnabled);
        }

        private async Task JoinSessionAsync()
        {
            try
            {
                var user = SecureStorage.LoadUser();
                var name = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Guest";

                var result = await _session.StartJoinAsync(ServerUrl, SessionCode, name, user?.ImageBase64);

                if (result.Success)
                {
                    StatusMessage = $"Connected to session {SessionCode}.";

                    var parameters = new NavigationParameters
                    {
                        { "IsHost", false },
                        { "IsParticipant", true },
                        { "SessionCode", _session.RoomCode }
                    };

                    _regionManager.RequestNavigate("ContentRegion", "WhiteBoardView", parameters);
                }
                else
                {
                    StatusMessage = result.Error ?? $"Session {SessionCode} not found.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }
    }
}
