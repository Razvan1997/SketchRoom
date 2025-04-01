
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;

namespace DrawingStateService
{
    public class DrawingStateService 
    {
        public Brush SelectedColor { get; set; }
        public bool IsSelectionModeEnabled { get; set; }
        public bool IsDraggingText { get; set; } = false;
    }
}
