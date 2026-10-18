using System.Text.Json;
using MemePad.Core.Input;
using MemePad.Core.Model;
using MemePad.Core.Settings;

namespace MemePad.Core.Tests;

public class HotkeyTests
{
    private const int VK_1 = 0x31, VK_2 = 0x32, VK_F12 = 0x7B, VK_PAUSE = 0x13;
    private const int VK_LCONTROL = 0xA2, VK_RCONTROL = 0xA3, VK_LSHIFT = 0xA0;

    [Theory]
    [InlineData("Ctrl+1")]
    [InlineData("Alt+Shift+F12")]
    [InlineData("Mouse 5")]
    [InlineData("Pause")]
    [InlineData("Ctrl+Shift+F5")]
    public void Parse_and_format_roundtrip(string text)
    {
        Assert.True(Hotkey.TryParse(text, out var h));
        Assert.Equal(text, h.ToString());
    }

    [Fact]
    public void Parse_produces_expected_values()
    {
        Assert.True(Hotkey.TryParse("Alt+Shift+F12", out var h));
        Assert.Equal(new Hotkey(Mods.Alt | Mods.Shift, VK_F12), h);
        Assert.True(Hotkey.TryParse("Mouse 4", out var m));
        Assert.Equal(new Hotkey(Mods.None, 0x05), m);
        Assert.False(Hotkey.TryParse("Ctrl+", out _));
        Assert.False(Hotkey.TryParse("Banana", out _));
    }

    [Fact]
    public void Serializes_as_string_in_settings_json()
    {
        var json = JsonSerializer.Serialize(new AppSettings { StopHotkey = new Hotkey(Mods.None, VK_PAUSE) }, SettingsStore.Json);
        Assert.Contains("\"stopHotkey\": \"Pause\"", json);
        var back = JsonSerializer.Deserialize<AppSettings>(json, SettingsStore.Json)!;
        Assert.Equal(new Hotkey(Mods.None, VK_PAUSE), back.StopHotkey);
    }

    private static HotkeyMatcher MatcherWith(params (Hotkey key, string action)[] map)
    {
        var m = new HotkeyMatcher();
        m.SetBindings(map.ToDictionary(x => x.key, x => (object)x.action));
        return m;
    }

    [Fact]
    public void Fires_once_on_down_not_on_autorepeat()
    {
        var m = MatcherWith((new Hotkey(Mods.Ctrl, VK_1), "a"));
        Assert.Empty(m.OnInput(new InputEvent(VK_LCONTROL, true)));
        Assert.Equal(["a"], m.OnInput(new InputEvent(VK_1, true)));
        Assert.Empty(m.OnInput(new InputEvent(VK_1, true)));   // autorepeat
        Assert.Empty(m.OnInput(new InputEvent(VK_1, false)));
        Assert.Equal(["a"], m.OnInput(new InputEvent(VK_1, true)));
    }

    [Fact]
    public void Requires_exact_modifiers()
    {
        var m = MatcherWith((new Hotkey(Mods.Ctrl, VK_1), "a"));
        m.OnInput(new InputEvent(VK_LCONTROL, true));
        m.OnInput(new InputEvent(VK_LSHIFT, true));
        Assert.Empty(m.OnInput(new InputEvent(VK_1, true)));
        Assert.Empty(m.OnInput(new InputEvent(VK_2, true)));
    }

    [Fact]
    public void Left_and_right_ctrl_both_count()
    {
        var m = MatcherWith((new Hotkey(Mods.Ctrl, VK_1), "a"));
        m.OnInput(new InputEvent(VK_RCONTROL, true));
        Assert.Equal(["a"], m.OnInput(new InputEvent(VK_1, true)));
        m.OnInput(new InputEvent(VK_1, false));
        m.OnInput(new InputEvent(VK_RCONTROL, false));
        Assert.Empty(m.OnInput(new InputEvent(VK_1, true)));
    }

    [Fact]
    public void Plain_key_without_modifiers_fires()
    {
        var m = MatcherWith((new Hotkey(Mods.None, VK_PAUSE), "stop"));
        Assert.Equal(["stop"], m.OnInput(new InputEvent(VK_PAUSE, true)));
    }

    [Fact]
    public void Conflict_detected_within_page_not_across_pages()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var key = new Hotkey(Mods.Ctrl, VK_1);
        var page1 = new HotkeyPage(Guid.NewGuid(), "One", new() { [a] = key });
        var page2 = new HotkeyPage(Guid.NewGuid(), "Two", new());

        Assert.Equal([a], HotkeyMatcher.FindConflicts(page1, key, exceptSound: b));
        Assert.Empty(HotkeyMatcher.FindConflicts(page1, key, exceptSound: a));
        Assert.Empty(HotkeyMatcher.FindConflicts(page2, key, exceptSound: b));
    }
}
