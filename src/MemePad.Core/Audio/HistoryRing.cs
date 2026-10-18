namespace MemePad.Core.Audio;

/// <summary>Keeps the most recent N seconds of interleaved audio; older samples are overwritten.</summary>
public sealed class HistoryRing(AudioFormat format, int seconds)
{
    private readonly object _gate = new();
    private readonly float[] _buffer = new float[format.SampleRate * format.Channels * seconds];
    private int _write;
    private bool _full;

    public AudioFormat Format { get; } = format;

    public void Write(ReadOnlySpan<float> samples)
    {
        lock (_gate)
        {
            if (samples.Length >= _buffer.Length)
            {
                samples[^_buffer.Length..].CopyTo(_buffer);
                _write = 0;
                _full = true;
                return;
            }

            var first = Math.Min(samples.Length, _buffer.Length - _write);
            samples[..first].CopyTo(_buffer.AsSpan(_write));
            samples[first..].CopyTo(_buffer);
            if (_write + samples.Length >= _buffer.Length) _full = true;
            _write = (_write + samples.Length) % _buffer.Length;
        }
    }

    /// <summary>The buffered audio, oldest first.</summary>
    public float[] Snapshot()
    {
        lock (_gate)
        {
            if (!_full) return _buffer[.._write];
            var result = new float[_buffer.Length];
            _buffer.AsSpan(_write).CopyTo(result);
            _buffer.AsSpan(0, _write).CopyTo(result.AsSpan(_buffer.Length - _write));
            return result;
        }
    }
}
