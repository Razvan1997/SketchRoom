using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WhiteBoard.Core.Services.Interfaces;

namespace FooterModule.Factory
{
    public interface IWhiteBoardFactory
    {
        UserControl CreateNewWhiteBoard(out IToolManager toolManager);
    }
}
