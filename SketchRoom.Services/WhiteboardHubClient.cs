using Microsoft.AspNetCore.SignalR.Client;
using SketchRoom.Models;
using SketchRoom.Models.DTO;

namespace SketchRoom.Services
{
    public class WhiteboardHubClient
    {
        private HubConnection _connection;

        public async Task ConnectAsync()
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
                return;

            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/whiteboardhub")
                .WithAutomaticReconnect()
                .Build();

            _connection.Closed += async (error) =>
            {
                Console.WriteLine("🔌 Disconnected from hub.");
                await Task.Delay(2000);
                await ConnectAsync();
            };

            await _connection.StartAsync();
            Console.WriteLine("✅ Connected to whiteboard hub.");
        }

        public async Task<string> CreateSessionAsync(string hostImageBase64)
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
                throw new InvalidOperationException("Not connected to hub.");

            return await _connection.InvokeAsync<string>("CreateSession", hostImageBase64);
        }

        public async Task<bool> JoinSessionAsync(string code, JoinSessionDto user)
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
                throw new InvalidOperationException("Not connected to hub.");

            return await _connection.InvokeAsync<bool>("JoinSession", code, user);
        }

        public async Task<StartRoomResult> StartRoomAsync(string code)
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
                throw new InvalidOperationException("Not connected to hub.");

            return await _connection.InvokeAsync<StartRoomResult>("StartRoom", code);
        }

        public async Task SendDrawLineAsync(DrawLineDto line)
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
                return;

            await _connection.InvokeAsync("SendDrawLine", line);
        }

        public async Task SendCursorPositionAsync(CursorPositionDto position)
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
                return;
            await _connection.InvokeAsync("UpdateCursorPosition", position);
        }

        public async Task SendLiveDrawPointAsync(LiveDrawPointDto point)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("StreamDrawPoint", point);
            }
        }

        public void OnClientJoined(Action<Participant> onJoined)
        {
            _connection.On<Participant>("ClientJoined", participant =>
            {
                onJoined?.Invoke(participant);
            });
        }

        public void OnRoomStarted(Action callback)
        {
            _connection.On("RoomStarted", () =>
            {
                callback?.Invoke();
            });
        }

        public void OnDrawLineReceived(Action<DrawLineDto> handler)
        {
            _connection.On<DrawLineDto>("ReceiveDrawLine", line =>
            {
                handler?.Invoke(line);
            });
        }

        public void OnCursorPositionReceived(Action<CursorPositionDto> handler)
        {
            _connection.On<CursorPositionDto>("ReceiveCursorPosition", handler);
        }

        public void OnLiveDrawPointReceived(Action<LiveDrawPointDto> handler)
        {
            _connection.On<LiveDrawPointDto>("ReceiveLiveDrawPoint", handler);
        }
    }
}
