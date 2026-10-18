using MemePad.Core.Audio;
using MemePad.Core.Model;

namespace MemePad.Core.Tests;

public class MixerTests
{
    // A tiny format keeps sample math readable: 1000 frames per second, mono.
    private static readonly AudioFormat Fmt = new(1000, 1);

    private static DecodedSound Constant(float value, double seconds = 1.0, Guid? id = null)
    {
        var samples = new float[(int)(Fmt.SampleRate * seconds) * Fmt.Channels];
        Array.Fill(samples, value);
        return new DecodedSound(id ?? Guid.NewGuid(), Fmt, samples);
    }

    private static (float[] Out, float[] Monitor) Render(Mixer m, int frames, float micValue = 0f)
    {
        var output = new float[frames * Fmt.Channels];
        var monitor = new float[frames * Fmt.Channels];
        var mic = new float[frames * Fmt.Channels];
        Array.Fill(mic, micValue);
        m.Render(output, mic, monitor);
        return (output, monitor);
    }

    private static void Play(Mixer m, DecodedSound s, PlaybackMode mode = PlaybackMode.Interrupt, double gainDb = 0,
        double trimStart = 0, double? trimEnd = null) =>
        m.Play(s, gainDb, TimeSpan.FromSeconds(trimStart), trimEnd is null ? null : TimeSpan.FromSeconds(trimEnd.Value), mode);

    [Fact]
    public void Silence_when_nothing_playing_passes_mic_through()
    {
        var m = new Mixer(Fmt) { MicVolume = 1f, SoundsVolume = 1f };
        var (output, _) = Render(m, 100, micValue: 0.5f);
        Assert.All(output, x => Assert.Equal(0.5f, x, 5));
    }

    [Fact]
    public void Trim_plays_only_window()
    {
        var m = new Mixer(Fmt) { SoundsVolume = 1f };
        Play(m, Constant(0.5f), trimStart: 0.25, trimEnd: 0.5);
        var (output, _) = Render(m, 1000);
        Assert.Equal(250, output.Count(x => x != 0f));
        Assert.All(output.Take(250), x => Assert.Equal(0.5f, x, 5));
    }

    [Fact]
    public void GainDb_minus6_halves_amplitude()
    {
        var m = new Mixer(Fmt) { SoundsVolume = 1f };
        Play(m, Constant(0.5f), gainDb: -6);
        var (output, _) = Render(m, 10);
        Assert.InRange(output[0], 0.25f * 0.99f, 0.2512f * 1.01f);
    }

    [Fact]
    public void Interrupt_mode_stops_previous()
    {
        var m = new Mixer(Fmt) { SoundsVolume = 1f };
        Play(m, Constant(0.1f));
        Play(m, Constant(0.2f), PlaybackMode.Interrupt);
        var (output, _) = Render(m, 10);
        Assert.Equal(0.2f, output[0], 5);
        Assert.Equal(1, m.VoiceCount);
    }

    [Fact]
    public void Overlap_mode_sums_different_sounds()
    {
        var m = new Mixer(Fmt) { SoundsVolume = 1f };
        Play(m, Constant(0.1f), PlaybackMode.Overlap);
        Play(m, Constant(0.2f), PlaybackMode.Overlap);
        var (output, _) = Render(m, 10);
        Assert.Equal(0.3f, output[0], 4);
    }

    [Fact]
    public void Overlap_same_sound_restarts_not_duplicates()
    {
        var m = new Mixer(Fmt) { SoundsVolume = 1f };
        var s = Constant(0.1f, seconds: 0.5);
        Play(m, s, PlaybackMode.Overlap);
        Render(m, 400);
        Play(m, s, PlaybackMode.Overlap);
        Assert.Equal(1, m.VoiceCount);
        var (output, _) = Render(m, 500);
        Assert.Equal(500, output.Count(x => x != 0f));   // full length again, not the 100 frames left
        Assert.Equal(0.1f, output[0], 5);                 // single copy, not doubled
    }

    [Fact]
    public void Voice_cap_16_evicts_oldest()
    {
        var m = new Mixer(Fmt, maxVoices: 16) { SoundsVolume = 1f };
        var sounds = Enumerable.Range(0, 40).Select(_ => Constant(0.01f)).ToList();
        foreach (var s in sounds) Play(m, s, PlaybackMode.Overlap);

        Assert.Equal(16, m.VoiceCount);
        Assert.Equal(sounds.Skip(24).Select(s => s.Id).ToHashSet(), m.ActiveSoundIds.ToHashSet());
    }

    [Fact]
    public void Limiter_keeps_output_within_unit_range()
    {
        var m = new Mixer(Fmt) { MicVolume = 1f, SoundsVolume = 1f };
        for (var i = 0; i < 16; i++) Play(m, Constant(1f), PlaybackMode.Overlap);
        var (output, monitor) = Render(m, 200, micValue: 1f);
        Assert.All(output, x => Assert.InRange(x, -0.99999f, 0.99999f));
        Assert.All(monitor, x => Assert.InRange(x, -0.99999f, 0.99999f));
    }

    [Fact]
    public void Limiter_is_transparent_below_knee()
    {
        var buf = new[] { 0.3f, -0.79f, 0.8f };
        new SoftLimiter().Process(buf);
        Assert.Equal([0.3f, -0.79f, 0.8f], buf);
    }

    [Fact]
    public void Monitor_contains_sounds_but_not_mic()
    {
        var m = new Mixer(Fmt) { MicVolume = 1f, SoundsVolume = 0.5f };
        Play(m, Constant(0.4f));
        var (output, monitor) = Render(m, 10, micValue: 0.3f);
        Assert.Equal(0.4f, monitor[0], 5);          // monitor is pre-volume sounds only
        Assert.Equal(0.3f + 0.2f, output[0], 5);
    }

    [Fact]
    public void Started_and_ended_events_fire_once()
    {
        var m = new Mixer(Fmt) { SoundsVolume = 1f };
        int started = 0, ended = 0;
        m.PlaybackStarted += () => started++;
        m.PlaybackEnded += () => ended++;

        Play(m, Constant(0.1f, seconds: 0.1), PlaybackMode.Overlap);
        Play(m, Constant(0.1f, seconds: 0.2), PlaybackMode.Overlap);
        Render(m, 150);
        Assert.Equal(0, ended);
        Assert.True(m.IsPlaying);
        Render(m, 100);
        Render(m, 100);

        Assert.Equal(1, started);
        Assert.Equal(1, ended);
        Assert.False(m.IsPlaying);
    }

    [Fact]
    public void StopAll_silences_and_fires_ended()
    {
        var m = new Mixer(Fmt) { SoundsVolume = 1f };
        var ended = 0;
        m.PlaybackEnded += () => ended++;
        Play(m, Constant(0.5f));
        m.StopAll();
        var (output, _) = Render(m, 10);
        Assert.All(output, x => Assert.Equal(0f, x));
        Assert.Equal(1, ended);
    }

    [Fact]
    public void Stereo_trim_counts_frames_not_samples()
    {
        var stereo = new AudioFormat(1000, 2);
        var samples = new float[2000];
        Array.Fill(samples, 0.5f);
        var m = new Mixer(stereo) { SoundsVolume = 1f };
        m.Play(new DecodedSound(Guid.NewGuid(), stereo, samples), 0, TimeSpan.FromSeconds(0.5), null, PlaybackMode.Interrupt);
        var output = new float[2000];
        m.Render(output, new float[2000], new float[2000]);
        Assert.Equal(1000, output.Count(x => x != 0f));   // 500 frames × 2 channels
    }
}
