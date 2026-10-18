namespace MemePad.Core.Input;

[Flags]
public enum Mods
{
    None = 0,
    Ctrl = 1,
    Alt = 2,
    Shift = 4,
    Win = 8,
}

/// <summary>A key combination: modifier set plus one Windows virtual-key code (mouse X1/X2 use 0x05/0x06).</summary>
public readonly record struct Hotkey(Mods Mods, int VirtualKey);
