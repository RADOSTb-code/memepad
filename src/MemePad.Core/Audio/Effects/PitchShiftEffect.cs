namespace MemePad.Core.Audio.Effects;

/// <summary>
/// Streaming pitch shifter: each channel writes into a short delay line that is read by two taps whose delay
/// sweeps at (1 - ratio) samples per sample. The taps sit half a window apart and are crossfaded with
/// sin²/cos² gains, hiding the jump when a tap wraps. Latency is at most one window (40 ms).
/// </summary>
public sealed class PitchShiftEffect(double ratio) : IVoiceEffect
{
    private const double WindowSeconds = 0.040;

    private ChannelState[] _channels = [];
    private AudioFormat _format;

    public void Reset() => _channels = [];

    public void Process(Span<float> interleaved, AudioFormat format)
    {
        if (format != _format || _channels.Length != format.Channels)
        {
            _format = format;
            var window = (int)(WindowSeconds * format.SampleRate);
            _channels = Enumerable.Range(0, format.Channels).Select(_ => new ChannelState(window)).ToArray();
        }

        var ch = format.Channels;
        for (var c = 0; c < ch; c++)
        {
            var state = _channels[c];
            for (var i = c; i < interleaved.Length; i += ch)
                interleaved[i] = state.Next(interleaved[i], ratio);
        }
    }

    private sealed class ChannelState(int window)
    {
        private readonly float[] _line = new float[window + 2];
        private int _write;
        private double _delay = window - 1;

        public float Next(float input, double ratio)
        {
            _line[_write] = input;

            var d1 = _delay;
            var d2 = (_delay + window / 2.0) % window;
            var g1 = Math.Sin(Math.PI * d1 / window);
            g1 *= g1;
            var output = g1 * Read(d1) + (1 - g1) * Read(d2);

            _delay += 1 - ratio;
            if (_delay < 0) _delay += window;
            else if (_delay >= window) _delay -= window;

            _write = (_write + 1) % _line.Length;
            return (float)output;
        }

        private double Read(double delay)
        {
            var pos = _write - delay;
            if (pos < 0) pos += _line.Length;
            var i0 = (int)pos;
            var frac = pos - i0;
            var i1 = (i0 + 1) % _line.Length;
            return _line[i0] * (1 - frac) + _line[i1] * frac;
        }
    }
}
