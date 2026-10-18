namespace MemePad.Core.Audio;

/// <summary>Interleaved float32 PCM layout used inside the mixer.</summary>
public readonly record struct AudioFormat(int SampleRate, int Channels)
{
    public int FramesFor(TimeSpan t) => (int)Math.Round(t.TotalSeconds * SampleRate);
}
