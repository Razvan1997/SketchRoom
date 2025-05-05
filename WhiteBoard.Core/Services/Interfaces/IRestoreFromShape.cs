using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IRestoreFromShape
    {
        void Restore( Dictionary<string, string> extraProperties);
    }
}
