using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IRotationTool
    {
        void StartRotation(IInteractiveShape shape, Point startMousePosition);
    }
}
