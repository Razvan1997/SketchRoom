using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Tools
{
    public class ToolStateMachine
    {
        private readonly Dictionary<string, IToolBehavior> _behaviors = new();
        private IToolBehavior? _activeBehavior;

        public void RegisterBehavior(string toolName, IToolBehavior behavior)
        {
            _behaviors[toolName] = behavior;
        }

        public void SetActive(string toolName)
        {
            _behaviors.TryGetValue(toolName, out _activeBehavior);
        }

        public void HandleMouseDown(Point pos, MouseButtonEventArgs e)
            => _activeBehavior?.OnMouseDown(pos, e);

        public void HandleMouseMove(Point pos, MouseEventArgs e)
            => _activeBehavior?.OnMouseMove(pos, e);

        public void HandleMouseUp(Point pos, MouseButtonEventArgs e)
            => _activeBehavior?.OnMouseUp(pos, e);
    }
}
