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
            if (_window != null)
                return;

            _window = new OverlayLoadingWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = true,
                ShowInTaskbar = false,
                Topmost = true
            };

            // Așează fereastra pe întreg ecranul principal
            _window.Left = 0;
            _window.Top = 0;
            _window.Width = SystemParameters.PrimaryScreenWidth;
            _window.Height = SystemParameters.PrimaryScreenHeight;

            _window.Show();

            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
            await Task.Delay(100);
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
