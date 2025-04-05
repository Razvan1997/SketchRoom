using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Tools
{
    public class ToolManager : IToolManager
    {
        private readonly Dictionary<string, IDrawingTool> _tools = new();

        public IDrawingTool? ActiveTool { get; private set; }

        public IEnumerable<IDrawingTool> Tools => _tools.Values;

        public void RegisterTool(IDrawingTool tool)
        {
            if (!_tools.ContainsKey(tool.Name))
                _tools[tool.Name] = tool;
        }

        public void SetActive(string toolName)
        {
            if (_tools.TryGetValue(toolName, out var tool))
                ActiveTool = tool;
        }

        public IDrawingTool? GetToolByName(string name)
        {
            _tools.TryGetValue(name, out var tool);
            return tool;
        }
    }
}
