using MemePad.App.Audio;
using MemePad.App.ViewModels;
using MemePad.Core.Audio;
using MemePad.Core.Model;

namespace MemePad.App.Services;

/// <summary>Adapts <see cref="AudioEngine"/> to the view model, and follows mixer replacements for start/end events.</summary>
public sealed class PlaybackService : IPlaybackService, IDisposable
{
    private readonly AudioEngine _engine;
    private Mixer _mixer;

    public PlaybackService(AudioEngine engine)
    {
        _engine = engine;
        _mixer = engine.Mixer;
        Subscribe(_mixer);
        engine.MixFormatChanged += OnMixFormatChanged;
    }

    public AudioFormat Format => _engine.MixFormat;
    public IReadOnlyCollection<Guid> PlayingIds => _engine.Mixer.ActiveSoundIds;

    public event Action? FormatChanged;
    public event Action? PlaybackStarted;
    public event Action? PlaybackEnded;

    public DecodeResult Decode(string path, Guid id) => SoundDecoder.Decode(path, Format, id);

    public void Play(DecodedSound sound, double gainDb, TimeSpan trimStart, TimeSpan? trimEnd, PlaybackMode mode)
    {
        // A sound decoded for an older device format is skipped until the library re-decodes after FormatChanged.
        if (sound.Format != _engine.MixFormat) return;
        _engine.Mixer.Play(sound, gainDb, trimStart, trimEnd, mode);
    }

    public void PlayTransient(DecodedSound sound) => Play(sound, 0, TimeSpan.Zero, null, PlaybackMode.Overlap);

    public void StopAll() => _engine.Mixer.StopAll();

    private void OnMixFormatChanged()
    {
        Unsubscribe(_mixer);
        _mixer = _engine.Mixer;
        Subscribe(_mixer);
        FormatChanged?.Invoke();
    }

    private void Subscribe(Mixer m)
    {
        m.PlaybackStarted += RaiseStarted;
        m.PlaybackEnded += RaiseEnded;
    }

    private void Unsubscribe(Mixer m)
    {
        m.PlaybackStarted -= RaiseStarted;
        m.PlaybackEnded -= RaiseEnded;
    }

    private void RaiseStarted() => PlaybackStarted?.Invoke();
    private void RaiseEnded() => PlaybackEnded?.Invoke();

    public void Dispose()
    {
        _engine.MixFormatChanged -= OnMixFormatChanged;
        Unsubscribe(_mixer);
    }
}
