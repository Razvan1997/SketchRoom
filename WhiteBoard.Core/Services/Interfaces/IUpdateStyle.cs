using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IUpdateStyle
    {
        void UpdateStyle(FontWeight fontWeight, double fontSize, Brush foreground);
    }
}
