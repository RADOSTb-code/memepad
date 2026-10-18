using System.Runtime.InteropServices;
using static MemePad.App.Input.NativeMethods;

namespace MemePad.App.Input;

/// <summary>
/// Synthesizes key or mouse-button presses (used to hold the Discord push-to-talk key). Each event carries
/// <see cref="Signature"/> so our own hook can recognize and ignore it.
/// </summary>
public static class KeySender
{
    /// <summary>Marker in dwExtraInfo identifying MemePad's own injected input.</summary>
    public const nuint Signature = 0x4D4D5044;

    public static void Down(int vk) => Send(vk, down: true, Signature);

    public static void Up(int vk) => Send(vk, down: false, Signature);

    /// <summary>Sends without the marker, as another application would. Used by tests.</summary>
    internal static void SendUnsigned(int vk, bool down) => Send(vk, down, 0);

    private static void Send(int vk, bool down, nuint extra)
    {
        var input = vk is 0x05 or 0x06 ? Mouse(vk, down, extra) : Keyboard(vk, down, extra);
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    private static INPUT Keyboard(int vk, bool down, nuint extra)
    {
        // Scan code plus VK: some apps (Discord among them) read keys by scan code.
        var scan = (ushort)MapVirtualKeyW((uint)vk, 0);
        var flags = (down ? 0u : KEYEVENTF_KEYUP) | (IsExtended(vk) ? KEYEVENTF_EXTENDEDKEY : 0u);
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion { ki = new KEYBDINPUT { wVk = (ushort)vk, wScan = scan, dwFlags = flags, dwExtraInfo = extra } },
        };
    }

    private static INPUT Mouse(int vk, bool down, nuint extra) => new()
    {
        type = INPUT_MOUSE,
        u = new InputUnion
        {
            mi = new MOUSEINPUT
            {
                mouseData = vk == 0x05 ? 1u : 2u,
                dwFlags = down ? MOUSEEVENTF_XDOWN : MOUSEEVENTF_XUP,
                dwExtraInfo = extra,
            },
        },
    };

    private static bool IsExtended(int vk) => vk is
        0x21 or 0x22 or 0x23 or 0x24 or 0x25 or 0x26 or 0x27 or 0x28   // PgUp PgDn End Home arrows
        or 0x2D or 0x2E or 0x6F                                         // Insert Delete Num/
        or 0xA3 or 0xA5 or 0x5B or 0x5C or 0x5D;                        // RCtrl RAlt Win Apps
}
