using MemePad.Core.Model;

namespace MemePad.Core.Audio;

/// <summary>
/// Sums the microphone and all playing sounds into the output stream, and the sounds alone into a monitor stream.
/// <see cref="Play"/> and <see cref="StopAll"/> may be called from any thread; <see cref="Render"/> runs on the
/// audio thread and does not allocate.
/// </summary>
public sealed class Mixer(AudioFormat format, int maxVoices = 16)
{
    private readonly object _gate = new();
    private readonly List<Voice> _voices = new(maxVoices);
    private readonly SoftLimiter _limiter = new();
    private bool _wasPlaying;

    public AudioFormat Format { get; } = format;
    public float MicVolume { get; set; } = 1f;
    public float SoundsVolume { get; set; } = 1f;

    public event Action? PlaybackStarted;
    public event Action? PlaybackEnded;

    public bool IsPlaying
    {
        get { lock (_gate) return _voices.Count > 0; }
    }

    internal int VoiceCount
    {
        get { lock (_gate) return _voices.Count; }
    }

    internal IReadOnlyList<Guid> ActiveSoundIds
    {
        get { lock (_gate) return _voices.Select(v => v.SoundId).ToList(); }
    }

    public void Play(DecodedSound sound, double gainDb, TimeSpan trimStart, TimeSpan? trimEnd, PlaybackMode mode)
    {
        if (sound.Format != Format)
            throw new ArgumentException($"Sound is {sound.Format}, mixer expects {Format}.", nameof(sound));

        var ch = Format.Channels;
        var startFrame = Math.Clamp(Format.FramesFor(trimStart), 0, sound.FrameCount);
        var endFrame = Math.Clamp(trimEnd is { } e ? Format.FramesFor(e) : sound.FrameCount, startFrame, sound.FrameCount);
        if (endFrame == startFrame) return;

        var voice = new Voice(sound, startFrame * ch, endFrame * ch, Gain.DbToLinear(gainDb));
        bool started;
        lock (_gate)
        {
            if (mode == PlaybackMode.Interrupt) _voices.Clear();
            else _voices.RemoveAll(v => v.SoundId == sound.Id);

            if (_voices.Count >= maxVoices) _voices.RemoveAt(0);   // list is oldest-first
            _voices.Add(voice);

            started = !_wasPlaying;
            _wasPlaying = true;
        }
        if (started) PlaybackStarted?.Invoke();
    }

    public void StopAll()
    {
        bool ended;
        lock (_gate)
        {
            _voices.Clear();
            ended = _wasPlaying;
            _wasPlaying = false;
        }
        if (ended) PlaybackEnded?.Invoke();
    }

    /// <summary>
    /// Fills <paramref name="output"/> with limited mic+sounds and <paramref name="monitorOut"/> with limited sounds only
    /// (before <see cref="SoundsVolume"/>). All three spans hold the same number of interleaved samples.
    /// </summary>
    public void Render(Span<float> output, ReadOnlySpan<float> mic, Span<float> monitorOut)
    {
        monitorOut.Clear();
        bool ended;
        lock (_gate)
        {
            for (var v = _voices.Count - 1; v >= 0; v--)
            {
                _voices[v].MixInto(monitorOut);
                if (_voices[v].Finished) _voices.RemoveAt(v);
            }
            ended = _wasPlaying && _voices.Count == 0;
            if (ended) _wasPlaying = false;
        }

        var micVol = MicVolume;
        var sndVol = SoundsVolume;
        for (var i = 0; i < output.Length; i++)
        {
            var m = i < mic.Length ? mic[i] : 0f;
            output[i] = m * micVol + monitorOut[i] * sndVol;
        }

        _limiter.Process(output);
        _limiter.Process(monitorOut);
        if (ended) PlaybackEnded?.Invoke();
    }
}
