using MemePad.Core.Audio;
using MemePad.Core.Audio.Effects;
using MemePad.Core.Model;

namespace MemePad.Core.Tests;

public class EffectsTests
{
    private static readonly AudioFormat Mono48k = new(48000, 1);

    private static float[] Sine(double hz, double seconds, AudioFormat f)
    {
        var frames = (int)(seconds * f.SampleRate);
        var buf = new float[frames * f.Channels];
        for (var i = 0; i < frames; i++)
            for (var c = 0; c < f.Channels; c++)
                buf[i * f.Channels + c] = 0.5f * (float)Math.Sin(2 * Math.PI * hz * i / f.SampleRate);
        return buf;
    }

    /// <summary>Runs the effect in 480-sample blocks like the audio thread does.</summary>
    private static float[] Run(IVoiceEffect fx, float[] input, AudioFormat f)
    {
        var buf = (float[])input.Clone();
        for (var off = 0; off < buf.Length; off += 480)
            fx.Process(buf.AsSpan(off, Math.Min(480, buf.Length - off)), f);
        return buf;
    }

    /// <summary>Goertzel magnitude of <paramref name="hz"/> over the last second of a mono signal.</summary>
    private static double Magnitude(float[] x, double hz, int sampleRate)
    {
        var start = x.Length - sampleRate;
        var k = 2 * Math.Cos(2 * Math.PI * hz / sampleRate);
        double s1 = 0, s2 = 0;
        for (var i = start; i < x.Length; i++)
        {
            var s0 = x[i] + k * s1 - s2;
            s2 = s1;
            s1 = s0;
        }
        return Math.Sqrt(s1 * s1 + s2 * s2 - k * s1 * s2);
    }

    private static double DominantHz(float[] x, int sampleRate) =>
        Enumerable.Range(200, 700).MaxBy(hz => Magnitude(x, hz, sampleRate));

    [Fact]
    public void Higher_raises_dominant_frequency()
    {
        var output = Run(VoiceEffects.Create(VoiceEffectKind.Higher)!, Sine(440, 2, Mono48k), Mono48k);
        Assert.InRange(DominantHz(output, 48000), 563, 613);
    }

    [Fact]
    public void Lower_lowers_dominant_frequency()
    {
        var output = Run(VoiceEffects.Create(VoiceEffectKind.Lower)!, Sine(440, 2, Mono48k), Mono48k);
        Assert.InRange(DominantHz(output, 48000), 310, 350);
    }

    [Fact]
    public void Robot_adds_sidebands_at_carrier_offsets()
    {
        var output = Run(VoiceEffects.Create(VoiceEffectKind.Robot)!, Sine(440, 2, Mono48k), Mono48k);
        var floor = Magnitude(output, 600, 48000);
        Assert.True(Magnitude(output, 370, 48000) > 50 * floor);
        Assert.True(Magnitude(output, 510, 48000) > 50 * floor);
    }

    [Fact]
    public void None_creates_no_effect()
    {
        Assert.Null(VoiceEffects.Create(VoiceEffectKind.None));
    }

    [Theory]
    [InlineData(VoiceEffectKind.Higher)]
    [InlineData(VoiceEffectKind.Lower)]
    [InlineData(VoiceEffectKind.Robot)]
    public void Effects_preserve_buffer_length_and_stay_finite(VoiceEffectKind kind)
    {
        var stereo = new AudioFormat(48000, 2);
        var rng = new Random(42);
        var noise = new float[48000 * 10 * 2];
        for (var i = 0; i < noise.Length; i++) noise[i] = (float)(rng.NextDouble() * 2 - 1);

        var output = Run(VoiceEffects.Create(kind)!, noise, stereo);

        Assert.Equal(noise.Length, output.Length);
        Assert.All(output, x => Assert.True(float.IsFinite(x)));
    }

    [Fact]
    public void Pitch_shift_keeps_channels_independent()
    {
        var stereo = new AudioFormat(48000, 2);
        var input = Sine(440, 2, stereo);
        for (var i = 1; i < input.Length; i += 2) input[i] = 0f;   // right channel silent

        var output = Run(VoiceEffects.Create(VoiceEffectKind.Higher)!, input, stereo);

        for (var i = 1; i < output.Length; i += 2) Assert.Equal(0f, output[i]);
    }
}
