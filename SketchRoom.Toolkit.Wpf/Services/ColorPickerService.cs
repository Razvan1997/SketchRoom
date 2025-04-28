using SketchRoom.Toolkit.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WhiteBoard.Core.Services.Interfaces;

namespace SketchRoom.Toolkit.Wpf.Services
{
    public class ColorPickerService : IColorPickerService
    {
        public Brush? PickColor(Brush initialColor)
        {
            var picker = new ColorPickerWindow(initialColor)
            {
                Owner = Application.Current.MainWindow
            };

            if (picker.ShowDialog() == true)
            {
                return picker.SelectedColor;
            }

            return null;
        }
    }
}
