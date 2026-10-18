using MemePad.App.ViewModels;
using MemePad.Core.Input;
using MemePad.Core.Model;
using MemePad.Core.Settings;

namespace MemePad.App.Tests;

public sealed class SettingsViewModelTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("memepad-set-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static SettingsViewModel Make(AppSettings s, Func<string, HotkeyCapture?>? capture = null) => new(
        s,
        microphones: [new DeviceOption("mic-1", "USB Mic"), new DeviceOption("mic-2", "Headset")],
        outputs: [new DeviceOption("out-1", "Speakers")],
        voices: ["Irina", "Zira"],
        captureHotkey: capture ?? (_ => null));

    [Fact]
    public void Settings_changes_persist_via_store()
    {
        var store = new SettingsStore(_dir);
        var settings = new AppSettings();
        var vm = Make(settings);

        vm.SelectedMic = vm.Microphones.Single(d => d.Id == "mic-2");
        vm.SelectedMonitor = vm.Outputs.Single(d => d.Id == "out-1");
        vm.MonitorEnabled = false;
        vm.MonitorVolume = 0.3f;
        vm.PttEnabled = true;
        vm.Mode = PlaybackMode.Overlap;
        vm.SwallowHotkeys = true;
        vm.Effect = VoiceEffectKind.Lower;
        vm.TtsVoice = "Zira";
        vm.TtsRate = 3;
        vm.HistorySeconds = 60;
        vm.Language = "en";
        vm.ApplyTo(settings);
        store.Save(settings);

        var loaded = store.Load();
        Assert.Equal("mic-2", loaded.MicDeviceId);
        Assert.Equal("out-1", loaded.MonitorDeviceId);
        Assert.False(loaded.MonitorEnabled);
        Assert.Equal(0.3f, loaded.MonitorVolume);
        Assert.True(loaded.PttEnabled);
        Assert.Equal(PlaybackMode.Overlap, loaded.Mode);
        Assert.True(loaded.SwallowHotkeys);
        Assert.Equal(VoiceEffectKind.Lower, loaded.Effect);
        Assert.Equal("Zira", loaded.TtsVoice);
        Assert.Equal(3, loaded.TtsRate);
        Assert.Equal(60, loaded.HistorySeconds);
        Assert.Equal("en", loaded.Language);
    }

    [Theory]
    [InlineData(5, 10)]
    [InlineData(10, 10)]
    [InlineData(45, 45)]
    [InlineData(500, 120)]
    public void History_seconds_clamped_10_to_120(int input, int expected)
    {
        var vm = Make(new AppSettings());
        vm.HistorySeconds = input;
        Assert.Equal(expected, vm.HistorySeconds);
    }

    [Fact]
    public void Tts_rate_clamped_minus5_plus5()
    {
        var vm = Make(new AppSettings());
        vm.TtsRate = 9;
        Assert.Equal(5, vm.TtsRate);
        vm.TtsRate = -9;
        Assert.Equal(-5, vm.TtsRate);
    }

    [Fact]
    public void Default_device_option_maps_to_null_id()
    {
        var settings = new AppSettings { MicDeviceId = "mic-1" };
        var vm = Make(settings);
        Assert.Equal("mic-1", vm.SelectedMic!.Id);

        vm.SelectedMic = vm.Microphones[0];   // first entry is "system default"
        vm.ApplyTo(settings);

        Assert.Null(vm.Microphones[0].Id);
        Assert.Null(settings.MicDeviceId);
    }

    [Fact]
    public void Unknown_saved_device_keeps_its_id_until_changed()
    {
        var settings = new AppSettings { MicDeviceId = "unplugged-mic" };
        var vm = Make(settings);
        vm.ApplyTo(settings);
        Assert.Equal("unplugged-mic", settings.MicDeviceId);
    }

    [Fact]
    public void Hotkey_capture_sets_and_clears_global_keys()
    {
        var settings = new AppSettings { StopHotkey = new Hotkey(Mods.None, 0x13) };
        var next = new HotkeyCapture(new Hotkey(Mods.Ctrl, 0x27));
        var vm = Make(settings, _ => next);

        vm.CaptureCommand.Execute(HotkeySlot.NextPage);
        next = new HotkeyCapture(null);
        vm.CaptureCommand.Execute(HotkeySlot.Stop);
        vm.ApplyTo(settings);

        Assert.Equal(new Hotkey(Mods.Ctrl, 0x27), settings.NextPageHotkey);
        Assert.Null(settings.StopHotkey);
        Assert.Equal("Ctrl+Right", vm.NextPageText);
    }
}
