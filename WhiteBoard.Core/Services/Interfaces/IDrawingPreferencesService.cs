using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IDrawingPreferencesService : INotifyPropertyChanged
    {
        Brush SelectedColor { get; set; }
        double FontSize { get; set; }
        FontWeight FontWeight { get; set; }
        bool IsApplyBackgroundColor { get; set; }
        bool IsApplyZIndexOrder { get; set; }
    }
}
