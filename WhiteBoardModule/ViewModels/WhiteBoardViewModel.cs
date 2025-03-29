using SketchRoom.Models;
using SketchRoom.Models.DTO;
using SketchRoom.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WhiteBoardModule.Events;

namespace WhiteBoardModule.ViewModels
{
    public class WhiteBoardViewModel : BindableBase, INavigationAware
    {
        private static readonly Rect A1Bounds = new Rect(0, 0, 2244, 3185);
        private DateTime _lastCursorSendTime = DateTime.MinValue;
        private const int CursorSendIntervalMs = 16; // ~60 FPS
        private readonly IEventAggregator _eventAggregator;
        private readonly WhiteboardHubClient _hubClient;
        private bool _isDrawing = false;
        private Polyline _currentLine;
        private Polyline _remoteLine;

        public bool IsHost { get; private set; }
        public bool IsParticipant { get; private set; }
        public string SessionCode { get; private set; }

        public ObservableCollection<UIElement> DrawingElements { get; } = new();

        public WhiteBoardViewModel(WhiteboardHubClient hubClient, IEventAggregator eventAggregator)
        {
            _hubClient = hubClient;
            _eventAggregator = eventAggregator;
        }

        public void CanvasMouseDown(Point logicalPos)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control) return;

            _isDrawing = true;
            _currentLine = new Polyline { Stroke = Brushes.Black, StrokeThickness = 2 };
            _currentLine.Points.Add(logicalPos);
            DrawingElements.Add(_currentLine);
        }

        public void CanvasMouseMove(Point logicalPos)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control) return;

            if (_isDrawing && _currentLine != null)
            {
                _currentLine.Points.Add(logicalPos);
                if (IsHost)
                {
                    _ = _hubClient.SendLiveDrawPointAsync(new LiveDrawPointDto
                    {
                        SessionCode = SessionCode,
                        X = logicalPos.X,
                        Y = logicalPos.Y
                    });
                }
            }

            if (IsHost)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastCursorSendTime).TotalMilliseconds >= CursorSendIntervalMs)
                {
                    _lastCursorSendTime = now;
                    _ = _hubClient.SendCursorPositionAsync(new CursorPositionDto
                    {
                        SessionCode = SessionCode,
                        X = logicalPos.X,
                        Y = logicalPos.Y
                    });
                }
            }
        }

        public async void CanvasMouseUp(Point logicalPos)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control) return;
            _isDrawing = false;

            if (_currentLine != null)
            {
                _currentLine.Points.Add(logicalPos);
                if (IsHost)
                {
                    await _hubClient.SendDrawLineAsync(new DrawLineDto
                    {
                        SessionCode = SessionCode,
                        Color = "Black",
                        Thickness = 2,
                        Points = _currentLine.Points.Select(p => new PointDto { X = p.X, Y = p.Y }).ToList()
                    });
                }
                _currentLine = null;
            }
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("IsHost"))
                IsHost = navigationContext.Parameters.GetValue<bool>("IsHost");

            if (navigationContext.Parameters.ContainsKey("IsParticipant"))
                IsParticipant = navigationContext.Parameters.GetValue<bool>("IsParticipant");

            if (navigationContext.Parameters.ContainsKey("SessionCode"))
                SessionCode = navigationContext.Parameters.GetValue<string>("SessionCode");

            if (IsParticipant)
            {
                _hubClient.OnDrawLineReceived(line =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var polyline = new Polyline
                        {
                            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(line.Color)),
                            StrokeThickness = line.Thickness
                        };

                        foreach (var pointDto in line.Points)
                        {
                            polyline.Points.Add(new Point(pointDto.X, pointDto.Y));
                        }

                        DrawingElements.Add(polyline);
                        _remoteLine = null; // finalizează linia live
                    });
                });

                _hubClient.OnLiveDrawPointReceived(point =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (_remoteLine == null)
                        {
                            _remoteLine = new Polyline
                            {
                                Stroke = Brushes.DarkBlue,
                                StrokeThickness = 2
                            };

                            DrawingElements.Add(_remoteLine);
                        }

                        _remoteLine.Points.Add(new Point(point.X, point.Y));
                    });
                });

                _hubClient.OnCursorPositionReceived(cursor =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existingImage = DrawingElements
                            .OfType<Image>()
                            .FirstOrDefault(el => el.Tag?.ToString() == "HostCursor");

                        if (existingImage == null)
                        {
                            existingImage = new Image
                            {
                                Width = 20,
                                Height = 20,
                                Tag = "HostCursor"
                            };

                            DrawingElements.Add(existingImage);
                        }

                        if (!string.IsNullOrEmpty(cursor.HostImageBase64))
                        {
                            var bitmap = new BitmapImage();
                            using var ms = new MemoryStream(Convert.FromBase64String(cursor.HostImageBase64));
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.EndInit();

                            existingImage.Source = bitmap;
                        }

                        Canvas.SetLeft(existingImage, cursor.X - 20);
                        Canvas.SetTop(existingImage, cursor.Y - 20);
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

        public void CanvasMouseEnter(object sender, MouseEventArgs e) { }
    }

}
