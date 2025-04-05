using SketchRoom.Models.DTO;
using SketchRoom.Services;
using SketchRoom.Toolkit.Wpf.Controls;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WhiteBoard.Core.Colaboration.Interfaces;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoardModule.Events;

namespace WhiteBoardModule.ViewModels
{
    public class WhiteBoardViewModel : BindableBase, INavigationAware
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ICollaborationService _collaborationService;
        private IWhiteBoardAdapter? _whiteboardAdapter;

        public bool IsHost { get; private set; }
        public bool IsParticipant { get; private set; }
        public string SessionCode { get; private set; } = string.Empty;

        public WhiteBoardViewModel(IEventAggregator eventAggregator)
        {
            _collaborationService = ContainerLocator.Container.Resolve<ICollaborationService>();
            _eventAggregator = eventAggregator;
        }

        public void SetControlAdapter(IWhiteBoardAdapter adapter)
        {
            _whiteboardAdapter = adapter;
            _collaborationService.AttachWhiteboard(adapter);
        }

        public async void OnLineDrawn(List<Point> points)
        {
            if (!IsHost || string.IsNullOrEmpty(SessionCode)) return;

            var drawingService = ContainerLocator.Container.Resolve<DrawingStateService.DrawingStateService>();
            string colorString = (drawingService.SelectedColor as SolidColorBrush)?.Color.ToString() ?? "#000000";

            await _collaborationService.SendLineAsync(points, colorString, 2);
        }

        public void OnDrawPointLive(Point point)
        {
            if (!IsHost || string.IsNullOrEmpty(SessionCode)) return;
            _ = _collaborationService.SendLivePointAsync(point);
        }

        public void OnMouseMoved(Point pos)
        {
            if (!IsHost || string.IsNullOrEmpty(SessionCode)) return;
            _ = _collaborationService.SendCursorPositionAsync(pos);
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            IsHost = navigationContext.Parameters.GetValue<bool>("IsHost");
            IsParticipant = navigationContext.Parameters.GetValue<bool>("IsParticipant");
            SessionCode = navigationContext.Parameters.GetValue<string>("SessionCode");

            _collaborationService.Initialize(SessionCode, IsHost, IsParticipant);

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
