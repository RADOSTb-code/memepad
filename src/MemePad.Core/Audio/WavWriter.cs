using System.Text;

namespace MemePad.Core.Audio;

public static class WavWriter
{
    /// <summary>Writes a RIFF/WAVE file with IEEE-float 32-bit samples (format tag 3).</summary>
    public static void WriteFloat32(string path, AudioFormat format, ReadOnlySpan<float> samples)
    {
        const short bits = 32;
        var blockAlign = (short)(format.Channels * bits / 8);
        var dataBytes = samples.Length * sizeof(float);

        using var w = new BinaryWriter(File.Create(path), Encoding.ASCII);
        w.Write("RIFF"u8);
        w.Write(36 + dataBytes);
        w.Write("WAVE"u8);
        w.Write("fmt "u8);
        w.Write(16);
        w.Write((short)3);
        w.Write((short)format.Channels);
        w.Write(format.SampleRate);
        w.Write(format.SampleRate * blockAlign);
        w.Write(blockAlign);
        w.Write(bits);
        w.Write("data"u8);
        w.Write(dataBytes);
        foreach (var s in samples) w.Write(s);
    }
}
