using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WhiteBoardModule.Events;

namespace SketchRoom.Windows
{
    public static class LoadingOverlayHelper
    {
        private static OverlayLoadingWindow? _window;

        public static async void Show()
        {
            //if (_window != null)
            //    return;

            //_window = new OverlayLoadingWindow
            //{
            //    Owner = Application.Current.MainWindow,
            //    WindowStartupLocation = WindowStartupLocation.Manual,
            //    Left = Application.Current.MainWindow.Left,
            //    Top = Application.Current.MainWindow.Top,
            //    Width = Application.Current.MainWindow.ActualWidth,
            //    Height = Application.Current.MainWindow.ActualHeight
            //};

            //_window.Show();

            //// 🔁 Așteaptă ca spinnerul să se redibuiască complet
            //await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
            //await Task.Delay(100);
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
