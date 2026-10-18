using System.Globalization;

namespace MemePad.Core.Input;

/// <summary>Human-readable names for Windows virtual-key codes, stable across keyboard layouts.</summary>
internal static class KeyNames
{
    private static readonly Dictionary<int, string> ByCode = Build();
    private static readonly Dictionary<string, int> ByName =
        ByCode.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

    private static Dictionary<int, string> Build()
    {
        var map = new Dictionary<int, string>
        {
            [0x05] = "Mouse 4", [0x06] = "Mouse 5",
            [0x08] = "Backspace", [0x09] = "Tab", [0x0D] = "Enter", [0x13] = "Pause", [0x14] = "CapsLock",
            [0x1B] = "Esc", [0x20] = "Space", [0x21] = "PageUp", [0x22] = "PageDown", [0x23] = "End",
            [0x24] = "Home", [0x25] = "Left", [0x26] = "Up", [0x27] = "Right", [0x28] = "Down",
            [0x2C] = "PrintScreen", [0x2D] = "Insert", [0x2E] = "Delete", [0x91] = "ScrollLock",
            [0x6A] = "Num*", [0x6B] = "Num+", [0x6D] = "Num-", [0x6E] = "Num.", [0x6F] = "Num/",
            [0xBA] = ";", [0xBB] = "=", [0xBC] = ",", [0xBD] = "-", [0xBE] = ".", [0xBF] = "/",
            [0xC0] = "`", [0xDB] = "[", [0xDC] = "\\", [0xDD] = "]", [0xDE] = "'",
        };
        for (var i = 0; i <= 9; i++)
        {
            map[0x30 + i] = i.ToString(CultureInfo.InvariantCulture);
            map[0x60 + i] = "Num" + i.ToString(CultureInfo.InvariantCulture);
        }
        for (var c = 'A'; c <= 'Z'; c++) map[c] = c.ToString();
        for (var f = 1; f <= 24; f++) map[0x6F + f] = "F" + f.ToString(CultureInfo.InvariantCulture);
        return map;
    }

    public static string NameOf(int vk) =>
        ByCode.TryGetValue(vk, out var name) ? name : "VK" + vk.ToString("X2", CultureInfo.InvariantCulture);

    public static bool TryParse(string text, out int vk)
    {
        if (ByName.TryGetValue(text, out vk)) return true;
        return text.StartsWith("VK", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out vk)
            && vk is > 0 and < 0x100;
    }

    /// <summary>Modifier flag for a modifier key (generic, left or right variant), or None for other keys.</summary>
    public static Mods ModifierOf(int vk) => vk switch
    {
        0x11 or 0xA2 or 0xA3 => Mods.Ctrl,
        0x10 or 0xA0 or 0xA1 => Mods.Shift,
        0x12 or 0xA4 or 0xA5 => Mods.Alt,
        0x5B or 0x5C => Mods.Win,
        _ => Mods.None,
    };
}
