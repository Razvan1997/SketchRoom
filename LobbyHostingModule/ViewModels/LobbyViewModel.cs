using SketchRoom.Database;
using SketchRoom.Models;
using SketchRoom.Realtime.Contracts;
using SketchRoom.Realtime.Server;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WhiteBoard.Core.Collaboration;

namespace LobbyHostingModule.ViewModels
{
    public class LobbyViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly CollaborationSession _session;
        private readonly EmbeddedRealtimeServer _server;
        private bool _isStartLobbyEnabled = true;
        private bool _isStartSessionEnabled = true;
        private string _sessionCode = string.Empty;

        public bool IsStartLobbyEnabled
        {
            get => _isStartLobbyEnabled;
            set => SetProperty(ref _isStartLobbyEnabled, value);
        }

        public bool IsStartSessionEnabled
        {
            get => _isStartSessionEnabled;
            set => SetProperty(ref _isStartSessionEnabled, value);
        }

        public string SessionCode
        {
            get => _sessionCode;
            set => SetProperty(ref _sessionCode, value);
        }

        public ObservableCollection<Participant> ConnectedParticipants { get; } = new();
        public ICommand StartLobbyCommand { get; }
        public ICommand StartSessionCommand { get; }

        public LobbyViewModel(IRegionManager regionManager, CollaborationSession session, EmbeddedRealtimeServer server)
        {
            _regionManager = regionManager;
            _session = session;
            _server = server;

            StartLobbyCommand = new DelegateCommand(OnStartLobby, () => IsStartLobbyEnabled)
                                .ObservesProperty(() => IsStartLobbyEnabled);

            StartSessionCommand = new DelegateCommand(OnStartSession, () => IsStartSessionEnabled)
                                .ObservesProperty(() => IsStartSessionEnabled);

            _session.StateChanged += OnRoomStateChanged;
        }

        private void OnRoomStateChanged(RoomStateDto state)
        {
            ConnectedParticipants.Clear();
            foreach (var p in state.Participants)
            {
                ConnectedParticipants.Add(new Participant
                {
                    ConnectionId = p.UserId,
                    FirstName = p.DisplayName,
                    ImageBase64 = p.AvatarBase64
                });
            }
        }

        private async void OnStartLobby()
        {
            IsStartLobbyEnabled = false;
            try
            {
                var user = SecureStorage.LoadUser();
                await _server.StartAsync();

                var name = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Host";
                var result = await _session.StartHostAsync(CollaborationSession.DefaultLocalUrl, name, user?.ImageBase64);

                SessionCode = result.Success ? _session.RoomCode : $"Error: {result.Error}";
            }
            catch (Exception ex)
            {
                SessionCode = $"Error: {ex.Message}";
            }
            IsStartLobbyEnabled = true;
        }

        private void OnStartSession()
        {
            if (!_session.IsActive) return;

            var parameters = new NavigationParameters
            {
                { "IsHost", true },
                { "IsParticipant", false },
                { "SessionCode", _session.RoomCode }
            };

            _regionManager.RequestNavigate("ContentRegion", "WhiteBoardView", parameters);
        }
    }
}
