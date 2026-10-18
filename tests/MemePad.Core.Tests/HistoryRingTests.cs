using System.Text;
using MemePad.Core.Audio;

namespace MemePad.Core.Tests;

public class HistoryRingTests
{
    private static readonly AudioFormat Fmt = new(10, 2);   // 10 frames/s, stereo → 20 samples per second

    private static float[] Ramp(int start, int count) => Enumerable.Range(start, count).Select(i => (float)i).ToArray();

    [Fact]
    public void Snapshot_before_full_returns_written_only()
    {
        var ring = new HistoryRing(Fmt, seconds: 2);
        ring.Write(Ramp(0, 6));
        Assert.Equal(Ramp(0, 6), ring.Snapshot());
    }

    [Fact]
    public void Snapshot_after_wrap_returns_last_N_seconds_in_order()
    {
        var ring = new HistoryRing(Fmt, seconds: 2);   // capacity 40 samples
        ring.Write(Ramp(0, 30));
        ring.Write(Ramp(30, 25));
        Assert.Equal(Ramp(15, 40), ring.Snapshot());
    }

    [Fact]
    public void Single_write_larger_than_capacity_keeps_tail()
    {
        var ring = new HistoryRing(Fmt, seconds: 1);
        ring.Write(Ramp(0, 55));
        Assert.Equal(Ramp(35, 20), ring.Snapshot());
    }

    [Fact]
    public void Wav_header_valid()
    {
        var path = Path.Combine(Path.GetTempPath(), $"memepad-{Guid.NewGuid():N}.wav");
        try
        {
            var f = new AudioFormat(48000, 2);
            WavWriter.WriteFloat32(path, f, new float[] { 0.1f, -0.1f, 0.2f, -0.2f });
            var b = File.ReadAllBytes(path);

            Assert.Equal("RIFF", Encoding.ASCII.GetString(b, 0, 4));
            Assert.Equal(b.Length - 8, BitConverter.ToInt32(b, 4));
            Assert.Equal("WAVE", Encoding.ASCII.GetString(b, 8, 4));
            Assert.Equal("fmt ", Encoding.ASCII.GetString(b, 12, 4));
            Assert.Equal(3, BitConverter.ToInt16(b, 20));          // IEEE float
            Assert.Equal(2, BitConverter.ToInt16(b, 22));          // channels
            Assert.Equal(48000, BitConverter.ToInt32(b, 24));
            Assert.Equal(48000 * 2 * 4, BitConverter.ToInt32(b, 28));
            Assert.Equal(8, BitConverter.ToInt16(b, 32));          // block align
            Assert.Equal(32, BitConverter.ToInt16(b, 34));
            var dataAt = Encoding.ASCII.GetString(b).IndexOf("data", StringComparison.Ordinal);
            Assert.Equal(16, BitConverter.ToInt32(b, dataAt + 4));
            Assert.Equal(-0.2f, BitConverter.ToSingle(b, dataAt + 8 + 12));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
