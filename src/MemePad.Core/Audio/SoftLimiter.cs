namespace MemePad.Core.Audio;

/// <summary>Leaves samples up to the knee untouched and bends louder ones smoothly so output never reaches ±1.</summary>
public sealed class SoftLimiter
{
    private const float Knee = 0.8f;
    private const float Headroom = 1f - Knee;

    public void Process(Span<float> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            var x = buffer[i];
            var a = Math.Abs(x);
            if (a <= Knee) continue;
            var bent = Knee + Headroom * MathF.Tanh((a - Knee) / Headroom);
            buffer[i] = MathF.CopySign(Math.Min(bent, 0.99999f), x);
        }
    }
}
