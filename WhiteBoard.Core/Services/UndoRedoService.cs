using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Services
{
    public class UndoRedoService
    {
        private readonly Dictionary<Guid, Stack<IUndoableCommand>> _undoStacks = new();
        private readonly Dictionary<Guid, Stack<IUndoableCommand>> _redoStacks = new();

        private readonly IWhiteBoardTabService _tabService;

        public UndoRedoService(IWhiteBoardTabService tabService)
        {
            _tabService = tabService;
        }

        private Guid? CurrentTabId => _tabService.CurrentTab?.Id;

        public bool CanUndo => CurrentTabId != null && _undoStacks.TryGetValue(CurrentTabId.Value, out var stack) && stack.Any();
        public bool CanRedo => CurrentTabId != null && _redoStacks.TryGetValue(CurrentTabId.Value, out var stack) && stack.Any();

        public void ExecuteCommand(IUndoableCommand command)
        {
            var tabId = CurrentTabId;
            if (tabId == null) return;

            if (!_undoStacks.ContainsKey(tabId.Value))
                _undoStacks[tabId.Value] = new Stack<IUndoableCommand>();

            if (!_redoStacks.ContainsKey(tabId.Value))
                _redoStacks[tabId.Value] = new Stack<IUndoableCommand>();

            command.Execute();
            _undoStacks[tabId.Value].Push(command);
            _redoStacks[tabId.Value].Clear();
        }

        public void Undo()
        {
            var tabId = CurrentTabId;
            if (tabId == null || !_undoStacks.TryGetValue(tabId.Value, out var undoStack) || undoStack.Count == 0)
                return;

            var command = undoStack.Pop();
            command.Undo();
            _redoStacks[tabId.Value].Push(command);
        }

        public void Redo()
        {
            var tabId = CurrentTabId;
            if (tabId == null || !_redoStacks.TryGetValue(tabId.Value, out var redoStack) || redoStack.Count == 0)
                return;

            var command = redoStack.Pop();
            command.Execute();
            _undoStacks[tabId.Value].Push(command);
        }
    }
}
