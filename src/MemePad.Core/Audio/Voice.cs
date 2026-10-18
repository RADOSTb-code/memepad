namespace MemePad.Core.Audio;

/// <summary>One playing instance of a sound: a read cursor over its trimmed sample window.</summary>
internal sealed class Voice(DecodedSound sound, int startSample, int endSample, float gain)
{
    public Guid SoundId => sound.Id;
    public bool Finished => Position >= endSample;
    private int Position { get; set; } = startSample;

    /// <summary>Adds this voice into <paramref name="sum"/> and advances the cursor; check <see cref="Finished"/> afterwards.</summary>
    public void MixInto(Span<float> sum)
    {
        var src = sound.Samples;
        var n = Math.Min(sum.Length, endSample - Position);
        for (var i = 0; i < n; i++) sum[i] += src[Position + i] * gain;
        Position += n;
    }
}
