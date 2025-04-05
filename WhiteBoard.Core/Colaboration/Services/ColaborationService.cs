using SketchRoom.Models.DTO;
using SketchRoom.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WhiteBoard.Core.Colaboration.Interfaces;

namespace WhiteBoard.Core.Colaboration.Services
{
    public class CollaborationService : ICollaborationService
    {
        private readonly WhiteboardHubClient _hubClient;
        private IWhiteBoardAdapter? _whiteboard;
        private bool _isHost;
        private bool _isParticipant;
        private string _sessionCode = string.Empty;
        private DateTime _lastCursorSendTime = DateTime.MinValue;
        private const int CursorSendIntervalMs = 16;

        public CollaborationService(WhiteboardHubClient hubClient)
        {
            _hubClient = hubClient;
        }

        public void Initialize(string sessionCode, bool isHost, bool isParticipant)
        {
            _sessionCode = sessionCode;
            _isHost = isHost;
            _isParticipant = isParticipant;

            if (_isParticipant)
            {
                _hubClient.OnDrawLineReceived(line =>
                {
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        _whiteboard?.StartNewRemoteLine();

                        SolidColorBrush color = Brushes.Black;
                        try
                        {
                            var brush = (Brush)new BrushConverter().ConvertFromString(line.Color);
                            if (brush is SolidColorBrush solid)
                                color = solid;
                        }
                        catch { }

                        var points = line.Points.Select(p => new Point(p.X, p.Y));
                        _whiteboard?.AddLine(points, color, line.Thickness);
                    });
                });

                _hubClient.OnLiveDrawPointReceived(point =>
                {
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        _whiteboard?.AddLivePoint(new Point(point.X, point.Y), Brushes.DarkBlue);
                    });
                });

                _hubClient.OnCursorPositionReceived(cursor =>
                {
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        BitmapImage? image = null;
                        if (!string.IsNullOrEmpty(cursor.HostImageBase64))
                        {
                            try
                            {
                                using var ms = new MemoryStream(Convert.FromBase64String(cursor.HostImageBase64));
                                image = new BitmapImage();
                                image.BeginInit();
                                image.CacheOption = BitmapCacheOption.OnLoad;
                                image.StreamSource = ms;
                                image.EndInit();
                            }
                            catch
                            {
                                Console.WriteLine("Failed to parse host cursor image.");
                            }
                        }

                        _whiteboard?.MoveCursorImage(new Point(cursor.X, cursor.Y), image);
                    });
                });
            }
        }

        public void AttachWhiteboard(IWhiteBoardAdapter whiteboard)
        {
            _whiteboard = whiteboard;
        }

        public async Task SendLineAsync(IEnumerable<Point> points, string colorHex, double thickness)
        {
            if (!_isHost || string.IsNullOrEmpty(_sessionCode)) return;

            var dto = new DrawLineDto
            {
                SessionCode = _sessionCode,
                Color = colorHex,
                Thickness = thickness,
                Points = points.Select(p => new PointDto { X = p.X, Y = p.Y }).ToList()
            };

            try
            {
                await _hubClient.SendDrawLineAsync(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending line: " + ex.Message);
            }
        }

        public async Task SendLivePointAsync(Point point)
        {
            if (!_isHost || string.IsNullOrEmpty(_sessionCode)) return;

            await _hubClient.SendLiveDrawPointAsync(new LiveDrawPointDto
            {
                SessionCode = _sessionCode,
                X = point.X,
                Y = point.Y
            });
        }

        public async Task SendCursorPositionAsync(Point position, string? imageBase64 = null)
        {
            if (!_isHost || string.IsNullOrEmpty(_sessionCode)) return;

            var now = DateTime.UtcNow;
            if ((now - _lastCursorSendTime).TotalMilliseconds < CursorSendIntervalMs)
                return;

            _lastCursorSendTime = now;

            await _hubClient.SendCursorPositionAsync(new CursorPositionDto
            {
                SessionCode = _sessionCode,
                X = position.X,
                Y = position.Y,
                HostImageBase64 = imageBase64
            });
        }
    }
}
