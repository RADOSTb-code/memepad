using System.Runtime.InteropServices;
using MemePad.Core.Input;
using static MemePad.App.Input.NativeMethods;

namespace MemePad.App.Input;

/// <summary>
/// System-wide keyboard and mouse (X1/X2 buttons) hook on its own message-loop thread, so hotkeys work while a
/// game has focus. <see cref="Input"/> runs inside the hook: it must return quickly (Windows drops slow hooks) and
/// its result decides whether the event is swallowed. Events injected by <see cref="KeySender"/> are skipped.
/// </summary>
public sealed class LowLevelInput : IDisposable
{
    private readonly HookProc _keyboardProc;
    private readonly HookProc _mouseProc;
    private readonly ManualResetEventSlim _ready = new();
    private Thread? _thread;
    private uint _threadId;
    private nint _keyboardHook;
    private nint _mouseHook;
    private Exception? _startError;

    public LowLevelInput()
    {
        // Keep the delegates in fields: the native hook holds only a raw pointer to them.
        _keyboardProc = KeyboardProc;
        _mouseProc = MouseProc;
    }

    /// <summary>Return true to swallow the event so the focused app does not see it.</summary>
    public event Func<InputEvent, bool>? Input;

    public void Start()
    {
        if (_thread is not null) return;
        _thread = new Thread(Run) { IsBackground = true, Name = "MemePad input hook", Priority = ThreadPriority.AboveNormal };
        _thread.Start();
        _ready.Wait();
        if (_startError is not null) throw _startError;
    }

    private void Run()
    {
        _threadId = GetCurrentThreadId();
        var module = GetModuleHandleW(null);
        _keyboardHook = SetWindowsHookExW(WH_KEYBOARD_LL, _keyboardProc, module, 0);
        _mouseHook = SetWindowsHookExW(WH_MOUSE_LL, _mouseProc, module, 0);
        if (_keyboardHook == 0 || _mouseHook == 0)
            _startError = new InvalidOperationException($"SetWindowsHookEx failed ({Marshal.GetLastWin32Error()}).");
        _ready.Set();
        if (_startError is not null) return;

        while (GetMessageW(out _, 0, 0, 0) > 0) { }

        UnhookWindowsHookEx(_keyboardHook);
        UnhookWindowsHookEx(_mouseHook);
    }

    private nint KeyboardProc(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var msg = (int)wParam;
            var isDown = msg is WM_KEYDOWN or WM_SYSKEYDOWN;
            var isUp = msg is WM_KEYUP or WM_SYSKEYUP;
            if ((isDown || isUp) && data.dwExtraInfo != KeySender.Signature && Dispatch(new InputEvent((int)data.vkCode, isDown)))
                return 1;
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private nint MouseProc(int nCode, nint wParam, nint lParam)
    {
        var msg = (int)wParam;
        if (nCode >= 0 && msg is WM_XBUTTONDOWN or WM_XBUTTONUP)
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var button = (data.mouseData >> 16) & 0xFFFF;
            var vk = button == 1 ? 0x05 : button == 2 ? 0x06 : 0;
            if (vk != 0 && data.dwExtraInfo != KeySender.Signature && Dispatch(new InputEvent(vk, msg == WM_XBUTTONDOWN)))
                return 1;
        }
        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private bool Dispatch(InputEvent e)
    {
        try
        {
            return Input?.Invoke(e) ?? false;
        }
        catch (Exception)
        {
            return false;   // never let a handler bug break system input
        }
    }

    public void Dispose()
    {
        if (_thread is null) return;
        PostThreadMessageW(_threadId, WM_QUIT, 0, 0);
        _thread.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        _ready.Dispose();
    }
}
