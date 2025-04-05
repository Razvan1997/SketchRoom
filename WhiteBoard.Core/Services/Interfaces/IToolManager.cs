using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IToolManager
    {
        IDrawingTool? ActiveTool { get; }

        IEnumerable<IDrawingTool> Tools { get; }

        void RegisterTool(IDrawingTool tool);

        void SetActive(string toolName);

        IDrawingTool? GetToolByName(string name);
    }
}
