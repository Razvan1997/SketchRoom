using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IShapeRenderer
    {
        UIElement Render();

        UIElement CreatePreview();
        void SetInitialSize(double width, double height)
        {
            // implementare implicită: nu face nimic
        }
    }
}
