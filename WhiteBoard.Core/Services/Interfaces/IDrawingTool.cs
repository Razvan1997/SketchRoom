using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WhiteBoard.Core.Models;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IDrawingTool
    {
        string Name { get; }
        void OnMouseDown(Point position);
        void OnMouseMove(Point position);
        void OnMouseUp(Point position);
    }
}
