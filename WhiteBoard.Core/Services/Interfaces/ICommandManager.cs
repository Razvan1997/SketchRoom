using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface ICommandManager
    {
        void Execute(IUndoableCommand command);
        void Undo();
        void Redo();
    }
}
