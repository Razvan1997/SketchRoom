using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteBoard.Core.Models;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface ISerializableShape
    {
        BPMNShapeModelWithPosition ExportData();
    }
}
