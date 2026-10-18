using System.Text.Json;
using System.Text.Json.Serialization;

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
[JsonConverter(typeof(HotkeyJsonConverter))]
public readonly record struct Hotkey(Mods Mods, int VirtualKey)
{
    private static readonly (Mods Flag, string Name)[] ModNames =
        [(Mods.Ctrl, "Ctrl"), (Mods.Alt, "Alt"), (Mods.Shift, "Shift"), (Mods.Win, "Win")];

    public override string ToString()
    {
        var parts = new List<string>(5);
        foreach (var (flag, name) in ModNames)
            if (Mods.HasFlag(flag)) parts.Add(name);
        parts.Add(KeyNames.NameOf(VirtualKey));
        return string.Join("+", parts);
    }

    public static bool TryParse(string? text, out Hotkey hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var parts = text.Split('+');
        var mods = Mods.None;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            var flag = ModNames.FirstOrDefault(m => m.Name.Equals(parts[i].Trim(), StringComparison.OrdinalIgnoreCase)).Flag;
            if (flag == Mods.None) return false;
            mods |= flag;
        }

        if (!KeyNames.TryParse(parts[^1].Trim(), out var vk)) return false;
        hotkey = new Hotkey(mods, vk);
        return true;
    }
}

internal sealed class HotkeyJsonConverter : JsonConverter<Hotkey>
{
    public override Hotkey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Hotkey.TryParse(reader.GetString(), out var h) ? h : throw new JsonException($"Invalid hotkey '{reader.GetString()}'.");

    public override void Write(Utf8JsonWriter writer, Hotkey value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());

    public override Hotkey ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Read(ref reader, typeToConvert, options);

    public override void WriteAsPropertyName(Utf8JsonWriter writer, Hotkey value, JsonSerializerOptions options) =>
        writer.WritePropertyName(value.ToString());
}
