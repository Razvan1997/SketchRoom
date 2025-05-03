using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IClipboardService
    {
        void Copy(IInteractiveShape shape);
        IInteractiveShape? Paste(Point position);
    }
}
