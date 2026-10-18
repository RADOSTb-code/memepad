using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemePad.Core.Audio;
using MemePad.Core.Model;

namespace MemePad.App.ViewModels;

/// <summary>
/// Trim and gain editor for one sound. Edits only the library entry; the audio file itself is never touched.
/// </summary>
public sealed partial class EditorViewModel : ObservableObject
{
    private const double MinLengthSeconds = 0.05;
    public const double MinGainDb = -24;
    public const double MaxGainDb = 12;

    private readonly Sound _sound;
    private readonly DecodedSound _decoded;
    private readonly Action<Sound> _preview;

    public EditorViewModel(Sound sound, DecodedSound decoded, Action<Sound> preview)
    {
        _sound = sound;
        _decoded = decoded;
        _preview = preview;
        DurationSeconds = decoded.Duration.TotalSeconds;
        _trimStartSeconds = Math.Clamp(sound.TrimStart.TotalSeconds, 0, Math.Max(0, DurationSeconds - MinLengthSeconds));
        _trimEndSeconds = Math.Clamp(sound.TrimEnd?.TotalSeconds ?? DurationSeconds, _trimStartSeconds + MinLengthSeconds, DurationSeconds);
        _gainDb = Math.Clamp(sound.GainDb, MinGainDb, MaxGainDb);
    }

    public string Title => _sound.Title;
    public double DurationSeconds { get; }

    private double _trimStartSeconds;
    private double _trimEndSeconds;
    private double _gainDb;

    /// <summary>Start of the played window; kept at least 50 ms before the end.</summary>
    public double TrimStartSeconds
    {
        get => _trimStartSeconds;
        set => SetProperty(ref _trimStartSeconds, Math.Clamp(value, 0, Math.Max(0, TrimEndSeconds - MinLengthSeconds)));
    }

    /// <summary>End of the played window; kept at least 50 ms after the start and within the file.</summary>
    public double TrimEndSeconds
    {
        get => _trimEndSeconds;
        set => SetProperty(ref _trimEndSeconds, Math.Clamp(value, Math.Min(DurationSeconds, TrimStartSeconds + MinLengthSeconds), DurationSeconds));
    }

    public double GainDb
    {
        get => _gainDb;
        set => SetProperty(ref _gainDb, Math.Clamp(value, MinGainDb, MaxGainDb));
    }

    /// <summary>Peak level per bucket across the whole file, 0..1, for drawing the waveform.</summary>
    public float[] Peaks(int buckets)
    {
        var result = new float[buckets];
        var samples = _decoded.Samples;
        if (samples.Length == 0 || buckets <= 0) return result;

        var per = (double)samples.Length / buckets;
        for (var b = 0; b < buckets; b++)
        {
            var from = (int)(b * per);
            var to = Math.Min(samples.Length, (int)((b + 1) * per));
            var peak = 0f;
            for (var i = from; i < to; i++) peak = Math.Max(peak, Math.Abs(samples[i]));
            result[b] = Math.Min(1f, peak);
        }
        return result;
    }

    /// <summary>The library entry with the current edits; an end at the file's end is stored as null.</summary>
    public Sound Result() => _sound with
    {
        TrimStart = TimeSpan.FromSeconds(TrimStartSeconds),
        TrimEnd = DurationSeconds - TrimEndSeconds < 0.001 ? null : TimeSpan.FromSeconds(TrimEndSeconds),
        GainDb = GainDb,
    };

    [RelayCommand]
    private void Preview() => _preview(Result());
}
