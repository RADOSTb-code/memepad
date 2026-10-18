using MemePad.Core.Audio;
using MemePad.Core.Audio.Effects;
using MemePad.Core.Model;

namespace MemePad.App.Audio;

/// <summary>
/// Owns the mixer and keeps an audio pipeline running: opens devices, and on any failure or device change
/// tears the pipeline down and retries until it works again. Never throws device errors to callers.
/// </summary>
public sealed class AudioEngine : IDisposable
{
    private readonly IAudioEndpoints _endpoints;
    private readonly TimeSpan _retryDelay;
    private readonly object _gate = new();
    private readonly Timer _retryTimer;
    private IDisposable? _pipeline;
    private string? _micId;
    private string? _monitorId;
    private bool _started;
    private bool _disposed;
    private float[] _micScratch = new float[4096];
    private volatile IVoiceEffect? _effect;
    private VoiceEffectKind _effectKind;
    private bool _effectEnabled;

    public AudioEngine(IAudioEndpoints endpoints, TimeSpan? retryDelay = null)
    {
        _endpoints = endpoints;
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(500);
        _retryTimer = new Timer(_ => TryOpen());
        _endpoints.DevicesChanged += OnDevicesChanged;
        MixFormat = new AudioFormat(48000, 2);
        Mixer = new Mixer(MixFormat);
    }

    public EngineStatus Status { get; private set; } = EngineStatus.Stopped;
    public AudioFormat MixFormat { get; private set; }
    public Mixer Mixer { get; private set; }
    public float MonitorVolume { get; set; } = 0.6f;
    public bool MonitorEnabled { get; set; } = true;

    public event Action<EngineStatus>? StatusChanged;

    /// <summary>Raised when the cable's format changed and a new <see cref="Mixer"/> replaced the old one; sounds must be re-decoded.</summary>
    public event Action? MixFormatChanged;

    public VoiceEffectKind Effect
    {
        set
        {
            _effectKind = value;
            UpdateEffect();
        }
    }

    public bool EffectEnabled
    {
        set
        {
            _effectEnabled = value;
            UpdateEffect();
        }
    }

    /// <summary>Starts or restarts the pipeline with the given devices (null = defaults).</summary>
    public void Start(string? micId, string? monitorId)
    {
        lock (_gate)
        {
            _micId = micId;
            _monitorId = monitorId;
            _started = true;
        }
        TryOpen();
    }

    private void UpdateEffect() => _effect = _effectEnabled ? VoiceEffects.Create(_effectKind) : null;

    private void OnDevicesChanged() => ScheduleRetry();

    private void ScheduleRetry()
    {
        lock (_gate)
        {
            if (_disposed || !_started) return;
            _retryTimer.Change(_retryDelay, Timeout.InfiniteTimeSpan);
        }
    }

    private void TryOpen()
    {
        EngineStatus newStatus;
        var formatChanged = false;
        lock (_gate)
        {
            if (_disposed || !_started) return;
            _pipeline?.Dispose();
            _pipeline = null;

            try
            {
                var cable = _endpoints.FindCableInputId();
                var format = _endpoints.GetMixFormat(cable);
                if (format != MixFormat)
                {
                    var old = Mixer;
                    MixFormat = format;
                    Mixer = new Mixer(format) { MicVolume = old.MicVolume, SoundsVolume = old.SoundsVolume };
                    formatChanged = true;
                }

                _pipeline = _endpoints.OpenPipeline(new PipelineRequest(cable, _micId, _monitorId, format, Render, OnPipelineFailed));
                newStatus = cable is null ? EngineStatus.NoCable : EngineStatus.Running;
            }
            catch (Exception)
            {
                newStatus = EngineStatus.Reconnecting;
                _retryTimer.Change(_retryDelay, Timeout.InfiniteTimeSpan);
            }
        }

        if (formatChanged) MixFormatChanged?.Invoke();
        SetStatus(newStatus);
    }

    private void OnPipelineFailed(Exception error)
    {
        SetStatus(EngineStatus.Reconnecting);
        ScheduleRetry();
    }

    private void SetStatus(EngineStatus status)
    {
        if (Status == status) return;
        Status = status;
        StatusChanged?.Invoke(status);
    }

    /// <summary>Audio-thread callback: effect on the mic, mix, then monitor volume.</summary>
    private void Render(Span<float> output, ReadOnlySpan<float> mic, Span<float> monitor)
    {
        if (_micScratch.Length < mic.Length) _micScratch = new float[mic.Length];
        var micBuf = _micScratch.AsSpan(0, mic.Length);
        mic.CopyTo(micBuf);
        _effect?.Process(micBuf, MixFormat);

        Mixer.Render(output, micBuf, monitor);

        var vol = MonitorEnabled ? MonitorVolume : 0f;
        for (var i = 0; i < monitor.Length; i++) monitor[i] *= vol;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _endpoints.DevicesChanged -= OnDevicesChanged;
            _retryTimer.Dispose();
            _pipeline?.Dispose();
            _pipeline = null;
        }
    }
}
