using SketchRoom.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WhiteBoard.Core.Models;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IDropService
    {
        // Raised after a shape/text/image element is placed on the canvas (local or restore).
        event Action<FrameworkElement>? ElementPlaced;

        // Raised after a locally-created element's content changes (e.g. text edited).
        event Action<FrameworkElement>? ElementEdited;

        Dictionary<FrameworkElement, BPMNNode> _nodeMap { get; }
        FrameworkElement? HandleDrop(BPMNShapeModel shape, Point dropPos);
        void RegisterNodeWhenReady(FrameworkElement element);
        void RegisterNodeWhenReadyRestore(FrameworkElement element, string id, Dictionary<string, BPMNNode> nodeMap);
        void SetupConnectorButton(FrameworkElement element);
        void PlaceElementOnCanvas(FrameworkElement element, Point position);

        void MoveOverlayImageToWhiteBoard(FrameworkElement element, Point absolutePosition);
        bool TryGetShapeWrapper(FrameworkElement element, out BpmnWhiteBoardElementXaml? wrapper);
        FrameworkElement? HandleDropSavedElements(BPMNShapeModelWithPosition shape, Point dropPos, IInteractiveShape interactiveShape);
        void TryRegisterNodeImmediate(FrameworkElement element);
    }
}
