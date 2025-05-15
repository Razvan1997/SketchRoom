using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SketchRoom.Windows
{
    public static class LoadingOverlayHelper
    {
        private static OverlayLoadingWindow? _window;

        public static void Show()
        {
            if (_window == null)
            {
                _window = new OverlayLoadingWindow
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = Application.Current.MainWindow.Left,
                    Top = Application.Current.MainWindow.Top,
                    Width = Application.Current.MainWindow.ActualWidth,
                    Height = Application.Current.MainWindow.ActualHeight
                };

                _window.Show();
            }
        }

        public static void Hide()
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }
        }
    }
}
