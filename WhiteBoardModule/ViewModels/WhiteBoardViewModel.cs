using SketchRoom.Toolkit.Wpf.Controls;
using WhiteBoard.Core.Collaboration;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoardModule.Collaboration;
using WhiteBoardModule.Events;

namespace WhiteBoardModule.ViewModels
{
    public class WhiteBoardViewModel : BindableBase, INavigationAware
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly CollaborationSession _session;

        public bool IsHost { get; private set; }
        public bool IsParticipant { get; private set; }
        public string SessionCode { get; private set; } = string.Empty;

        public WhiteBoardViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _session = ContainerLocator.Container.Resolve<CollaborationSession>();
        }

        // Wires the active tab's canvas into the collaboration session: builds the
        // remote applier and routes local cursor moves to the hub.
        public void AttachWhiteboard(WhiteBoardControl control, IDrawingService drawingService)
        {
            var shapeFactory = ContainerLocator.Container.Resolve<IGenericShapeFactory>();
            var applier = new WhiteBoardCanvasApplier(control, drawingService, shapeFactory);
            _session.AttachCanvas(applier, drawingService);

            control.MouseMoved += p => _session.ReportLocalCursor(p.X, p.Y);
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            IsHost = navigationContext.Parameters.GetValue<bool>("IsHost");
            IsParticipant = navigationContext.Parameters.GetValue<bool>("IsParticipant");
            SessionCode = navigationContext.Parameters.GetValue<string>("SessionCode");

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
