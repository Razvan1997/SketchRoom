using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using WhiteBoard.Core.Models;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface ISelectionService
    {
        Rect GetBoundsFromPoints(IEnumerable<Point> points);
        void HandleSelection(Rect bounds, Canvas canvas);
        void ClearSelection(Canvas canvas);
        IReadOnlyList<UIElement> SelectedElements { get; }
        event EventHandler SelectionChanged;
        void DeselectAll(Canvas canvas);
        void UpdateSelectionMarkersPosition();
        IEnumerable<BPMNConnection> GetAllConnections();
    }
}
