using SketchRoom.Database;
using SketchRoom.Models;
using SketchRoom.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LobbyHostingModule.ViewModels
{
    public class LobbyViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly WhiteboardHubClient _hubClient;
        private bool _isStartLobbyEnabled = true;
        private bool _isStartSessionEnabled = true;
        private string _sessionCode;
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

        public LobbyViewModel(IRegionManager regionManager,  WhiteboardHubClient hubClient)
        {
            _regionManager = regionManager;
            _hubClient = hubClient;

            StartLobbyCommand = new DelegateCommand(OnStartLobby, CanStartLobby)
                                .ObservesProperty(() => IsStartLobbyEnabled);

            StartSessionCommand = new DelegateCommand(OnStartSession, CanStartSession)
                                .ObservesProperty(() => IsStartSessionEnabled);
        }

        private bool CanStartSession()
        {
            return IsStartSessionEnabled;
        }

        private async void OnStartSession()
        {
            try
            {
                var result = await _hubClient.StartRoomAsync(SessionCode);

                if (result.Success)
                {
                    //foreach (var participant in result.Participants)
                    //{
                    //    ConnectedParticipants.Add(participant);
                    //}

                    //StatusMessage = $"✅ Room started with {result.Participants.Count} participant(s)";


                    var parameters = new NavigationParameters
                    {
                        { "IsHost", true },
                        { "IsParticipant", false },
                        { "SessionCode", SessionCode }
                    };

                    _regionManager.RequestNavigate("ContentRegion", "WhiteBoardView", parameters);
                }
                else
                {
                    //StatusMessage = "❌ Failed to start room";
                }
            }
            catch (Exception ex)
            {
                //StatusMessage = $"❌ Error: {ex.Message}";
            }
        }

        private async void OnStartLobby()
        {
            IsStartLobbyEnabled = false;

            try
            {
                var user = SecureStorage.LoadUser();

                if(user != null)
                {
                    await _hubClient.ConnectAsync();
                    SessionCode = await _hubClient.CreateSessionAsync(user.ImageBase64);

                    _hubClient.OnClientJoined(participant =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ConnectedParticipants.Add(participant);
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                SessionCode = $"❌ Error: {ex.Message}";
            }

            IsStartLobbyEnabled = true;
        }

        private bool CanStartLobby()
        {
            return IsStartLobbyEnabled;
        }
    }
}
