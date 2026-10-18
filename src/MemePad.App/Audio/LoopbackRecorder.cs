using System.IO;
using System.Runtime.InteropServices;
using MemePad.Core.Audio;
using NAudio.Wave;

namespace MemePad.App.Audio;

/// <summary>
/// Continuously keeps the last N seconds of whatever plays on the default output device (game, Discord, …),
/// so a hotkey can save "what just happened" as a new sound.
/// </summary>
public sealed class LoopbackRecorder : IDisposable
{
    private readonly object _gate = new();
    private WasapiLoopbackCapture? _capture;
    private HistoryRing? _ring;
    private int _seconds;
    private float[] _convert = [];

    public LoopbackRecorder(int seconds)
    {
        _seconds = seconds;
    }

    /// <summary>(Re)starts capturing the current default output; call again after device changes.</summary>
    public void Restart(int? seconds = null)
    {
        lock (_gate)
        {
            if (seconds is { } s) _seconds = s;
            StopLocked();
            try
            {
                var capture = new WasapiLoopbackCapture();
                var wf = capture.WaveFormat;
                _ring = new HistoryRing(new AudioFormat(wf.SampleRate, wf.Channels), _seconds);
                capture.DataAvailable += OnData;
                capture.StartRecording();
                _capture = capture;
            }
            catch (COMException)
            {
                _capture = null;   // no output device right now; the next Restart tries again
            }
        }
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        var capture = (WasapiLoopbackCapture)sender!;
        var ring = _ring;
        if (ring is null || e.BytesRecorded == 0) return;

        var wf = capture.WaveFormat;
        if (wf.Encoding == WaveFormatEncoding.IeeeFloat || (wf is WaveFormatExtensible && wf.BitsPerSample == 32))
        {
            ring.Write(MemoryMarshal.Cast<byte, float>(e.Buffer.AsSpan(0, e.BytesRecorded)));
        }
        else if (wf.BitsPerSample == 16)
        {
            var pcm = MemoryMarshal.Cast<byte, short>(e.Buffer.AsSpan(0, e.BytesRecorded));
            if (_convert.Length < pcm.Length) _convert = new float[pcm.Length];
            for (var i = 0; i < pcm.Length; i++) _convert[i] = pcm[i] / 32768f;
            ring.Write(_convert.AsSpan(0, pcm.Length));
        }
    }

    /// <summary>Writes the buffered audio to <paramref name="directory"/> and returns the file path, or null if nothing was captured.</summary>
    public string? SaveClip(string directory)
    {
        HistoryRing? ring;
        lock (_gate) ring = _ring;
        if (ring is null) return null;

        var samples = ring.Snapshot();
        if (samples.Length == 0) return null;

        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"clip-{DateTime.Now:yyyyMMdd-HHmmss}.wav");
        WavWriter.WriteFloat32(path, ring.Format, samples);
        return path;
    }

    private void StopLocked()
    {
        if (_capture is null) return;
        _capture.DataAvailable -= OnData;
        try { _capture.StopRecording(); } catch (COMException) { }
        _capture.Dispose();
        _capture = null;
    }

    public void Dispose()
    {
        lock (_gate) StopLocked();
    }
}
