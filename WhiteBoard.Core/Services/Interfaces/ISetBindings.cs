using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface ISetBindings
    {
        void SetShapeWithBindings(ShapeType shape);
        void SetShape(ShapeType shape);
    }
}
