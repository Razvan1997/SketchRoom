using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Events
{
    public static class ShapeSelectionEventBus
    {
        public static event Action<IUpdateStyle?>? ShapeSelected;

        public static void Publish(IUpdateStyle? shape)
        {
            ShapeSelected?.Invoke(shape);
        }

        public static void Subscribe(Action<IUpdateStyle?> callback)
        {
            ShapeSelected += callback;
        }

        public static void Unsubscribe(Action<IUpdateStyle?> callback)
        {
            ShapeSelected -= callback;
        }
    }
}
