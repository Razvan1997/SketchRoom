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
        private string _displayName = string.Empty;
        private string? _avatarBase64;
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

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string SessionCode
        {
            get => _sessionCode;
            set => SetProperty(ref _sessionCode, value);
        }

        // Host server address; defaults to localhost for same-machine testing.
        // Set to the host LAN address (e.g. http://192.168.1.10:5000) to join over LAN.
        // For Online rooms use the central server URL (Task 10 finalizes config).
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

            var user = SecureStorage.LoadUser();
            if (user != null)
            {
                _displayName = $"{user.FirstName} {user.LastName}".Trim();
                _avatarBase64 = user.ImageBase64;
            }

            StartParticipationCommand = new DelegateCommand(async () => await JoinSessionAsync(), () => IsStartParticipationEnabled)
                                .ObservesProperty(() => IsStartParticipationEnabled);

            _session.KickedFromRoom += OnKicked;
        }

        private void OnKicked()
        {
            StatusMessage = "You were removed from the room by the host.";
            _regionManager.RequestNavigate("ContentRegion", "ParticipationView");
        }

        private async Task JoinSessionAsync()
        {
            IsStartParticipationEnabled = false;
            try
            {
                var name = string.IsNullOrWhiteSpace(DisplayName) ? "Guest" : DisplayName.Trim();
                StatusMessage = "Joining...";

                var result = await _session.StartJoinAsync(ServerUrl, SessionCode, name, _avatarBase64);

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
                    StatusMessage = result.Error switch
                    {
                        "ROOM_NOT_FOUND" => $"Room {SessionCode} was not found.",
                        "ROOM_FULL" => "This room is full.",
                        _ => result.Error ?? $"Could not join room {SessionCode}."
                    };
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsStartParticipationEnabled = true;
            }
        }
    }
}
