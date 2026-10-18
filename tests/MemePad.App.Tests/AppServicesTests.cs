using MemePad.App.Audio;
using MemePad.App.Services;
using MemePad.App.ViewModels;
using MemePad.Core.Audio;
using MemePad.Core.Input;
using MemePad.Core.Model;
using MemePad.Core.Settings;

namespace MemePad.App.Tests;

public sealed class AppServicesTests : IDisposable
{
    private const int VK_F13 = 0x7C;
    private readonly string _dir = Directory.CreateTempSubdirectory("memepad-svc-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private sealed class Endpoints : IAudioEndpoints
    {
        public event Action? DevicesChanged { add { } remove { } }
        public string? FindCableInputId() => "cable";
        public AudioFormat GetMixFormat(string? renderId) => new(48000, 2);
        public IDisposable OpenPipeline(PipelineRequest request) => new Noop();

        private sealed class Noop : IDisposable
        {
            public void Dispose() { }
        }
    }

    private sealed class NoDialogs : IDialogs
    {
        public HotkeyCapture? CaptureHotkey(string title, Func<Hotkey, string?> conflictCheck) => null;
        public bool EditSound(Sound sound, DecodedSound decoded, out Sound edited) { edited = sound; return false; }
        public bool EditSettings(AppSettings settings) => false;
        public string? AskText(string title, string initial) => null;
    }

    [Fact]
    public void Changing_volume_while_playing_keeps_push_to_talk_held()
    {
        var downs = new List<int>();
        var ups = new List<int>();
        using var engine = new AudioEngine(new Endpoints());
        using var recorder = new LoopbackRecorder(10);
        using var services = new AppServices(_dir, new SettingsStore(_dir), engine, recorder, new NoDialogs(), downs.Add, ups.Add);
        var settings = new AppSettings { PttEnabled = true, PttKey = new Hotkey(Mods.None, VK_F13) };
        services.Start(settings);

        var tone = new DecodedSound(Guid.NewGuid(), engine.MixFormat, new float[48000 * 2 * 5]);
        services.Playback.Play(tone, 0, TimeSpan.Zero, null, PlaybackMode.Interrupt);
        Assert.Equal([VK_F13], downs);

        settings.MicVolume = 0.4f;            // user drags the mic slider mid-sound
        services.ApplyAudioSettings(settings);

        Assert.Empty(ups);
    }

    [Fact]
    public void Changing_ptt_key_while_held_releases_old_key()
    {
        var downs = new List<int>();
        var ups = new List<int>();
        using var engine = new AudioEngine(new Endpoints());
        using var recorder = new LoopbackRecorder(10);
        using var services = new AppServices(_dir, new SettingsStore(_dir), engine, recorder, new NoDialogs(), downs.Add, ups.Add);
        var settings = new AppSettings { PttEnabled = true, PttKey = new Hotkey(Mods.None, VK_F13) };
        services.Start(settings);
        services.Playback.Play(new DecodedSound(Guid.NewGuid(), engine.MixFormat, new float[48000 * 2 * 5]), 0, TimeSpan.Zero, null, PlaybackMode.Interrupt);

        settings.PttKey = new Hotkey(Mods.None, 0x7D);
        services.ApplyAudioSettings(settings);

        Assert.Equal([VK_F13], ups);   // the old key is not left stuck down
    }
}
