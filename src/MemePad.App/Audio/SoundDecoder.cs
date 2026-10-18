using System.IO;
using MemePad.Core.Audio;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MemePad.App.Audio;

public sealed record DecodeResult(DecodedSound? Sound, string? Error);

/// <summary>Decodes audio files fully into the mixer format. Reports failures in the result instead of throwing.</summary>
public static class SoundDecoder
{
    public static DecodeResult Decode(string path, AudioFormat target, Guid id = default)
    {
        if (!File.Exists(path)) return new DecodeResult(null, "File not found");

        try
        {
            if (Path.GetExtension(path).Equals(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                // Own the stream: VorbisWaveReader leaks its file handle when its constructor throws on bad data.
                using var file = File.OpenRead(path);
                using var vorbis = new VorbisWaveReader(file, closeOnDispose: false);
                return Decode(vorbis, target, id);
            }

            using var reader = new MediaFoundationReader(path);
            return Decode(reader, target, id);
        }
        catch (Exception ex)
        {
            return new DecodeResult(null, ex.Message);
        }
    }

    internal static DecodeResult Decode(WaveStream reader, AudioFormat target, Guid id)
    {
        var provider = Convert(reader.ToSampleProvider(), target);
        var estimate = (int)Math.Min(int.MaxValue / 2, reader.TotalTime.TotalSeconds * target.SampleRate * target.Channels * 1.05 + 1024);
        var samples = new List<float>(Math.Max(estimate, 1024));
        var chunk = new float[target.SampleRate * target.Channels / 10];
        int read;
        while ((read = provider.Read(chunk, 0, chunk.Length)) > 0) samples.AddRange(chunk.AsSpan(0, read));

        if (samples.Count == 0) return new DecodeResult(null, "No audio in file");
        return new DecodeResult(new DecodedSound(id == default ? Guid.NewGuid() : id, target, samples.ToArray()), null);
    }

    /// <summary>Adapts channel count first (cheaper to resample fewer channels when downmixing), then sample rate.</summary>
    internal static ISampleProvider Convert(ISampleProvider source, AudioFormat target)
    {
        var p = source;
        if (p.WaveFormat.Channels != target.Channels) p = new ChannelAdapter(p, target.Channels);
        if (p.WaveFormat.SampleRate != target.SampleRate) p = new WdlResamplingSampleProvider(p, target.SampleRate);
        return p;
    }
}
