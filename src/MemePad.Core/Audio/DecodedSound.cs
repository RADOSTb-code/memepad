namespace MemePad.Core.Audio;

/// <summary>A sound fully decoded into the mixer's format and kept in memory so playback never waits on disk.</summary>
public sealed class DecodedSound(Guid id, AudioFormat format, float[] samples)
{
    public Guid Id { get; } = id;
    public AudioFormat Format { get; } = format;
    public float[] Samples { get; } = samples;
    public int FrameCount => Samples.Length / Format.Channels;
    public TimeSpan Duration => TimeSpan.FromSeconds((double)FrameCount / Format.SampleRate);
}
