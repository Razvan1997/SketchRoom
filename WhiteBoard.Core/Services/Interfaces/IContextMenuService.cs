using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IContextMenuService
    {
        ContextMenu CreateContextMenu(ShapeContextType contextType, object owner);
    }
}
