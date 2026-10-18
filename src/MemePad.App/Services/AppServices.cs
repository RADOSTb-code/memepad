using System.Diagnostics;
using System.IO;
using MemePad.App.Audio;
using MemePad.App.Input;
using MemePad.App.ViewModels;
using MemePad.Core.Audio;
using MemePad.Core.Model;
using MemePad.Core.Ptt;
using MemePad.Core.Settings;

namespace MemePad.App.Services;

/// <summary>Real side effects behind <see cref="IAppServices"/>: audio engine, PTT, recorder, speech and the settings file.</summary>
public sealed class AppServices : IAppServices, IDisposable
{
    private sealed class StopwatchClock : IClock
    {
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        public TimeSpan Now => _sw.Elapsed;
    }

    private readonly SettingsStore _store;
    private readonly AudioEngine _engine;
    private readonly PlaybackService _playback;
    private readonly LoopbackRecorder _recorder;
    private readonly SpeechSynth _speech = new();
    private readonly PttController _ptt;
    private readonly Timer _pttTimer;
    private readonly string _dataDir;
    private int _pttVk;
    private string? _micId;
    private string? _monitorId;
    private int _historySeconds;

    public AppServices(string dataDir, SettingsStore store, AudioEngine engine, LoopbackRecorder recorder, IDialogs dialogs)
    {
        _dataDir = dataDir;
        _store = store;
        _engine = engine;
        _recorder = recorder;
        Dialogs = dialogs;
        _playback = new PlaybackService(engine);
        _ptt = new PttController(new StopwatchClock(), () => KeySender.Down(_pttVk), () => KeySender.Up(_pttVk), TimeSpan.FromMilliseconds(200));
        _playback.PlaybackStarted += _ptt.OnPlaybackStarted;
        _playback.PlaybackEnded += _ptt.OnPlaybackEnded;
        _pttTimer = new Timer(_ => _ptt.Tick(), null, 20, 20);
    }

    public IPlaybackService Playback => _playback;
    public IDialogs Dialogs { get; }

    public void Save(AppSettings settings) => _store.Save(settings);

    /// <summary>Starts the engine and recorder with the saved settings.</summary>
    public void Start(AppSettings settings)
    {
        _micId = settings.MicDeviceId;
        _monitorId = settings.MonitorDeviceId;
        _historySeconds = settings.HistorySeconds;
        _engine.Start(_micId, _monitorId);
        _recorder.Restart(_historySeconds);
        ApplyAudioSettings(settings);
    }

    public void ApplyAudioSettings(AppSettings settings)
    {
        _engine.Mixer.MicVolume = settings.MicVolume;
        _engine.Mixer.SoundsVolume = settings.SoundsVolume;
        _engine.MonitorVolume = settings.MonitorVolume;
        _engine.MonitorEnabled = settings.MonitorEnabled;
        _engine.Effect = settings.Effect;
        _engine.EffectEnabled = settings.EffectEnabled;

        // The PTT key is held without modifiers: Discord binds push-to-talk to a single key.
        _ptt.Enabled = false;
        _pttVk = settings.PttKey?.VirtualKey ?? 0;
        _ptt.Enabled = settings.PttEnabled && _pttVk != 0;

        if (settings.MicDeviceId != _micId || settings.MonitorDeviceId != _monitorId)
        {
            _micId = settings.MicDeviceId;
            _monitorId = settings.MonitorDeviceId;
            _engine.Start(_micId, _monitorId);
        }

        if (settings.HistorySeconds != _historySeconds)
        {
            _historySeconds = settings.HistorySeconds;
            _recorder.Restart(_historySeconds);
        }
    }

    public DecodeResult Speak(string text, AppSettings settings) =>
        _speech.Synthesize(text, settings.TtsVoice, settings.TtsRate, _playback.Format);

    public string? SaveSpeechFile(string text, AppSettings settings)
    {
        var result = Speak(text, settings);
        if (result.Sound is not { } sound) return null;
        var dir = Path.Combine(_dataDir, "Speech");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"tts-{DateTime.Now:yyyyMMdd-HHmmss}.wav");
        WavWriter.WriteFloat32(path, sound.Format, sound.Samples);
        return path;
    }

    public string? SaveClip() => _recorder.SaveClip(Path.Combine(_dataDir, "Recordings"));

    public void RestartRecorder() => _recorder.Restart();

    public void Dispose()
    {
        _pttTimer.Dispose();
        _ptt.Enabled = false;
        _playback.Dispose();
    }
}
