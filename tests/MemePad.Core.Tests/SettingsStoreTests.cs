using MemePad.Core.Input;
using MemePad.Core.Model;
using MemePad.Core.Settings;

namespace MemePad.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "memepad-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Roundtrip_preserves_all_fields()
    {
        var store = new SettingsStore(_dir);
        var sound = new Sound(Guid.NewGuid(), @"C:\memes\boom.mp3", "Boom", true, -3.5, TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(2));
        var page = new HotkeyPage(Guid.NewGuid(), "CS2", new Dictionary<Guid, Hotkey> { [sound.Id] = new Hotkey(Mods.Ctrl, 0x31) });
        var s = new AppSettings
        {
            Sounds = [sound],
            Pages = [page],
            ActivePage = 0,
            StopHotkey = new Hotkey(Mods.None, 0x13),
            Mode = PlaybackMode.Overlap,
            SwallowHotkeys = true,
            MicVolume = 0.7f,
            SoundsVolume = 0.5f,
            MonitorVolume = 0.25f,
            MonitorEnabled = false,
            MicDeviceId = "mic-1",
            MonitorDeviceId = "out-1",
            PttEnabled = true,
            PttKey = new Hotkey(Mods.None, 0x56),
            HistorySeconds = 45,
            Effect = VoiceEffectKind.Robot,
            EffectEnabled = true,
            TtsVoice = "Irina",
            TtsRate = 2,
            Language = "ru",
        };

        store.Save(s);
        var loaded = store.Load();

        Assert.Equal(sound, loaded.Sounds.Single());
        Assert.Equal(page.Id, loaded.Pages.Single().Id);
        Assert.Equal("CS2", loaded.Pages.Single().Name);
        Assert.Equal(new Hotkey(Mods.Ctrl, 0x31), loaded.Pages.Single().Bindings[sound.Id]);
        Assert.Equal(new Hotkey(Mods.None, 0x13), loaded.StopHotkey);
        Assert.Equal(PlaybackMode.Overlap, loaded.Mode);
        Assert.True(loaded.SwallowHotkeys);
        Assert.Equal(0.7f, loaded.MicVolume);
        Assert.Equal(0.5f, loaded.SoundsVolume);
        Assert.Equal(0.25f, loaded.MonitorVolume);
        Assert.False(loaded.MonitorEnabled);
        Assert.Equal("mic-1", loaded.MicDeviceId);
        Assert.Equal("out-1", loaded.MonitorDeviceId);
        Assert.True(loaded.PttEnabled);
        Assert.Equal(new Hotkey(Mods.None, 0x56), loaded.PttKey);
        Assert.Equal(45, loaded.HistorySeconds);
        Assert.Equal(VoiceEffectKind.Robot, loaded.Effect);
        Assert.True(loaded.EffectEnabled);
        Assert.Equal("Irina", loaded.TtsVoice);
        Assert.Equal(2, loaded.TtsRate);
        Assert.Equal("ru", loaded.Language);
    }

    [Fact]
    public void Missing_file_returns_defaults_with_one_page_named_General()
    {
        var loaded = new SettingsStore(_dir).Load();

        Assert.Empty(loaded.Sounds);
        var page = Assert.Single(loaded.Pages);
        Assert.Equal(AppSettings.DefaultPageName, page.Name);
        Assert.Equal(PlaybackMode.Interrupt, loaded.Mode);
        Assert.Equal(30, loaded.HistorySeconds);
    }

    [Fact]
    public void Corrupt_json_returns_defaults_and_keeps_backup()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "settings.json"), "{ not json");

        var loaded = new SettingsStore(_dir).Load();

        Assert.Single(loaded.Pages);
        Assert.True(File.Exists(Path.Combine(_dir, "settings.json.bad")));
    }

    [Fact]
    public void Save_is_atomic_no_tmp_left()
    {
        var store = new SettingsStore(_dir);
        store.Save(new AppSettings());
        store.Save(new AppSettings { HistorySeconds = 60 });

        Assert.Equal(60, store.Load().HistorySeconds);
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }
}
