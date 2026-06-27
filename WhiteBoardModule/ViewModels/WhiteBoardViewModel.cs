using System;
using System.Windows;
using SketchRoom.Toolkit.Wpf.Controls;
using WhiteBoard.Core.Collaboration;
using WhiteBoard.Core.Helpers;
using WhiteBoard.Core.Models;
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

        private WhiteBoardControl? _attachedControl;
        private Action<Point>? _cursorHandler;
        private Action<FrameworkElement>? _addHandler;
        private Action<FrameworkElement>? _moveHandler;
        private Action<Guid>? _removeHandler;
        private Action<BPMNConnection>? _connectorHandler;

        // Wires the active tab's canvas into the collaboration session: builds the
        // remote applier, routes local cursor moves and local element edits to the hub.
        // Detaches the previously attached control first so handlers never accumulate.
        public void AttachWhiteboard(WhiteBoardControl control, IDrawingService drawingService)
        {
            DetachWhiteboard();

            var shapeFactory = ContainerLocator.Container.Resolve<IGenericShapeFactory>();
            var applier = new WhiteBoardCanvasApplier(control, drawingService, shapeFactory);
            _session.AttachCanvas(applier, drawingService);

            _cursorHandler    = p  => _session.ReportLocalCursor(p.X, p.Y);
            _addHandler       = fe => BroadcastShape(control, fe, isUpdate: false);
            _moveHandler      = fe => BroadcastShape(control, fe, isUpdate: true);
            _removeHandler    = id => _session.SendElementRemoved(id);
            _connectorHandler = c  => BroadcastConnector(c);

            control.MouseMoved        += _cursorHandler;
            control.ElementAddedLocal += _addHandler;
            control.ElementMovedLocal += _moveHandler;
            control.ElementRemovedLocal += _removeHandler;
            control.ConnectorAddedLocal += _connectorHandler;

            _attachedControl = control;
        }

        private void DetachWhiteboard()
        {
            if (_attachedControl == null) return;

            if (_cursorHandler != null)    _attachedControl.MouseMoved        -= _cursorHandler;
            if (_addHandler != null)       _attachedControl.ElementAddedLocal -= _addHandler;
            if (_moveHandler != null)      _attachedControl.ElementMovedLocal -= _moveHandler;
            if (_removeHandler != null)    _attachedControl.ElementRemovedLocal -= _removeHandler;
            if (_connectorHandler != null) _attachedControl.ConnectorAddedLocal -= _connectorHandler;

            _attachedControl = null;
        }

        private void BroadcastShape(WhiteBoardControl control, FrameworkElement fe, bool isUpdate)
        {
            if (!_session.IsActive) return;
            if (!control._dropService.TryGetShapeWrapper(fe, out var wrapper) || wrapper == null) return;

            BPMNShapeModelWithPosition model;
            try { model = wrapper.ExportData(); } catch { return; }

            var dto = ElementMapper.TryToDto(model);
            if (dto == null) return;

            if (Guid.TryParse(ShapeMetadata.GetShapeId(fe), out var stableId))
                dto.Id = stableId;

            if (isUpdate) _session.SendElementUpdated(dto);
            else _session.SendElementAdded(dto);
        }

        private void BroadcastConnector(BPMNConnection conn)
        {
            if (!_session.IsActive) return;

            var model = conn.Export();
            if (model == null) return;

            var id = Guid.TryParse(model.ShapeId, out var g) ? g : Guid.NewGuid();
            _session.SendElementAdded(ElementMapper.ToDto(model, id));
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
