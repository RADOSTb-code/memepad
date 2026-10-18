using NAudio.Wave;

namespace MemePad.App.Audio;

/// <summary>Changes channel count: anything to mono averages; otherwise output channels wrap around the input ones (mono is duplicated).</summary>
internal sealed class ChannelAdapter(ISampleProvider source, int channels) : ISampleProvider
{
    private readonly int _inCh = source.WaveFormat.Channels;
    private float[] _buffer = [];

    public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, channels);

    public int Read(float[] buffer, int offset, int count)
    {
        var frames = count / channels;
        var need = frames * _inCh;
        if (_buffer.Length < need) _buffer = new float[need];

        var gotFrames = source.Read(_buffer, 0, need) / _inCh;
        for (var f = 0; f < gotFrames; f++)
        {
            var inBase = f * _inCh;
            var outBase = offset + f * channels;
            if (channels == 1)
            {
                var sum = 0f;
                for (var c = 0; c < _inCh; c++) sum += _buffer[inBase + c];
                buffer[outBase] = sum / _inCh;
            }
            else
            {
                for (var c = 0; c < channels; c++) buffer[outBase + c] = _buffer[inBase + c % _inCh];
            }
        }
        return gotFrames * channels;
    }
}
