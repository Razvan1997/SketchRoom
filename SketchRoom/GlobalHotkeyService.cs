using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Input;

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

        public void RegisterHotkey(string modifierKey, string mainKey)
        {
            var helper = new WindowInteropHelper(_window);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);

            var modifier = GetModifierValue(modifierKey);
            var vk = GetVirtualKey(mainKey);

            RegisterHotKey(helper.Handle, _hotkeyId, modifier, vk);
        }

        private uint GetModifierValue(string key)
        {
            return key.ToUpperInvariant() switch
            {
                "CTRL" => 0x0002,
                "SHIFT" => 0x0004,
                "ALT" => 0x0001,
                _ => 0x0000
            };
        }

        private uint GetVirtualKey(string key)
        {
            if (Enum.TryParse<Key>(key.ToUpperInvariant(), out var parsedKey))
            {
                return (uint)KeyInterop.VirtualKeyFromKey(parsedKey);
            }

            // fallback: F12 dacă e invalid
            return (uint)KeyInterop.VirtualKeyFromKey(Key.F12);
        }

        private void ParseHotkeys(string k1, string k2, out uint modifiers, out uint virtualKey)
        {
            modifiers = 0;
            virtualKey = 0;

            string[] keys = new[] { k1.Trim().ToUpperInvariant(), k2.Trim().ToUpperInvariant() };

            foreach (var key in keys)
            {
                switch (key)
                {
                    case "CTRL": modifiers |= 0x0002; break;
                    case "SHIFT": modifiers |= 0x0004; break;
                    case "ALT": modifiers |= 0x0001; break;
                    default:
                        if (virtualKey == 0 && Enum.TryParse<Key>(key, out var parsedKey))
                        {
                            virtualKey = (uint)KeyInterop.VirtualKeyFromKey(parsedKey);
                        }
                        break;
                }
            }

            // fallback: dacă nu am nicio tastă validă, folosim F12
            if (virtualKey == 0)
            {
                virtualKey = (uint)KeyInterop.VirtualKeyFromKey(Key.F12);
            }
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
