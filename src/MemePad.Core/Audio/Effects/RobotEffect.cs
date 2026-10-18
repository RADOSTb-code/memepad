namespace MemePad.Core.Audio.Effects;

/// <summary>Metallic voice: ring modulation by a 70 Hz carrier, blended 70/30 with the dry signal.</summary>
public sealed class RobotEffect : IVoiceEffect
{
    private const double CarrierHz = 70;
    private const float Wet = 0.7f;
    private const float Dry = 0.3f;

    private double _phase;

    public void Reset() => _phase = 0;

    public void Process(Span<float> interleaved, AudioFormat format)
    {
        var ch = format.Channels;
        var step = 2 * Math.PI * CarrierHz / format.SampleRate;
        for (var i = 0; i < interleaved.Length; i += ch)
        {
            var carrier = (float)Math.Sin(_phase);
            for (var c = 0; c < ch && i + c < interleaved.Length; c++)
            {
                var x = interleaved[i + c];
                interleaved[i + c] = Wet * x * carrier + Dry * x;
            }
            _phase += step;
            if (_phase > 2 * Math.PI) _phase -= 2 * Math.PI;
        }
    }
}
