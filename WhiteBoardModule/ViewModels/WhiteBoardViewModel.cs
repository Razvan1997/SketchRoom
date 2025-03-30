using SketchRoom.Models.DTO;
using SketchRoom.Services;
using SketchRoom.Toolkit.Wpf.Controls;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WhiteBoardModule.Events;

namespace WhiteBoardModule.ViewModels
{
    public class WhiteBoardViewModel : BindableBase, INavigationAware
    {
        private const int CursorSendIntervalMs = 16;
        private DateTime _lastCursorSendTime = DateTime.MinValue;

        private readonly IEventAggregator _eventAggregator;
        private readonly WhiteboardHubClient _hubClient;
        private WhiteBoardControl? _whiteboardAdapter;

        public bool IsHost { get; private set; }
        public bool IsParticipant { get; private set; }
        public string SessionCode { get; private set; } = string.Empty;

        public WhiteBoardViewModel(WhiteboardHubClient hubClient, IEventAggregator eventAggregator)
        {
            _hubClient = hubClient;
            _eventAggregator = eventAggregator;
        }

        public void SetControlAdapter(WhiteBoardControl adapter)
        {
            _whiteboardAdapter = adapter;
        }

        public async void OnLineDrawn(List<Point> points)
        {
            if (!IsHost || string.IsNullOrEmpty(SessionCode)) return;

            var drawingService = ContainerLocator.Container.Resolve<DrawingStateService.DrawingStateService>();
            string colorString = (drawingService.SelectedColor as SolidColorBrush)?.Color.ToString() ?? "#000000";

            var dto = new DrawLineDto
            {
                SessionCode = SessionCode,
                Color = colorString,
                Thickness = 2,
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

        public void OnDrawPointLive(Point point)
        {
            if (!IsHost || string.IsNullOrEmpty(SessionCode)) return;

            _ = _hubClient.SendLiveDrawPointAsync(new LiveDrawPointDto
            {
                SessionCode = SessionCode,
                X = point.X,
                Y = point.Y
            });
        }

        public void OnMouseMoved(Point pos)
        {
            if (!IsHost || string.IsNullOrEmpty(SessionCode)) return;

            var now = DateTime.UtcNow;
            if ((now - _lastCursorSendTime).TotalMilliseconds >= CursorSendIntervalMs)
            {
                _lastCursorSendTime = now;

                _ = _hubClient.SendCursorPositionAsync(new CursorPositionDto
                {
                    SessionCode = SessionCode,
                    X = pos.X,
                    Y = pos.Y
                });
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            IsHost = navigationContext.Parameters.GetValue<bool>("IsHost");
            IsParticipant = navigationContext.Parameters.GetValue<bool>("IsParticipant");
            SessionCode = navigationContext.Parameters.GetValue<string>("SessionCode");

            if (IsParticipant)
            {
                _hubClient.OnDrawLineReceived(line =>
                {
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        _whiteboardAdapter?.StartNewRemoteLine(); 

                        SolidColorBrush color = Brushes.Black;
                        try
                        {
                            var brush = (Brush)new BrushConverter().ConvertFromString(line.Color);
                            if (brush is SolidColorBrush solid)
                                color = solid;
                        }
                        catch { }

                        var points = line.Points.Select(p => new Point(p.X, p.Y));
                        _whiteboardAdapter?.AddLine(points, color, line.Thickness);
                    });
                });

                _hubClient.OnLiveDrawPointReceived(point =>
                {
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        _whiteboardAdapter?.AddLivePoint(new Point(point.X, point.Y), Brushes.DarkBlue);
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

                        _whiteboardAdapter?.MoveCursorImage(new Point(cursor.X, cursor.Y), image);
                    });
                });
            }

            _eventAggregator.GetEvent<SessionContextEvent>().Publish(new SessionContext
            {
                IsHost = IsHost,
                SessionCode = SessionCode
            });
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }

}
