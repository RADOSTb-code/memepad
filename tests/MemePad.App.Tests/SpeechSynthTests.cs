using MemePad.App.Audio;
using MemePad.Core.Audio;

namespace MemePad.App.Tests;

public class SpeechSynthTests
{
    private static readonly AudioFormat Target = new(48000, 2);

    [Fact]
    public void Synthesize_nonempty_returns_audio()
    {
        var synth = new SpeechSynth();
        if (synth.Voices().Count == 0) return;   // machine without SAPI voices: nothing to check

        var result = synth.Synthesize("good game", voice: null, rate: 0, Target);

        Assert.Null(result.Error);
        var sound = Assert.IsType<DecodedSound>(result.Sound);
        Assert.Equal(Target, sound.Format);
        Assert.True(sound.Duration > TimeSpan.FromMilliseconds(200));
        Assert.Contains(sound.Samples, x => Math.Abs(x) > 0.01f);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_text_returns_error(string text)
    {
        var result = new SpeechSynth().Synthesize(text, voice: null, rate: 0, Target);
        Assert.Null(result.Sound);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public void Unknown_voice_falls_back_to_default()
    {
        var synth = new SpeechSynth();
        if (synth.Voices().Count == 0) return;

        var result = synth.Synthesize("test", voice: "No Such Voice 123", rate: 0, Target);

        Assert.Null(result.Error);
        Assert.NotNull(result.Sound);
    }
}
