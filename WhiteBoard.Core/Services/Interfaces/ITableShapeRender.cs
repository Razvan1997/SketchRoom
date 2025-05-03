using System.Windows;
using System.Windows.Media;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface ITableShapeRender
    {
        void AddRowAbove(int currentRow);
        void AddRowBelow(int currentRow);
        void AddColumnLeft(int currentColumn);
        void AddColumnRight(int currentColumn);
        void DeleteRow(int currentRow);
        void DeleteColumn(int currentColumn);
        void ChangeHeaderBackground(Brush color); // pentru viitor, schimbare culoare header
        int? GetLastRowClicked();
        int? GetLastColumnClicked();
        void ChangeBorderColor(Brush color);
        void AddOverlayElement(UIElement element, Point relativePosition);
    }
}
