using SketchRoom.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IDropService
    {
        FrameworkElement? HandleDrop(BPMNShapeModel shape, Point dropPos);
        void RegisterNodeWhenReady(FrameworkElement element);
        void SetupConnectorButton(FrameworkElement element);
        void PlaceElementOnCanvas(FrameworkElement element, Point position);

        void MoveOverlayImageToWhiteBoard(FrameworkElement element, Point absolutePosition);
    }
}
