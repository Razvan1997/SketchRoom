using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IShapeAddedXaml
    {
        IShapeRenderer? Renderer { get; }
        ITableShapeRender? TableShape => Renderer is IShapeTableProvider p ? p.TableShape : null;
        ShapeType GetShapeType();
    }
}
