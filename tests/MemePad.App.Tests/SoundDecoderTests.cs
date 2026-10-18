using MemePad.App.Audio;
using MemePad.Core.Audio;

namespace MemePad.App.Tests;

public sealed class SoundDecoderTests : IDisposable
{
    private static readonly AudioFormat Target = new(48000, 2);
    private readonly string _dir = Directory.CreateTempSubdirectory("memepad-dec-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Decode_wav_44100_mono_to_48000_stereo_length_and_duplication()
    {
        var path = Path.Combine(_dir, "tone.wav");
        var src = new float[44100];
        for (var i = 0; i < src.Length; i++) src[i] = 0.5f * (float)Math.Sin(2 * Math.PI * 440 * i / 44100);
        WavWriter.WriteFloat32(path, new AudioFormat(44100, 1), src);

        var result = SoundDecoder.Decode(path, Target);

        Assert.Null(result.Error);
        var sound = Assert.IsType<DecodedSound>(result.Sound);
        Assert.Equal(Target, sound.Format);
        Assert.InRange(sound.Samples.Length, 96000 * 0.99, 96000 * 1.01);
        for (var i = 0; i < sound.Samples.Length; i += 2) Assert.Equal(sound.Samples[i], sound.Samples[i + 1]);
        Assert.Contains(sound.Samples, x => Math.Abs(x) > 0.4f);
    }

    [Fact]
    public void Decode_missing_file_returns_error_not_throw()
    {
        var result = SoundDecoder.Decode(Path.Combine(_dir, "nope.mp3"), Target);
        Assert.Null(result.Sound);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public void Decode_garbage_file_returns_error_not_throw()
    {
        var path = Path.Combine(_dir, "broken.mp3");
        var junk = new byte[4096];
        new Random(7).NextBytes(junk);
        File.WriteAllBytes(path, junk);

        var result = SoundDecoder.Decode(path, Target);

        Assert.Null(result.Sound);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public void Decode_garbage_ogg_returns_error_not_throw()
    {
        var path = Path.Combine(_dir, "broken.ogg");
        File.WriteAllText(path, "definitely not vorbis");

        var result = SoundDecoder.Decode(path, Target);

        Assert.Null(result.Sound);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }
}
