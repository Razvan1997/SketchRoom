using Prism.Navigation.Regions;
using SketchRoom.Database;
using SketchRoom.Models.DTO;
using SketchRoom.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ParticipationModule.ViewModels
{
    public class ParticipationViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly WhiteboardHubClient _hubClient;
        private string _sessionCode;
        private string _statusMessage;
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

        public ICommand StartParticipationCommand { get; }
        public ParticipationViewModel(IRegionManager regionManager, WhiteboardHubClient hubClient)
        {
            _regionManager = regionManager;
            _hubClient = hubClient;

            StartParticipationCommand = new DelegateCommand(async () => await JoinSessionAsync(), CanStartParticipation)
                                .ObservesProperty(() => IsStartParticipationEnabled);
        }

        private bool CanStartParticipation()
        {
            return IsStartParticipationEnabled;
        }

        private async Task JoinSessionAsync()
        {
            try
            {
                var user = SecureStorage.LoadUser();

                if (user != null)
                {
                    var participant = new JoinSessionDto
                    {
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        ImageBase64 = user.ImageBase64
                    };

                    await _hubClient.ConnectAsync();

                    RegisterRoomStartedHandlerOnce();

                    bool success = await _hubClient.JoinSessionAsync(SessionCode, participant);

                    StatusMessage = success
                        ? $"Connected to session {SessionCode}. Waiting for host to start..."
                        : $"Session {SessionCode} not found.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
            }
        }

        private bool _roomStartedHandlerRegistered = false;
        private void RegisterRoomStartedHandlerOnce()
        {
            if (_roomStartedHandlerRegistered)
                return;

            _hubClient.OnRoomStarted(() =>
            {
                StatusMessage = "✅ Session has started!";
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var parameters = new NavigationParameters
                    {
                        { "IsHost", false },
                        { "IsParticipant", true },
                        { "SessionCode", SessionCode }
                    };

                    _regionManager.RequestNavigate("ContentRegion", "WhiteBoardView", parameters);
                });
            });

            _roomStartedHandlerRegistered = true;
        }
    }
}
