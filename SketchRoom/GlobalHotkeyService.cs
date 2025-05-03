using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows;

namespace SketchRoom
{
    public class GlobalHotkeyService : IDisposable
    {
        private readonly Window _window;
        private readonly int _hotkeyId = 9000; // orice ID unic
        private HwndSource _source;

        public event Action? HotkeyPressed;

        public GlobalHotkeyService(Window window)
        {
            _window = window;
        }

        public void RegisterHotkey()
        {
            var helper = new WindowInteropHelper(_window);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);

            // MOD_SHIFT = 0x0004, G = 0x47
            RegisterHotKey(helper.Handle, _hotkeyId, 0x0004, 0x47);
        }

        public void UnregisterHotkey()
        {
            var helper = new WindowInteropHelper(_window);
            UnregisterHotKey(helper.Handle, _hotkeyId);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
            {
                HotkeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterHotkey();
            _source?.RemoveHook(HwndHook);
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
