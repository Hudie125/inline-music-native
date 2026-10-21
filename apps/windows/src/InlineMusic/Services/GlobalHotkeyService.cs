using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace InlineMusic.Services;

public interface IGlobalHotkeyService : IDisposable
{
    void Initialize(IntPtr windowHandle);
    event Action? PlayPauseTriggered;
    event Action? NextTriggered;
    event Action? PreviousTriggered;
    event Action? ToggleMiniPlayerTriggered;
}

public class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int WM_HOTKEY = 0x0312;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_NOREPEAT = 0x4000;

    private const int HOTKEY_PLAYPAUSE = 9001;
    private const int HOTKEY_NEXT = 9002;
    private const int HOTKEY_PREV = 9003;
    private const int HOTKEY_MINIPLAYER = 9004;

    private const uint VK_SPACE = 0x20;
    private const uint VK_RIGHT = 0x27;
    private const uint VK_LEFT = 0x25;
    private const uint VK_M = 0x4D;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private IntPtr _windowHandle;
    private HwndSource? _source;

    public event Action? PlayPauseTriggered;
    public event Action? NextTriggered;
    public event Action? PreviousTriggered;
    public event Action? ToggleMiniPlayerTriggered;

    public void Initialize(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(HwndHook);

        // Register default shortcuts (Ctrl + Alt + Space, Ctrl + Alt + Right/Left, Ctrl + Alt + M)
        RegisterHotKey(_windowHandle, HOTKEY_PLAYPAUSE, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_SPACE);
        RegisterHotKey(_windowHandle, HOTKEY_NEXT, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_RIGHT);
        RegisterHotKey(_windowHandle, HOTKEY_PREV, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_LEFT);
        RegisterHotKey(_windowHandle, HOTKEY_MINIPLAYER, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_M);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            switch (id)
            {
                case HOTKEY_PLAYPAUSE:
                    PlayPauseTriggered?.Invoke();
                    handled = true;
                    break;
                case HOTKEY_NEXT:
                    NextTriggered?.Invoke();
                    handled = true;
                    break;
                case HOTKEY_PREV:
                    PreviousTriggered?.Invoke();
                    handled = true;
                    break;
                case HOTKEY_MINIPLAYER:
                    ToggleMiniPlayerTriggered?.Invoke();
                    handled = true;
                    break;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(_windowHandle, HOTKEY_PLAYPAUSE);
            UnregisterHotKey(_windowHandle, HOTKEY_NEXT);
            UnregisterHotKey(_windowHandle, HOTKEY_PREV);
            UnregisterHotKey(_windowHandle, HOTKEY_MINIPLAYER);
            _source?.RemoveHook(HwndHook);
            _windowHandle = IntPtr.Zero;
        }
    }
}
