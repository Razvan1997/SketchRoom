using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhiteBoard.Core.Events
{
    public class ShapeActionEventArgs : EventArgs
    {
        public ShapeActionType ActionType { get; }
        public object? Parameter { get; }

        public ShapeActionEventArgs(ShapeActionType actionType, object? parameter = null)
        {
            ActionType = actionType;
            Parameter = parameter;
        }
    }

    public enum ShapeActionType
    {
        ChangeBackgroundColor,
        ChangeStrokeColor,
        ChangeBorderThickness,
        ChangeForegroundColor,
        Rotate,
        // Adaugi aici toate tipurile de acțiuni viitoare
    }
}
