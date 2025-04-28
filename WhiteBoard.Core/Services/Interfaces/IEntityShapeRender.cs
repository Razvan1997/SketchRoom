using System.Windows;
using System.Windows.Media;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IShapeEntityRenderer
    {
        void AddRow();
        void RemoveRowAt(UIElement targetRow);
        UIElement? LastRightClickedRow { get; }

        void ChangeHeaderBackground(Brush newBackground);
    }
}
