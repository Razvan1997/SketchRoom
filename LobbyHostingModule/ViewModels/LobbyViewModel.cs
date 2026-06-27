using SketchRoom.Database;
using SketchRoom.Realtime.Contracts;
using SketchRoom.Realtime.Server;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WhiteBoard.Core.Collaboration;

namespace LobbyHostingModule.ViewModels
{
    public sealed class LobbyParticipant : BindableBase
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarBase64 { get; set; }
        public bool IsHost { get; set; }
        public bool CanKick { get; set; }
    }

    public class LobbyViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly CollaborationSession _session;
        private readonly EmbeddedRealtimeServer _server;

        private bool _isLanMode;
        private bool _isBusy;
        private bool _isHosting;
        private bool _isLocked;
        private string _sessionCode = string.Empty;
        private string _lanAddress = string.Empty;
        private string _statusMessage = string.Empty;

        // Task 10 seam: central (Online) server URL. Replace this default with a value
        // sourced from app settings/config once Task 10 finalizes configuration.
        private string _centralServerUrl = CollaborationSession.DefaultLocalUrl;

        public bool IsOnlineMode
        {
            get => !_isLanMode;
            set { if (value) IsLanMode = false; }
        }

        public bool IsLanMode
        {
            get => _isLanMode;
            set
            {
                if (SetProperty(ref _isLanMode, value))
                    RaisePropertyChanged(nameof(IsOnlineMode));
            }
        }

        public string CentralServerUrl
        {
            get => _centralServerUrl;
            set => SetProperty(ref _centralServerUrl, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public bool IsHosting
        {
            get => _isHosting;
            set => SetProperty(ref _isHosting, value);
        }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (SetProperty(ref _isLocked, value))
                    RaisePropertyChanged(nameof(LockButtonText));
            }
        }

        public string LockButtonText => IsLocked ? "Unlock room" : "Lock room";

        public string SessionCode
        {
            get => _sessionCode;
            set => SetProperty(ref _sessionCode, value);
        }

        // LAN-only: "http://<lan-ip>:<port>" others type into the join screen.
        public string LanAddress
        {
            get => _lanAddress;
            set => SetProperty(ref _lanAddress, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<LobbyParticipant> ConnectedParticipants { get; } = new();

        public ICommand CreateRoomCommand { get; }
        public ICommand StartSessionCommand { get; }
        public ICommand KickCommand { get; }
        public ICommand ClearBoardCommand { get; }
        public ICommand ToggleLockCommand { get; }

        public LobbyViewModel(IRegionManager regionManager, CollaborationSession session, EmbeddedRealtimeServer server)
        {
            _regionManager = regionManager;
            _session = session;
            _server = server;

            CreateRoomCommand = new DelegateCommand(async () => await OnCreateRoomAsync(), () => !IsBusy && !IsHosting)
                .ObservesProperty(() => IsBusy).ObservesProperty(() => IsHosting);
            StartSessionCommand = new DelegateCommand(OnStartSession, () => IsHosting)
                .ObservesProperty(() => IsHosting);
            KickCommand = new DelegateCommand<string>(async userId => await OnKickAsync(userId));
            ClearBoardCommand = new DelegateCommand(async () => await _session.ClearBoardAsync());
            ToggleLockCommand = new DelegateCommand(async () => await OnToggleLockAsync());

            _session.StateChanged += OnRoomStateChanged;
        }

        private void OnRoomStateChanged(RoomStateDto state)
        {
            ConnectedParticipants.Clear();
            foreach (var p in state.Participants)
            {
                ConnectedParticipants.Add(new LobbyParticipant
                {
                    UserId = p.UserId,
                    DisplayName = p.DisplayName,
                    AvatarBase64 = p.AvatarBase64,
                    IsHost = p.IsHost,
                    CanKick = _session.IsHost && p.UserId != _session.UserId
                });
            }
            IsLocked = state.IsLocked;
        }

        private async System.Threading.Tasks.Task OnCreateRoomAsync()
        {
            IsBusy = true;
            StatusMessage = "Creating room...";
            try
            {
                var user = SecureStorage.LoadUser();
                var name = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Host";
                if (string.IsNullOrWhiteSpace(name)) name = "Host";

                string baseUrl;
                if (IsLanMode)
                {
                    LanAddress = await _server.StartAsync();
                    baseUrl = CollaborationSession.DefaultLocalUrl;
                }
                else
                {
                    LanAddress = string.Empty;
                    baseUrl = CentralServerUrl;
                }

                var result = await _session.StartHostAsync(baseUrl, name, user?.ImageBase64);
                if (result.Success)
                {
                    SessionCode = _session.RoomCode;
                    IsHosting = true;
                    StatusMessage = IsLanMode
                        ? $"Room ready. Share code {SessionCode} and address {LanAddress}."
                        : $"Room ready. Share code {SessionCode}.";
                }
                else
                {
                    StatusMessage = $"Could not create room: {result.Error}";
                    if (IsLanMode) await _server.StopAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                if (IsLanMode) { try { await _server.StopAsync(); } catch { /* best-effort */ } }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async System.Threading.Tasks.Task OnKickAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;
            try { await _session.KickUserAsync(userId); }
            catch (Exception ex) { StatusMessage = $"Kick failed: {ex.Message}"; }
        }

        private async System.Threading.Tasks.Task OnToggleLockAsync()
        {
            try { await _session.SetLockAsync(!IsLocked); }
            catch (Exception ex) { StatusMessage = $"Lock failed: {ex.Message}"; }
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
